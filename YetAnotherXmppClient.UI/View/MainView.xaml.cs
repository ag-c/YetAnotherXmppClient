using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
using StarDebris.Avalonia.MessageBox;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class MainView : ReactiveUserControl<MainViewModel>
    {
        public Button LogoutButton => this.FindControl<Button>("logoutButton");
        public Button ServiceDiscoveryButton => this.FindControl<Button>("serviceDiscoveryButton");
        public Button BlockingButton => this.FindControl<Button>("blockingButton");

        public MainView()
        {
            this.InitializeComponent();
            this.WhenActivated(
                d =>
                {
                    d(this.BindCommand(this.ViewModel, x => x.LogoutCommand, x => x.LogoutButton));
                    d(this.BindCommand(this.ViewModel, x => x.ShowServiceDiscoveryCommand, x => x.ServiceDiscoveryButton));
                    d(this.BindCommand(this.ViewModel, x => x.ShowBlockingCommand, x => x.BlockingButton));

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
                        .ShowServiceDiscovery
                        .RegisterHandler(
                            async interaction =>
                            {
                                var window = new ServiceDiscoveryWindow(new ServiceDiscoveryViewModel(interaction.Input.Mediator, interaction.Input.Jid));
                                await window.ShowDialog(MainWindow.Instance);
                                interaction.SetOutput(Unit.Default);
                            }));
                    d(Interactions
                        .ShowBlocking
                        .RegisterHandler(
                            async interaction =>
                                {
                                    var window = new BlockingWindow(new BlockingViewModel(interaction.Input));
                                    await window.ShowDialog(MainWindow.Instance);
                                    interaction.SetOutput(Unit.Default);
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
