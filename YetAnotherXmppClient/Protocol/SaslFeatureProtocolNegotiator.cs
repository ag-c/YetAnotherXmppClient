using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Protocol;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient
{
    public class SaslFeatureProtocolNegotiator : IFeatureProtocolNegotiator
    {
        private readonly AsyncXmppStream xmppStream;
        private readonly IEnumerable<string> clientMechanisms;

        public XName FeatureName { get; } = XNames.sasl_mechanisms;

        public SaslFeatureProtocolNegotiator(AsyncXmppStream xmppStream/*Stream serverStream*/, IEnumerable<string> clientMechanisms) //: base(serverStream)
        {
            this.xmppStream = xmppStream;
            this.clientMechanisms = clientMechanisms;
        }

        public Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            //6.3.3. Mechanism Preferences
            var mechanismToTry = this.clientMechanisms.Intersect(((MechanismsFeature)feature).Mechanisms).FirstOrDefault();
            Log.Debug($"Trying SASL mechanism '{mechanismToTry}'");
            if (mechanismToTry == null)
            {
                throw new InvalidOperationException("no supported sasl mechanism");
            }

            return this.NegotiateInternalAsync(mechanismToTry, options);
        }

        private async Task<bool> NegotiateInternalAsync(string mechanismToTry, Dictionary<string, string> options)
        {
            var username = options["username"];
            var password = options["password"];
            //6.4.2. Initiation
            await this.WriteInitiationAsync(mechanismToTry, username, password);

            //6.4.3. Challenge-Response Sequence
            XElement xElem;
            while(true)
            {
                //var xmlFragment = await this.xmlReader.ReadElementOrClosingTagAsync();//this.xmlReader.ReadNextElementAsync();
                //Expect(() => xmlFragment.PartType == XmlPartType.Element);
                //xElem = XElement.Parse(xmlFragment.RawXml);
                xElem = await this.xmppStream.ReadElementAsync();
                if (xElem.Name == XNames.sasl_challenge)
                {
                    await this.xmppStream.WriteAsync(new XElement(XNames.sasl_response).ToString());
                }
                else
                {
                    break;
                }
            }
            
            //6.4.5. SASL Failure
            //6.4.6. SASL Success
            Expect(XNames.sasl_success, actual: xElem.Name, context: xElem);

            return true;
        }

        private async Task WriteInitiationAsync(string mechanism, string username, string password)
        {
            //UNDONE xelement
            var stringWriter = new StringWriter();
            using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings {Async = true, OmitXmlDeclaration = true}))
            {
                await xmlWriter.WriteStartElementAsync("", XNames.sasl_auth.LocalName, XNames.sasl_auth.NamespaceName);
                await xmlWriter.WriteAttributeStringAsync("", XNames.sasl_mechanism.LocalName, null, mechanism);
                await xmlWriter.WriteStringAsync(
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{(char) 0}{username}{(char) 0}{password}")));
                await xmlWriter.WriteEndElementAsync();
            }

            await this.xmppStream.WriteAsync(stringWriter.ToString());
        }
    }
}