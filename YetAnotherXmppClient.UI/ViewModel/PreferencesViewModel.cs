using System;
using ReactiveUI;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;
using YetAnotherXmppClient.Infrastructure.Queries;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class PreferencesViewModel : ReactiveObject
    {
        private readonly IMediator mediator;

        public Action CloseAction { get; set; }
        public System.Windows.Input.ICommand SaveCommand { get; }

        public bool SendChatStateNotifications { get; set; }

        public PreferencesViewModel(IMediator mediator)
        {
            this.mediator = mediator;
            this.SaveCommand = ReactiveCommand.Create(this.Save);

            this.SendChatStateNotifications = (bool)mediator.Query<GetPreferenceValueQuery, object>(new GetPreferenceValueQuery("SendChatStateNotifications"));
        }

        public void Save()
        {
            this.mediator.Execute(new SetPreferenceValueCommand("SendChatStateNotifications", this.SendChatStateNotifications));
            this.CloseAction?.Invoke();
        }
    }
}
