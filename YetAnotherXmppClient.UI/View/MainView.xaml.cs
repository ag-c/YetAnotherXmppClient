using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using Microsoft.EntityFrameworkCore.Internal;
using ReactiveUI;
using YetAnotherXmppClient.Protocol.Handler;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class MainView : ReactiveUserControl<MainViewModel>
    {
        public TabControl ChatSessionsTabControl => this.FindControl<TabControl>("chatSessionsTabControl");

        public MainView()
        {
            this.InitializeComponent();
            this.WhenActivated(
                d =>
                {
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
                        .RegisterHandlerAndNotify(
                            async interaction =>
                            {
                                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                                await Dispatcher.UIThread.InvokeAsync(async () =>
                                    {
                                        var window = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Request", $"Allow {interaction.Input} to see your status?", ButtonEnum.YesNo);
                                        var result = await window.Show();
                                        tcs.SetResult(result == ButtonResult.Yes);
                                    });
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
                    d(Interactions
                        .ShowPrivateXmlStorage
                        .RegisterHandler(
                            async interaction =>
                                {
                                    var window = new PrivateXmlStorageWindow(new PrivateXmlStorageViewModel(interaction.Input));
                                    await window.ShowDialog(MainWindow.Instance);
                                    interaction.SetOutput(Unit.Default);
                                }));
                    d(Interactions
                        .ShowMood
                        .RegisterHandler(
                            async interaction =>
                                {
                                    var window = new MoodWindow(new MoodViewModel());
                                    var moodAndText = await window.ShowDialog<(Mood?,string)>(MainWindow.Instance);
                                    interaction.SetOutput(moodAndText);
                                }));
                    d(Interactions
                        .ShowPreferences
                        .RegisterHandler(
                            async interaction =>
                                {
                                    var window = new PreferencesWindow(new PreferencesViewModel(interaction.Input));
                                    await window.ShowDialog(MainWindow.Instance);
                                    interaction.SetOutput(Unit.Default);
                                }));

                    //this.ViewModel.WhenAnyValue(vm => vm.LogText).Subscribe(_ => this.Image.InvalidateVisual());

                    this.ChatSessionsTabControl.SelectionChanged += (sender, args) =>
                        {
                            if (args.AddedItems.Any())
                            {
                                this.ViewModel.HandleSessionActivation((ChatSessionViewModel)args.AddedItems[0], true);
                            }
                            if (args.RemovedItems.Any())
                            {
                                this.ViewModel.HandleSessionActivation((ChatSessionViewModel)args.RemovedItems[0], false);
                            }
                        };
                    this.GotFocus += this.HandleGotFocus;
                });
        }

        private void HandleGotFocus(object sender, GotFocusEventArgs e)
        {
            this.ViewModel.AttestActivity();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
