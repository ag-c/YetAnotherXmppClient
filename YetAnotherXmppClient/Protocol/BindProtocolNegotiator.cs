using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.StanzaParts;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
{
    public class BindProtocolNegotiator : IFeatureProtocolNegotiator
    {
        private readonly AsyncXmppStream xmppServerStream;
        private readonly Dictionary<string, string> runtimeParameters;

        public Jid JidForConnectedResource { get; set; }

        public XName FeatureName { get; } = XNames.bind_bind;


        public BindProtocolNegotiator(AsyncXmppStream xmppServerStream, Dictionary<string, string> runtimeParameters)
        {
            this.xmppServerStream = xmppServerStream;
            this.runtimeParameters = runtimeParameters;
        }

        public async Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            var resource = options["resource"];

            var iq = new Iq(IqType.set, new Bind(resource));

            var iqResp = await this.xmppServerStream.WriteIqAndReadReponseAsync(iq);

            Expect("result", iqResp.Attribute("type")?.Value, iqResp);

            this.runtimeParameters["jid"] = iqResp.Element(XNames.bind_bind).Element(XNames.bind_jid).Value;

            return true;
        }
    }
}