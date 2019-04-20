using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ReactiveUI;
using StarDebris.Avalonia.MessageBox;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class MainView : ReactiveUserControl<MainViewModel>
    {
        public Button LogoutButton => this.FindControl<Button>("logoutButton");

        public MainView()
        {
            this.InitializeComponent();
            this.WhenActivated(
                d =>
                {
                    d(this.BindCommand(this.ViewModel, x => x.LogoutCommand, x => x.LogoutButton));
                    //d(Interactions
                    //    .Login
                    //    .RegisterHandler(
                    //        async interaction =>
                    //        {
                    //            var window = new LoginWindow();
                    //            var credentials = await window.ShowDialog<LoginCredentials>(this);

                    //            interaction.SetOutput(credentials);
                    //        }));
                    d(Interactions
                        .SubscriptionRequest
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
                    d(Interactions
                        .AddRosterItem
                        .RegisterHandler(
                            async interaction =>
                            {
                                var window = new AddRosterItemWindow();
                                var rosterItemInfo = await window.ShowDialog<RosterItemInfo>(MainWindow.Instance);

                                interaction.SetOutput(rosterItemInfo);
                            }));

                    //this.ViewModel.WhenAnyValue(vm => vm.LogText).Subscribe(_ => this.Image.InvalidateVisual());
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
