using System.IO;
using System.Xml;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Protocol
{
    public class ProtocolHandlerBase
    {
        protected Stream serverStream;
        protected XmlReader xmlReader;
        //protected XmlStreamReader xmlReader;
        protected TextWriter textWriter;

        //protected AsyncXmppStream xmppServerStream;
        
        public ProtocolHandlerBase(Stream serverStream)
        {
            this.RecreateStreams(serverStream);
            //-----
            //this.xmppServerStream = new AsyncXmppStream(serverStream);
        }
        
        protected void RecreateStreams(Stream serverStream)
        {
            this.serverStream = serverStream;
            this.xmlReader = XmlReader.Create(serverStream, new XmlReaderSettings { Async = true, ConformanceLevel = ConformanceLevel.Fragment, IgnoreWhitespace = true });
            //this.xmlReader = new XmlStreamReader(serverStream);
            this.textWriter = new DebugTextWriter(new StreamWriter(serverStream));
            //-----
            //this.xmppServerStream = new AsyncXmppStream(serverStream);
        }
    }
}