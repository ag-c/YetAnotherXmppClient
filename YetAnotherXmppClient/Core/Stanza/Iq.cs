using System;
using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.Stanza
{
    public enum IqType
    {
        get,
        set,
        result,
    }

    public class Iq : XElement
    {
        public string Id
        {
            get => this.Attribute("id")?.Value;
            set => this.SetAttributeValue("id", value);
        }

        public IqType Type
        {
            get => (IqType)Enum.Parse(typeof(IqType), this.Attribute("type")?.Value);
            set => this.SetAttributeValue("type", value.ToString());
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

        public Iq(IqType type, object content=null) : base("{jabber:client}iq", content) //{jabber:client}
        {
            this.Id = Guid.NewGuid().ToString();
            this.Type = type;
        }

        private Iq(params object[] content) : base("{jabber:client}iq", content) //{jabber:client}
        {
        }

        public static implicit operator string(Iq iq)
        {
            return iq.ToString();
        }

        public static Iq FromXElement(XElement xElem)
        {
            var iq = new Iq(xElem.Elements());
            foreach(var attr in xElem.Attributes())
                iq.SetAttributeValue(attr.Name, attr.Value);
            return iq;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}