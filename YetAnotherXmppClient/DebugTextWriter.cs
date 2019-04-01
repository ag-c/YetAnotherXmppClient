using System.IO;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace YetAnotherXmppClient
{
    public class DebugTextWriter : TextWriter
    {
        private StringWriter debugWriter;
        private readonly TextWriter decoratee;
        public override Encoding Encoding { get; }

        public DebugTextWriter(TextWriter decoratee)
        {
            this.decoratee = decoratee;
            this.debugWriter = new StringWriter();
        }

        public override void Close()
        {
            this.decoratee.Close();
        }

        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            await this.decoratee.WriteAsync(buffer, index, count);
            await this.debugWriter.WriteAsync(buffer, index, count);
        }

        public override async Task WriteAsync(string value)
        {
            await this.decoratee.WriteAsync(value);
            await this.debugWriter.WriteAsync(value);
        }

        public override async Task WriteAsync(char value)
        {
            await this.decoratee.WriteAsync(value);
            await this.debugWriter.WriteAsync(value);
        }

        public override void Write(char value)
        {
            this.decoratee.Write(value);
            this.debugWriter.Write(value);
        }

        public override void Write(char[] buffer)
        {
            this.decoratee.Write(buffer);
            this.debugWriter.Write(buffer);
        }

        public override void Write(string value)
        {
            this.decoratee.Write(value);
            this.debugWriter.Write(value);
        }

        public override void Flush()
        {
            this.decoratee.Flush();
            this.PrintAndReset();
        }

        public override async Task FlushAsync()
        {
            await this.decoratee.FlushAsync();
            //await this.debugWriter.FlushAsync();
            this.PrintAndReset();
        }

        public override async Task WriteLineAsync(string value)
        {
            await this.decoratee.WriteLineAsync(value);
            await this.debugWriter.WriteLineAsync(value);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.PrintAndReset();
        }

        private void PrintAndReset()
        {
            if(!string.IsNullOrEmpty(debugWriter.ToString()))
                Log.Verbose($"Written: {this.debugWriter}");
            debugWriter = new StringWriter();

        }
    }
}