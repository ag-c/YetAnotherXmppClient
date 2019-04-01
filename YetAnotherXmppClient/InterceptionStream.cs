using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;

namespace YetAnotherXmppClient
{
    public class WriteInterceptionStream : Stream
    {
        private MemoryStream debugStream = new MemoryStream();
        private Stream decoratee;

        public WriteInterceptionStream(Stream decoratee)
        {
            //if (decoratee == null) throw new ArgumentNullException("decoratee");
            //if (debugStream == null) throw new ArgumentNullException("debugStream");

            //if (!debugStream.CanWrite)
            //{
            //    throw new ArgumentException("debugStream is not writable");
            //}

            decoratee = decoratee;
            debugStream = debugStream;
        }

        public override void Flush()
        {
            decoratee.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return this.decoratee.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return decoratee.Seek(offset, origin);
        }
        
        public override void SetLength(long value)
        {
            decoratee.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return decoratee.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return decoratee.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            decoratee.Write(buffer, offset, count);
            debugStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await decoratee.WriteAsync(buffer, offset, count, cancellationToken);
            await debugStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override bool CanRead => decoratee.CanRead;

        public override bool CanSeek => decoratee.CanSeek;

        public override bool CanWrite => decoratee.CanWrite;

        public override long Length => decoratee.Length;

        public override long Position
        {
            get => decoratee.Position;
            set => decoratee.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            decoratee.Dispose();
            var str = Encoding.UTF8.GetString(this.debugStream.ToArray());
            Log.Verbose($"Written to Stream: {str}");
        }
    }
}
