using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Core.StanzaParts;
using YetAnotherXmppClient.Extensions;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
{
    public class Presence
    {
        public string From { get; set; }
        //public string To { get; set; }
        public int? Priority { get; set; }
        public PresenceShow Show { get; set; }
        public IEnumerable<string> Stati { get; set; }
    }

    public class PresenceProtocolHandler : IPresenceReceivedCallback
    {
        private readonly AsyncXmppStream xmppStream;

        public Func<string, Task<bool>> OnSubscriptionRequestReceived { get; set; }

        // <full-jid, presence>
        public ConcurrentDictionary<string, Presence> PresenceByJid { get; } = new ConcurrentDictionary<string, Presence>();


        public PresenceProtocolHandler(AsyncXmppStream xmppStream)
        {
            this.xmppStream = xmppStream;

            this.xmppStream.RegisterPresenceCallback(this);
        }

        public async Task<bool> RequestSubscriptionAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.subscribe)
            {
                To = contactJid.ToBareJid()
            };

            await this.xmppStream.WriteAsync(presence);
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

            await this.xmppStream.WriteAsync(presence);
        }

        public async Task UnsubscribeAsync(string contactJid)
        {
            var presence = new Core.Stanza.Presence(PresenceType.unsubscribe)
            {
                To = contactJid.ToBareJid()
            };

            await this.xmppStream.WriteAsync(presence);
        }

        //UNDONE async
        async void IPresenceReceivedCallback.PresenceReceived(XElement presenceXElem)
        {
            Expect("presence", presenceXElem.Name, presenceXElem);

            if (presenceXElem.Attribute("type")?.Value == PresenceType.subscribe.ToString())
            {
                var fromValue = presenceXElem.Attribute("from").Value;

                var responseType = await (this.OnSubscriptionRequestReceived?.Invoke(fromValue) ?? Task.FromResult(false))
                    ? PresenceType.subscribed
                    : PresenceType.unsubscribed;

                var response = new Core.Stanza.Presence(responseType)
                {
                    To = fromValue
                };

                await this.xmppStream.WriteAsync(response);
            }
            else if(!presenceXElem.HasAttribute("type"))
            {
                Expect(() => presenceXElem.HasAttribute("from"), presenceXElem);

                var fromVal = presenceXElem.Attribute("from").Value;
                var showElem = presenceXElem.Element("show");
                var prioElem = presenceXElem.Element("priority");


                this.PresenceByJid.AddOrUpdate(fromVal, _ => new Presence
                    {
                        From = fromVal,
                        Show = showElem != null
                            ? (PresenceShow) Enum.Parse(typeof(PresenceShow), showElem.Value)
                            : PresenceShow.None,
                        Stati = presenceXElem.Elements("status").Select(xe => xe.Value),
                        Priority = prioElem != null ? int.Parse(prioElem.Value) : (int?) null
                    },
                    (_, existing) =>
                    {
                        existing.From = fromVal;
                        existing.Show = showElem != null
                            ? (PresenceShow) Enum.Parse(typeof(PresenceShow), showElem.Value)
                            : PresenceShow.None;
                        existing.Stati = presenceXElem.Elements("status").Select(xe => xe.Value);
                        existing.Priority = prioElem != null ? int.Parse(prioElem.Value) : (int?) null;
                        return existing;
                    });
            }
        }

        public Task BroadcastPresenceAsync(PresenceShow? show = null, string status = null)
        {
            var presence = new Core.Stanza.Presence(show, status);

            return this.xmppStream.WriteAsync(presence);
        }

        public Task SendUnavailableAsync()
        {
            return this.xmppStream.WriteAsync(new Core.Stanza.Presence(PresenceType.unavailable));
        }
    }
}
