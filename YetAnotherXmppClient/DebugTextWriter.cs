using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherXmppClient
{
    public class DebugTextWriterDecorator : TextWriter
    {
        private StringWriter debugWriter;
        private readonly Action<string> onFlushedAction;

        public TextWriter Decoratee { get; }

        public override Encoding Encoding { get; }

        public DebugTextWriterDecorator(TextWriter decoratee, Action<string> onFlushedAction)
        {
            this.Decoratee = decoratee;
            this.onFlushedAction = onFlushedAction;
            this.debugWriter = new StringWriter();
        }

        public override void Close()
        {
            this.Decoratee.Close();
        }

        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            await this.Decoratee.WriteAsync(buffer, index, count);
            await this.debugWriter.WriteAsync(buffer, index, count);
        }

        public override async Task WriteAsync(string value)
        {
            await this.Decoratee.WriteAsync(value);
            await this.debugWriter.WriteAsync(value);
        }

        public override async Task WriteAsync(char value)
        {
            await this.Decoratee.WriteAsync(value);
            await this.debugWriter.WriteAsync(value);
        }

        public override void Write(char value)
        {
            this.Decoratee.Write(value);
            this.debugWriter.Write(value);
        }

        public override void Write(char[] buffer)
        {
            this.Decoratee.Write(buffer);
            this.debugWriter.Write(buffer);
        }

        public override void Write(string value)
        {
            this.Decoratee.Write(value);
            this.debugWriter.Write(value);
        }

        public override void Flush()
        {
            this.Decoratee.Flush();
            this.RaiseOnFlushed();
        }

        public override async Task FlushAsync()
        {
            await this.Decoratee.FlushAsync();
            //await this.debugWriter.FlushAsync();
            this.RaiseOnFlushed();
        }

        public override async Task WriteLineAsync(string value)
        {
            await this.Decoratee.WriteLineAsync(value);
            await this.debugWriter.WriteLineAsync(value);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.RaiseOnFlushed();
        }

        private void RaiseOnFlushed()
        {
            if (!string.IsNullOrEmpty(debugWriter.ToString()))
            {
                this.onFlushedAction?.Invoke(this.debugWriter.ToString());
                debugWriter = new StringWriter();
            }

        }
    }
}