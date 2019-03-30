using Xunit;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Protocol;

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
    }
}
