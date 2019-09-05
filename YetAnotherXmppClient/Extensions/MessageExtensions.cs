using System.Xml.Linq;
using YetAnotherXmppClient.Core.Stanza;

namespace YetAnotherXmppClient.Extensions
{
    public static class MessageExtensions
    {
        public static Message CreateResponse(this Message msg, XElement content, string from = null)
        {
            return new Message(content)
                       {
                           From = from ?? msg.To,
                           To = msg.From,
                           Id = msg.Id
                       };
        }
    }
}
