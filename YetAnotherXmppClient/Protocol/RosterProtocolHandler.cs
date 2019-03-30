using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Protocol
{
    public class RosterItem
    {
        public string Jid { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Groups { get; set; }
    }
    
    public class RosterProtocolHandler : IServerIqCallback
    {
        private readonly XmppStream xmppStream;
        private readonly Dictionary<string, string> runtimeParameters;


        public RosterProtocolHandler(XmppStream xmppStream)
        {
            this.xmppStream = xmppStream;
            this.xmppStream.RegisterServerIqCallback(XNamespaces.roster, this);
        }
        
        
        List<RosterItem> currentRosterItems = new List<RosterItem>();
        
        public async Task<IEnumerable<RosterItem>> RequestRosterAsync()
        {
            var iq = new Iq(IqType.get, new XElement(XNames.roster_query))
            {
                From = this.runtimeParameters["jid"]
            };

//            await this.textWriter.WriteAndFlushAsync(iq);

//            var iqResp = await this.ReadIqStanzaAsync();
            var iqResp = await this.xmppServerStream.WriteIqAndReadReponseAsync(iq);


            Expect("result", iqResp.Attribute("type")?.Value, iqResp);
            Expect(iq.Id, iqResp.Attribute("id")?.Value, iqResp);

            var ver = iqResp.Element(XNames.roster_query).Attribute("ver");
            if (iqResp.Element(XNames.roster_query).IsEmpty)
                return new RosterItem[0];

            var rosterItems = new List<RosterItem>();
            foreach (var item in iqResp.Elements(XNames.roster_query).Elements(XNames.roster_item))
            {
                rosterItems.Add(new RosterItem
                {
                    Jid = item.Attribute("jid").Value,
                    Name = item.Attribute("name").Value,
                    Groups = item.Elements(XNames.roster_group)?.Select(xe => xe.Value)
                });
            }

            this.currentRosterItems = rosterItems;
            return rosterItems;
        }

        public async Task<bool> AddRosterItemAsync(string bareJid, string name, string group)
        {
            var iq = new Iq(IqType.set,
                new XElement(XNames.roster_query,
                    new XElement(XNames.roster_item, new XAttribute("jid", bareJid), new XAttribute("name", name),
                        string.IsNullOrEmpty(group) ? null : new XElement("group", group))));

//            await this.textWriter.WriteAndFlushAsync(iq);

//            var iqResp = await this.ReadIqStanzaAsync();
            var iqResp = await this.xmppServerStream.WriteIqAndReadReponseAsync(iq);

            if (iqResp.HasErrorType())
            {
                Log.Logger.Error($"Failed to add roster item: {iqResp}");
                return false;
            }

            Expect("result", iqResp.Attribute("type")?.Value, iqResp);
            Expect(iq.Id, iqResp.Attribute("id")?.Value, iqResp);

            return true;
        }

        public async Task<bool> DeleteRosterItemAsync(string bareJid)
        {
            var iq = new Iq(IqType.set,
                new XElement(XNames.roster_query,
                    new XElement(XNames.roster_item, new XAttribute("jid", bareJid),
                        new XAttribute("subscription", "remove"))));

            var iqResp = await this.xmppServerStream.WriteIqAndReadReponseAsync(iq);

            if (iqResp.HasErrorType())
            {
                Log.Logger.Error($"Failed to delete roster item: {iqResp}");
                return false;
            }

            Expect("result", iqResp.Attribute("type")?.Value, iqResp);
            //Expect(iq.Id, iqResp.Attribute("id")?.Value, iqResp);

            return true;
        }

        public void IqReceived(XElement iqElem)
        {
            Log.Logger.Verbose($"ImProtocolHandler handles iq sent by server: " + iqElem);


            if (iqElem.HasAttribute("from") &&
                iqElem.Attribute("from").Value != this.runtimeParameters["jid"].ToBareJid())
            {
                // 2.1.6.: A receiving client MUST ignore the stanza unless it has no 'from'
                // attribute(i.e., implicitly from the bare JID of the user's
                // account) or it has a 'from' attribute whose value matches the
                // user's bare JID <user@domainpart>.
                return;
            }

            if (iqElem.FirstNode is XElement queryElem && queryElem.Name == XNames.roster_query)
            {
                var itemElem = queryElem.Element(XNames.roster_item);
                if (itemElem.Attribute("subscription")?.Value == "remove")
                {
                    this.currentRosterItems.RemoveAll(ri => ri.Jid == itemElem.Attribute())
                }
                else
                {
                    //update
                }

                //UNDONE reply to server (2.1.6.  Roster Push)
            }

        }
    }
}