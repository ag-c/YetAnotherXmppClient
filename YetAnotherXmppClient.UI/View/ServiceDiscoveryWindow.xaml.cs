using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class ServiceDiscoveryWindow : ReactiveWindow<ServiceDiscoveryViewModel>
    {
        public ServiceDiscoveryWindow(ServiceDiscoveryViewModel viewModel)
        {
            this.DataContext = viewModel;
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
