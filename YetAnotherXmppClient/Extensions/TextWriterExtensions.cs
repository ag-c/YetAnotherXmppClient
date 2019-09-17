using System.IO;
using System.Threading.Tasks;

namespace YetAnotherXmppClient.Extensions
{
    public static class TextWriterExtensions
    {
        public static async Task WriteAndFlushAsync(this TextWriter textWriter, string str)
        {
            await textWriter.WriteAsync(str).ConfigureAwait(false);
            await textWriter.FlushAsync().ConfigureAwait(false);
        }
    }
}
