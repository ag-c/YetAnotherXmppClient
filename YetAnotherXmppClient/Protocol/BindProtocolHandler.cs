using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
{
    public class Bind : XElement
    {
        public Bind(string resource = null)
            : base(XNames.bind_bind, resource == null ? null : new XElement(XNames.bind_resource, resource))
        {
        }
    }

    public class BindProtocolHandler : /*ProtocolHandlerBase,*/ IFeatureProtocolHandler
    {
        private readonly AsyncXmppStream xmppServerStream;
        private readonly Dictionary<string, string> runtimeParameters;

        public Jid JidForConnectedResource { get; set; }

        public XName FeatureName { get; } = XNames.bind_bind;


        public BindProtocolHandler(AsyncXmppStream xmppServerStream/*Stream serverStream*/, Dictionary<string, string> runtimeParameters) //: base(serverStream)
        {
            this.xmppServerStream = xmppServerStream;
            this.runtimeParameters = runtimeParameters;
        }


        public async Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            var resource = options["resource"];

            var iq = new Iq(IqType.set, new Bind(resource));

//            await this.textWriter.WriteAndFlushAsync(iq);

//            var iqResp = await this.xmlReader.ReadIqStanzaAsync();
            var iqResp = await this.xmppServerStream.WriteIqAndReadReponseAsync(iq);

            Expect("result", iqResp.Attribute("type")?.Value, iqResp);
            Expect(iq.Id, iqResp.Attribute("id")?.Value, iqResp);

            this.runtimeParameters["jid"] = iqResp.Element(XNames.bind_bind).Element(XNames.bind_jid).Value;

            return true;
        }
    }
}