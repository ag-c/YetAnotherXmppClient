using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace YetAnotherXmppClient.Protocol.Negotiator
{
    interface IFeatureProtocolNegotiator
    {
        XName FeatureName { get; }
        Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options);
    }
}