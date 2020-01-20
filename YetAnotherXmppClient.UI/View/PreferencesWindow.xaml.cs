using System.Windows.Input;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class PreferencesWindow : ReactiveWindow<PreferencesViewModel>
    {
        public PreferencesWindow()
        {
        }

        public PreferencesWindow(PreferencesViewModel viewModel)
        {
            this.DataContext = viewModel;
            viewModel.CloseAction = this.Close;

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
