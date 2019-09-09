using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.Tests
{
    public class StanzaPartsTest
    {
        [Fact]
        public void Bind()
        {
            var iq = Iq.FromXElement(XElement.Parse(@"<iq id='wy2xa82b4' type='result'>
                                                       <bind xmlns='urn:ietf:params:xml:ns:xmpp-bind'>
                                                         <jid>juliet@im.example.com/balcony</jid>
                                                       </bind>
                                                      </iq>"));
            var bind = iq.GetContent<Bind>();

            bind.Jid.Should().Be("juliet@im.example.com/balcony");
        }

        [Fact]
        public void RosterQuery()
        {
            var iq = Iq.FromXElement(XElement.Parse(@"<iq id='hu2bac18'
                                                          to='juliet@example.com/balcony'
                                                          type='result'>
                                                        <query xmlns='jabber:iq:roster' ver='ver11'>
                                                          <item jid='romeo@example.net'
                                                                name='Romeo'
                                                                subscription='both'>
                                                            <group>Friends</group>
                                                          </item>
                                                          <item jid='mercutio@example.com'
                                                                name='Mercutio'
                                                                subscription='from'/>
                                                        </query>
                                                      </iq>"));
            //var rosterQuery = new RosterQuery(iq.Elements().First());
            var rosterQuery = iq.GetContent<RosterQuery>();

            rosterQuery.Ver.Should().Be("ver11");
            rosterQuery.Items.Should().HaveCount(2);
            rosterQuery.Items.Should().BeEquivalentTo(new
                                                          {
                                                              Jid = "romeo@example.net",
                                                              ItemName = "Romeo",
                                                              Subscription = SubscriptionState.both,
                                                              Groups = new[] { "Friends" }
                                                          }, new
                                                                 {
                                                                     Jid = "mercutio@example.com",
                                                                     ItemName = "Mercutio",
                                                                     Subscription = SubscriptionState.from,
                                                                 });
        }

        [Fact]
        public void Blocklist()
        {
            var iq = Iq.FromXElement(XElement.Parse(@"<iq type='result' id='blocklist1'>
                                                        <blocklist xmlns='urn:xmpp:blocking'>
                                                          <item jid='romeo@montague.net'/>
                                                          <item jid='iago@shakespeare.lit'/>
                                                        </blocklist>
                                                      </iq>"));
            var blocklist = iq.GetContent<Blocklist>();

            blocklist.Jids.Should().BeEquivalentTo("romeo@montague.net", "iago@shakespeare.lit");
        }

        [Fact]
        public void EmptyBlocklist()
        {
            var iq = Iq.FromXElement(XElement.Parse(@"<iq type='result' id='blocklist1'>
                                                        <blocklist xmlns='urn:xmpp:blocking'/>
                                                      </iq>"));
            var blocklist = iq.GetContent<Blocklist>();

            blocklist.Jids.Should().BeEmpty();
        }
    }
}
