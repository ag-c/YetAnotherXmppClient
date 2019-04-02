using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace YetAnotherXmppClient.Tests
{
    public class DebugTextWriterTest
    {
        [Fact]
        public void WriteEmptyString()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriter(new StringWriter(), str => onFlushedArg = str);

            debugTextWriter.Write("");

            onFlushedArg.Should().BeNull();
        }

        [Fact]
        public void WriteWithoutFlush()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriter(new StringWriter(), str => onFlushedArg = str);

            debugTextWriter.Write("123");

            onFlushedArg.Should().BeNull();
        }

        [Fact]
        public void WriteAndFlush_MultipleTimes()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriter(new StringWriter(), str => onFlushedArg = str);

            debugTextWriter.Write("123");
            debugTextWriter.Flush();

            onFlushedArg.Should().Be("123");

            debugTextWriter.Write("345");
            debugTextWriter.Flush();

            onFlushedArg.Should().Be("345");
        }

        [Fact]
        public async Task WriteAndFlushAsync()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriter(new StringWriter(), str => onFlushedArg = str);

            await debugTextWriter.WriteAsync("123");
            await debugTextWriter.FlushAsync();

            onFlushedArg.Should().Be("123");
        }

        [Fact]
        public void DisposeShouldFlush()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriter(new StringWriter(), str => onFlushedArg = str);

            debugTextWriter.Write("123");
            debugTextWriter.Dispose();

            onFlushedArg.Should().Be("123");
        }
    }
}
