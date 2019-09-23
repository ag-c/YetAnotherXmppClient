using System.Reactive;
using ReactiveUI;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI
{
    static class Interactions
    {
        public static Interaction<Unit, LoginCredentials> Login { get; } = new Interaction<Unit, LoginCredentials>();
        public static Interaction<string, bool> SubscriptionRequest { get; } = new Interaction<string, bool>();
        public static Interaction<Unit, RosterItemInfo> AddRosterItem { get; } = new Interaction<Unit, RosterItemInfo>();
        public static Interaction<(IMediator Mediator, string Jid), Unit> ShowServiceDiscovery { get; } = new Interaction<(IMediator, string), Unit>();
        public static Interaction<IMediator, Unit> ShowBlocking { get; } = new Interaction<IMediator, Unit>();
    }
}
