using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol;
using Presence = YetAnotherXmppClient.Core.Stanza.Presence;

namespace YetAnotherXmppClient.Core
{
    public class AsyncXmppStream
    {
        //private readonly Stream serverStream;
        private XmlReader xmlReader;
        private TextWriter textWriter;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<XElement>> iqCompletionSources = new ConcurrentDictionary<string, TaskCompletionSource<XElement>>();
        private readonly ConcurrentDictionary<XNamespace, IServerIqCallback> serverIqCallbacks = new ConcurrentDictionary<XNamespace, IServerIqCallback>();
        private IPresenceCallback presenceCallback;
        private IMessageStanzaCallback messageCallback;


        public AsyncXmppStream(Stream serverStream)
        {
            //this.serverStream = serverStream;
            this.RecreateStreams(serverStream);
        }

        public void RecreateStreams(Stream serverStream)
        {
            this.xmlReader = XmlReader.Create(serverStream, new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment, IgnoreWhitespace = true });
            this.textWriter = new DebugTextWriter(new StreamWriter(serverStream));
        }

        public void RegisterServerIqCallback(XNamespace iqContentNamespace, IServerIqCallback callback)
        {
            serverIqCallbacks.TryAdd(iqContentNamespace, callback);
        }

        public async Task<XElement> WritePresenceAndReadReponseAsync(Presence presence)
        {
            Log.Logger.Verbose($"WriteIqAndReadReponseAsync ({presence.Id})");

            var tcs = new TaskCompletionSource<XElement>(TaskCreationOptions.RunContinuationsAsynchronously);

            this.iqCompletionSources.TryAdd(presence.Id, tcs);

            await this.textWriter.WriteAndFlushAsync(presence);

            return await this.ReadUntilReponseReceivedAsync(presence.Id);
        }

        public async Task<XElement> WriteIqAndReadReponseAsync(Iq iq)
        {
            Log.Logger.Verbose($"WriteIqAndReadReponseAsync ({iq.Id})");

            var tcs = new TaskCompletionSource<XElement>(TaskCreationOptions.RunContinuationsAsynchronously);

            this.iqCompletionSources.TryAdd(iq.Id, tcs);

            await this.textWriter.WriteAndFlushAsync(iq);

            return await this.ReadUntilReponseReceivedAsync(iq.Id);
        }

        private async Task<XElement> ReadUntilReponseReceivedAsync(string id)
        { 
            XElement xElem;
            do
            {
                xElem = await this.ReadSingleElementInternalAsync();
                if (xElem.IsIq() || xElem.Name == "presence")
                    this.OnIqReceived(xElem);
                else if (xElem.Name == "message")
                    this.OnMessageReceived(xElem);
                else
                    this.OnOtherElementReceived(xElem);
            } while (!(xElem.IsIq() || xElem.Name == "presence") || (xElem.IsIq() || xElem.Name == "presence") && xElem.Attribute("id").Value != id);


            return xElem;
        }


        private void OnIqReceived(XElement iqElement)
        {
            if (iqElement.HasAttribute("id") && this.iqCompletionSources.TryRemove(iqElement.Attribute("id").Value, out var tcs))
            {
                Log.Logger.Verbose($"Received iq/presence with awaiter ({iqElement.Attribute("id")?.Value})");
                tcs.SetResult(iqElement);
            }
            else
            {
                if (iqElement.Name == "presence")
                {
                    this.presenceCallback?.PresenceReceived(iqElement);
                }
                else if (iqElement.FirstNode is XElement iqContentElem &&
                    this.serverIqCallbacks.TryGetValue(iqContentElem.Name.Namespace, out var callback))
                {
                    callback.IqReceived(iqElement);
                }
                else
                {
                    Log.Logger.Verbose($"Received iq/presence WITHOUT awaiter or callback ({iqElement.Attribute("id")?.Value})");

                    if (iqElement.Name == "iq")
                    {
                        //UNDONE z.b. <service-unavailable/>
                    }
                }
            }
        }

        private void OnMessageReceived(XElement messageElem)
        {
            this.messageCallback?.MessageReceived(messageElem);
        }

        private void OnOtherElementReceived(XElement xElem)
        {
            Log.Logger.Error($"Received not expected element which is not handled ({xElem})");
        }

        private async Task<XElement> ReadSingleElementInternalAsync()
        {
            var xElem = await this.xmlReader.ReadNextElementAsync();
            //if (xElem.IsIq())
            //{
            //    this.OnIqReceived(xElem);
            //}

            return xElem;
        }

        public async Task<XElement> ReadNonIqElementAsync()
        {
            XElement xElem;
            do
            {
                xElem = await this.ReadSingleElementInternalAsync();
                if (xElem.IsIq())
                {
                    this.OnIqReceived(xElem);
                }
            } while (xElem.IsIq());

            return xElem;
        }


        public void StartReadLoop()
        {
            Task.Run(async () =>
            {
                try
                {
                    XElement xElem;
                    while (true)
                    {
                        xElem = await this.ReadSingleElementInternalAsync();
                        if (xElem.IsIq() || xElem.Name == "presence")
                            this.OnIqReceived(xElem);
                        else
                            this.OnOtherElementReceived(xElem);
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.Error("XmppStream-READLOOP exited: " + e);
                }
            });
        }

        public Task WriteAsync(string message)
        {
            return this.textWriter.WriteAndFlushAsync(message);
        }

        public void RegisterPresenceCallback(IPresenceCallback callback)
        {
            this.presenceCallback = callback;
        }

        public void RegisterMessageCallback(IMessageStanzaCallback callback)
        {
            this.messageCallback = callback;
        }
    }

    public interface IPresenceCallback
    {
        void PresenceReceived(XElement presenceXElem);
    }

    static class XElementExtensions
    {
        public static bool IsIq(this XElement xElem)
        {
            return xElem.Name == "iq";
        }
    }
}
