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

        public string Thread => this.Element("{jabber:client}thread")?.Value;

        public Message(object content) : base("{jabber:client}message", content)
        {
        }

        public Message(string body, string thread) : base("{jabber:client}message", new XElement("body", body), thread != null ? new XElement("thread", thread) : null)
        {
        }

        private Message(params object[] content) : base("{jabber:client}message", content)
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
