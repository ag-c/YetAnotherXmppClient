using System.Xml.XmlDiff;
using Xunit;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Protocol;
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

            Assert.NotNull(iq.Element("bind"));
            Assert.True(iq.Element("bind").IsEmpty);
        }

        [Fact]
        public void Bind_WithResource()
        {
            var iq = new Iq(IqType.set, new Bind("resource1"));
            var xml = iq.ToString();

            Assert.False(iq.Element("bind").IsEmpty);
            Assert.Equal("resource1", iq.Element("bind").Element("resource").Value);
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
    }
}
