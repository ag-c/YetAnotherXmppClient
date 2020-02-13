using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
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
            this.WhenActivated(d =>
                {
                    d(Interactions.ShowStorePrivateXmlStorageResponse.RegisterHandler(async interaction =>
                        {
                            //var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                            await Dispatcher.UIThread.InvokeAsync(async () =>
                                {
                                    var window = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Reponse", interaction.Input, ButtonEnum.Ok);
                                    var result = await window.Show();
                                });
                            interaction.SetOutput(Unit.Default);
                        }));
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
