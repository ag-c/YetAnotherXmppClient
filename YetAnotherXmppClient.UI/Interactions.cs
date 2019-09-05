using System.Reactive;
using ReactiveUI;
using YetAnotherXmppClient.Protocol.Handler;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI
{
    static class Interactions
    {
        public static Interaction<Unit, LoginCredentials> Login { get; } = new Interaction<Unit, LoginCredentials>();
        public static Interaction<string, bool> SubscriptionRequest { get; } = new Interaction<string, bool>();
        public static Interaction<Unit, RosterItemInfo> AddRosterItem { get; } = new Interaction<Unit, RosterItemInfo>();
        public static Interaction<ServiceDiscoveryProtocolHandler, Unit> ShowServiceDiscovery { get; } = new Interaction<ServiceDiscoveryProtocolHandler, Unit>();

    }
}
