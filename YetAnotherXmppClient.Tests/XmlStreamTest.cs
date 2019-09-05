using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Tests
{
    public class XmlStreamTest
    {
        [Fact]
        public async Task ReadOpeningTagAsync()
        {
            var xml = "<root><a/><b/></root>";
            var xmlStream = new XmlStream(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

            var (name, attributes) = await xmlStream.ReadOpeningTagAsync();

            name.Should().Be("root");
            attributes.Should().BeEmpty();
        }

        [Fact]
        public async Task ReadOpeningTagAsync_WithAttributes()
        {
            var xml = "<root a1=\"v1\" a2=\"v2\"><a><b></root>";
            var xmlStream = new XmlStream(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

            var (name, attributes) = await xmlStream.ReadOpeningTagAsync();

            name.Should().Be("root");
            attributes.Should().Equal(new Dictionary<string, string>
                                          {
                                              ["a1"] = "v1",
                                              ["a2"] = "v2"
                                          });
        }


        [Fact]
        public async Task ElementCallbacks()
        {
            var xml = "<root><a/><b/></root>";
            var xmlStream = new XmlStream(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
            bool callbackACalled = false;
            bool callbackBCalled = false;
            xmlStream.RegisterElementCallback(xe => xe.Name=="a", _ => callbackACalled = true);
            xmlStream.RegisterElementCallback(xe => xe.Name=="b", _ => callbackBCalled = true);

            await xmlStream.ReadOpeningTagAsync();
            var xElem = await xmlStream.ReadElementAsync();

            xElem.Name.LocalName.Should().Be("a");
            callbackACalled.Should().BeTrue();

            xElem = await xmlStream.ReadElementAsync();

            xElem.Name.LocalName.Should().Be("b");
            callbackBCalled.Should().BeTrue();
        }
    }
}
