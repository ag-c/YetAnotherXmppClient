using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
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
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
