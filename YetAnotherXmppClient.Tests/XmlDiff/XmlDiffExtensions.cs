using System.IO;

namespace YetAnotherXmppClient.Tests.XmlDiff
{
    public static class XmlDiffExtensions
    {
        public static bool Compare(this System.Xml.XmlDiff.XmlDiff xmlDiff, string source, string target)
        {
            var sourceStream = new MemoryStream();
            var sourceWriter = new StreamWriter(sourceStream);
            sourceWriter.Write(source);
            sourceWriter.Flush();
            sourceStream.Seek(0, SeekOrigin.Begin);

            var targetStream = new MemoryStream();
            var targetWriter = new StreamWriter(targetStream);
            targetWriter.Write(target);
            targetWriter.Flush();
            targetStream.Seek(0, SeekOrigin.Begin);

            return xmlDiff.Compare(sourceStream, targetStream);
        }
    }
}
