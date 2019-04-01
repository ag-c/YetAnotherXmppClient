using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //XEP-0199: XMPP Ping
    class PingProtocolHandler : ProtocolHandlerBase, IIqReceivedCallback
    {
        private bool isNotSupportedByServer;

        public PingProtocolHandler(AsyncXmppStream xmppStream, Dictionary<string, string> runtimeParameters) : base(xmppStream, runtimeParameters)
        {
            this.XmppStream.RegisterIqNamespaceCallback(XNamespaces.ping, this);
        }

        //4.2 Client-To-Server Pings
        public async Task<bool> PingAsync() //UNDONE timeout
        {
            if (isNotSupportedByServer)
                return false;

            var iqResp = await this.XmppStream.WriteIqAndReadReponseAsync(new Iq(IqType.get, new XElement(XNames.ping_ping))
            {
                From = this.RuntimeParameters["jid"],
                To = new Jid(this.RuntimeParameters["jid"]).Server
            });

            if(iqResp.Elements("error").Any())
                this.isNotSupportedByServer = true;

            return !this.isNotSupportedByServer;
        }

        //4.1 Server-To-Client Pings & 4.4 Client-to-Client Pings
        void IIqReceivedCallback.IqReceived(Iq iq)
        {
            var content = iq.Elements().Single();

            if (content.Name == XNames.ping_ping)
            {   
                this.XmppStream.WriteAsync(new Iq(IqType.result)
                {
                    Id = iq.Id,
                    From = this.RuntimeParameters["jid"],
                    To = iq.From
                });
            }
        }
    }
}
