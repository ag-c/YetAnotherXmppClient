using System;
using System.Xml.Linq;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Core.Stanza
{
    public enum IqType
    {
        get,
        set,
        result,
        error
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
            get => (IqType)Enum.Parse(typeof(IqType), this.Attribute("type").Value);
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

        //copy ctor for element from server
        private Iq(XElement messageXElem)
            : base("{jabber:client}iq", messageXElem.ElementsAndAttributes())
        {
        }

        public Iq(IqType type, object content=null, string name= "iq") : base(name, content) //{jabber:client}
        {
            this.Id = Guid.NewGuid().ToString();
            this.Type = type;
        }


        public static implicit operator string(Iq iq)
        {
            return iq.ToString();
        }

        public static Iq FromXElement(XElement xElem)
        {
            Expectation.Expect("iq", xElem.Name.LocalName, xElem);
            return new Iq(xElem);
        }
    }

    public class IqGet : Iq
    {
        public IqGet(object content = null, string name = "iq")
            : base(IqType.get, content, name)
        {
        }
    }

    public class IqSet : Iq
    {
        public IqSet(object content = null, string name = "iq")
            : base(IqType.set, content, name)
        {
        }
    }
}