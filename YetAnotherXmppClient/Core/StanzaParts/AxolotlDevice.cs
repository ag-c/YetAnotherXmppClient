using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    class AxolotlDevice : XElement
    {
        public AxolotlDevice(int deviceId)
            : base(XNames.axolotl_device, new XAttribute("id", deviceId.ToString()))
        {
        }
    }
}
