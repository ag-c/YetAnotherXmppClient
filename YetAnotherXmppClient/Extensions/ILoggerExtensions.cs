﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using YetAnotherXmppClient.Protocol;
using YetAnotherXmppClient.Protocol.Handler;

namespace YetAnotherXmppClient
{
    public static class ILoggerExtensions
    {
        public static void XmppStreamContent(this ILogger logger, string message)
        {
            logger.ForContext("IsXmppStreamContent", true).Verbose(message);
        }

        public static void StreamNegotiationStatus(this ILogger logger, IEnumerable<Feature> features)
        {
            if (features.All(f => !f.IsRequired))
            {
                Log.Debug("No more features required - stream negotiation is complete");
            }
            else
            {
                var requiredFeaturesNames = features.Where(f => f.IsRequired).Select(f => f.Name.LocalName);
                Log.Debug($"Stream negotiation is incomplete, required features: {string.Join(", ", requiredFeaturesNames)}");
            }
        }

        public static void CurrentRosterItems(this ILogger logger, IEnumerable<RosterItem> rosterItems)
        {
            var sw = new StringWriter();
            sw.WriteLine("Current roster items in cache:");
            foreach(var (ri, index) in rosterItems.Indexed())
                sw.WriteLine(index + " - " + ri.ToString());
            Log.Verbose(sw.ToString());
        }

        public static void LogIfMissingSubscriptionRequestHandler(this ILogger logger, bool isHandlerMissing)
        {
            if(isHandlerMissing)
                Log.Verbose("Warning: No handler is registered to accept/reject subscription requests");
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
