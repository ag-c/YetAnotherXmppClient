using FluentAssertions;
using Xunit;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Tests
{
    public class ExtensionsTest
    {
        [Fact]
        public void ToBareJid()
        {
            "local@domain/resource".ToBareJid().Should().Be("local@domain");
            "local@domain".ToBareJid().Should().Be("local@domain");
        }
    }
}
