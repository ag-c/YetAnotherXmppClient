using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Nito.AsyncEx;
using Serilog;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Core
{
    public interface IIqReceivedCallback
    {
        void IqReceived(Iq iq);
    }

    public interface IMessageReceivedCallback
    {
        void MessageReceived(Message message);
    }

    public interface IPresenceReceivedCallback
    {
        void PresenceReceived(XElement presenceXElem);
    }

    public class AsyncXmppStream
    {
        private XmlReader xmlReader;
        private TextWriter textWriter;
        private readonly AsyncLock xmlReaderLock = new AsyncLock();

        //TCS by ids of outgoing iq-stanzas that are expecting responses from the server
        private readonly ConcurrentDictionary<string, TaskCompletionSource<XElement>> iqCompletionSources = new ConcurrentDictionary<string, TaskCompletionSource<XElement>>();
        //iq-callbacks registered by xml-namespaces of iq child elements
        private readonly ConcurrentDictionary<XNamespace, IIqReceivedCallback> serverIqCallbacks = new ConcurrentDictionary<XNamespace, IIqReceivedCallback>();
        //callback for incoming presence-stanzas
        private IPresenceReceivedCallback presenceCallback;
        //callback for incoming message-stanzas
        private IMessageReceivedCallback messageCallback;
        // handlers for specific child elements in message-stanzas
        private readonly ConcurrentDictionary<XName, IMessageReceivedCallback> messageContentHandlers = new ConcurrentDictionary<XName, IMessageReceivedCallback>();

        public Stream BaseStream { get; private set; }


        public AsyncXmppStream(Stream serverStream)
        {
            this.Reinitialize(serverStream);
        }


        public void Reinitialize(Stream serverStream)
        {
            this.BaseStream = serverStream;
            var nt = new NameTable();
            var nsmgr = new XmlNamespaceManager(nt);
            nsmgr.AddNamespace("stream", "http://etherx.jabber.org/streams");
            var context = new XmlParserContext(nt, nsmgr, null, XmlSpace.None);
            this.xmlReader = XmlReader.Create(serverStream, new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment, IgnoreWhitespace = true }, context);
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

        public async Task<(string Name, Dictionary<string, string> Attributes)> ReadOpeningTagAsync()
        {
            ThrowIfLoopRunning();

            Log.Debug("Reading opening tag ''..");

            await this.xmlReader.MoveToContentAsync();
            
            Expect(() => this.xmlReader.NodeType == XmlNodeType.Element);

            return (xmlReader.Name, await this.xmlReader.GetAllAttributesAsync());
        }

        public Task<XElement> ReadElementAsync()
        {
            ThrowIfLoopRunning();

            return this.InternalReadElementAsync();
        }

        public async Task<XElement> InternalReadElementAsync()
        {
            using (await this.xmlReaderLock.LockAsync())
            {
                var xElem = await this.xmlReader.ReadNextElementAsync();

                Log.Logger.XmppStreamContent($"Read from stream: {xElem}");

                return xElem;
            }
        }

        public async Task<XElement> WriteIqAndReadReponseAsync(Iq iq)
        {
            Log.Verbose($"WriteIqAndReadReponseAsync ({iq.Id})");

            var tcs = new TaskCompletionSource<XElement>(TaskCreationOptions.RunContinuationsAsynchronously);

            this.iqCompletionSources.TryAdd(iq.Id, tcs);

            await this.textWriter.WriteAndFlushAsync(iq);

            if (this.isLoopRunning)
            {   // let the read loop receive the response
                return await tcs.Task;
            }

            return await this.ReadUntilStanzaResponseAsync(iq.Id);
        }

        private async Task<XElement> ReadUntilStanzaResponseAsync(string id)
        {
            ThrowIfLoopRunning();

            XElement xElem;
            do
            {
                xElem = await this.InternalReadElementAsync();
                this.ProcessInboundElement(xElem);
            }
            while (!xElem.IsStanza() || xElem.IsStanza() && xElem.Attribute("id")?.Value != id);

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
            var iq = Iq.FromXElement(iqElement);

            if (iq.Id != null && this.iqCompletionSources.TryRemove(iq.Id, out var tcs))
            {
                Log.Verbose($"Received iq with awaiter ({iq.Id})");
                tcs.SetResult(iqElement);
            }
            else
            {
                if (iqElement.FirstNode is XElement iqChildElem &&
                    this.serverIqCallbacks.TryGetValue(iqChildElem.Name.Namespace, out var callback))
                {
                    //UNDONE sind weitere kindelemente möglich?
                    callback.IqReceived(iq);
                }
                else
                {
                    Log.Verbose($"Received iq WITHOUT awaiter or callback ({iq.Id})");

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
            var message = Message.FromXElement(messageElem);

            this.messageCallback?.MessageReceived(message);

            // handle specific content/child elements
            foreach (var contentElem in messageElem.Elements())
            {
                if (this.messageContentHandlers.TryGetValue(contentElem.Name, out var callback))
                {
                    callback.MessageReceived(message);
                }
            }
        }

        private void OnNonStanzaElementReceived(XElement xElem)
        {
            Log.Error($"Received non-stanza element, which was not expected and is not handled: ({xElem})");
        }

        public Task WriteAsync(string message)
        {
            return this.textWriter.WriteAndFlushAsync(message);
        }

        private bool isLoopRunning;
        public async Task RunReadLoopAsync(CancellationToken token)
        {
            this.isLoopRunning = true;
            while (true)
            {
                token.ThrowIfCancellationRequested();

                var xElem = await this.InternalReadElementAsync();
                this.ProcessInboundElement(xElem);
            }
        }

        private void ThrowIfLoopRunning()
        {
            if (this.isLoopRunning)
                throw new InvalidOperationException("Cannot read explicitly, stream is in read loop");
        }
    }
}
