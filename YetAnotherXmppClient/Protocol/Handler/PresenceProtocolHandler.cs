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
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.Infrastructure.Queries;
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
    public class PresenceProtocolHandler : ProtocolHandlerBase, IPresenceReceivedCallback, IAsyncCommandHandler<BroadcastPresenceCommand>
    {
        // <full-jid, presence>
        public ConcurrentDictionary<string, Presence> PresenceByJid { get; } = new ConcurrentDictionary<string, Presence>();


        public PresenceProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, null, mediator)
        {
            this.XmppStream.RegisterPresenceCallback(this);
            this.Mediator.RegisterHandler<BroadcastPresenceCommand>(this);
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

            await this.XmppStream.WriteElementAsync(presence).ConfigureAwait(false);
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

            await this.XmppStream.WriteElementAsync(presence).ConfigureAwait(false);
        }

        public async Task UnsubscribeAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.unsubscribe)
            {
                To = contactJid.ToBareJid()
            };

            await this.XmppStream.WriteElementAsync(presence).ConfigureAwait(false);
        }

        //UNDONE async
        async Task IPresenceReceivedCallback.HandlePresenceReceivedAsync(Core.Stanza.Presence presence)
        {
            Expect(XNames.presence, presence.Name, presence);

            if (!presence.Type.HasValue || presence.Type == PresenceType.unavailable)
            {
                Expect(() => presence.HasAttribute("from"), presence);

                this.PresenceByJid.AddAndUpdate(presence.From, existing => UpdatePresence(existing, presence));

                await this.Mediator.PublishAsync(new PresenceEvent
                                                     {
                                                         Jid = new Jid(presence.From),
                                                         IsAvailable = presence.IsAvailable
                                                     }).ConfigureAwait(false);
            }
            else if (presence.Type == PresenceType.subscribe)
            {
                var requestGranted =
                    await this.Mediator.QueryAsync<SubscriptionRequestQuery, bool>(new SubscriptionRequestQuery(bareJid: presence.From))
                        .ConfigureAwait(false); //UNDONE why generic types not inferred
                var responseType = requestGranted ? PresenceType.subscribed : PresenceType.unsubscribed;

                var response = new Core.Stanza.Presence(responseType)
                                   {
                                       To = presence.From
                                   };

                await this.XmppStream.WriteElementAsync(response).ConfigureAwait(false);
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

        Task IAsyncCommandHandler<BroadcastPresenceCommand>.HandleCommandAsync(BroadcastPresenceCommand command)
        {
            return this.BroadcastPresenceAsync(command.Show, command.Status);
        }
    }
}
