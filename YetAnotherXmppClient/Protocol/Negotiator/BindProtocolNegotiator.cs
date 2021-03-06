using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Negotiator
{
    public class BindProtocolNegotiator : IFeatureProtocolNegotiator
    {
        private readonly XmppStream xmppServerStream;
        private readonly Dictionary<string, string> runtimeParameters;

        public Jid ConnectedJid => new Jid(this.runtimeParameters["jid"]);

        public XName FeatureName { get; } = XNames.bind_bind;
        public bool IsNegotiated { get; private set; }

        public BindProtocolNegotiator(XmppStream xmppServerStream, Dictionary<string, string> runtimeParameters)
        {
            this.xmppServerStream = xmppServerStream;
            this.runtimeParameters = runtimeParameters;
        }

        public async Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            var resource = options["resource"];

            var requestIq = new Iq(IqType.set, new Bind(resource));

            var responseIq = await this.xmppServerStream.WriteIqAndReadReponseAsync(requestIq).ConfigureAwait(false);

            Expect(IqType.result, responseIq.Type, responseIq);

            var bind = responseIq.GetContent<Bind>();

            this.runtimeParameters["jid"] = bind.Jid;

            this.IsNegotiated = true;
            return true;
        }
    }
}