using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;

namespace YetAnotherXmppClient.Protocol
{
    public class Presence
    {
        public string From { get; set; }
        //public string To { get; set; }
        public int? Priority { get; set; }
        public PresenceShow Show { get; set; }
        public IEnumerable<string> Stati { get; set; }
    }

    public class PresenceProtocolHandler : IPresenceCallback
    {
        private readonly AsyncXmppStream xmppStream;

        public Func<string, bool> OnSubscriptionRequestReceived { get; set; }

        // <full-jid, presence>
        public ConcurrentDictionary<string, Presence> PresenceByJid { get; } = new ConcurrentDictionary<string, Presence>();


        public PresenceProtocolHandler(AsyncXmppStream xmppStream)
        {
            this.xmppStream = xmppStream;
            this.xmppStream.RegisterPresenceCallback(this);
        }

        public async Task<bool> RequestSubscriptionAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.subscribe)
            {
                To = contactJid.ToBareJid()
            };

            var presenceResp = await this.xmppStream.WritePresenceAndReadReponseAsync(presence);

            if (presenceResp.HasErrorType())
            {
                Log.Logger.Error("Failed to request presence subscription");
                return false;
            }

            return true;
        }

        public async Task CancelSubscriptionAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.unsubscribed)
            {
                To = contactJid.ToBareJid()
            };

            await this.xmppStream.WriteAsync(presence);
        }

        public async Task UnsubscribeAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.unsubscribe)
            {
                To = contactJid.ToBareJid()
            };

            await this.xmppStream.WriteAsync(presence);
        }

        void IPresenceCallback.PresenceReceived(XElement presenceXElem)
        {
            Expectation.Expect("presence", presenceXElem.Name, presenceXElem);

            if (presenceXElem.Attribute("type")?.Value == PresenceType.subscribe.ToString())
            {
                var fromValue = presenceXElem.Attribute("from").Value;

                var responseType = this.OnSubscriptionRequestReceived?.Invoke(fromValue) ?? false
                    ? PresenceType.subscribed
                    : PresenceType.unsubscribed;

                var response = new Core.Stanza.Presence(responseType)
                {
                    To = fromValue
                };

                this.xmppStream.WriteAsync(response);
            }
            else if(!presenceXElem.HasAttribute("type"))
            {
                Expectation.Expect(() => presenceXElem.HasAttribute("from"), presenceXElem);

                var fromVal = presenceXElem.Attribute("from").Value;
                var showElem = presenceXElem.Element("show");
                var prioElem = presenceXElem.Element("priority");

                new Presence
                {
                    From = fromVal,
                    Show = showElem != null
                        ? (PresenceShow) Enum.Parse(typeof(PresenceShow), showElem.Value) : PresenceShow.None,
                    Stati = presenceXElem.Elements("status").Select(xe => xe.Value),
                    Priority = prioElem != null ? int.Parse(prioElem.Value) : (int?) null
                };

                this.PresenceByJid.AddOrUpdate(fromVal, _ => )
            }
        }

        public Task BroadcastPresenceAsync(PresenceShow? show = null, string status = null)
        {
            var presence = new Core.Stanza.Presence(show, status);

            return this.xmppStream.WriteAsync(presence);
        }

        public Task SendUnavailableAsync()
        {
            return this.xmppStream.WriteAsync(new Core.Stanza.Presence(PresenceType.unavailable));
        }
    }
}
