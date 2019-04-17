using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData.Binding;
using ReactiveUI;
using StarDebris.Avalonia.MessageBox;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class MainWindow : ReactiveWindow<MainViewModel>
    {
        public Button LoginButton => this.FindControl<Button>("loginButton");
        public Button StartChatButton => this.FindControl<Button>("startChatButton");

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated(
                d =>
                {
                    d(this.BindCommand(this.ViewModel, x => x.LoginCommand, x => x.LoginButton));
                    d(this.BindCommand(this.ViewModel, x => x.StartChatCommand, x => x.StartChatButton));
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

                    var logScrollViewer = this.FindControl<ScrollViewer>("logScrollViewer");
                    cmd = new ActionCommand(async obj => await Dispatcher.UIThread.InvokeAsync(() => logScrollViewer.ScrollToEnd()));
                    this.ViewModel.WhenPropertyChanged(x => x.LogText).InvokeCommand(cmd);
                });
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
