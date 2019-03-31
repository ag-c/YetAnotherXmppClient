using System;
using System.Xml.Linq;

namespace YetAnotherXmppClient.Core
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

        public Iq(IqType type, object content) : base("iq", content)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Type = type;
        }

        public static implicit operator string(Iq iq)
        {
            return iq.ToString();
        }
    }
}