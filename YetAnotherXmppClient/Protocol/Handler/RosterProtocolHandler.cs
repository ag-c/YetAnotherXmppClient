using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.Infrastructure.Queries;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Handler
{
    public class RosterItem
    {
        public string Jid { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Groups { get; set; }
        public SubscriptionState Subscription { get; set; }
        public bool IsSubscriptionPending { get; set; }

        public override string ToString()
        {
            return this.Jid + "\t\t\t" + this.Name + "\t\t\t" + this.Subscription;
        }

        public static RosterItem FromXElement(XElement elem)
        {
            return new RosterItem
            {
                Jid = elem.Attribute("jid").Value,
                Name = elem.Attribute("name")?.Value,
                Groups = elem.Elements(XNames.roster_group)?.Select(xe => xe.Value),
                Subscription = elem.HasAttribute("subscription") ? (SubscriptionState)Enum.Parse(typeof(SubscriptionState), elem.Attribute("subscription").Value) : SubscriptionState.none
        };
        }
    }


    public class RosterProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback, 
        IAsyncQueryHandler<AddRosterItemQuery, bool>,
        IAsyncQueryHandler<DeleteRosterItemQuery, bool>,
        IAsyncQueryHandler<RosterItemsQuery, IEnumerable<RosterItem>>
    {
        private readonly ConcurrentDictionary<string, RosterItem> currentRosterItems = new ConcurrentDictionary<string, RosterItem>();
        private readonly IIqFactory iqFactory;


        public RosterProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.iqFactory = new DefaultClientIqFactory(() => runtimeParameters["jid"]);
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.roster, this);
            this.Mediator.RegisterHandler<AddRosterItemQuery, bool>(this);
            this.Mediator.RegisterHandler<DeleteRosterItemQuery, bool>(this);
            this.Mediator.RegisterHandler<RosterItemsQuery, IEnumerable<RosterItem>>(this);
        }
        
        public async Task<IEnumerable<RosterItem>> RequestRosterAsync()
        {
            var requestIq = this.iqFactory.CreateGetIq(new RosterQuery());

            var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq).ConfigureAwait(false);

            Expect(IqType.result, responseIq.Type, responseIq);

            if (responseIq.IsEmpty)
            {
                //UNDONE
                throw new NotImplementedException("6121/2.6.3.: ...an empty IQ-result (thus indicating that any roster modifications will be sent via roster pushes..");
            }

            var rosterQuery = responseIq.GetContent<RosterQuery>();

            var ver = rosterQuery.Ver; //UNDONE

            this.currentRosterItems.Clear();
            foreach (var item in rosterQuery.Items)
            {
                this.currentRosterItems.TryAdd(item.Jid, RosterItem.FromXElement(item));
            }

            await this.RaiseRosterUpdatedAsync().ConfigureAwait(false);

            return this.currentRosterItems.Values;
        }

        private Task RaiseRosterUpdatedAsync()
        {
            Log.Logger.CurrentRosterItems(this.currentRosterItems.Values);
            return this.Mediator.PublishAsync(new RosterUpdateEvent(this.currentRosterItems.Values));
        }

        public async Task<bool> AddRosterItemAsync(string bareJid, string name, IEnumerable<string> groups)
        {
            var requestIq = this.iqFactory.CreateSetIq(new RosterQuery(bareJid, name, groups));

            await this.XmppStream.WriteElementAsync(requestIq).ConfigureAwait(false);
            //var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq);

            //if (responseIq.IsErrorType())
            //{
            //    Log.Error($"Failed to add roster item: {responseIq}");
            //    return false;
            //}

            //Expectation.Expect("result", responseIq.Attribute("type")?.Value, responseIq);

            this.currentRosterItems.TryAdd(bareJid, new RosterItem { Jid = bareJid, Name = name});

            await this.RaiseRosterUpdatedAsync().ConfigureAwait(false);

            return true;
        }

        public async Task<bool> DeleteRosterItemAsync(string bareJid)
        {
            var requestIq = this.iqFactory.CreateSetIq(new RosterQuery(bareJid, remove: true), from: this.RuntimeParameters["jid"]);
            
            //var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq);
            await this.XmppStream.WriteElementAsync(requestIq).ConfigureAwait(false);

            //if (responseIq.IsErrorType())
            //{
            //    Log.Error($"Failed to delete roster item: {responseIq}");
            //    return false;
            //}

            //Expect("result", responseIq.Attribute("type")?.Value, responseIq);

            this.currentRosterItems.TryRemove(bareJid, out _);

            await this.RaiseRosterUpdatedAsync().ConfigureAwait(false);

            return true;
        }

        async Task IIqReceivedCallback.HandleIqReceivedAsync(Iq iq)
        {
            Log.Verbose($"ImProtocolHandler handles roster iq sent by server: " + iq);

            if (iq.From != null && iq.From != this.RuntimeParameters["jid"].ToBareJid())
            {
                // 2.1.6.: A receiving client MUST ignore the stanza unless it has no 'from'
                // attribute(i.e., implicitly from the bare JID of the user's
                // account) or it has a 'from' attribute whose value matches the
                // user's bare JID <user@domainpart>.
                return;
            }

            // Roster push
            if (iq.Type == IqType.set && iq.HasElement(XNames.roster_query))
            {
                var rosterQuery = iq.GetContent<RosterQuery>();
                var rosterItem = rosterQuery.Items.Single();
                if (rosterItem.Subscription == SubscriptionState.remove)
                {
                    this.currentRosterItems.TryRemove(rosterItem.Jid, out _);
                }
                else
                {
                    this.currentRosterItems.AddAndUpdate(rosterItem.Jid, item =>
                    {
                        item.Jid = rosterItem.Jid;
                        item.Name = rosterItem.ItemName;
                        item.Groups = rosterItem.Groups;
                        item.Subscription = rosterItem.Subscription;
                        item.IsSubscriptionPending = rosterItem.Attribute("ask")?.Value == "subscribe";
                    });
                }

                await this.RaiseRosterUpdatedAsync().ConfigureAwait(false);

                //UNDONE reply to server (2.1.6.  Roster Push)
            }
            else if(iq.Type == IqType.result)
            {
                //Debugger.Break();
            }
        }

        Task<bool> IAsyncQueryHandler<AddRosterItemQuery, bool>.HandleQueryAsync(AddRosterItemQuery query)
        {
            return this.AddRosterItemAsync(query.BareJid, query.Name, query.Groups);
        }

        Task<bool> IAsyncQueryHandler<DeleteRosterItemQuery, bool>.HandleQueryAsync(DeleteRosterItemQuery query)
        {
            return this.DeleteRosterItemAsync(query.BareJid);
        }

        Task<IEnumerable<RosterItem>> IAsyncQueryHandler<RosterItemsQuery, IEnumerable<RosterItem>>.HandleQueryAsync(RosterItemsQuery query)
        {
            return Task.FromResult<IEnumerable<RosterItem>>(this.currentRosterItems.Values);
        }
    }
}