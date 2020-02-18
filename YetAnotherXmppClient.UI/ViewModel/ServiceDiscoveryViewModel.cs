using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class ServiceDiscoveryViewModel : ReactiveObject
    {
        private readonly IMediator mediator;

        public ObservableCollection<EntityInfo> RootEntityInfo { get; set; } = new ObservableCollection<EntityInfo>();

        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }


        public ServiceDiscoveryViewModel(IMediator mediator, string jid)
        {
            this.mediator = mediator;

            this.RefreshCommand = ReactiveCommand.CreateFromTask(() => this.RefreshAsync(jid));

            this.RefreshAsync(jid);
        }

        private async Task RefreshAsync(string jid)
        {
            var info = await this.mediator.QueryEntityInformationTreeAsync(jid);
            this.RootEntityInfo.Clear();
            this.RootEntityInfo.Add(info);
        }
    }
}
