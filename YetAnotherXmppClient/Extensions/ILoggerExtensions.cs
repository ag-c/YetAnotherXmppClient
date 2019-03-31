using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using YetAnotherXmppClient.Protocol;

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

        public static void CurrentRosterItems(this ILogger logger, IEnumerable<RosterItem> rosterItems)
        {
            var sw = new StringWriter();
            sw.WriteLine("Current roster items in cache:");
            foreach(var (ri, index) in rosterItems.Indexed())
                sw.WriteLine(index + " - " + ri.ToString());
            Log.Logger.Verbose(sw.ToString());
        }
    }

    static class IEnumerableExtensions
    {
        public static IEnumerable<(T Data, int Index)> Indexed<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Select((itm, index) => (itm, index));
        }
    }
}
