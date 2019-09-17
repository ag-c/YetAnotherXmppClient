using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol.Handler;
using static YetAnotherXmppClient.Expectation;
using Presence = YetAnotherXmppClient.Core.Stanza.Presence;

namespace YetAnotherXmppClient.Core
{
    public interface IIqReceivedCallback
    {
        Task IqReceivedAsync(Iq iq);
    }

    public interface IMessageReceivedCallback
    {
        Task MessageReceivedAsync(Message message);
    }

    public interface IPresenceReceivedCallback
    {
        Task PresenceReceivedAsync(Presence presence);
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
            return new DebugTextWriterDecorator(new StreamWriter(stream), OnWriterFlushed);
        }

        private static void OnWriterFlushed(string str)
        {
            Log.Logger.XmppStreamContent($"Written: {str}");
        }

        public void RegisterIqNamespaceCallback(XNamespace iqContentNamespace, IIqReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "iq" && xe.FirstElement().NamespaceEquals(iqContentNamespace),
                xe => callback.IqReceivedAsync(Iq.FromXElement(xe))
            );
        }

        public void RegisterPresenceCallback(IPresenceReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "presence",
                xe => callback.PresenceReceivedAsync(Presence.FromXElement(xe))
            );
        }

        public void RegisterMessageCallback(IMessageReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "message",
                xe => callback.MessageReceivedAsync(Message.FromXElement(xe))
            );
        }
        
        public void RegisterMessageContentCallback(XName contentName, IMessageReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "message" && xe.Elements().Any(e => e.Name == contentName),
                xe => callback.MessageReceivedAsync(Message.FromXElement(xe))
            );
        }

        public void RegisterPresenceContentCallback(XName contentName, IPresenceReceivedCallback callback)
        {
            this.RegisterElementCallback(
                xe => xe.Name.LocalName == "presence" && xe.Elements().Any(e => e.Name == contentName),
                xe => callback.PresenceReceivedAsync(Presence.FromXElement(xe))
            );
        }

        public async Task WriteInitialStreamHeaderAsync(Jid jid, string version)
        {
            Log.Debug("Writing intial stream header..");

            using (var xmlWriter = XmlWriter.Create(this.UnderlyingStream, new XmlWriterSettings { Async = true, WriteEndDocumentOnClose = false }))
            {
                await xmlWriter.WriteStartDocumentAsync().ConfigureAwait(false);
                await xmlWriter.WriteStartElementAsync("stream", "stream", "http://etherx.jabber.org/streams").ConfigureAwait(false);
                await xmlWriter.WriteAttributeStringAsync("", "from", null, jid).ConfigureAwait(false);
                await xmlWriter.WriteAttributeStringAsync("", "to", null, jid.Server).ConfigureAwait(false);
                await xmlWriter.WriteAttributeStringAsync("", "version", null, version).ConfigureAwait(false);
                await xmlWriter.WriteAttributeStringAsync("xml", "lang", null, "en").ConfigureAwait(false);
                await xmlWriter.WriteAttributeStringAsync("xmlns", "", null, "jabber:client").ConfigureAwait(false);
            }
        }

        public async Task<Dictionary<string, string>> ReadResponseStreamHeaderAsync()
        {
            Log.Debug("Reading response stream header..");

            var (name, attributes) = await this.ReadOpeningTagAsync().ConfigureAwait(false);

            if (name == "stream:error")
            {
                var error = await this.ReadElementAsync().ConfigureAwait(false);
                throw new XmppException(error.ToString());
            }
            Expect("stream:stream", actual: name);

            return attributes;
        }

        //4.3.2. Stream Features Format
        public async Task<IEnumerable<Feature>> ReadStreamFeaturesAsync()
        {
            Log.Debug("Reading stream features..");

            var xElem = await this.ReadElementAsync().ConfigureAwait(false);

            Expect("features", actual: xElem.Name.LocalName, context: xElem);

            return Features.FromXElement(xElem);
        }

        public async Task<Iq> WriteIqAndReadReponseAsync(Iq iq)
        {
            var readUntilMatchTask = this.ReadUntilElementMatchesAsync(xe => xe.IsIq() && xe.Attribute("id")?.Value == iq.Id);

            await this.WriteElementAsync(iq).ConfigureAwait(false);

            var iqResponse = await readUntilMatchTask.ConfigureAwait(false);

            return Iq.FromXElement(iqResponse);
        }
    }
}
