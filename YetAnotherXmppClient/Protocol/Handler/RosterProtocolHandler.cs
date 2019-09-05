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


    public class RosterProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        private ConcurrentDictionary<string, RosterItem> currentRosterItems = new ConcurrentDictionary<string, RosterItem>();
        private readonly IIqFactory iqFactory;

        public event EventHandler<IEnumerable<RosterItem>> RosterUpdated;


        public RosterProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
            this.iqFactory = new DefaultClientIqFactory(() => runtimeParameters["jid"]);
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.roster, this);
        }
        
        public async Task<IEnumerable<RosterItem>> RequestRosterAsync()
        {
            var requestIq = this.iqFactory.CreateGetIq(new RosterQuery());

            var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq);

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

            this.RaiseRosterUpdated();

            return this.currentRosterItems.Values;
        }

        private void RaiseRosterUpdated()
        {
            Log.Logger.CurrentRosterItems(this.currentRosterItems.Values);
            this.RosterUpdated?.Invoke(this, this.currentRosterItems.Values);
        }

        public async Task<bool> AddRosterItemAsync(string bareJid, string name, IEnumerable<string> groups)
        {
            var requestIq = this.iqFactory.CreateSetIq(new RosterQuery(bareJid, name, groups));

            await this.XmppStream.WriteElementAsync(requestIq);
            //var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq);

            //if (responseIq.IsErrorType())
            //{
            //    Log.Error($"Failed to add roster item: {responseIq}");
            //    return false;
            //}

            //Expectation.Expect("result", responseIq.Attribute("type")?.Value, responseIq);

            this.currentRosterItems.TryAdd(bareJid, new RosterItem { Jid = bareJid, Name = name});

            this.RaiseRosterUpdated();

            return true;
        }

        public async Task<bool> DeleteRosterItemAsync(string bareJid)
        {
            var requestIq = this.iqFactory.CreateSetIq(new RosterQuery(bareJid, remove: true), from: this.RuntimeParameters["jid"]);
            
            //var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq);
            await this.XmppStream.WriteElementAsync(requestIq);

            //if (responseIq.IsErrorType())
            //{
            //    Log.Error($"Failed to delete roster item: {responseIq}");
            //    return false;
            //}

            //Expect("result", responseIq.Attribute("type")?.Value, responseIq);

            this.currentRosterItems.TryRemove(bareJid, out _);

            this.RaiseRosterUpdated();

            return true;
        }

        void IIqReceivedCallback.IqReceived(Iq iq)
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

                this.RaiseRosterUpdated();

                //UNDONE reply to server (2.1.6.  Roster Push)
            }
            else if(iq.Type == IqType.result)
            {
                //Debugger.Break();
            }
        }
    }
}