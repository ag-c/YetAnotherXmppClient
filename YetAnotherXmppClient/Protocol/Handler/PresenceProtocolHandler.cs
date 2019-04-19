using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
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
        public PresenceShow? Show { get; set; }
        public IEnumerable<string> Stati { get; set; }
    }

    //RFC 6121 
    public class PresenceProtocolHandler : ProtocolHandlerBase, IPresenceReceivedCallback
    {
        public Func<string, Task<bool>> OnSubscriptionRequestReceived { get; set; }

        // <full-jid, presence>
        public ConcurrentDictionary<string, Presence> PresenceByJid { get; } = new ConcurrentDictionary<string, Presence>();


        public PresenceProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters)
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

        //3.2.1.  Client Generation of Subscription Cancellation
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

            if (!presence.Type.HasValue)
            {
                Expect(() => presence.HasAttribute("from"), presence);

                this.PresenceByJid.AddAndUpdate(presence.From, existing => UpdatePresence(existing, presence));
            }
            else if (presence.Type == PresenceType.subscribe)
            {
                // reject subscription request if no handler is registered (TODO correct behavior?)
                Log.Logger.LogIfMissingSubscriptionRequestHandler(this.OnSubscriptionRequestReceived == null);

                var responseType = this.OnSubscriptionRequestReceived != null && await this.OnSubscriptionRequestReceived(presence.From) 
                                       ? PresenceType.subscribed
                                       : PresenceType.unsubscribed;

                var response = new Core.Stanza.Presence(responseType)
                {
                    To = presence.From
                };

                await this.XmppStream.WriteElementAsync(response);
            }

            Presence UpdatePresence(Presence existing, Core.Stanza.Presence newPresenceElem)
            {
                existing.Jid = new Jid(presence.From);
                existing.Show = newPresenceElem.Show;
                existing.Stati = newPresenceElem.Stati;
                existing.Priority = newPresenceElem.Priority;
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
