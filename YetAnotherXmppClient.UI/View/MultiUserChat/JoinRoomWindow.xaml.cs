using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace YetAnotherXmppClient.UI.View.MultiUserChat
{
    public class JoinRoomWindow : ReactiveWindow<JoinRoomWindow>
    {
        public string RoomJid { get; set; }
        public string Nickname { get; set; }

        public ICommand JoinCommand { get; }
        public ICommand CancelCommand { get; }

        public JoinRoomWindow()
        {
            this.JoinCommand = ReactiveCommand.Create(() => this.Close((this.RoomJid, this.Nickname)));
            this.CancelCommand = ReactiveCommand.Create(() => this.Close(null));
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
