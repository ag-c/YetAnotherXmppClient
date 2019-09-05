using System.Xml.Linq;

namespace YetAnotherXmppClient.Core.StanzaParts
{
    class VersionQuery : XElement
    {
        public VersionQuery(string name, string version, string os)
            : base(XNames.version_query,
                       new XElement(XNames.version_name, name),
                       new XElement(XNames.version_version, version),
                       new XElement(XNames.version_os, os))
        {
            
        }
    }
}
