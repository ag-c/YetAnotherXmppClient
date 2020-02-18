namespace YetAnotherXmppClient.Infrastructure.Queries
{
    internal class EntitySupportsFeatureQuery : IQuery<bool>
    {
        /// <summary>
        /// If null, then the server is queried
        /// </summary>
        public string Jid { get; }
        public string ProtocolNamespace { get; }

        public EntitySupportsFeatureQuery(string jid, string protocolNamespace)
        {
            this.Jid = jid;
            this.ProtocolNamespace = protocolNamespace;
        }
    }
}
