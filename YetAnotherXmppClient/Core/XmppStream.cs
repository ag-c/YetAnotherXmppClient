using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
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

    public class XmppStream : XmlStream
    {
        private static readonly XmlParserContext xmlParserContext;


        static XmppStream()
        {
            // creating a context for the xmlreader with a preconfigured namespace
            var nt = new NameTable();
            var nsmgr = new XmlNamespaceManager(nt);
            nsmgr.AddNamespace("stream", "http://etherx.jabber.org/streams");
            xmlParserContext = new XmlParserContext(nt, nsmgr, null, XmlSpace.None);
        }

        public XmppStream(Stream serverStream) : base(serverStream)
        {
            this.Reinitialize(serverStream);
        }


        protected override XmlReader CreateReader(Stream stream)
        {
            return XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreWhitespace = true,
                IgnoreComments = true
            }, xmlParserContext);
        }

        protected override TextWriter CreateWriter(Stream stream)
        {
            return new DebugTextWriter(new StreamWriter(stream), OnWriterFlushed);
        }

        private static void OnWriterFlushed(string str)
        {
            Log.Logger.XmppStreamContent($"Written: {str}");
        }

        public void RegisterIqNamespaceCallback(XNamespace iqContentNamespace, IIqReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "iq" && xe.FirstElement().NamespaceEquals(iqContentNamespace),
                xe => callback.IqReceived(Iq.FromXElement(xe))
            );
        }

        public void RegisterPresenceCallback(IPresenceReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "presence",
                callback.PresenceReceived
            );
        }

        public void RegisterMessageCallback(IMessageReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "message",
                xe => callback.MessageReceived(Message.FromXElement(xe))
            );
        }
        
        public void RegisterMessageContentCallback(XName contentName, IMessageReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "message" && xe.FirstElement().Name == contentName,
                xe => callback.MessageReceived(Message.FromXElement(xe))
            );
        }

        public async Task WriteInitialStreamHeaderAsync(Jid jid, string version)
        {
            Log.Debug("Writing intial stream header..");

            using (var xmlWriter = XmlWriter.Create(this.UnderlyingStream, new XmlWriterSettings { Async = true, WriteEndDocumentOnClose = false }))
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

            var (name, attributes) = await this.ReadOpeningTagAsync();

            if (name == "stream:error")
            {
                var error = await this.ReadElementAsync();
                throw new XmppException(error.ToString());
            }
            Expect("stream:stream", actual: name);

            return attributes;
        }

        //4.3.2. Stream Features Format
        public async Task<IEnumerable<Feature>> ReadStreamFeaturesAsync()
        {
            Log.Debug("Reading stream features..");

            var xElem = await this.ReadElementAsync();

            Expect("features", actual: xElem.Name.LocalName, context: xElem);

            return Features.FromXElement(xElem);
        }

        public async Task<XElement> WriteIqAndReadReponseAsync(Iq iq)
        {
            //UNDONE register callback before writing
            await this.WriteElementAsync(iq);

            return await this.ReadUntilElementMatchesAsync(xe => xe.IsIq() && xe.Attribute("id")?.Value == iq.Id);
        }
    }
}
