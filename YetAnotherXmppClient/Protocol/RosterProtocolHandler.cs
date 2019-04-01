using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
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
    }

    interface IIqFactory
    {
        Iq CreateSetIq(object content);
        Iq CreateGetIq(object content);
    }
    class DefaultClientIqFactory : IIqFactory
    {
        private readonly Func<string> fromFunc;

        public DefaultClientIqFactory(Func<string> fromFunc)
        {
            this.fromFunc = fromFunc;
        }

        public Iq CreateSetIq(object content)
        {
            return this.CreateInternal(IqType.set, content);
        }

        public Iq CreateGetIq(object content)
        {
            return this.CreateInternal(IqType.get, content);
        }

        private Iq CreateInternal(IqType iqType, object content)
        {
            var iq = new Iq(iqType, content);
            if (this.fromFunc != null)
                iq.From = this.fromFunc();
            return iq;
        }
    }
    public class RosterProtocolHandler : IIqReceivedCallback
    {
        private readonly AsyncXmppStream xmppStream;
        private readonly Dictionary<string, string> runtimeParameters;
        private IIqFactory iqFactory;

        public event EventHandler<IEnumerable<RosterItem>> RosterUpdated;


        public RosterProtocolHandler(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters)
        {
            this.xmppStream = xmppStream;
            this.iqFactory = new DefaultClientIqFactory(() => runtimeParameters["jid"]);
            this.xmppStream.RegisterIqNamespaceCallback(XNamespaces.roster, this);
        }
        
        
        List<RosterItem> currentRosterItems = new List<RosterItem>();
        

        public async Task<IEnumerable<RosterItem>> RequestRosterAsync()
        {
            var iq = this.iqFactory.CreateGetIq(new RosterQuery());

            var iqResp = await this.xmppStream.WriteIqAndReadReponseAsync(iq);

            Expect("result", iqResp.Attribute("type")?.Value, iqResp);

            if (iqResp.IsEmpty)
            {
                Debugger.Break();
                //UNDONE
                //6121/2.6.3.: ...an empty IQ-result (thus
                //indicating that any roster modifications will be sent via roster
                //pushes..
            }

            var queryElem = iqResp.Element(XNames.roster_query);

            var ver = queryElem?.Attribute("ver").Value; //UNDONE

            if (queryElem.IsEmpty)
                return new RosterItem[0];

            var rosterItems = new List<RosterItem>();
            foreach (var item in queryElem.Elements(XNames.roster_item))
            {
                rosterItems.Add(new RosterItem
                {
                    Jid = item.Attribute("jid").Value,
                    Name = item.Attribute("name")?.Value,
                    Groups = item.Elements(XNames.roster_group)?.Select(xe => xe.Value),
                    Subscription = item.Attribute("subscription")?.Value ?? "<not set>"
                });
            }

            this.currentRosterItems = rosterItems;
            Log.Logger.CurrentRosterItems(this.currentRosterItems);
            this.RosterUpdated?.Invoke(this, this.currentRosterItems);
            return rosterItems;
        }

        public async Task<bool> AddRosterItemAsync(string bareJid, string name, IEnumerable<string> groups)
        {
            var iq = this.iqFactory.CreateSetIq(new RosterQuery(bareJid, name, groups));

            var iqResp = await this.xmppStream.WriteIqAndReadReponseAsync(iq);

            if (iqResp.IsErrorType())
            {
                Log.Error($"Failed to add roster item: {iqResp}");
                return false;
            }

            Expect("result", iqResp.Attribute("type")?.Value, iqResp);

            return true;
        }

        //class IqBuilder
        //{
        //    public static IqBuilder New => new IqBuilder();

        //    private IqType iqType;
        //    public IqBuilder SetType
        //    {
        //        get
        //        {
        //            this.iqType = IqType.set;
        //            return this;
        //        }
        //    }
        //    public IqBuilder GetType
        //    {
        //        get
        //        {
        //            this.iqType = IqType.get;
        //            return this;
        //        }
        //    }

        //    private string from;
        //    public IqBuilder From(string from)
        //    {
        //        this.from = from;
        //        return this;
        //    }

        //    private object content;
        //    public IqBuilder WithContent(object obj)
        //    {
        //        this.content = content;
        //        return this;
        //    }

        //    public Iq Build()
        //    {
        //        var iq = new Iq(this.iqType, this.content);
        //        if (this.@from != null)
        //            iq.From = this.@from;
        //        return iq;
        //    }
        //}

        public async Task<bool> DeleteRosterItemAsync(string bareJid)
        {
            var iq = this.iqFactory.CreateSetIq(new RosterQuery(bareJid, remove: true));
            //var iq = new Iq(IqType.set, new RosterQuery(bareJid, remove: true))
            //{
            //    From = this.runtimeParameters["jid"]
            //};
            //IqBuilder.New.SetType
            //    .From(this.runtimeParameters["jid"])
            //    .WithContent(new RosterQuery(bareJid, remove: true))
            //    .Build();

            var iqResp = await this.xmppStream.WriteIqAndReadReponseAsync(iq);

            if (iqResp.IsErrorType())
            {
                Log.Error($"Failed to delete roster item: {iqResp}");
                return false;
            }

            Expect("result", iqResp.Attribute("type")?.Value, iqResp);

            return true;
        }

        void IIqReceivedCallback.IqReceived(XElement iqElem)
        {
            Log.Verbose($"ImProtocolHandler handles roster iq sent by server: " + iqElem);


            if (iqElem.HasAttribute("from") &&
                iqElem.Attribute("from").Value != this.runtimeParameters["jid"].ToBareJid())
            {
                // 2.1.6.: A receiving client MUST ignore the stanza unless it has no 'from'
                // attribute(i.e., implicitly from the bare JID of the user's
                // account) or it has a 'from' attribute whose value matches the
                // user's bare JID <user@domainpart>.
                return;
            }

            // Roster push
            if (iqElem.FirstNode is XElement queryElem && queryElem.Name == XNames.roster_query)
            {
                Expect(IqType.set.ToString(), iqElem.Attribute("type")?.Value, iqElem);

                var itemElem = queryElem.Element(XNames.roster_item);
                if (itemElem.Attribute("subscription")?.Value == "remove")
                {
                    this.currentRosterItems.RemoveAll(ri => ri.Jid == itemElem.Attribute("jid")?.Value);
                }
                else
                {
                    bool needsAdd = false;
                    var localRosterItem = this.currentRosterItems.FirstOrDefault(ri => ri.Jid == itemElem.Attribute("jid")?.Value);
                    if (localRosterItem == null)
                    {
                        needsAdd = true;
                        localRosterItem = new RosterItem();
                    }

                    localRosterItem.Name = itemElem.Attribute("name")?.Value;
                    localRosterItem.Groups = itemElem.Elements(XNames.roster_group)?.Select(xe => xe.Value);
                    localRosterItem.Subscription = itemElem.Attribute("subscription")?.Value ?? "<not set>";
                    localRosterItem.IsSubscriptionPending = itemElem.Attribute("ask")?.Value == "subscribe";

                    if (needsAdd)
                        this.currentRosterItems.Add(localRosterItem);
                }

                Log.Logger.CurrentRosterItems(this.currentRosterItems);
                this.RosterUpdated?.Invoke(this, this.currentRosterItems);

                //UNDONE reply to server (2.1.6.  Roster Push)
            }

        }

    }
}