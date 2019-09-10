using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Remotion.Linq.Clauses;
using Serilog;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Core.Stanza;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.Protocol.Handler;
using YetAnotherXmppClient.Protocol.Negotiator;
using static YetAnotherXmppClient.Expectation;

namespace YetAnotherXmppClient.Protocol
{
    public interface IFeatureOptionsProvider
    {
        Dictionary<string, string> GetOptions(XName featureName);
    }

    public class MainProtocolHandler : IDisposable
    {
        private static readonly string Version = "1.0";
        private static readonly IEnumerable<string> Mechanisms = new[] {"PLAIN"};

        private readonly XmppStream xmppStream;

        private readonly IEnumerable<IFeatureProtocolNegotiator> featureNegotiators;
        private readonly IFeatureOptionsProvider featureOptionsProvider;

        private readonly IMediator mediator;
        //private readonly FeatureOptionsDictionary featureOptionsDict = new FeatureOptionsDictionary();

        readonly Dictionary<string, string> runtimeParameters = new Dictionary<string, string>();

        public bool IsNegotiationFinished { get; private set; }

        public event EventHandler<Exception> FatalErrorOccurred;

        private static IEnumerable<Type> ProtocolHandlerTypes = new[]
                                                                    {
                                                                        typeof(RosterProtocolHandler),
                                                                        typeof(PresenceProtocolHandler),
                                                                        typeof(ImProtocolHandler),
                                                                        typeof(ServiceDiscoveryProtocolHandler),
                                                                        typeof(EntityTimeProtocolHandler),
                                                                        typeof(PingProtocolHandler),
                                                                        typeof(MessageReceiptsProtocolHandler),
                                                                        typeof(PepProtocolHandler),
                                                                        typeof(VCardProtocolHandler),
                                                                        typeof(SoftwareVersionProtocolHandler),
                                                                        typeof(BlockingProtocolHandler)
                                                                    };
        private readonly Dictionary<Type, ProtocolHandlerBase> protocolHandlers = new Dictionary<Type, ProtocolHandlerBase>();


        public MainProtocolHandler(Stream serverStream, IFeatureOptionsProvider featureOptionsProvider, IMediator mediator)
        {
            this.featureOptionsProvider = featureOptionsProvider;
            this.mediator = mediator;
            this.xmppStream = new XmppStream(serverStream);

            // create protocol handlers
            foreach (var type in ProtocolHandlerTypes)
            {
                var instance = (ProtocolHandlerBase)Activator.CreateInstance(type, this.xmppStream, this.runtimeParameters, this.mediator);
                this.protocolHandlers.Add(type, instance);
            }

            this.featureNegotiators = new IFeatureProtocolNegotiator[]
            {
                new StartTlsProtocolNegotiator(this.xmppStream), 
                new SaslFeatureProtocolNegotiator(this.xmppStream, Mechanisms, this.mediator),
                new BindProtocolNegotiator(this.xmppStream, this.runtimeParameters),
                new Rfc3921SessionProtocolNegotiator(this.xmppStream, this.runtimeParameters)
            };
        }

        public T Get<T>() where T : class
        {
            if(this.protocolHandlers.TryGetValue(typeof(T), out var protoHandler))
                return (T)(object)protoHandler;

            return (T)this.featureNegotiators.First(fn => fn is T);
        }

        public async Task RunAsync(Jid jid, CancellationToken ct)
        {
            try
            {
                await this.RestartStreamAsync(jid);

                var features = await this.xmppStream.ReadStreamFeaturesAsync();

                Log.Logger.StreamNegotiationStatus(features);

                if (await this.NegotiateFeaturesAsync(features, jid))
                {
                    // stream needs to be restarted after these features have been negotiated
                    await this.RunAsync(jid, ct);
                    return;
                }

                await this.OnStreamNegotiationCompletedAsync();

                await this.xmppStream.RunReadLoopAsync(ct);
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
                feature = this.SelectFeatureToNegotiate(features);

                await this.NegotiateFeatureAsync(feature);

                if (feature.IsStreamRestartRequired())
                {   // stream needs to be restarted after these features have been negotiated
                    return true;
                }
                features = features.Without(feature);
            }
            while (feature != null);

            return false;
        }

        private Feature SelectFeatureToNegotiate(IEnumerable<Feature> features)
        {
            // any mandatory feature?
            var mandatoryFeature = features.FirstOrDefault(f => f.IsRequired && !this.featureNegotiators.IsNegotiated(f));

            // Choose the first feature for which we have a handler
            return mandatoryFeature ?? features.FirstOrDefault(this.featureNegotiators.IsNegotiable);
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
            Log.Debug("Stream negotiation is complete");

            // initial roster get & presence set
            var rosterItems = await this.Get<RosterProtocolHandler>().RequestRosterAsync();
            await this.Get<PresenceProtocolHandler>().BroadcastPresenceAsync();


            var b = await this.Get<PingProtocolHandler>().PingAsync();

            var omemoHandler = new OmemoProtocolHandler(this.Get<PepProtocolHandler>(), this.xmppStream, this.runtimeParameters);
            await omemoHandler.InitializeAsync();

            this.IsNegotiationFinished = true;
            await this.mediator.PublishAsync(new StreamNegotiationCompletedEvent { ConnectedJid = this.runtimeParameters["jid"] });
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
            await this.Get<PresenceProtocolHandler>().SendUnavailableAsync();

            await this.xmppStream.WriteClosingTagAsync("stream:stream");
        }

        public void Dispose()
        {
            this.xmppStream.Dispose();
        }
    }

    static class ProtocolNegotiatorsExtensions
    {
        public static bool IsNegotiated(this IEnumerable<IFeatureProtocolNegotiator> negotiators, Feature feature)
        {
            return negotiators.FirstOrDefault(n => n.FeatureName == feature.Name)?.IsNegotiated ?? false;
        }

        public static bool IsNegotiable(this IEnumerable<IFeatureProtocolNegotiator> negotiators, Feature feature)
        {
            return negotiators.Any(fn => fn.FeatureName == feature.Name && !fn.IsNegotiated);
        }
    }
}
