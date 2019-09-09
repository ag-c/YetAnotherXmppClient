using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class BlockingViewModel : ReactiveObject
    {
        private readonly BlockingProtocolHandler handler;

        public ReactiveCommand<Unit, Unit> BlockCommand { get; }
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

        public BlockingViewModel(BlockingProtocolHandler handler)
        {
            this.handler = handler;
            this.BlockCommand = ReactiveCommand.CreateFromTask(this.OnBlockAsync);
            this.UnblockAllCommand = ReactiveCommand.CreateFromTask(this.OnUnblockAllAsync);

            handler.RetrieveBlockListAsync().ContinueWith(task => this.BlockedJids = task.Result);
        }

        private async Task OnBlockAsync()
        {
            var success = await this.handler.BlockAsync(this.BareJid);
            if (success)
                this.BareJid = string.Empty;

            this.BlockedJids = await this.handler.RetrieveBlockListAsync();
        }

        private async Task OnUnblockAllAsync()
        {
            var success = await this.handler.UnblockAllAsync();

            this.BlockedJids = await this.handler.RetrieveBlockListAsync();
        }
    }
}
