using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using YetAnotherXmppClient.Protocol.Handler.MultiUserChat;

namespace YetAnotherXmppClient.UI.ViewModel.MultiUserChat
{
    public class RoomMessage
    {
        public DateTime Time { get; }
        public string Text { get; }
    }
    public class OccupantMessage : RoomMessage
    {
        public string Nickname { get; set; }
    }

    public class RoomViewModel : ReactiveObject
    {
        private readonly Room room;

        public string RoomJid { get; }
        public ObservableCollection<Occupant> Occupants { get; } = new ObservableCollection<Occupant>();

        private string subject;
        public string Subject
        {
            get => this.subject;
            set => this.RaiseAndSetIfChanged(ref this.subject, value);
        }

        public string TextToSend { get; set; }

        public ReactiveCommand<Unit, Unit> SendCommand { get; }

        public RoomViewModel(Room room)
        {
            this.room = room;
            this.SendCommand = ReactiveCommand.CreateFromTask(this.SendMessageToAllOccupantsAsync);

            this.RoomJid = room.Jid;
            this.Subject = room.Subject;
            room.OccupantsUpdated += this.HandleOccupantsUpdated;
            room.SubjectChanged += this.HandleSubjectChanged;
            room.ErrorOccurred += this.HandleErrorOccurred;
        }

        private void HandleErrorOccurred(object? sender, string errorText)
        {
            Interactions.ShowRoomError.Handle((this.room.Jid, errorText));
        }

        private Task SendMessageToAllOccupantsAsync(CancellationToken arg)
        {
            return this.room.SendMessageToAllOccupantsAsync(this.TextToSend);
        }

        private void HandleSubjectChanged(object? sender, string subject)
        {
            this.Subject = subject;
        }

        private void HandleOccupantsUpdated(object? sender, (Occupant Occupant, OccupantUpdateCause Cause) e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (e.Cause == OccupantUpdateCause.Added)
                    {
                        this.Occupants.Add(e.Occupant);
                    }
                    else if (e.Cause == OccupantUpdateCause.Changed)
                    {
                        //do we need something to do?
                    }
                    else if (e.Cause == OccupantUpdateCause.Removed)
                    {
                        this.Occupants.Remove(e.Occupant);
                    }
                });
        }
    }
}
