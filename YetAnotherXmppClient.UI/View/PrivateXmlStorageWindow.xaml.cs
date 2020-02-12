using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class PrivateXmlStorageWindow : ReactiveWindow<PrivateXmlStorageViewModel>
    {
        public PrivateXmlStorageWindow()
        {
        }

        public PrivateXmlStorageWindow(PrivateXmlStorageViewModel viewModel)
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
