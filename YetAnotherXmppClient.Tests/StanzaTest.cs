using System.Linq;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using FluentAssertions;
using Xunit;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Tests.XmlDiff;

namespace YetAnotherXmppClient.Tests
{
    public class StanzaTest
    {
        [Fact]
        public void Bind_NoResource()
        {
            var iq = new Iq(IqType.set, new Bind());
            var xml = iq.ToString();

            Assert.NotNull(iq.Element(XNames.bind_bind));
            Assert.True(iq.Element(XNames.bind_bind)?.IsEmpty);
        }

        [Fact]
        public void Bind_WithResource()
        {
            var iq = new Iq(IqType.set, new Bind("resource1"));
            var xml = iq.ToString();

            Assert.False(iq.Element(XNames.bind_bind).IsEmpty);
            Assert.Equal("resource1", iq.Element(XNames.bind_bind)?.Element(XNames.bind_resource)?.Value);
        }

        [Fact]
        public void RosterItem()
        {
            var expectedXml = @"<iq from='juliet@example.com/balcony'
                                      id='di43b2x9'
                                      type='set'>
                                    <query xmlns='jabber:iq:roster'>
                                      <item jid='user@server'
                                            name='contactname'>
                                        <group>group1</group>
                                        <group>group2</group>
                                      </item>
                                    </query>
                                  </iq>";

            var iq = new Iq(IqType.set, new RosterQuery("user@server", "contactname", new[] {"group1", "group2"}))
            {
                Id = "di43b2x9",
                From = "juliet@example.com/balcony"
            };
            var xml = iq.ToString();

            var xmlDiff = new System.Xml.XmlDiff.XmlDiff();
            xmlDiff.Option = (XmlDiffOption) ((int) XmlDiffOption.NormalizeNewline - 1);

            Assert.True(xmlDiff.Compare(xml, expectedXml));
        }

        [Fact]
        public void PubSubPublish()
        {
            var expectedXml = @"<iq from='juliet@capulet.lit/balcony' type='set' id='pub1'>
                                  <pubsub xmlns='http://jabber.org/protocol/pubsub'>
                                    <publish node='http://jabber.org/protocol/tune'>
                                      <item>
                                        test
                                      </item>
                                    </publish>
                                  </pubsub>
                                </iq>";

            var iq = new Iq(IqType.set, new PubSubPublish("http://jabber.org/protocol/tune", null, "test"))
                         {
                             Id = "pub1",
                             From = "juliet@capulet.lit/balcony"
                         };

            var xmlDiff = new System.Xml.XmlDiff.XmlDiff();
            xmlDiff.Option = (XmlDiffOption)((int)XmlDiffOption.NormalizeNewline - 1);

            Assert.True(xmlDiff.Compare(iq, expectedXml));
        }

        [Fact]
        public void Iq_FromXElement()
        {
            var iqXml = @"<iq type='set'
                            from='francisco@denmark.lit/barracks'
                            to='pubsub.shakespeare.lit'
                            id='sub1'>
                          <test/>
                        </iq>";
            var iq = Iq.FromXElement(XElement.Parse(iqXml));

            Assert.Equal("francisco@denmark.lit/barracks", iq.From);
            Assert.Equal("pubsub.shakespeare.lit", iq.To);
            Assert.Equal("sub1", iq.Id);
            Assert.Equal(IqType.set, iq.Type);
            Assert.Equal("<test />", iq.Elements().Single().ToString());
        }

        [Fact]
        public void Message_FromXElement()
        {
            var xml = @"<message
                           from='juliet@example.com/balcony'
                           to='romeo@example.net'
                           type='chat'
                           xml:lang='en'>
                         <body>My ears have not yet drunk a hundred words</body>
                         <thread>e0ffe42b28561960c6b12b944a092794b9683a38</thread>
                       </message>";

            var message = Message.FromXElement(XElement.Parse(xml));
            message.From.Should().Be("juliet@example.com/balcony");
            message.To.Should().Be("romeo@example.net");
            message.Type.Should().Be(MessageType.chat);
            message.Body.Should().Be("My ears have not yet drunk a hundred words");
            message.Thread.Should().Be("e0ffe42b28561960c6b12b944a092794b9683a38");
        }

        [Fact]
        public void Message_CloneAndAddElement()
        {
            var message = new Message
                              {
                                  From = "juliet@example.com/balcony",
                                  To = "romeo@example.net",
                                  Type = MessageType.chat,
                              };

            message = message.CloneAndAddElement(new XElement("test", "active"));
            message.Element("test").Should().NotBeNull();
            message.Element("test").Value.Should().Be("active");
        }

        [Fact]
        public void Presence_FromXElement()
        {
            var xml = @"<presence
                           from='juliet@example.com/balcony'
                           to='romeo@example.net'
                           xml:lang='en'>
                         <show>away</show>
                         <status>be right back</status>
                         <priority>5</priority>
                       </presence>";

            var presence = Presence.FromXElement(XElement.Parse(xml));
            presence.From.Should().Be("juliet@example.com/balcony");
            presence.To.Should().Be("romeo@example.net");
            presence.Type.Should().BeNull();
            presence.Show.Should().Be(PresenceShow.away);
            presence.Stati.Single().Should().Be("be right back");
            presence.Priority.Should().Be(5);

        }
    }
}
