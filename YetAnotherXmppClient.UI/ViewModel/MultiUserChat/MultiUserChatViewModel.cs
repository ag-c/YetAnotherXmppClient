using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Queries.MultiUserChat;
using YetAnotherXmppClient.Protocol.Handler.MultiUserChat;

namespace YetAnotherXmppClient.UI.ViewModel.MultiUserChat
{
    public class MultiUserChatViewModel : ReactiveObject
    {
        private readonly IMediator mediator;

        public ReactiveCommand<Unit, Unit> JoinRoomCommand { get; }

        public ObservableCollection<RoomViewModel> Rooms { get; } = new ObservableCollection<RoomViewModel>();

        private RoomViewModel selectedRoom;

        public RoomViewModel SelectedRoom
        {
            get => this.selectedRoom;
            set => this.RaiseAndSetIfChanged(ref this.selectedRoom, value);
        }

        public MultiUserChatViewModel(IMediator mediator)
        {
            this.mediator = mediator;
            this.JoinRoomCommand = ReactiveCommand.CreateFromTask(this.JoinRoomAsync);
        }

        private async Task JoinRoomAsync(CancellationToken ct)
        {
            var (roomJid, nickname) = await Interactions.JoinRoom.Handle(Unit.Default);
            var room = await this.mediator.QueryAsync<EnterRoomQuery, Room>(new EnterRoomQuery(roomJid, nickname));
            room.Exited += this.HandleRoomExited;
            this.Rooms.Add(new RoomViewModel(room));
        }

        private void HandleRoomExited(object? sender, EventArgs e)
        {
            var room = (Room)sender;
            this.Rooms.Remove(this.Rooms.First(vm => vm.RoomJid == room.Jid));
        }
    }
}
