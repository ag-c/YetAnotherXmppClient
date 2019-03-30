using System.Xml.Linq;

namespace YetAnotherXmppClient
{
    public class XNames
    {
        public static readonly XName proceed = "{urn:ietf:params:xml:ns:xmpp-tls}proceed";
        public static readonly XName failure = "{urn:ietf:params:xml:ns:xmpp-tls}failure";

        public static readonly XName starttls = "{urn:ietf:params:xml:ns:xmpp-tls}starttls";
        
        public static readonly XName sasl_mechanisms = "{urn:ietf:params:xml:ns:xmpp-sasl}mechanisms";
        public static readonly XName sasl_mechanism = "{urn:ietf:params:xml:ns:xmpp-sasl}mechanism";
        public static readonly XName sasl_auth = "{urn:ietf:params:xml:ns:xmpp-sasl}auth";
        public static readonly XName sasl_challenge = "{urn:ietf:params:xml:ns:xmpp-sasl}challenge";
        public static readonly XName sasl_success = "{urn:ietf:params:xml:ns:xmpp-sasl}success";
        public static readonly XName sasl_response = "{urn:ietf:params:xml:ns:xmpp-sasl}response";
        
        public static readonly XName bind_bind = "{urn:ietf:params:xml:ns:xmpp-bind}bind";
        public static readonly XName bind_resource = "{urn:ietf:params:xml:ns:xmpp-bind}resource";
        public static readonly XName bind_jid = "{urn:ietf:params:xml:ns:xmpp-bind}jid";

        public static readonly XName session_session = "{urn:ietf:params:xml:ns:xmpp-session}session";

        public static readonly XName rosterver_ver = "{urn:xmpp:features:rosterver}ver";

        public static readonly XName roster_query = "{jabber:iq:roster}query";
        public static readonly XName roster_item = "{jabber:iq:roster}item";
        public static readonly XName roster_group = "{jabber:iq:roster}group";
    }

    public class XNamespaces
    {
        public static readonly XNamespace stream = "http://etherx.jabber.org/streams";

        public static readonly XNamespace bind = "urn:ietf:params:xml:ns:xmpp-bind";
        public static readonly XNamespace roster = "jabber:iq:roster";
    }
}