using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
using StarDebris.Avalonia.MessageBox;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class MainWindow : ReactiveWindow<MainViewModel>
    {
        public Button LoginButton => this.FindControl<Button>("loginButton");
        public Button LogoutButton => this.FindControl<Button>("logoutButton");
        public ScrollViewer LogScrollViewer => this.FindControl<ScrollViewer>("logScrollViewer");

        public MainWindow()
        {
            this.WhenActivated(
                d =>
                {
                    d(this.BindCommand(this.ViewModel, x => x.LoginCommand, x => x.LoginButton));
                    d(this.BindCommand(this.ViewModel, x => x.LogoutCommand, x => x.LogoutButton));
                    d(this
                        .ViewModel
                        .LoginInteraction
                        .RegisterHandler(
                            async interaction =>
                            {
                                var window = new LoginWindow();
                                var credentials = await window.ShowDialog<LoginCredentials>(this);

                                interaction.SetOutput(credentials);
                            }));
                    d(this
                        .ViewModel
                        .SubscriptionRequestInteraction
                        .RegisterHandler(
                            async interaction =>
                            {
                                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                    new MessageBox($"Allow {interaction.Input} to see your status?",
                                        (dialogResult, e) => { tcs.SetResult(dialogResult.result == MessageBoxButtons.Yes); },
                                        MessageBoxStyle.Info, MessageBoxButtons.Yes | MessageBoxButtons.No).Show()
                                );
                                interaction.SetOutput(await tcs.Task);
                            }));
                    d(this
                        .ViewModel
                        .AddRosterItemInteraction
                        .RegisterHandler(
                            async interaction =>
                            {
                                var window = new AddRosterItemWindow();
                                var rosterItemInfo = await window.ShowDialog<RosterItemInfo>(this);

                                interaction.SetOutput(rosterItemInfo);
                            }));

                    
                    //cmd = new ActionCommand(async obj => await Dispatcher.UIThread.InvokeAsync(() => this.LogScrollViewer.ScrollToEnd()));
                    //this.ViewModel.WhenPropertyChanged(x => x.LogText)
                    this.ViewModel.WhenAnyValue(vm => vm.LogText)
                        .Subscribe(async _ => await Dispatcher.UIThread.InvokeAsync(() => this.LogScrollViewer.ScrollToEnd()));
                });
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private ICommand cmd;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    static class ScrollViewerExtensions
    {
        public static void ScrollToEnd(this ScrollViewer scrollViewer)
        {
            scrollViewer.Offset = new Vector(0, scrollViewer.Extent.Height);
        }
    }
}
