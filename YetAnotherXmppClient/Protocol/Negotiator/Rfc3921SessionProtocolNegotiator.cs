using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Negotiator
{
    public class Rfc3921SessionProtocolNegotiator : IFeatureProtocolNegotiator
    {
        private readonly XmppStream xmppServerStream;
        private readonly Dictionary<string, string> runtimeParameters;

        public XName FeatureName { get; } = XNames.session_session;
        public bool IsNegotiated { get; private set; }

        public Rfc3921SessionProtocolNegotiator(XmppStream xmppServerStream, Dictionary<string, string> runtimeParameters)
        {
            this.xmppServerStream = xmppServerStream;
            this.runtimeParameters = runtimeParameters;
        }

        public async Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            await EstablishSessionAsync();
            this.IsNegotiated = true;
            return true;
        }

        public async Task EstablishSessionAsync()
        {
            Log.Debug("Establishing RFC3921 session..");

            var iq = new Iq(IqType.set, new XElement(XNames.session_session))
            {
                To = new Jid(this.runtimeParameters["jid"]).Server
            };

            var iqResp = await this.xmppServerStream.WriteIqAndReadReponseAsync(iq);

            Expect("iq", iqResp.Name.LocalName, iqResp);
            Expect("result", iqResp.Attribute("type").Value, iqResp);
        }
    }
}
