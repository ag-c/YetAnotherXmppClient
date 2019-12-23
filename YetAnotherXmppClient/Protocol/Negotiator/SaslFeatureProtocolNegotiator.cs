using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Events;

namespace YetAnotherXmppClient.Protocol.Negotiator
{
    public class SaslFeatureProtocolNegotiator : IFeatureProtocolNegotiator
    {
        private readonly XmppStream xmppStream;
        private readonly IEnumerable<string> clientMechanisms;
        private readonly IMediator mediator;

        public XName FeatureName { get; } = XNames.sasl_mechanisms;
        public bool IsNegotiated { get; private set; }


        public SaslFeatureProtocolNegotiator(XmppStream xmppStream/*Stream serverStream*/, IEnumerable<string> clientMechanisms, IMediator mediator) //: base(serverStream)
        {
            this.xmppStream = xmppStream;
            this.clientMechanisms = clientMechanisms;
            this.mediator = mediator;
        }

        public async Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            //6.3.3. Mechanism Preferences
            var mechanismToTry = this.clientMechanisms.Intersect(((MechanismsFeature)feature).Mechanisms).FirstOrDefault();

            Log.Debug($"Trying SASL mechanism '{mechanismToTry}'");

            if (mechanismToTry == null)
            {
                throw new InvalidOperationException("no supported sasl mechanism");
            }

            var success = await this.NegotiateInternalAsync(mechanismToTry, options).ConfigureAwait(false);
            if (success)
            {
                this.IsNegotiated = true;
                await this.mediator.PublishAsync(new LoggedInEvent()).ConfigureAwait(false);
            }

            return success;

        }

        private async Task<bool> NegotiateInternalAsync(string mechanismToTry, Dictionary<string, string> options)
        {
            var username = options["username"];
            var password = options["password"];
            //6.4.2. Initiation
            await this.WriteInitiationAsync(mechanismToTry, username, password).ConfigureAwait(false);

            //6.4.3. Challenge-Response Sequence
            XElement xElem;
            while(true)
            {
                xElem = await this.xmppStream.ReadElementAsync().ConfigureAwait(false);
                if (xElem.Name == XNames.sasl_challenge)
                {
                    await this.xmppStream.WriteElementAsync(new XElement(XNames.sasl_response)).ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }
            
            //6.4.5. SASL Failure
            //6.4.6. SASL Success
            Expectation.Expect(XNames.sasl_success, actual: xElem.Name, context: xElem);

            return true;
        }

        private async Task WriteInitiationAsync(string mechanism, string username, string password)
        {
            var xElem = new XElement(XNames.sasl_auth, new XAttribute(XNames.sasl_mechanism.LocalName, mechanism),
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{(char) 0}{username}{(char) 0}{password}"))
            );

            await this.xmppStream.WriteElementAsync(xElem).ConfigureAwait(false);
        }
    }
}