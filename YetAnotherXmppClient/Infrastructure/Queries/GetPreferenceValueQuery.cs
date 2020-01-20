namespace YetAnotherXmppClient.Infrastructure.Queries
{
    public class GetPreferenceValueQuery : IQuery<object>
    {
        public string PreferenceName { get; }
        public object DefaultValue { get; }

        public GetPreferenceValueQuery(string preferenceName, object defaultValue = null)
        {
            this.PreferenceName = preferenceName;
            this.DefaultValue = defaultValue;
        }
    }
}
