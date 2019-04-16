using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.Stanza
{
    public class Message : XElement
    {
        public string Id
        {
            get => this.Attribute("id")?.Value;
            set => this.SetAttributeValue("id", value);
        }

        public string From
        {
            get => this.Attribute("from")?.Value;
            set => this.SetAttributeValue("from", value);
        }

        public string To
        {
            get => this.Attribute("to")?.Value;
            set => this.SetAttributeValue("to", value);
        }

        public string Type
        {
            get => this.Attribute("type")?.Value;
            set => this.SetAttributeValue("type", value);
        }

        public string Thread
        {
            get => this.Element("thread")?.Value;
        }

        public Message(object content) : base("message", content)
        {
        }

        private Message(params object[] content) : base("message", content)
        {
        }

        public static implicit operator string(Message iq)
        {
            return iq.ToString();
        }

        public static Message FromXElement(XElement xElem)
        {
            var message = new Message(xElem.Elements());
            foreach (var attr in xElem.Attributes())
                message.SetAttributeValue(attr.Name, attr.Value);
            return message;
        }
    }
}
