using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;
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
        
        private CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private TcpClient tcpClient;

        private Jid jid;
        private string password;

        public MainProtocolHandler ProtocolHandler { get; private set; }

        public Func<string, Task<bool>> OnSubscriptionRequestReceived { get; set; }
        public event EventHandler<IEnumerable<RosterItem>> RosterUpdated;
        public Action<Jid, string> OnMessageReceived { get; set; }

        public async Task StartAsync(Jid jid, string password)
        {
            this.jid = jid;
            this.password = password;
            this.tcpClient = new TcpClient();
            
            Log.Information($"Connecting to {jid.Server}:{DefaultPort}..");
            
            await this.tcpClient.ConnectAsync(jid.Server, DefaultPort);
            
            Log.Information($"Connection established");

            this.ProtocolHandler = new MainProtocolHandler(this.tcpClient.GetStream(), this);
            this.ProtocolHandler.FatalErrorOccurred += this.HandleFatalProtocolErrorOccurred;
            this.ProtocolHandler.RosterHandler.RosterUpdated += (s, e) => this.RosterUpdated?.Invoke(this, e);
            this.ProtocolHandler.PresenceHandler.OnSubscriptionRequestReceived = this.OnSubscriptionRequestReceived;
            this.ProtocolHandler.ImProtocolHandler.OnMessageReceived = this.OnMessageReceived;

            Task.Run(() => this.ProtocolHandler.RunAsync(jid, this.cancelTokenSource.Token).ContinueWith(_ => HandleProtocolHandlingEnded()));
        }

        public async Task ShutdownAsync()
        {
            await this.ProtocolHandler.TerminateSessionAsync();
            this.cancelTokenSource.Cancel(false);
        }

        private void HandleFatalProtocolErrorOccurred(object sender, Exception e)
        {
            if(e is NotExpectedProtocolException ex)
            {
                Log.Fatal($"{ex.ThrownBy}: Expected {ex.Expected}, but was {ex.Actual}.\nContext: {ex.Context}");
            }
            else
            {
                Log.Fatal(e, $"Fatal error occurred in protocol handler: {e}");
            }
        }

        private void HandleProtocolHandlingEnded()
        {
            Log.Information($"The protocol handler stopped working for unknown reason");
        }

        public Dictionary<string, string> GetOptions(XName featureName)
        {
            if (featureName == XNames.starttls)
            {
                return new Dictionary<string, string>
                           {
                               ["server"] = this.jid.Server,
                           };
            }
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