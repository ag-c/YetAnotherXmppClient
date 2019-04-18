using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace YetAnotherXmppClient.Extensions
{
    static class XElementExtensions
    {
        public static bool IsErrorType(this XElement xElem)
        {
            return xElem.Attribute("type")?.Value == "error";
        }

        public static bool HasAttribute(this XElement xElem, string name)
        {
            return xElem.Attribute(name) != null;
        }

        public static bool HasElement(this XElement xElem, XName name)
        {
            return xElem.Element(name) != null;
        }

        public static bool IsStanza(this XElement xElem)
        {
            return xElem.Name.LocalName == "iq" ||
                   xElem.Name.LocalName == "presence" ||
                   xElem.Name.LocalName == "message";
        }

        public static bool IsIq(this XElement xElem)
        {
            return xElem.Name.LocalName == "iq";
        }

        public static XElement FirstElement(this XElement xElem)
        {
            return xElem.Elements().FirstOrDefault();
        }

        public static bool NamespaceEquals(this XElement xElem, XNamespace ns)
        {
            if (xElem == null)
                return false;

            return xElem.Name.Namespace == ns;
        }

        public static IEnumerable<object> ElementsAndAttributes(this XElement xElem)
        {
            return xElem.Elements().Concat<object>(xElem.Attributes());
        }
    }
}
