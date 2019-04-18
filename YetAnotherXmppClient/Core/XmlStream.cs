using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Nito.AsyncEx;
using Serilog;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Core
{
    public class XmlStream : IDisposable
    {
        private XmlReader xmlReader;
        protected TextWriter textWriter;
        private readonly AsyncLock readerLock = new AsyncLock();
        private readonly AsyncLock writerLock = new AsyncLock();

        private readonly List<(Func<XElement, bool> MatchFunc, Action<XElement> Callback, bool IsOneTime)> matchingCallbacks = new List<(Func<XElement, bool> MatchFunc, Action<XElement> Callback, bool IsOneTime)>();

        public Stream UnderlyingStream { get; private set; }


        public XmlStream(Stream serverStream)
        {
            this.Reinitialize(serverStream);
        }

        public void Reinitialize(Stream stream)
        {
            this.UnderlyingStream = stream;
            this.xmlReader = this.CreateReader(stream);
            this.textWriter = this.CreateWriter(stream);
        }

        public void Reset()
        {
            this.Reinitialize(this.UnderlyingStream);
        }

        protected virtual XmlReader CreateReader(Stream stream)
        {
            return XmlReader.Create(stream);
        }

        protected virtual TextWriter CreateWriter(Stream stream)
        {
            return new StreamWriter(stream);
        }

        public void RegisterElementCallback(Func<XElement, bool> matchFunc, Action<XElement> callback, bool oneTime = false)
        {
            this.matchingCallbacks.Add((matchFunc, callback, oneTime));
        }

        protected async Task<XElement> ReadUntilElementMatchesAsync(Func<XElement, bool> matchFunc)
        {
            if (this.isLoopRunning)
            {   // let the read loop receive the response
                var tcs = new TaskCompletionSource<XElement>(TaskCreationOptions.RunContinuationsAsynchronously);

                this.RegisterElementCallback(matchFunc, tcs.SetResult, oneTime: true);

                return await tcs.Task;
            }
            else
            {   // read elements until a match
                XElement xElem;
                do
                {
                    xElem = await this.InternalReadAndProcessElementAsync();
                }
                while (!matchFunc(xElem));

                return xElem;
            }
        }

        public async Task<(string Name, Dictionary<string, string> Attributes)> ReadOpeningTagAsync()
        {
            ThrowIfLoopRunning();

            Log.Debug("Reading xml opening tag..");

            await this.xmlReader.MoveToContentAsync();

            Expect(() => this.xmlReader.NodeType == XmlNodeType.Element);

            return (xmlReader.Name, await this.xmlReader.GetAllAttributesAsync());
        }

        public Task<XElement> ReadElementAsync()
        {
            ThrowIfLoopRunning();

            return this.InternalReadAndProcessElementAsync();
        }

        private async Task<XElement> InternalReadAndProcessElementAsync(CancellationToken ct = default)
        {
            using (await this.readerLock.LockAsync())
            {
                var xElem = await this.xmlReader.ReadNextElementAsync(ct);

                Log.Logger.XmppStreamContent($"Read from stream: {xElem}");

                this.ProcessInboundElement(xElem);

                return xElem;
            }
        }

        public Task WriteElementAsync(XElement elem)
        {
            return this.textWriter.WriteAndFlushAsync(elem.ToString());
        }

        public async Task WriteClosingTagAsync(string name)
        {
            using (await this.writerLock.LockAsync())
            {
                await this.textWriter.WriteAsync($"</ {name}>");
            }
        }

        private void ProcessInboundElement(XElement xElem)
        {
            var hasMatch = false;
            for (var i = this.matchingCallbacks.Count-1; i >= 0; i--)
            {
                var (matchFunc, callback, isOneTime) = this.matchingCallbacks[i];
                if (matchFunc(xElem))
                {
                    hasMatch = true;
                    callback(xElem);
                    if(isOneTime)
                        this.matchingCallbacks.RemoveAt(i);
                }
            }

            if (!hasMatch && this.isLoopRunning) //UNDONE loop egal?
            {
                Log.Information($"Processed element WITHOUT a handler (id={xElem.Attribute("id")?.Value})");
            }
        }

        private bool isLoopRunning;

        public async Task RunReadLoopAsync(CancellationToken ct)
        {
            this.isLoopRunning = true;
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                await this.InternalReadAndProcessElementAsync(ct);
            }
        }

        private void ThrowIfLoopRunning()
        {
            if (this.isLoopRunning)
                throw new InvalidOperationException("Cannot read explicitly, stream is in read loop");
        }

        public void Dispose()
        {
            this.UnderlyingStream.Dispose();
            this.xmlReader.Dispose();
            this.textWriter.Dispose();
        }
    }
}
