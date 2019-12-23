using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class BlockingViewModel : ReactiveObject
    {
        private readonly IMediator mediator;
        public ReactiveCommand<Unit, Unit> BlockCommand { get; }
        public ReactiveCommand<Unit, Unit> UnblockCommand { get; }
        public ReactiveCommand<Unit, Unit> UnblockAllCommand { get; }

        private string bareJid;
        public string BareJid
        {
            get => this.bareJid;
            set => this.RaiseAndSetIfChanged(ref this.bareJid, value);
        }

        private IEnumerable<string> blockedJids;
        public IEnumerable<string> BlockedJids
        {
            get => this.blockedJids;
            set => this.RaiseAndSetIfChanged(ref this.blockedJids, value);
        }

        public string SelectedBlockedJid { get; set; }

        public BlockingViewModel(IMediator mediator)
        {
            this.mediator = mediator;
            this.BlockCommand = ReactiveCommand.CreateFromTask(this.OnBlockAsync);
            this.UnblockCommand = ReactiveCommand.CreateFromTask(this.OnUnblockAsync);
            this.UnblockAllCommand = ReactiveCommand.CreateFromTask(this.OnUnblockAllAsync);

            this.mediator.QueryAsync<RetrieveBlockListQuery, IEnumerable<string>>(new RetrieveBlockListQuery()).ContinueWith(t => this.BlockedJids = t.Result);
        }

        private async Task OnBlockAsync()
        {
            var success = await this.mediator.QueryAsync<BlockQuery, bool>(new BlockQuery { BareJid = this.BareJid });
            if (success)
                this.BareJid = string.Empty;

            this.BlockedJids = await this.mediator.QueryAsync<RetrieveBlockListQuery, IEnumerable<string>>(new RetrieveBlockListQuery());
        }

        private async Task OnUnblockAsync()
        {
            if (this.SelectedBlockedJid == null)
                return;

            var success = await this.mediator.QueryAsync<UnblockQuery, bool>(new UnblockQuery { BareJid = this.SelectedBlockedJid }).ConfigureAwait(false);

            this.BlockedJids = await this.mediator.QueryAsync<RetrieveBlockListQuery, IEnumerable<string>>(new RetrieveBlockListQuery()).ConfigureAwait(false);
        }

        private async Task OnUnblockAllAsync()
        {
            var success = await this.mediator.QueryAsync<UnblockAllQuery, bool>(new UnblockAllQuery()).ConfigureAwait(false);

            this.BlockedJids = await this.mediator.QueryAsync<RetrieveBlockListQuery, IEnumerable<string>>(new RetrieveBlockListQuery()).ConfigureAwait(false);
        }
    }
}
