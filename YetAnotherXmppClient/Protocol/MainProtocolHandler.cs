using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;
using YetAnotherXmppClient.Core;

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

        private readonly AsyncXmppStream xmppStream;

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
            this.xmppStream = new AsyncXmppStream(serverStream);

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

                var features = await this.ReadStreamFeaturesAsync();

                Log.Logger.StreamNegotiationStatus(features);

                Feature feature;
                do
                {
                    feature = this.ChooseFeatureToNegotiateNext(features);

                    await this.NegotiateFeatureAsync(feature);

                    if (feature?.Name == XNames.starttls || feature?.Name == XNames.sasl_mechanisms)
                    {   // stream needs to be restarted after these features have been negotiated
                        await this.RunAsync(jid, token);
                        return;
                    }
                    features = features.Where(f => !f.Equals(feature));
                }
                while (feature != null);

                Log.Debug("Stream negotiation is complete");

                // initial roster get & presence set
                var rosterItems = await this.RosterHandler.RequestRosterAsync();
                await this.PresenceHandler.BroadcastPresenceAsync();

                this.NegotiationFinished?.Invoke(this, EventArgs.Empty);

                var pepHandler = new PepProtocolHandler(this.xmppStream, this.runtimeParameters);
                var y = await pepHandler.DetermineSupportAsync();

                await this.xmppStream.RunLoopAsync(new CancellationTokenSource().Token);


                //var mandatoryFeatures = features.Where(f => f.IsRequired);
                //if (mandatoryFeatures.Any())
                //{
                //    if (features.Any(f => f.Name.LocalName == "starttls"))
                //    {
                //        var starttlsHandler = new StartTlsProtocolHandler(this.xmppStream);
                //        await starttlsHandler.NegotiateAsync(new Feature()/*UNDONE*/, this.featureOptionsProvider.GetOptions(XNames.starttls));

                //        await this.RunAsync(jid, token);
                //        return;
                //    }

                //    var feature = mandatoryFeatures.First();
                //    if (features.Any(f => f is MechanismsFeature))
                //    {
                //        var saslHandler = new SaslFeatureProtocolHandler(this.xmppStream, Mechanisms);

                //        var mechanismsFeature = features.OfType<MechanismsFeature>().First();

                //        await saslHandler.NegotiateAsync(mechanismsFeature, this.featureOptionsProvider.GetOptions(XNames.sasl_mechanisms));

                //        await this.RunAsync(jid, token);
                //        return;
                //    }
                //    if (feature.Name == XNames.bind_bind)
                //    {
                //        var bindHandler = new BindProtocolHandler(this.xmppStream/*this.serverStream*/, runtimeParameters);

                //        await bindHandler.NegotiateAsync(feature, this.featureOptionsProvider.GetOptions(XNames.bind_bind));

                //        //var x = await ReadElementFromStreamAsync();

                //        if (features.Any(f => f.Name == XNames.session_session))
                //        {
                //            var pepHandler = new PepProtocolHandler(this.xmppStream, this.runtimeParameters);
                //            var y = await pepHandler.DetermineSupportAsync();


                //            var rosterItems = await this.RosterHandler.RequestRosterAsync();

                //            //                            await RosterHandler.AddRosterItemAsync("agg1n@jabber.ccc.de", "agg1nccc", new[]{"Ichgruppe2"});

                //            //                            rosterItems = await RosterHandler.RequestRosterAsync();

                //            ////                            rosterHandler.UpdateRosterItemAsync()

                //            //                            await RosterHandler.DeleteRosterItemAsync("jf@draugr.de");

                //            //                            rosterItems = await RosterHandler.RequestRosterAsync();


                //            //var x = await ReadElementFromStreamAsync();


                //            await PresenceHandler.BroadcastPresenceAsync();
                //            await PresenceHandler.RequestSubscriptionAsync("jf@draugr.de");



                //            //this.xmppServerStream.StartAsyncReadLoop();
                //            await this.xmppStream.RunLoopAsync(new CancellationTokenSource().Token);

                //            //var imHandler = new ImProtocolHandler(this.xmppServerStream/*this.serverStream*/, runtimeParameters);
                //            //await imHandler.EstablishSessionAsync();

                //            //await imHandler.SendMessage("agg1n@jabber.ccc.de", "test2");
                //        }

                //        if (features.Any(f => f.Name == XNames.rosterver_ver))
                //        {

                //        }
                //    }
                //    //                    if(feature)
                //}
                //else
                //{
                //    //MAY negotiate voluntary features

                //    //if (features.Any(f => f is MechanismsFeature))
                //    //{
                //    //    var saslHandler = new SaslFeatureProtocolHandler(this.serverStream, Mechanisms);

                //    //    var mechanismsFeature = features.OfType<MechanismsFeature>().First();

                //    //    await saslHandler.NegotiateAsync(mechanismsFeature, this.featureOptionsProvider.GetOptions(XNames.sasl_mechanisms));

                //    //    await this.RunAsync(jid, token);
                //    //    return;
                //    //}
                //}
            }
            catch (Exception e)
            {
                this.FatalErrorOccurred?.Invoke(this, e);
            }
        }

        private Feature ChooseFeatureToNegotiateNext(IEnumerable<Feature> features)
        {
            //var startTlsFeature = features.FirstOrDefault(f => f.Name == XNames.starttls);
            //if (startTlsFeature != null)
            //{
            //    return startTlsFeature;
            //}

            var mandatoryFeatures = features.Where(f => f.IsRequired);
            if (mandatoryFeatures.Any())
            {
                return mandatoryFeatures.First();
            }

            if (features.Any(f => f is MechanismsFeature))
            {
                //UNDONE 6.4.1.
                return features.OfType<MechanismsFeature>().First();
            }

            //                var notNegotiatedFeatureNames = this.featureHandlers.Where(fh => !fh.IsSuccessfullyNegotiated).Select(fh => fh.FeatureName);
            //                var featureNameToNegotiate = features.Select(f => f.Name).Intersect(notNegotiatedFeatureNames).FirstOrDefault();
            //                return features.First(f => f.Name == featureNameToNegotiate);

            // Choose the first feature for which we have a handler
            return features.FirstOrDefault(f => this.featureNegotiators.Any(fh => fh.FeatureName == f.Name));
        }

        private async Task NegotiateFeatureAsync(Feature feature)
        {
            var handler = this.featureNegotiators.FirstOrDefault(fh => fh.FeatureName == feature.Name);
            if (handler == null)
            {
                Log.Logger.Error($"Cannot negotiate feature '{feature.Name.LocalName}' - missing protocol handler");
                if (feature.IsRequired)
                {
                    throw new XmppException($"Unable to negotiate required feature '{feature.Name}' - no handler");
                }
                return;
            }

            var options = this.featureOptionsProvider.GetOptions(feature.Name);
            var success = await handler.NegotiateAsync(feature, options);
        }

        //private async Task NegotiateFeature(XName name)
        //{
        //    var handler = this.featureHandlers.FirstOrDefault(fh => fh.Name == name);
        //    if (handler == null)
        //    {
        //        //TOLOG
        //        return;
        //    }

        //    var options = this.featureOptionsProvider.GetOptions(name);

        //    var success = await featureProtocolHandler.NegotiateAsync(options);
        //}


        Dictionary<string, string> namespaces = new Dictionary<string, string>();
        //4.2.Opening a Stream
        private async Task RestartStreamAsync(Jid jid)
        {
            Log.Verbose("Restarting stream..");
            //Streams neu aufsetzen, da der XmlReader sonst nicht mit ein evtuellen xml-deklaration klarkommen würde
            this.xmppStream.Reinitialize(this.xmppStream.BaseStream);

            await this.xmppStream.WriteInitialStreamHeaderAsync(jid, Version);

            var attributes = await this.xmppStream.ReadResponseStreamHeaderAsync();
            //foreach(var attr in attributes.Where(kvp => kvp.Key.StartsWith("xmlns:")))
            //    namespaces.Add(attr.Key, attr);
//            ValidateInitialStreamHeaderAttributes(attributes)
            Expectation.Expect(() => attributes.ContainsKey("id"));
            //this.streamId = attributes["id"];

            //4.7.2. to //MUST verify the identity of the other entity
        }

        //4.3.2. Stream Features Format
        private async Task<IEnumerable<Feature>> ReadStreamFeaturesAsync()
        {
            Log.Debug("Reading stream features..");

            //var xElem = await this.ReadElementFromStreamAsync();

            //Expect("stream:features", actual: xElem.Name.ToString(), context: xElem);


            //Expect("stream:features", actual: this.xmlReader.Name);
            var xElem = await this.xmppStream.ReadElementAsync();
            Expectation.Expect("features", actual: xElem.Name.LocalName);
            return Features.FromXElement(xElem);
        }


        public async Task TerminateSessionAsync()
        {
            await this.PresenceHandler.SendUnavailableAsync();

            await this.xmppStream.WriteAsync("</stream:stream");
        }
    }
}
