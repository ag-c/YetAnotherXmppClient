using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData.Binding;
using ReactiveUI;

namespace YetAnotherXmppClient.UI
{
    public class MainWindow : ReactiveWindow<MainViewModel>
    {
        public Button LoginButton => this.FindControl<Button>("loginButton");

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
                    var textBox = this.FindControl<TextBox>("logTextBox");

                    cmd = new ActionCommand(async obj => await Dispatcher.UIThread.InvokeAsync(() => ((ScrollViewer)textBox.Parent).Offset = new Vector(0, ((ScrollViewer)textBox.Parent).Extent.Height)));
                    this.ViewModel.WhenPropertyChanged(x => x.LogText).InvokeCommand(cmd);
                });
        }

        private ICommand cmd;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            //var textBox = this.FindControl<TextBox>("logTextBox");

            //cmd = new ActionCommand(obj => ((ScrollViewer)textBox.Parent).Offset = new Vector(0, ((ScrollViewer)textBox.Parent).Extent.Height));
            //this.ViewModel.LogText.ToObservable().InvokeCommand(cmd);
            //textBox.WhenPropertyChanged(x => x.Text)
            //    .InvokeCommand(cmd);
            //textBox.GetObservable(TextBox.TextProperty).InvokeCommand(cmd);
            //textBox

            //var timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (sender, args) => ((ScrollViewer)textBox.Parent).Offset = new Vector(0, ((ScrollViewer)textBox.Parent).Extent.Height));
            //timer.Start();

            //var button = this.FindControl<Button>("loginButton");
            //button.Click += ButtonOnClick;
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var window = new AddRosterItemWindow();
            window.ShowDialog(this);
        }
    }
}
