using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Tests
{
    public class XmlStreamReaderTest
    {
        [Fact]
        public async Task ReadOpeningTagAsync_StreamOpening()
        {
            var stream = CreateStream(@"<?xml version='1.0'?>
                            <stream:stream
                            from='juliet@im.example.com'
                            to='im.example.com'
                            version='1.0'
                            xml:lang='en'
                            xmlns='jabber:client'
                            xmlns:stream='http://etherx.jabber.org/streams'>");
            var reader = new XmlStreamReader(stream);

            var segment = await reader.ReadOpeningTagAsync();

            Assert.True(segment.PartType == XmlPartType.OpeningTag);
            Assert.True(segment.Attributes.ContainsKey("from"));
            Assert.Equal(segment.Attributes.Count, 6);
            Assert.Equal("juliet@im.example.com", segment.Attributes["from"]);
        }

        [Fact]
        public async Task ReadElementOrClosingTagAsync_StreamClosing()
        {
            var stream = CreateStream(@"</stream:stream>");
            var reader = new XmlStreamReader(stream);

            var segment = await reader.ReadElementOrClosingTagAsync();

            Assert.True(segment.PartType == XmlPartType.ClosingTag);
        }

        [Fact]
        public async Task ReadElementOrClosingTagAsync_Features()
        {
            var content = @"<stream:features>
                            <starttls xmlns='urn:ietf:params:xml:ns:xmpp-tls'>
                            <required />
                            </starttls>
                        </stream:features>";
            var stream = CreateStream(content);
            var reader = new XmlStreamReader(stream);

            var segment = await reader.ReadElementOrClosingTagAsync();

            Assert.True(segment.PartType == XmlPartType.Element);
            Assert.True(segment.RawXml == content.Replace("\r", "").Replace("\n",""));
        }
        //
        [Fact]
        public async Task ReadElementOrClosingTagAsync_Proceed()
        {
            var stream = CreateStream(@"<proceed xmlns='urn:ietf:params:xml:ns:xmpp-tls'/>");
            var reader = new XmlStreamReader(stream);

            var segment = await reader.ReadElementOrClosingTagAsync();

            Assert.True(segment.PartType == XmlPartType.Element);
            Assert.True(segment.RawXml == "<proceed xmlns='urn:ietf:params:xml:ns:xmpp-tls'/>");
        }

        [Fact]
        public async Task ReadOpeningTagAsync()
        {
            
        }

        private static MemoryStream CreateStream(string str)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(str);
            writer.Flush();
            ms.Position = 0;
            return ms;
        }
    }
}
