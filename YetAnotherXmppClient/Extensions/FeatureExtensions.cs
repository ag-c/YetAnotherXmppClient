namespace YetAnotherXmppClient.Extensions
{
    static class FeatureExtensions
    {
        public static bool IsStreamRestartRequired(this Feature feature)
        {
            return feature?.Name == XNames.starttls || feature?.Name == XNames.sasl_mechanisms;
        }
    }
}
