using System;
using System.IO;
using System.Net.Mail;
using System.Xml.Linq;
using Xunit;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Tests
{
    public class ProtocolHandlerTest
    {
        [Fact]
        public void Test1()
        {
            var x = XElement.Parse("</stream>");
            var serverStream = new MemoryStream();
            var handler = new MainProtocolHandler(serverStream, null);
            //handler.RunAsync(new Jid("user@server"),);

        }
    }
}
