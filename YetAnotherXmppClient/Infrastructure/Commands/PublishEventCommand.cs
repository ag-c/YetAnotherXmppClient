using System.Xml.Linq;

namespace YetAnotherXmppClient.Infrastructure.Commands
{
    public class PublishEventCommand : ICommand
    {
        public string Node { get; set; }

        public XElement Content { get; set; }
    }
}
