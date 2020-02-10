using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class MoodWindow : ReactiveWindow<MoodViewModel>
    {
        public MoodWindow()
        {
        }

        public MoodWindow(MoodViewModel viewModel)
        {
            this.DataContext = viewModel;
            viewModel.SubmitAction = (mood, text) => this.Close((mood, text));

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
