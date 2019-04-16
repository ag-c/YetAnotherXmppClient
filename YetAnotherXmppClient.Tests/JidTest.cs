using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Xunit;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Tests
{
    public class JidTest
    {
        [Fact]
        public void FullJid()
        {
            var jid = new Jid("user@server/res");

            jid.Local.Should().Be("user");
            jid.Server.Should().Be("server");
            jid.Resource.Should().Be("res");
            jid.ToString().Should().Be("user@server/res");
        }

        [Fact]
        public void WithoutResource()
        {
            var jid = new Jid("user@server");

            jid.Local.Should().Be("user");
            jid.Server.Should().Be("server");
            jid.Resource.Should().BeNull();
            jid.ToString().Should().Be("user@server");
        }
    }
}
