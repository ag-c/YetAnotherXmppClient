using System.Collections.Generic;

namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class AddRosterItemQuery : IQuery<bool>
    {
        public string BareJid { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Groups { get; set; }
    }
}
