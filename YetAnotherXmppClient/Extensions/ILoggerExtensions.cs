using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace YetAnotherXmppClient
{
    public static class ILoggerExtensions
    {
        public static void StreamNegotiationStatus(this ILogger logger, IEnumerable<Feature> features)
        {
            if (features.All(f => !f.IsRequired))
            {
                Log.Logger.Debug("No more features required - stream negotiation is complete");
            }
            else
            {
                var requiredFeaturesNames = features.Where(f => f.IsRequired).Select(f => f.Name.LocalName);
                Log.Logger.Debug($"Stream negotiation is incomplete, required features: {string.Join(", ", requiredFeaturesNames)}");
            }
        }
    }
}
