﻿namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class DeleteRosterItemQuery : IQuery<bool>
    {
        public string BareJid { get; set; }

        public DeleteRosterItemQuery(string bareJid)
        {
            this.BareJid = bareJid;
        }
    }
}
