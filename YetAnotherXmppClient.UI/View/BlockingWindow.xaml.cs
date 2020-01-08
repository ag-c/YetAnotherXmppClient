using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class BlockingWindow : ReactiveWindow<BlockingViewModel>
    {
        public Button BlockButton => this.FindControl<Button>("blockButton");
        public Button UnblockButton => this.FindControl<Button>("unblockButton");
        public Button UnblockAllButton => this.FindControl<Button>("unblockAllButton");

        public BlockingWindow()
        {
        }

        public BlockingWindow(BlockingViewModel viewModel)
        {
            this.DataContext = viewModel;
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated(
                d =>
                    {
                        d(this.BindCommand(this.ViewModel, x => x.BlockCommand, x => x.BlockButton));
                        d(this.BindCommand(this.ViewModel, x => x.UnblockCommand, x => x.UnblockButton));
                        d(this.BindCommand(this.ViewModel, x => x.UnblockAllCommand, x => x.UnblockAllButton));
                    });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
