using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.Protocol.Handler.MultiUserChat
{
    public class RoomInfo : EntityInfo
    {
        internal RoomInfo(EntityInfo entityInfo)
        {
            this.Jid = entityInfo.Jid;
            this.Identities = entityInfo.Identities;
            this.Features = entityInfo.Features;
        }
    }
}