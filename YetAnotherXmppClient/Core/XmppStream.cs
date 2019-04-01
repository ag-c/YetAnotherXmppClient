﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Extensions;
using Presence = YetAnotherXmppClient.Core.Stanza.Presence;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Core
{
    public interface IIqReceivedCallback
    {
        void IqReceived(XElement xElem);
    }

    public interface IMessageReceivedCallback
    {
        void MessageReceived(XElement messageElem);
    }

    public interface IPresenceReceivedCallback
    {
        void PresenceReceived(XElement presenceXElem);
    }

    public class AsyncXmppStream
    {
        //private readonly Stream serverStream;
        private XmlReader xmlReader;
        private TextWriter textWriter;
        //TCS by ids of outgoing iq-stanzas that are expecting responses from the server
        private readonly ConcurrentDictionary<string, TaskCompletionSource<XElement>> iqCompletionSources = new ConcurrentDictionary<string, TaskCompletionSource<XElement>>();
        //iq-callbacks registered by xml-namespaces of iq child elements
        private readonly ConcurrentDictionary<XNamespace, IIqReceivedCallback> serverIqCallbacks = new ConcurrentDictionary<XNamespace, IIqReceivedCallback>();
        //callback for incoming presence-stanzas
        private IPresenceReceivedCallback presenceCallback;
        //callback for incoming message-stanzas
        private IMessageReceivedCallback messageCallback;


        public AsyncXmppStream(Stream serverStream)
        {
            //this.serverStream = serverStream;
            this.Reinitialize(serverStream);
        }

        public Stream BaseStream { get; private set; }

        public void Reinitialize(Stream serverStream)
        {
            this.BaseStream = serverStream;
            this.xmlReader = XmlReader.Create(serverStream, new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment, IgnoreWhitespace = true });
            this.textWriter = new DebugTextWriter(new StreamWriter(serverStream));
        }

        public void RegisterIqNamespaceCallback(XNamespace iqContentNamespace, IIqReceivedCallback callback)
        {
            serverIqCallbacks.TryAdd(iqContentNamespace, callback);
        }

        public void RegisterPresenceCallback(IPresenceReceivedCallback callback)
        {
            this.presenceCallback = callback;
        }

        public void RegisterMessageCallback(IMessageReceivedCallback callback)
        {
            this.messageCallback = callback;
        }

        private readonly ConcurrentDictionary<XName, IMessageReceivedCallback> messageContentHandlers = new ConcurrentDictionary<XName, IMessageReceivedCallback>();
        public void RegisterMessageContentCallback(XName elementName, IMessageReceivedCallback callback)
        {
            this.messageContentHandlers.TryAdd(elementName, callback);
        }

        public async Task WriteInitialStreamHeaderAsync(Jid jid, string version)
        {
            Log.Debug("Writing intial stream header..");

            using (var xmlWriter = XmlWriter.Create(textWriter, new XmlWriterSettings { Async = true, WriteEndDocumentOnClose = false }))
            {
                await xmlWriter.WriteStartDocumentAsync();
                await xmlWriter.WriteStartElementAsync("stream", "stream", "http://etherx.jabber.org/streams");
                await xmlWriter.WriteAttributeStringAsync("", "from", null, jid);
                await xmlWriter.WriteAttributeStringAsync("", "to", null, jid.Server);
                await xmlWriter.WriteAttributeStringAsync("", "version", null, version);
                await xmlWriter.WriteAttributeStringAsync("xml", "lang", null, "en");
                await xmlWriter.WriteAttributeStringAsync("xmlns", "", null, "jabber:client");
            }
        }

        public async Task<Dictionary<string, string>> ReadResponseStreamHeaderAsync()
        {
            Log.Debug("Reading response stream header..");

            //while (xmlReader.NodeType == XmlNodeType.EndElement)
            //    await xmlReader.ReadAsync();

            await this.xmlReader.MoveToContentAsync();
            Expect("stream:stream", actual: this.xmlReader.Name);

            return await this.xmlReader.GetAllAttributesAsync();
        }

        public async Task<XElement> ReadElementAsync()
        {
            var xElem = await this.xmlReader.ReadNextElementAsync();
            //TOLOG
            return xElem;
        }


        public async Task<XElement> WriteIqAndReadReponseAsync(Iq iq)
        {
            Log.Verbose($"WriteIqAndReadReponseAsync ({iq.Id})");

            var tcs = new TaskCompletionSource<XElement>(TaskCreationOptions.RunContinuationsAsynchronously);

            this.iqCompletionSources.TryAdd(iq.Id, tcs);

            await this.textWriter.WriteAndFlushAsync(iq);

            return await this.ReadUntilResponseAsync(iq.Id);
        }

        private async Task<XElement> ReadUntilResponseAsync(string id)
        { 
            XElement xElem;
            do
            {
                xElem = await this.xmlReader.ReadNextElementAsync();
                this.ProcessInboundElement(xElem);

            } while (!xElem.IsStanza() || xElem.IsStanza() && xElem.Attribute("id")?.Value != id);

            return xElem;
        }

        private void ProcessInboundElement(XElement xElem)
        {
            switch (xElem.Name.LocalName)
            {
                case "iq":
                    this.OnIqReceived(xElem);
                    break;
                case "presence":
                    this.OnPresenceReceived(xElem);
                    break;
                case "message":
                    this.OnMessageReceived(xElem);
                    break;
                default:
                    this.OnNonStanzaElementReceived(xElem);
                    break;
            }
        }

        private void OnIqReceived(XElement iqElement)
        {
            var id = iqElement.Attribute("id")?.Value;

            if (iqElement.HasAttribute("id") && this.iqCompletionSources.TryRemove(id, out var tcs))
            {
                Log.Verbose($"Received iq with awaiter ({id})");
                tcs.SetResult(iqElement);
            }
            else
            {
                if (iqElement.FirstNode is XElement iqChildElem &&
                    this.serverIqCallbacks.TryGetValue(iqChildElem.Name.Namespace, out var callback))
                {
                    //UNDONE sind weitere kindelemente möglich?
                    callback.IqReceived(iqElement);
                }
                else
                {
                    Log.Verbose($"Received iq WITHOUT awaiter or callback ({id})");

                    if (iqElement.Name == "iq")
                    {
                        //UNDONE e.g. <service-unavailable/>
                    }
                }
            }
        }


        private void OnPresenceReceived(XElement presenceElement)
        {
            this.presenceCallback?.PresenceReceived(presenceElement);
        }

        private void OnMessageReceived(XElement messageElem)
        {
            this.messageCallback?.MessageReceived(messageElem);

            // handle specific content/child elements
            foreach (var contentElem in messageElem.Elements())
            {
                if (this.messageContentHandlers.TryGetValue(contentElem.Name, out var callback))
                {
                    callback.MessageReceived(messageElem);
                }
            }
        }

        private void OnNonStanzaElementReceived(XElement xElem)
        {
            Log.Error($"Received non-stanza element, which was not expected and is not handled: ({xElem})");
        }

        public void StartAsyncReadLoop()
        {
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var xElem = await this.xmlReader.ReadNextElementAsync();
                        this.ProcessInboundElement(xElem);
                    }
                }
                catch (Exception e)
                {
                    Log.Error("XmppStream-READLOOP exited: " + e);
                }
            });
        }

        public Task WriteAsync(string message)
        {
            return this.textWriter.WriteAndFlushAsync(message);
        }
    }
}
