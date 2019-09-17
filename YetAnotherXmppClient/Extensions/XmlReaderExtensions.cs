using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Serilog;

namespace YetAnotherXmppClient.Extensions
{
    public static class XmlReaderExtensions
    {
        public static async Task<Dictionary<string, string>> GetAllAttributesAsync(this XmlReader xmlReader)
        {
            var attrs = new Dictionary<string, string>();
            while (xmlReader.MoveToNextAttribute())
            {
                attrs.Add(xmlReader.Name, xmlReader.Value);
            }

            //await xmlReader.ReadAsync();
            return attrs;
        }

        private static bool wasLastEmpty = false;
        public static async Task<XElement> ReadNextElementAsync(this XmlReader xmlReader, CancellationToken ct=default)
        {
            if (wasLastEmpty && xmlReader.NodeType == XmlNodeType.Element)
                await xmlReader.ReadAsync().WithCancellation(ct).ConfigureAwait(false);
            while (xmlReader.NodeType == XmlNodeType.EndElement || xmlReader.NodeType == XmlNodeType.Attribute)
                await xmlReader.ReadAsync().WithCancellation(ct).ConfigureAwait(false);

            await xmlReader.MoveToContentAsync().WithCancellation(ct).ConfigureAwait(false);
            if(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "stream:stream")
                throw new Exception("end of xmpp stream");
            //await xmlReader.ReadAsync();
            Expectation.Expect(() => xmlReader.NodeType == XmlNodeType.Element);
            var subXmlReader = xmlReader.ReadSubtree();
            await subXmlReader.MoveToContentAsync().WithCancellation(ct).ConfigureAwait(false);
            var xElem = XNode.ReadFrom(subXmlReader) as XElement;

            wasLastEmpty = xElem.IsEmpty;

            return xElem;
        }
    }
}