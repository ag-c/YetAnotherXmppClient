using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol.Handler;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
{
    public interface IFeatureOptionsProvider
    {
        Dictionary<string, string> GetOptions(XName featureName);
    }

    //public class FeatureOptionsProvider : IFeatureOptionsProvider
    //{
    //    public Dictionary<string, object> GetOptions(XName featureName)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //static class FeatureOptionsBuilder
    //{
    //    public static FeatureOptionsDictionary Build(Dictionary<string, string> configuration);
    //}
    public class MainProtocolHandler
    {
        private static readonly string Version = "1.0";
        private static readonly IEnumerable<string> Mechanisms = new[] {"PLAIN"};

        private readonly XmppStream xmppStream;

        private readonly IEnumerable<IFeatureProtocolNegotiator> featureNegotiators;
        private readonly IFeatureOptionsProvider featureOptionsProvider;
        //private readonly FeatureOptionsDictionary featureOptionsDict = new FeatureOptionsDictionary();

        readonly Dictionary<string, string> runtimeParameters = new Dictionary<string, string>();

        public event EventHandler<Exception> FatalErrorOccurred;
        public event EventHandler NegotiationFinished;

        public RosterProtocolHandler RosterHandler { get; }
        public PresenceProtocolHandler PresenceHandler { get; }
        public ImProtocolHandler ImProtocolHandler { get; }
        private MessageReceiptsProtocolHandler messageReceiptsHandler;


        public MainProtocolHandler(Stream serverStream, IFeatureOptionsProvider featureOptionsProvider)
        {
            this.featureOptionsProvider = featureOptionsProvider;
            this.xmppStream = new XmppStream(serverStream);

            this.RosterHandler = new RosterProtocolHandler(this.xmppStream, this.runtimeParameters);
            this.PresenceHandler = new PresenceProtocolHandler(this.xmppStream);
            this.ImProtocolHandler = new ImProtocolHandler(this.xmppStream, this.runtimeParameters);

            this.featureNegotiators = new IFeatureProtocolNegotiator[]
            {
                new StartTlsProtocolNegotiator(this.xmppStream), 
                new SaslFeatureProtocolNegotiator(this.xmppStream, Mechanisms),
                new BindProtocolNegotiator(this.xmppStream, this.runtimeParameters),
                //new ImProtocolHandler(serverStream),
            };
        }


        public async Task RunAsync(Jid jid, CancellationToken token)
        {
            try
            {
                await this.RestartStreamAsync(jid);

                var features = await this.xmppStream.ReadStreamFeaturesAsync();

                Log.Logger.StreamNegotiationStatus(features);

                if (await this.NegotiateFeaturesAsync(features, jid))
                {
                    // stream needs to be restarted after these features have been negotiated
                    await this.RunAsync(jid, token);
                    return;
                }

                Log.Debug("Stream negotiation is complete");

                await this.OnStreamNegotiationCompletedAsync();

                await this.xmppStream.RunReadLoopAsync(new CancellationTokenSource().Token);
            }
            catch (Exception e)
            {
                this.FatalErrorOccurred?.Invoke(this, e);
            }
        }

        // returns true if restart of the stream is required
        private async Task<bool> NegotiateFeaturesAsync(IEnumerable<Feature> features, Jid jid)
        {
            Feature feature;
            do
            {
                feature = this.SelectFeatureToNegotiateNext(features);

                await this.NegotiateFeatureAsync(feature);

                if (feature.IsStreamRestartRequired())
                {   // stream needs to be restarted after these features have been negotiated
                    return true;
                }
                features = features.Where(f => !f.Equals(feature));
            }
            while (feature != null);

            return false;
        }

        private Feature SelectFeatureToNegotiateNext(IEnumerable<Feature> features)
        {
            var mandatoryFeatures = features.Where(f => f.IsRequired);
            if (mandatoryFeatures.Any())
            {
                return mandatoryFeatures.First();
            }

            // Choose the first feature for which we have a handler
            return features.FirstOrDefault(f => this.featureNegotiators.Any(fh => fh.FeatureName == f.Name));
        }

        private async Task NegotiateFeatureAsync(Feature feature)
        {
            if (feature == null)
                return;

            var handler = this.featureNegotiators.FirstOrDefault(fh => fh.FeatureName == feature.Name);
            if (handler == null)
            {
                Log.Error($"Cannot negotiate feature '{feature.Name.LocalName}' - missing protocol handler");
                if (feature.IsRequired)
                {
                    throw new XmppException($"Unable to negotiate required feature '{feature.Name}' - no handler");
                }
                return;
            }

            Log.Debug($"Negotiating feature '{feature.Name}'..");

            var options = this.featureOptionsProvider.GetOptions(feature.Name);
            var success = await handler.NegotiateAsync(feature, options);
        }

        private async Task OnStreamNegotiationCompletedAsync()
        {
            // initial roster get & presence set
            var rosterItems = await this.RosterHandler.RequestRosterAsync();
            await this.PresenceHandler.BroadcastPresenceAsync();

            // create remaining protocol handlers
            var discoHandler = new ServiceDiscoveryProtocolHandler(this.xmppStream, this.runtimeParameters);
            var timeHandler = new EntityTimeProtocolHandler(this.xmppStream, runtimeParameters);
            var pingHandler = new PingProtocolHandler(this.xmppStream, this.runtimeParameters);
            var b = await pingHandler.PingAsync();

            var messageReceiptsHandler = new MessageReceiptsProtocolHandler(this.xmppStream, this.runtimeParameters);

            var pepHandler = new PepProtocolHandler(this.xmppStream, this.runtimeParameters);
            var y = await pepHandler.DetermineSupportAsync();

            var omemoHandler = new OmemoProtocolHandler(pepHandler, this.xmppStream, this.runtimeParameters);
            await omemoHandler.InitializeAsync();

            this.NegotiationFinished?.Invoke(this, EventArgs.Empty);
        }

        //4.2.Opening a Stream
        private async Task RestartStreamAsync(Jid fullJid)
        {
            Log.Verbose("Restarting stream..");

            //Streams neu aufsetzen, da der XmlReader sonst nicht mit ein evtuellen xml-deklaration klarkommen würde
            this.xmppStream.Reset();

            await this.xmppStream.WriteInitialStreamHeaderAsync(fullJid, Version);

            var attributes = await this.xmppStream.ReadResponseStreamHeaderAsync();

//            ValidateInitialStreamHeaderAttributes(attributes)
            Expect(() => attributes.ContainsKey("id"));
            //this.streamId = attributes["id"];

            //4.7.2. to //MUST verify the identity of the other entity
        }

        public async Task RegisterAsync(CancellationToken ct)
        {
            var jid = new Jid(Guid.NewGuid() + "@draugr.de/resource");

            await this.RestartStreamAsync(jid).ConfigureAwait(false);

            var features = await this.xmppStream.ReadStreamFeaturesAsync().ConfigureAwait(false);

            if (features.Any(f => f.Name == XNames.starttls))
            {
                bool b = await this.NegotiateFeaturesAsync(features, jid);
                await this.RestartStreamAsync(jid).ConfigureAwait(false);

                features = await this.xmppStream.ReadStreamFeaturesAsync().ConfigureAwait(false);
            }

            if (!features.Any(f => f.Name == XNames.register_register))
            {
                throw new NotExpectedProtocolException("no register feature", "register feature");
            }

            var resp = await this.xmppStream.WriteIqAndReadReponseAsync(new Iq(IqType.get, new XElement(XNames.register_query))
            {
                To = jid.Server
            }).ConfigureAwait(false);

            //UNDONE should never be true
            if (resp.Element(XNames.register_query)?.Element(XNames.register_registered) != null)
            {
                Log.Error("The entity is already registered.");
            }

            var x = resp.Element(XNames.register_query).Element(XNames.data_x);
            if (x != null)
            {

            }
        }

        public async Task TerminateSessionAsync()
        {
            await this.PresenceHandler.SendUnavailableAsync();

            await this.xmppStream.WriteClosingTagAsync("stream:stream");
        }
    }
}
