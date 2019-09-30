using System.Xml.Linq;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient
{
    public class XNames
    {
        public static readonly XName presence = "{jabber:client}presence";

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

        public static readonly XName receipts_request = "{urn:xmpp:receipts}request";
        public static readonly XName receipts_received = "{urn:xmpp:receipts}received";

        public static readonly XName register_register = "{http://jabber.org/features/iq-register}register";
        public static readonly XName register_query = "{jabber:iq:register}query";
        public static readonly XName register_registered = "{jabber:iq:register}registered";

        public static readonly XName discoitems_query = "{http://jabber.org/protocol/disco#items}query";
        public static readonly XName discoitems_item = "{http://jabber.org/protocol/disco#items}item";
        public static readonly XName discoinfo_query = "{http://jabber.org/protocol/disco#info}query";
        public static readonly XName discoinfo_identity = "{http://jabber.org/protocol/disco#info}identity";
        public static readonly XName discoinfo_feature = "{http://jabber.org/protocol/disco#info}feature";

        public static readonly XName pubsub_pubsub = "{http://jabber.org/protocol/pubsub}pubsub";
        public static readonly XName pubsub_publish = "{http://jabber.org/protocol/pubsub}publish";
        public static readonly XName pubsub_item = "{http://jabber.org/protocol/pubsub}item";
        public static readonly XName pubsub_items = "{http://jabber.org/protocol/pubsub}items";
        public static readonly XName pubsub_subscribe = "{http://jabber.org/protocol/pubsub}subscribe";

        public static readonly XName pubsubevent_event = "{http://jabber.org/protocol/pubsub#event}event";
        public static readonly XName pubsubevent_items = "{http://jabber.org/protocol/pubsub#event}items";
        public static readonly XName pubsubevent_item = "{http://jabber.org/protocol/pubsub#event}item";

        public static readonly XName ping_ping = "{urn:xmpp:ping}ping";

        public static readonly XName data_x = "{jabber:x:data}x";

        public static readonly XName vcard_temp_update_x = "{vcard-temp:x:update}x";
        public static readonly XName vcard_temp_update_photo = "{vcard-temp:x:update}photo";
        public static readonly XName vcard_temp_vcard = "{vcard-temp}vCard";

        public static readonly XName axolotl_list = "{eu.siacs.conversations.axolotl}list";
        public static readonly XName axolotl_device = "{eu.siacs.conversations.axolotl}device";
        public static readonly XName axolotl_bundle = "{eu.siacs.conversations.axolotl}bundle";
        public static readonly XName axolotl_signedPreKeyPublic = "{eu.siacs.conversations.axolotl}signedPreKeyPublic";
        public static readonly XName axolotl_signedPreKeySignature = "{eu.siacs.conversations.axolotl}signedPreKeySignature";
        public static readonly XName axolotl_identityKey = "{eu.siacs.conversations.axolotl}identityKey";
        public static readonly XName axolotl_prekeys = "{eu.siacs.conversations.axolotl}prekeys";
        public static readonly XName axolotl_preKeyPublic = "{eu.siacs.conversations.axolotl}preKeyPublic";
        public static readonly XName axolotl_encrypted = "{eu.siacs.conversations.axolotl}encrypted";
        public static readonly XName axolotl_header = "{eu.siacs.conversations.axolotl}header";
        public static readonly XName axolotl_key = "{eu.siacs.conversations.axolotl}key";

        public static readonly XName version_query = "{jabber:iq:version}query";
        public static readonly XName version_name = "{jabber:iq:version}name";
        public static readonly XName version_version = "{jabber:iq:version}version";
        public static readonly XName version_os = "{jabber:iq:version}os";

        public static readonly XName blocking_blocklist = "{urn:xmpp:blocking}blocklist";
        public static readonly XName blocking_item = "{urn:xmpp:blocking}item";
        public static readonly XName blocking_block = "{urn:xmpp:blocking}block";
        public static readonly XName blocking_unblock = "{urn:xmpp:blocking}unblock";

        public static readonly XName time_time = "{urn:xmpp:time}time";
        public static readonly XName time_utc = "{urn:xmpp:time}utc";
        public static readonly XName time_tzo = "{urn:xmpp:time}tzo";

        public static readonly XName last_query = "{jabber:iq:last}query";

        public static readonly XName chatstates_active = "{http://jabber.org/protocol/chatstates}active";
        public static readonly XName chatstates_composing = "{http://jabber.org/protocol/chatstates}composing";
        public static readonly XName chatstates_paused = "{http://jabber.org/protocol/chatstates}paused";
        public static readonly XName chatstates_inactive = "{http://jabber.org/protocol/chatstates}inactive";
        public static readonly XName chatstates_gone = "{http://jabber.org/protocol/chatstates}gone";
    }

    public class XNamespaces
    {
        public static readonly XNamespace stream = "http://etherx.jabber.org/streams";
        public static readonly XNamespace bind = "urn:ietf:params:xml:ns:xmpp-bind";
        public static readonly XNamespace roster = "jabber:iq:roster";
        public static readonly XNamespace discoinfo = "http://jabber.org/protocol/disco#info";
        public static readonly XNamespace time = "urn:xmpp:time";
        public static readonly XNamespace ping = "urn:xmpp:ping";
        public static readonly XNamespace version = "jabber:iq:version";
    }
}