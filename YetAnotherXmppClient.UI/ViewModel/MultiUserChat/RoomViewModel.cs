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

        private string textToSend;
        public string TextToSend
        {
            get => this.textToSend;
            set => this.RaiseAndSetIfChanged(ref this.textToSend, value);
        }

        public ReactiveCommand<Unit, Unit> ExitCommand { get; }
        public ReactiveCommand<Unit, Unit> SendCommand { get; }

        public RoomViewModel(Room room)
        {
            this.room = room;
            this.ExitCommand = ReactiveCommand.CreateFromTask(this.ExitAsync);
            this.SendCommand = ReactiveCommand.CreateFromTask(this.SendMessageToAllOccupantsAsync);

            this.RoomJid = room.Jid;
            this.Subject = room.Subject;
            room.SelfUpdated += this.HandleSelfUpdated;
            room.OccupantsUpdated += this.HandleOccupantsUpdated;
            room.SubjectChanged += this.HandleSubjectChanged;
            room.NewMessage += this.HandleNewMessage;
            room.ErrorOccurred += this.HandleErrorOccurred;
        }

        private void HandleSelfUpdated(object? sender, (Occupant OldSelf, Occupant NewSelf) e)
        {
            this.OutputOccupantChange(e.OldSelf, e.NewSelf);
        }

        private void HandleErrorOccurred(object? sender, string errorText)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Messages.Add(new ErrorMessage(errorText));
                });
        }

        private Task ExitAsync(CancellationToken arg)
        {
            return this.room.ExitAsync();
        }

        private async Task SendMessageToAllOccupantsAsync(CancellationToken arg)
        {
            await this.room.SendMessageToAllOccupantsAsync(this.TextToSend);

            this.TextToSend = null;
        }



        private void HandleSubjectChanged(object? sender, (string Subject, string Nickname) e)
        {
            this.Subject = e.Subject;
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Messages.Add(new RoomMessage($"Room subject has been changed by {e.Nickname} to '{e.Subject}'"));
                });
        }

        private void HandleNewMessage(object? sender, (string MessageText, string Nickname, DateTime Time) e)
        {
            this.InternalAddMessageSorted(new OccupantMessage(e.Nickname, e.MessageText, e.Time));
        }

        private void HandleOccupantsUpdated(object? sender, (Occupant OldOccupant, Occupant NewOccupant, OccupantUpdateCause Cause) e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (e.Cause == OccupantUpdateCause.Added)
                    {
                        this.Occupants.Add(e.NewOccupant);
                    }
                    else if (e.Cause == OccupantUpdateCause.Changed)
                    {
                        this.OutputOccupantChange(e.OldOccupant, e.NewOccupant);
                    }
                    else if (e.Cause == OccupantUpdateCause.Removed)
                    {
                        this.Occupants.Remove(e.OldOccupant);
                    }
                });
        }

        private void OutputOccupantChange(Occupant oldOccupant, Occupant newOccupant)
        {
            if (oldOccupant == null || newOccupant == null)
                return;

            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (oldOccupant.Nickname != newOccupant.Nickname)
                        this.Messages.Add(new RoomMessage($"Nickname of {oldOccupant.Nickname} changed to {newOccupant.Nickname}"));
                    if (oldOccupant.Role != newOccupant.Role)
                        this.Messages.Add(new RoomMessage($"Role of {newOccupant.Nickname} changed from '{oldOccupant.Role}' to '{newOccupant.Role}'"));
                    if (oldOccupant.Affiliation != newOccupant.Affiliation)
                        this.Messages.Add(new RoomMessage($"Affilitation of {newOccupant.Nickname} changed from '{oldOccupant.Affiliation}' to '{newOccupant.Affiliation}'"));
                    if (oldOccupant.Show != newOccupant.Show)
                        this.Messages.Add(new RoomMessage($"Show of {newOccupant.Nickname} changed to '{newOccupant.Show}'"));
                    //UNDONE status
                });
        }

        private void InternalAddMessageSorted(RoomMessage message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var hasBeenAdded = false;
                    for (int i = this.Messages.Count - 1; i >= 0 ; i--)
                    {
                        if (message.Time > this.Messages[i].Time)
                        {
                            this.Messages.Insert(i + 1, message);
                            hasBeenAdded = true;
                            break;
                        }
                    }

                    if (!hasBeenAdded)
                    {
                        this.Messages.Insert(0, message);
                    }
                });
        }
    }
}
