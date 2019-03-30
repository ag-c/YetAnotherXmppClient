using System.IO;
using Serilog;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace YetAnotherXmppClient
{
    public class StartTLSFeatureHandler
    {
        public static async Task BeginNegotiationAsync(TextWriter writer)
        {
            Log.Logger.Debug("Initiating TLS negotiation..");

            XNamespace ns = "urn:ietf:params:xml:ns:xmpp-tls";
            var x = new XElement(ns + "starttls").ToString();

            //await writer.WriteAsync("<starttls xmlns='urn:ietf:params:xml:ns:xmpp-tls'/>");
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings {Async = true, OmitXmlDeclaration = true}))
            {
                await xmlWriter.WriteStartElementAsync("", XNames.starttls.LocalName, XNames.starttls.NamespaceName);
                await xmlWriter.WriteEndElementAsync();
            }

            //await xmlReader.ReadAsync();

            //Debug.Assert();
            //Expect.ElementName(xmlReader.)
            //if (xmlReader.Name != "stream:features")
            //{
            //    throw new NotExpectedException()
            //}

            //var xmlReader2 = xmlReader.ReadSubtree();
            //await xmlReader2.MoveToContentAsync();
            //var xElem = XNode.ReadFrom(xmlReader2) as XElement;
        }
    }
}
