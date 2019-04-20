using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    class AxolotlList : XElement
    {
        public IEnumerable<int> DeviceIds => this.Elements(XNames.axolotl_device)?.Select(d => int.Parse(d.Attribute("id").Value));

        private AxolotlList(XElement listXElem)
            : base(XNames.axolotl_list, listXElem.ElementsAndAttributes())
        {
        }

        public AxolotlList(IEnumerable<int> deviceIds)
            : base(XNames.axolotl_list, deviceIds.Select(did => new AxolotlDevice(did)))
        {
        }

        public static AxolotlList FromXElement(XElement listXElem)
        {
            return new AxolotlList(listXElem);
        }
    }
}
