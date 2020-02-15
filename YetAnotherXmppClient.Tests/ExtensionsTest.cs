using FluentAssertions;
using Xunit;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Tests
{
    public class ExtensionsTest
    {
        [Fact]
        public void String_ToBareJid()
        {
            "local@domain/resource".ToBareJid().Should().Be("local@domain");
            "local@domain".ToBareJid().Should().Be("local@domain");
        }

        [Fact]
        public void String_IsBareJid()
        {
            "local@domain/resource".IsBareJid().Should().BeFalse();
            "local@domain".IsBareJid().Should().BeTrue();
            "@".IsBareJid().Should().BeFalse();
            "@server".IsBareJid().Should().BeFalse();
            "local@".IsBareJid().Should().BeFalse();
        }
    }
}
