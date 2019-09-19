using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Core.Stanza
{
    public enum MessageType
    {
        chat,
        error,
        groupchat,
        headline,
        normal
    }

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

        public MessageType? Type
        {
            get => EnumHelper.Parse<MessageType>(this.Attribute("type")?.Value);
            set => this.SetAttributeValue("type", value.ToString());
        }

        public string Thread => this.ElementWithLocalName("thread")?.Value;
                                
        public string Body => this.ElementWithLocalName("body")?.Value;


        //copy ctor
        private Message(XElement messageXElem)
            : base(messageXElem.Name, messageXElem.ElementsAndAttributes())
        {
        }

        public Message(object content) : base("{jabber:client}message", content)
        {
        }

        public Message(string body, string thread) : base("message", new XElement("body", body), thread != null ? new XElement("thread", thread) : null)
        {
        }

        public static implicit operator string(Message iq)
        {
            return iq.ToString();
        }

        public static Message FromXElement(XElement xElem)
        {
            Expectation.Expect("message", xElem.Name.LocalName, xElem);
            return new Message(xElem);
        }
    }
}
