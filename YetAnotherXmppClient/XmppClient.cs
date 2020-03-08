using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Commands;
using YetAnotherXmppClient.Infrastructure.Queries;
using YetAnotherXmppClient.Protocol;
using YetAnotherXmppClient.Protocol.Handler;
using YetAnotherXmppClient.Protocol.Handler.ServiceDiscovery;

namespace YetAnotherXmppClient
{
    public class XmppClient : Mediator, IFeatureOptionsProvider, IQueryHandler<GetPreferenceValueQuery, object>, ICommandHandler<SetPreferenceValueCommand>
    {
        public static readonly int DefaultPort = 5222;

        private CancellationTokenSource cancelTokenSource;
        private TcpClient tcpClient;

        private Jid jid;
        private string password;

        private MainProtocolHandler ProtocolHandler { get; /*private*/ set; }

        public event EventHandler Disconnected;


        public XmppClient()
        {
            this.RegisterHandler<GetPreferenceValueQuery, object>(this);
            this.RegisterHandler<SetPreferenceValueCommand>(this);
        }

        public async Task StartAsync(Jid jid, string password)
        {
            // extend to full jid if resource has not been provided
            if (!jid.IsFull)
                jid = new Jid(jid, Guid.NewGuid().ToString());

            this.jid = jid;
            this.password = password;
            this.tcpClient = new TcpClient();
            this.cancelTokenSource = new CancellationTokenSource();

            Log.Information($"Connecting to {jid.Server}:{DefaultPort}..");
            
            await this.tcpClient.ConnectAsync(jid.Server, DefaultPort).ConfigureAwait(false);
            
            Log.Information($"Connection established");

            this.ProtocolHandler = new MainProtocolHandler(this.tcpClient.GetStream(), this, this);
            this.ProtocolHandler.FatalErrorOccurred += this.HandleFatalProtocolErrorOccurred;

            Task.Run(() => this.ProtocolHandler.RunAsync(jid, this.cancelTokenSource.Token).ContinueWith(_ => this.HandleProtocolHandlingEnded()));
        }

        public Task<bool> IsFeatureSupportedAsync(string name)
        {
            var handler = this.ProtocolHandler?.Get<ServiceDiscoveryProtocolHandler>();
            return handler?.IsFeatureSupportedAsync(name);
        }

        public async Task RegisterAsync(string server)
        {
            this.jid = new Jid($"unknown@{server}/resource");
            this.tcpClient = new TcpClient();

            Log.Information($"Connecting to {server}:{DefaultPort}..");

            await this.tcpClient.ConnectAsync(server, DefaultPort).ConfigureAwait(false);

            Log.Information($"Connection established");

            this.ProtocolHandler = new MainProtocolHandler(this.tcpClient.GetStream(), this, this);

            await this.ProtocolHandler.RegisterAsync(new CancellationTokenSource().Token).ConfigureAwait(false);
        }

        public async Task ShutdownAsync()
        {
            await this.ProtocolHandler.TerminateSessionAsync().ConfigureAwait(false);
            this.cancelTokenSource.Cancel(false);
            this.ProtocolHandler.Dispose();
            this.ProtocolHandler = null;
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

            this.ProtocolHandler?.Dispose();
            this.ProtocolHandler = null;

            this.Disconnected?.Invoke(this, EventArgs.Empty);
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

        private Dictionary<string, object> preferences = new Dictionary<string, object>
                                                             {
                                                                 ["SendChatStateNotifications"] = true
                                                             };
        object IQueryHandler<GetPreferenceValueQuery, object>.HandleQuery(GetPreferenceValueQuery query)
        {
            if (this.preferences.ContainsKey(query.PreferenceName))
                return this.preferences[query.PreferenceName];

            return query.DefaultValue;
        }

        void ICommandHandler<SetPreferenceValueCommand>.HandleCommand(SetPreferenceValueCommand command)
        {
            this.preferences[command.PreferenceName] = command.Value;
        }
    }
}