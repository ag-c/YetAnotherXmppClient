using System.Reactive;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.UI.ViewModel.MultiUserChat;

namespace YetAnotherXmppClient.UI.View.MultiUserChat
{
    public class MultiUserChatWindow : ReactiveWindow<MultiUserChatViewModel>
    {
        public MultiUserChatWindow()
        {
        }

        public MultiUserChatWindow(MultiUserChatViewModel viewModel)
        {
            this.DataContext = viewModel;
            this.InitializeComponent();
            this.WhenActivated(d =>
                {
                    d(Interactions.JoinRoom.RegisterHandler(async interaction =>
                        {
                            var window = new JoinRoomWindow();
                            var (roomJid, nickname) = await window.ShowDialog<(string, string)>(this);
                            if(!roomJid.IsBareJid())
                            {
                                var msgBoxWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", "Expected room JID to be in format 'room@service'!", ButtonEnum.Ok);
                                await msgBoxWindow.Show();
                            }
                            else if (string.IsNullOrWhiteSpace(nickname))
                            {
                                var msgBoxWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error", "Invalid nickname provided!", ButtonEnum.Ok);
                                await msgBoxWindow.Show();
                            }
                            interaction.SetOutput((roomJid, nickname));
                        }));
                });
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
