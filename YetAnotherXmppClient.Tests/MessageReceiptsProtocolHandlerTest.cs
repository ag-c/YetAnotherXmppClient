using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XmlDiff;
using Xunit;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol;
using YetAnotherXmppClient.Protocol.Handler;
using YetAnotherXmppClient.Tests.XmlDiff;

namespace YetAnotherXmppClient.Tests
{
    public class MessageReceiptsProtocolHandlerTest
    {
        [Fact]
        public async Task InboundMessage_ReceiptRequested()
        {
            var runtimeParameters = new Dictionary<string, string> {["jid"] = "kingrichard@royalty.england.lit/throne" };
            var ms = new NetStreamMock();
            var xmppStream = new XmppStream(ms);
            var handler = new MessageReceiptsProtocolHandler(xmppStream, runtimeParameters);

            // simulate receiving a message
            ms.AddInboundData(@"<message
                                    from='northumberland@shakespeare.lit/westminster'
                                    id='richard2-4.1.247'
                                    to='kingrichard@royalty.england.lit/throne'>
                                  <body>My lord, dispatch; read o'er these articles.</body>
                                  <request xmlns='urn:xmpp:receipts'/>
                                </message>");

            //xmppStream.StartAsyncReadLoop();
            xmppStream.RunReadLoopAsync(new CancellationTokenSource().Token);

            await Task.Delay(2000);

            // read the outbound message, written by MessageReceiptsProtocolHandler
            ms.OutboundStream.Seek(0, SeekOrigin.Begin);
            var xmlReader = XmlReader.Create(ms.OutboundStream, new XmlReaderSettings { Async = true, IgnoreWhitespace = true });
            var msgReceipt = (await xmlReader.ReadNextElementAsync()).ToString();

            var expectedMsgReceipt = @"<message
                                            from='kingrichard@royalty.england.lit/throne'
                                            id='richard2-4.1.247'
                                            to='northumberland@shakespeare.lit/westminster'>
                                          <received xmlns='urn:xmpp:receipts'/>
                                        </message>";

            var xmlDiff = new System.Xml.XmlDiff.XmlDiff();
            xmlDiff.Option = (XmlDiffOption)((int)XmlDiffOption.NormalizeNewline - 1);

            Assert.True(xmlDiff.Compare(msgReceipt, expectedMsgReceipt));
        }

        class NetStreamMock : Stream
        {
            public MemoryStream OutboundStream { get; } = new MemoryStream();
            public MemoryStream InboundStream { get; } = new MemoryStream();

            public override void Flush()
            {
                OutboundStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return OutboundStream.FlushAsync(cancellationToken);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return InboundStream.Read(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return InboundStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
            {
                return InboundStream.ReadAsync(buffer, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return InboundStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                var position = OutboundStream.Position;
                OutboundStream.Write(buffer, offset, count);
                OutboundStream.Seek(position, SeekOrigin.Begin);
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var position = OutboundStream.Position;
                await OutboundStream.WriteAsync(buffer, offset, count, cancellationToken);
                OutboundStream.Seek(position, SeekOrigin.Begin);
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
            {
                var position = OutboundStream.Position;
                await OutboundStream.WriteAsync(buffer, cancellationToken);
                OutboundStream.Seek(position, SeekOrigin.Begin);
            }

            public override bool CanRead => InboundStream.CanRead;
            public override bool CanSeek => InboundStream.CanSeek;
            public override bool CanWrite => OutboundStream.CanWrite;
            public override long Length => InboundStream.Length;
            public override long Position
            {
                get => InboundStream.Position;
                set => InboundStream.Position = value;
            }

            public void AddInboundData(string s)
            {
                var position = InboundStream.Position;
                var buffer = Encoding.UTF8.GetBytes(s);
                InboundStream.Write(buffer, 0, buffer.Length);
                InboundStream.Seek(position, SeekOrigin.Begin);
            }
        }
    }
}
