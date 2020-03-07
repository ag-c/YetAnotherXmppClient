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
        public DateTime Time { get; set; }
        public string Text { get; set; }
    }
    public class OccupantMessage : RoomMessage
    {
        public string Nickname { get; set; }
    }

    public class RoomViewModel : ReactiveObject
    {
        private readonly Room room;

        public string RoomJid { get; }

        private Occupant self;
        public Occupant Self
        {
            get => this.self;
            set => this.RaiseAndSetIfChanged(ref this.self, value);
        }

        public ObservableCollection<Occupant> Occupants { get; } = new ObservableCollection<Occupant>();

        private string subject;
        public string Subject
        {
            get => this.subject;
            set => this.RaiseAndSetIfChanged(ref this.subject, value);
        }

        public ObservableCollection<RoomMessage> Messages { get; } = new ObservableCollection<RoomMessage>();


        public string TextToSend { get; set; }

        public ReactiveCommand<Unit, Unit> ExitCommand { get; }
        public ReactiveCommand<Unit, Unit> SendCommand { get; }

        public RoomViewModel(Room room)
        {
            this.room = room;
            this.ExitCommand = ReactiveCommand.CreateFromTask(this.ExitAsync);
            this.SendCommand = ReactiveCommand.CreateFromTask(this.SendMessageToAllOccupantsAsync);

            this.RoomJid = room.Jid;
            this.Subject = room.Subject;
            room.SelfUpdated += (sender, self) => this.
            room.OccupantsUpdated += this.HandleOccupantsUpdated;
            room.SubjectChanged += this.HandleSubjectChanged;
            room.ErrorOccurred += this.HandleErrorOccurred;
        }

        private void HandleErrorOccurred(object? sender, string errorText)
        {
            Interactions.ShowRoomError.Handle((this.room.Jid, errorText));
        }

        private Task ExitAsync(CancellationToken arg)
        {
            return this.room.ExitAsync();
        }

        private Task SendMessageToAllOccupantsAsync(CancellationToken arg)
        {
            return this.room.SendMessageToAllOccupantsAsync(this.TextToSend);
        }

        private void HandleSubjectChanged(object? sender, (string Subject, string Nickname) e)
        {
            this.Subject = e.Subject;
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Messages.Add(new RoomMessage
                                          {
                                              Time = DateTime.Now,
                                              Text = $"Room subject has been changed by {e.Nickname} to '{e.Subject}"
                                          });
                });
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
