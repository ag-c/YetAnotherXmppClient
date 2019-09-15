using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient.UI.ViewModel
{
    //public class EntityViewModel
    //{
    //    private ObservableCollection<EntityViewModel> Entities { get; set; } = new ObservableCollection<EntityViewModel>();
    //}

    public class ServiceDiscoveryViewModel : ReactiveObject
    {
        private readonly IMediator mediator;

        //private EntityInfo rootEntityInfo;
        //public EntityInfo RootEntityInfo
        //{
        //    get => this.rootEntityInfo;
        //    set => this.RaiseAndSetIfChanged(ref this.rootEntityInfo, value);
        //}
        public ObservableCollection<EntityInfo> RootEntityInfo { get; set; } = new ObservableCollection<EntityInfo>();

        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }


        public ServiceDiscoveryViewModel(IMediator mediator)
        {
            this.mediator = mediator;
            this.RefreshCommand = ReactiveCommand.CreateFromTask(this.OnRefreshAsync);
        }

        private async Task OnRefreshAsync()
        {
            var info = await this.mediator.QueryAsync<QueryEntityInformationTreeQuery, EntityInfo>(new QueryEntityInformationTreeQuery());
            this.RootEntityInfo.Clear();
            this.RootEntityInfo.Add(info);
        }
    }
}
