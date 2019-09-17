using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol.Handler
{
    //XEP-0054 + XEP-0153
    public class VCardProtocolHandler : ProtocolHandlerBase, IPresenceReceivedCallback
    {
        private readonly XmppStream xmppStream;

        //<bareJid, vcard-xelement>
        private readonly ConcurrentDictionary<string, XElement> vCardElements = new ConcurrentDictionary<string, XElement>();

        public event EventHandler<(string BareJid, byte[] Bytes)> AvatarReceived;


        public VCardProtocolHandler(XmppStream xmppStream, Dictionary<string, string> runtimeParameters, IMediator mediator)
            : base(xmppStream, runtimeParameters, mediator)
        {
            this.xmppStream = xmppStream;
            this.XmppStream.RegisterPresenceContentCallback(XNames.vcard_temp_update_x, this);
        }

        public XElement GetVCard(string bareJid)
        {
            this.vCardElements.TryGetValue(bareJid.ToBareJid(), out var vcardElem);
            return vcardElem;
        }

        public async Task<XElement> RequestVCardAsync(string bareJid)
        {
            var iqResp = await this.xmppStream.WriteIqAndReadReponseAsync(new Iq(IqType.get, new XElement(XNames.vcard_temp_vcard), "iq")
                                                                              {
                                                                                  From = this.RuntimeParameters["jid"],
                                                                                  To = bareJid.ToBareJid()
                                                                              });

            Expect(IqType.result, iqResp.Type, iqResp);

            var vCardElem = iqResp.Element(XNames.vcard_temp_vcard);
            this.vCardElements.AddOrUpdate(bareJid.ToBareJid(), vCardElem, (_, __) => vCardElem);

            var base64Str = vCardElem.Element("{vcard-temp}PHOTO").Element("{vcard-temp}BINVAL").Value;
            var bytes = Convert.FromBase64String(base64Str);

            this.AvatarReceived?.Invoke(this, (iqResp.From, bytes));

            return vCardElem;
        }

        public async Task<bool> UpdateVCardAsync(XElement vCardElem)
        {
            Expect(XNames.vcard_temp_vcard, vCardElem.Name, vCardElem);

            var iqResp = await this.xmppStream.WriteIqAndReadReponseAsync(new Iq(IqType.set, vCardElem));

            if (iqResp.Type == IqType.result)
            {
                return true;
            }

            Log.Error("Expected result-iq for vCard update request, but got: " + iqResp);
            return false;
        }

        async Task IPresenceReceivedCallback.PresenceReceivedAsync(Core.Stanza.Presence presence)
        {
            var xElem = presence.Element(XNames.vcard_temp_update_x);
            var sha1Hash = xElem.Element(XNames.vcard_temp_update_photo)?.Value;

            if (sha1Hash == null)
                return;

            //UNDONE 3.2: Check per sha1 hash if image is cached

            await this.RequestVCardAsync(presence.From.ToBareJid());

            Log.Verbose($"Received vCard for contact '{presence.From.ToBareJid()}'.");
        }
    }
}
