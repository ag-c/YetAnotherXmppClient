using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using Serilog.Core;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Protocol;

namespace YetAnotherXmppClient
{
    public delegate bool OnSubscriptionRequestReceivedDelegate(Jid jid);

    public class XmppClient : IFeatureOptionsProvider
    {
        public static readonly int DefaultPort = 5222;
        
        private TcpClient tcpClient;

        private Jid jid;
        private string password;

        public Func<string, Task<bool>> OnSubscriptionRequestReceived { get; set; }
        public event EventHandler<IEnumerable<RosterItem>> RosterUpdated;
        public Action<Jid, string> OnMessageReceived { get; set; }

        public async Task StartAsync(Jid jid, string password)
        {
            this.jid = jid;
            this.password = password;
            this.tcpClient = new TcpClient();
            
            Log.Logger.Information($"Connecting to {jid.Server}:{DefaultPort}..");
            
            await this.tcpClient.ConnectAsync(jid.Server, DefaultPort);
            
            Log.Logger.Information($"Connection established");

            var protocolHandler = new MainProtocolHandler(this.tcpClient.GetStream(), this);
            protocolHandler.FatalErrorOccurred += this.HandleFatalProtocolErrorOccurred;
            protocolHandler.RosterHandler.RosterUpdated += (s, e) => this.RosterUpdated?.Invoke(this, e);
            protocolHandler.PresenceHandler.OnSubscriptionRequestReceived = this.OnSubscriptionRequestReceived;
            protocolHandler.ImProtocolHandler.OnMessageReceived = this.OnMessageReceived;

            Task.Run(() => protocolHandler.RunAsync(jid).ContinueWith(_ => HandleProtocolHandlingEnded()));
        }

        private void HandleFatalProtocolErrorOccurred(object sender, Exception e)
        {
            if(e is NotExpectedProtocolException ex)
            {
                Log.Logger.Fatal($"{ex.ThrownBy}: Expected {ex.Expected}, but was {ex.Actual}.\nContext: {ex.Context}");
            }
            else
            {
                Log.Logger.Fatal(e, $"Fatal error occurred in protocol handler: {e}");
            }
        }

        private void HandleProtocolHandlingEnded()
        {
            Log.Logger.Information($"The protocol handler stopped working for unknown reason");
        }

        public Dictionary<string, string> GetOptions(XName featureName)
        {
            if (featureName == XNames.sasl_mechanisms)
            {
                return new Dictionary<string, string>
                           {
                                ["username"] = this.jid.Local,
                                ["password"] = this.password
                           };
            }
            if (featureName == XNames.bind_bind)
            {
                return new Dictionary<string, string>
                           {
                               ["resource"] = this.jid.Resource,
                           };
            }

            return null;
        }
    }

//    static class TaskExtensions
//    {
//        public static void async Forget(this Task task)
//        {
//            await
//        }
//    }
}