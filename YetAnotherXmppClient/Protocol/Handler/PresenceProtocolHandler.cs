using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Handler
{
    public class Presence
    {
        public Jid Jid { get; set; }
        //public string To { get; set; }
        public int? Priority { get; set; }
        public PresenceShow Show { get; set; }
        public IEnumerable<string> Stati { get; set; }
    }

    public class PresenceProtocolHandler : ProtocolHandlerBase, IPresenceReceivedCallback
    {
        public Func<string, Task<bool>> OnSubscriptionRequestReceived { get; set; }

        // <full-jid, presence>
        public ConcurrentDictionary<string, Presence> PresenceByJid { get; } = new ConcurrentDictionary<string, Presence>();


        public PresenceProtocolHandler(XmppStream xmppStream)
            : base(xmppStream, null)
        {
            this.XmppStream.RegisterPresenceCallback(this);
        }

        public IEnumerable<Presence> GetAllPresencesForBareJid(string bareJid)
        {
            var fullJids = this.PresenceByJid.Keys.Where(k => k.StartsWith(bareJid));
            return fullJids.Select(fullJid => this.PresenceByJid[fullJid]);
        }

        public async Task<bool> RequestSubscriptionAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.subscribe)
            {
                To = contactJid.ToBareJid()
            };

            await this.XmppStream.WriteElementAsync(presence);
            //UNDONE no response
            //if (presenceResp.IsErrorType())
            //{
            //    Log.Error("Failed to request presence subscription");
            //    return false;
            //}

            return true;
        }

        public async Task CancelSubscriptionAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.unsubscribed)
            {
                To = contactJid.ToBareJid()
            };

            await this.XmppStream.WriteElementAsync(presence);
        }

        public async Task UnsubscribeAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.unsubscribe)
            {
                To = contactJid.ToBareJid()
            };

            await this.XmppStream.WriteElementAsync(presence);
        }

        //UNDONE async
        async void IPresenceReceivedCallback.PresenceReceived(Core.Stanza.Presence presence)
        {
            Expect(XNames.presence, presence.Name, presence);

            if (presence.Type == PresenceType.subscribe)
            {
                var responseType = await (this.OnSubscriptionRequestReceived?.Invoke(presence.From) ?? Task.FromResult(false))
                    ? PresenceType.subscribed
                    : PresenceType.unsubscribed;

                var response = new Core.Stanza.Presence(responseType)
                {
                    To = presence.From
                };

                await this.XmppStream.WriteElementAsync(response);
            }
            else if(!presence.HasAttribute("type"))
            {
                Expectation.Expect(() => presence.HasAttribute("from"), presence);

                var fromVal = presence.Attribute("from").Value;

                this.PresenceByJid.AddOrUpdate(fromVal, _ =>
                {
                    var instance = new Presence();
                    UpdatePresence(instance, presence);
                    return instance;
                }, (_, existing) => UpdatePresence(existing, presence));
            }

            Presence UpdatePresence(Presence existing, XElement newPresenceElem)
            {
                var fromVal = presence.Attribute("from").Value;
                var showElem = newPresenceElem.Element("show");
                var prioElem = newPresenceElem.Element("priority");

                existing.Jid = new Jid(fromVal);
                existing.Show = showElem != null
                    ? (PresenceShow)Enum.Parse(typeof(PresenceShow), showElem.Value)
                    : PresenceShow.None;
                existing.Stati = newPresenceElem.Elements("status").Select(xe => xe.Value);
                existing.Priority = prioElem != null ? int.Parse(prioElem.Value) : (int?)null;

                return existing;
            }
    }

        public Task BroadcastPresenceAsync(PresenceShow? show = null, string status = null)
        {
            var presence = new Core.Stanza.Presence(show, status);

            return this.XmppStream.WriteElementAsync(presence);
        }

        public Task SendUnavailableAsync()
        {
            return this.XmppStream.WriteElementAsync(new Core.Stanza.Presence(PresenceType.unavailable));
        }
    }
}
