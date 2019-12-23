using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace YetAnotherXmppClient.Tests
{
    public class DebugTextWriterDecoratorTest
    {
        [Fact]
        public void WriteEmptyString()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriterDecorator(new StringWriter(), str => onFlushedArg = str);

            debugTextWriter.Write("");

            onFlushedArg.Should().BeNull();
        }

        [Fact]
        public void WriteWithoutFlush()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriterDecorator(new StringWriter(), str => onFlushedArg = str);

            debugTextWriter.Write("123");

            onFlushedArg.Should().BeNull();
        }

        [Fact]
        public void WriteAndFlush_MultipleTimes()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriterDecorator(new StringWriter(), str => onFlushedArg = str);

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
            var debugTextWriter = new DebugTextWriterDecorator(new StringWriter(), str => onFlushedArg = str);

            await debugTextWriter.WriteAsync("123").ConfigureAwait(false);
            await debugTextWriter.FlushAsync().ConfigureAwait(false);

            onFlushedArg.Should().Be("123");
        }

        [Fact]
        public void DisposeShouldFlush()
        {
            string onFlushedArg = null;
            var debugTextWriter = new DebugTextWriterDecorator(new StringWriter(), str => onFlushedArg = str);

            debugTextWriter.Write("123");
            debugTextWriter.Dispose();

            onFlushedArg.Should().Be("123");
        }
    }
}
