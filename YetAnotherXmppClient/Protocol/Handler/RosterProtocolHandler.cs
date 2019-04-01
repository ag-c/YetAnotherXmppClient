using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public string Subscription { get; set; }
        public bool IsSubscriptionPending { get; set; }

        public override string ToString()
        {
            return this.Jid + "\t\t\t" + this.Name + "\t\t\t" + this.Subscription.ToUpper();
        }

        public static RosterItem FromXElement(XElement elem)
        {
            return new RosterItem
            {
                Jid = elem.Attribute("jid").Value,
                Name = elem.Attribute("name")?.Value,
                Groups = elem.Elements(XNames.roster_group)?.Select(xe => xe.Value),
                Subscription = elem.Attribute("subscription")?.Value ?? "<not set>"
            };
        }
    }


    public class RosterProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        private ConcurrentDictionary<string, RosterItem> currentRosterItems = new ConcurrentDictionary<string, RosterItem>();
        private readonly IIqFactory iqFactory;

        public event EventHandler<IEnumerable<RosterItem>> RosterUpdated;


        public RosterProtocolHandler(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters)
            : base(xmppStream, runtimeParameters)
        {
            this.iqFactory = new DefaultClientIqFactory(() => runtimeParameters["jid"]);
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.roster, this);
        }
        
        public async Task<IEnumerable<RosterItem>> RequestRosterAsync()
        {
            var requestIq = this.iqFactory.CreateGetIq(new RosterQuery());

            var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq);

            Expectation.Expect("result", responseIq.Attribute("type")?.Value, responseIq);

            if (responseIq.IsEmpty)
            {
                //UNDONE
                throw new NotImplementedException("6121/2.6.3.: ...an empty IQ-result (thus indicating that any roster modifications will be sent via roster pushes..");
            }

            var queryElem = responseIq.Element(XNames.roster_query);

            var ver = queryElem?.Attribute("ver")?.Value; //UNDONE

            this.currentRosterItems.Clear();

            foreach (var item in queryElem.Elements(XNames.roster_item))
            {
                this.currentRosterItems.TryAdd(item.Attribute("jid").Value, RosterItem.FromXElement(item));
            }

            Log.Logger.CurrentRosterItems(this.currentRosterItems.Values);
            this.RosterUpdated?.Invoke(this, this.currentRosterItems.Values);

            return this.currentRosterItems.Values;
        }

        public async Task<bool> AddRosterItemAsync(string bareJid, string name, IEnumerable<string> groups)
        {
            var requestIq = this.iqFactory.CreateSetIq(new RosterQuery(bareJid, name, groups));

            var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq);

            if (responseIq.IsErrorType())
            {
                Log.Error($"Failed to add roster item: {responseIq}");
                return false;
            }

            Expectation.Expect("result", responseIq.Attribute("type")?.Value, responseIq);

            return true;
        }

        public async Task<bool> DeleteRosterItemAsync(string bareJid)
        {
            var requestIq = this.iqFactory.CreateSetIq(new RosterQuery(bareJid, remove: true), from: this.RuntimeParameters["jid"]);
            
            var responseIq = await this.XmppStream.WriteIqAndReadReponseAsync(requestIq);

            if (responseIq.IsErrorType())
            {
                Log.Error($"Failed to delete roster item: {responseIq}");
                return false;
            }

            Expect("result", responseIq.Attribute("type")?.Value, responseIq);

            return true;
        }

        void IIqReceivedCallback.IqReceived(Iq iq)
        {
            Log.Verbose($"ImProtocolHandler handles roster iq sent by server: " + iq);

            if (iq.From != this.RuntimeParameters["jid"].ToBareJid())
            {
                // 2.1.6.: A receiving client MUST ignore the stanza unless it has no 'from'
                // attribute(i.e., implicitly from the bare JID of the user's
                // account) or it has a 'from' attribute whose value matches the
                // user's bare JID <user@domainpart>.
                return;
            }

            // Roster push
            if (iq.FirstNode is XElement queryElem && queryElem.Name == XNames.roster_query)
            {
                Expect(IqType.set.ToString(), iq.Attribute("type")?.Value, iq);

                var itemElem = queryElem.Element(XNames.roster_item);
                if (itemElem.Attribute("subscription")?.Value == "remove")
                {
                    this.currentRosterItems.TryRemove(itemElem.Attribute("jid").Value, out _);
                }
                else
                {
                    var jid = itemElem.Attribute("jid")?.Value;
                    this.currentRosterItems.AddAndUpdate(jid, item =>
                    {
                        item.Jid = jid;
                        item.Name = itemElem.Attribute("name")?.Value;
                        item.Groups = itemElem.Elements(XNames.roster_group)?.Select(xe => xe.Value);
                        item.Subscription = itemElem.Attribute("subscription")?.Value ?? "<not set>";
                        item.IsSubscriptionPending = itemElem.Attribute("ask")?.Value == "subscribe";
                    });
                }

                Log.Logger.CurrentRosterItems(this.currentRosterItems.Values);
                this.RosterUpdated?.Invoke(this, this.currentRosterItems.Values);

                //UNDONE reply to server (2.1.6.  Roster Push)
            }
        }
    }
}