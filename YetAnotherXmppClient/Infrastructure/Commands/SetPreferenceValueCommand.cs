namespace YetAnotherXmppClient.Infrastructure.Commands
{
    public class SetPreferenceValueCommand : ICommand
    {
        public string PreferenceName { get; }
        public object Value { get; }

        public SetPreferenceValueCommand(string preferenceName, object value)
        {
            this.PreferenceName = preferenceName;
            this.Value = value;
        }
    }
}
