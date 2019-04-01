using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Protocol
{
    class StartTlsProtocolHandler : IFeatureProtocolHandler
    {
        private readonly AsyncXmppStream xmppServerStream;
        public XName FeatureName { get; } = XNames.starttls;

        public StartTlsProtocolHandler(AsyncXmppStream xmppServerStream)
        {
            this.xmppServerStream = xmppServerStream;
        }

        public async Task<bool> NegotiateAsync(Feature feature, Dictionary<string, string> options)
        {
            Log.Debug("Initiating TLS negotiation..");
            await this.xmppServerStream.WriteAsync(new XElement(XNames.starttls).ToString());

            var xElem = await this.xmppServerStream.ReadElementAsync();
            if (xElem.Name == XNames.failure)
            {
                //UNDONE If the failure case occurs, the initiating entity MAY attempt to
                //reconnect as explained under Section 3.3.
                Log.Fatal($"Error: Reply to 'starttls' was '{xElem.Name}'");
                throw new NotExpectedProtocolException(xElem.Name.ToString(), XNames.proceed.ToString());
            }
            else if (xElem.Name == XNames.proceed)
            {
                var sslStream = new SslStream(this.xmppServerStream.BaseStream, false, this.UserCertificateValidationCallback);

                await sslStream.AuthenticateAsClientAsync(options["server"]);
                //UNDONE 5.4.3.3. TLS Success

                this.xmppServerStream.Reinitialize(sslStream);
            }
            else
            {
                //UNDONE auf localname=="proceed" prüfen
                throw new NotExpectedProtocolException(xElem.Name.ToString(), "proceed or failure");
            }

            return true;
        }

        private bool UserCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }
    }
}
