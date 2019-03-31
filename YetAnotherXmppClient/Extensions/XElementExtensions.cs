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

        public static bool IsIq(this XElement xElem)
        {
            return xElem.Name == "iq";
        }

        public static bool IsStanza(this XElement xElem)
        {
            return xElem.Name == "iq" ||
                   xElem.Name == "presence" ||
                   xElem.Name == "message";
        }
    }
}
