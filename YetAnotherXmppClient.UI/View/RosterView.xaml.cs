using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ReactiveUI;
using StarDebris.Avalonia.MessageBox;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol.Handler;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class RosterView : ReactiveUserControl<RosterViewModel>
    {
        public Image Image => this.FindControl<Image>("image");

        public RosterView()
        {
            this.InitializeComponent();
            this.WhenActivated(d =>
                {
                    //this.ViewModel.WhenAnyValue(vm => vm.RosterItems.).Subscribe(_ => this.Image.InvalidateVisual());
                    var timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1);
                    timer.Tick += (sender, args) =>
                        {
                            if (this.Image != null)
                            {
                                this.Image.InvalidateVisual();
                            }
                        };
                    timer.Start();

                    d(Interactions
                        .AddRosterItem
                        .RegisterHandler(
                            async interaction =>
                                {
                                    var window = new AddRosterItemWindow();
                                    var rosterItemInfo = await window.ShowDialog<RosterItemInfo>(MainWindow.Instance);

                                    interaction.SetOutput(rosterItemInfo);
                                }));
                    d(Interactions
                        .ShowLastActivity
                        .RegisterHandler(
                            async interaction =>
                                {
                                    var lastActivityInfo = await interaction.Input.Mediator.QueryAsync<LastActivityQuery, LastActivityInfo>(new LastActivityQuery
                                                                                                                                          {
                                                                                                                                              Jid = interaction.Input.Jid
                                                                                                                                          });
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                        new MessageBox($"Last activity was {lastActivityInfo.Seconds} seconds ({TimeSpan.FromSeconds(lastActivityInfo.Seconds)}) ago.",
                                            MessageBoxStyle.Info, MessageBoxButtons.Ok).Show()
                                    );
                                }));
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
