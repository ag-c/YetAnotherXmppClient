using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Protocol.Handler
{
    class OmemoProtocolHandler : ProtocolHandlerBase, IMessageReceivedCallback
    {
        private const string Node = "eu.siacs.conversations.axolotl.devicelist";
        private const string BundlesNode = "eu.siacs.conversations.axolotl.bundles:31415";

        private readonly PepProtocolHandler pepHandler;

        public OmemoProtocolHandler(PepProtocolHandler pepHandler, XmppStream xmppStream, Dictionary<string, string> runtimeParameters) 
            : base(xmppStream, runtimeParameters, null)
        {
            this.pepHandler = pepHandler;

            this.XmppStream.RegisterMessageContentCallback(XNames.pubsubevent_event, this);
        }

        public Task InitializeAsync()
        {
            return this.pepHandler.SubscribeToNodeAsync(Node);
        }

        //4.3
        public async Task AnnounceSupportAsync()
        {
            var deviceIds = new int[] { 123, 456 };

            await this.pepHandler.PublishEventAsync(Node, "current", new AxolotlList(deviceIds)).ConfigureAwait(false);
        }

        //4.4
        public async Task AnnounceIdentityKey()
        {
            string signedPreKeyPublic = null;
            string signedPreKeySignature = null;
            string identityKey = null;
            var preKeysPublic = new[] { "", "", "" };

            await this.pepHandler.PublishEventAsync(BundlesNode, "current",
                new XElement(XNames.axolotl_bundle, 
                    new XElement(XNames.axolotl_signedPreKeyPublic, new XAttribute("signedPreKeyId", "1"), signedPreKeyPublic),
                    new XElement(XNames.axolotl_signedPreKeySignature, signedPreKeySignature),
                    new XElement(XNames.axolotl_identityKey, identityKey),
                    new XElement(XNames.axolotl_prekeys, preKeysPublic.Select((key, idx) => new XElement(XNames.axolotl_preKeyPublic, new XAttribute("preKeyId", idx.ToString()), key))))).ConfigureAwait(false);
        }

        public async Task FetchingDevicesBundleInfo(string jid)
        {
            var iq = new Iq(IqType.get, new XElement(XNames.pubsub_pubsub, new XElement(XNames.pubsub_items, new XAttribute("node", BundlesNode))))
                         {
                             From = this.RuntimeParameters["jid"].ToBareJid(),
                             To = jid.ToBareJid()
                         };

            var ipResp = await this.XmppStream.WriteIqAndReadReponseAsync(iq).ConfigureAwait(false);
        }

        private int ownDeviceId;

        Task IMessageReceivedCallback.MessageReceivedAsync(Message message)
        {
            var listElem = message.Element(XNames.pubsubevent_event)?.Element(XNames.pubsubevent_items)?.Element(XNames.pubsubevent_item)?.Element(XNames.axolotl_list);
            if (listElem != null)
            {
                var list = AxolotlList.FromXElement(listElem);
                //devices MUST check that their own device ID is contained in the list whenever they receive a PEP update from their own account.
                //If they have been removed, they MUST reannounce themselves.
                if (list.DeviceIds.All(id => id != this.ownDeviceId))
                {
                    //TODO reannounce myself
                }
            }

            return Task.CompletedTask;
        }

        //4.7
        private void PreHandleIncomingMessage(Message message)
        {
            var keyElems = message.Element(XNames.axolotl_encrypted)?.Element(XNames.axolotl_header)?.Elements(XNames.axolotl_key);
            if (keyElems != null && keyElems.Any())
            {
                var matchingKeyElem = keyElems.FirstOrDefault(ke => ke.Attribute("rid")?.Value == this.ownDeviceId.ToString());
                if (matchingKeyElem == null)
                {
                    Log.Error("message contains no key element for own deviceid");
                    return;
                }

                var isPreKeySignalMessage = matchingKeyElem.Attribute("prekey")?.Value.ToLower() == "true";
            }
        }
    }
}
