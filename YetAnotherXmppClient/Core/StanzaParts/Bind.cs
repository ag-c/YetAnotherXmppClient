using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    public class Bind : XElement
    {
        public string Jid => this.Element(XNames.bind_jid)?.Value;


        //copy constructor
        private Bind(XElement bindXElem)
            : base(XNames.bind_bind, bindXElem.ElementsAndAttributes())
        {
        }

        //client
        public Bind(string resource = null) 
            : base(XNames.bind_bind, resource == null ? null : new XElement(XNames.bind_resource, resource))
        {
            
        }
    }
}
