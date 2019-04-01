using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;

namespace YetAnotherXmppClient.Protocol.Handler
{
    public class Presence
    {
        public string From { get; set; }
        //public string To { get; set; }
        public int? Priority { get; set; }
        public PresenceShow Show { get; set; }
        public IEnumerable<string> Stati { get; set; }
    }

    public class PresenceProtocolHandler : ProtocolHandlerBase, IPresenceReceivedCallback
    {
        public Func<string, Task<bool>> OnSubscriptionRequestReceived { get; set; }

        // <full-jid, presence>
        public ConcurrentDictionary<string, Presence> PresenceByJid { get; } = new ConcurrentDictionary<string, Presence>();


        public PresenceProtocolHandler(AsyncXmppStream xmppStream)
            : base(xmppStream, null)
        {
            this.XmppStream.RegisterPresenceCallback(this);
        }

        public async Task<bool> RequestSubscriptionAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.subscribe)
            {
                To = contactJid.ToBareJid()
            };

            await this.XmppStream.WriteAsync(presence);
            //UNDONE no response
            //if (presenceResp.IsErrorType())
            //{
            //    Log.Error("Failed to request presence subscription");
            //    return false;
            //}

            return true;
        }

        public async Task CancelSubscriptionAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.unsubscribed)
            {
                To = contactJid.ToBareJid()
            };

            await this.XmppStream.WriteAsync(presence);
        }

        public async Task UnsubscribeAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.unsubscribe)
            {
                To = contactJid.ToBareJid()
            };

            await this.XmppStream.WriteAsync(presence);
        }

        //UNDONE async
        async void IPresenceReceivedCallback.PresenceReceived(XElement presenceElem)
        {
            Expectation.Expect(XNames.presence, presenceElem.Name, presenceElem);

            if (presenceElem.Attribute("type")?.Value == PresenceType.subscribe.ToString())
            {
                var fromValue = presenceElem.Attribute("from").Value;

                var responseType = await (this.OnSubscriptionRequestReceived?.Invoke(fromValue) ?? Task.FromResult(false))
                    ? PresenceType.subscribed
                    : PresenceType.unsubscribed;

                var response = new Core.Stanza.Presence(responseType)
                {
                    To = fromValue
                };

                await this.XmppStream.WriteAsync(response);
            }
            else if(!presenceElem.HasAttribute("type"))
            {
                Expectation.Expect(() => presenceElem.HasAttribute("from"), presenceElem);

                var fromVal = presenceElem.Attribute("from").Value;

                this.PresenceByJid.AddOrUpdate(fromVal, _ =>
                {
                    var instance = new Presence();
                    UpdatePresence(instance, presenceElem);
                    return instance;
                }, (_, existing) => UpdatePresence(existing, presenceElem));
            }

            Presence UpdatePresence(Presence existing, XElement newPresenceElem)
            {
                var fromVal = presenceElem.Attribute("from").Value;
                var showElem = newPresenceElem.Element("show");
                var prioElem = newPresenceElem.Element("priority");

                existing.From = fromVal;
                existing.Show = showElem != null
                    ? (PresenceShow)Enum.Parse(typeof(PresenceShow), showElem.Value)
                    : PresenceShow.None;
                existing.Stati = newPresenceElem.Elements("status").Select(xe => xe.Value);
                existing.Priority = prioElem != null ? int.Parse(prioElem.Value) : (int?)null;

                return existing;
            }
    }

        public Task BroadcastPresenceAsync(PresenceShow? show = null, string status = null)
        {
            var presence = new Core.Stanza.Presence(show, status);

            return this.XmppStream.WriteAsync(presence);
        }

        public Task SendUnavailableAsync()
        {
            return this.XmppStream.WriteAsync(new Core.Stanza.Presence(PresenceType.unavailable));
        }
    }
}
