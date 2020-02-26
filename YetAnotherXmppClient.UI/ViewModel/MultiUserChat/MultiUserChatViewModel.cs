using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace YetAnotherXmppClient.UI.ViewModel.MultiUserChat
{
    public class MultiUserChatViewModel : ReactiveObject
    {
        public ReactiveCommand<Unit, Unit> JoinRoomCommand { get; }


        public MultiUserChatViewModel()
        {
            this.JoinRoomCommand = ReactiveCommand.CreateFromTask(this.JoinRoomAsync);
        }

        private async Task JoinRoomAsync(CancellationToken ct)
        {
            var (roomJid, nickname) = await Interactions.JoinRoom.Handle(Unit.Default);
            var fullJid = roomJid + "/" + nickname;
            //this.Mediator.QueryAsync<JoinRoomQuery, Room>(new JoinRoomQuery(fullJid));
        }
    }
}
