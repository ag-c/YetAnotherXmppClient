using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class ServiceDiscoveryWindow : ReactiveWindow<ServiceDiscoveryViewModel>
    {
        public Button RefreshButton => this.FindControl<Button>("refreshButton");

        public ServiceDiscoveryWindow(ServiceDiscoveryViewModel viewModel)
        {
            this.DataContext = viewModel;
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated(
                d =>
                {
                    d(this.BindCommand(this.ViewModel, x => x.RefreshCommand, x => x.RefreshButton));
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
