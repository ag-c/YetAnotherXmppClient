using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using YetAnotherXmppClient;
using YetAnotherXmppClient.Core;
using YetAnotherXmppClient.Extensions;
using YetAnotherXmppClient.Protocol;
using static YetAnotherXmppClient.Expectation;
using FeatureOptions = System.Collections.Generic.Dictionary<string, string>;
//using FeatureOptionsDictionary = System.Collections.Generic.Dictionary<System.Xml.Linq.XName, System.Collections.Generic.Dictionary<string, string>>;

namespace YetAnotherXmppClient
{
    public interface IFeatureOptionsProvider
    {
        Dictionary<string, string> GetOptions(XName featureName);
    }

    interface IFeatureProtocolHandler
    {
        XName FeatureName { get; }
        Task<bool> NegotiateAsync(Feature feature, FeatureOptions options);
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
    public class MainProtocolHandler : ProtocolHandlerBase
    {
        private static readonly string Version = "1.0";
        private static readonly IEnumerable<string> Mechanisms = new[] {"PLAIN"};

        private readonly IEnumerable<IFeatureProtocolHandler> featureHandlers;
        private readonly IFeatureOptionsProvider featureOptionsProvider;
        //private readonly FeatureOptionsDictionary featureOptionsDict = new FeatureOptionsDictionary();

        readonly Dictionary<string, string> runtimeParameters = new Dictionary<string, string>();

        private string streamId;
        
        public event EventHandler<Exception> FatalErrorOccurred;
        public event EventHandler NegotiationFinished;

        //---
        private readonly AsyncXmppStream xmppServerStream;

        public RosterProtocolHandler RosterHandler { get; }
        public PresenceProtocolHandler PresenceHandler { get; }
        public ImProtocolHandler ImProtocolHandler { get; }


        public MainProtocolHandler(Stream serverStream, IFeatureOptionsProvider featureOptionsProvider) : base(serverStream)
        {
            this.featureOptionsProvider = featureOptionsProvider;
            this.xmppServerStream = new AsyncXmppStream(serverStream);

            this.RosterHandler = new RosterProtocolHandler(this.xmppServerStream, runtimeParameters);
            this.PresenceHandler = new PresenceProtocolHandler(this.xmppServerStream);
            this.ImProtocolHandler = new ImProtocolHandler(this.xmppServerStream, this.runtimeParameters);
        }


        public async Task RunAsync(Jid jid)
        {
            try
            {
                await this.RestartStreamAsync(jid);
                
                var features = await this.ReadStreamFeaturesAsync();
                var isStreamNegotiationComplete = features.All(f => !f.IsRequired);

                Log.Logger.StreamNegotiationStatus(features);
               
                var mandatoryFeatures = features.Where(f => f.IsRequired);
                if (mandatoryFeatures.Any())
                {
                    if (features.Any(f => f.Name.LocalName == "starttls"))
                    {
                        await NegotiateTlsAsync(jid);

                        await this.RunAsync(jid);
                        return;
                    }
                    
                    var feature = mandatoryFeatures.First();
                    if (feature.Name == XNames.bind_bind)
                    {
                        var bindHandler = new BindProtocolHandler(this.xmppServerStream/*this.serverStream*/, runtimeParameters);

                        await bindHandler.NegotiateAsync(feature, this.featureOptionsProvider.GetOptions(XNames.bind_bind));

                        //var x = await ReadElementFromStreamAsync();

                        if (features.Any(f => f.Name == XNames.session_session))
                        {
                            var rosterItems = await this.RosterHandler.RequestRosterAsync();

//                            await RosterHandler.AddRosterItemAsync("agg1n@jabber.ccc.de", "agg1nccc", new[]{"Ichgruppe2"});

//                            rosterItems = await RosterHandler.RequestRosterAsync();

////                            rosterHandler.UpdateRosterItemAsync()

//                            await RosterHandler.DeleteRosterItemAsync("jf@draugr.de");

//                            rosterItems = await RosterHandler.RequestRosterAsync();


                            //var x = await ReadElementFromStreamAsync();


                            await PresenceHandler.BroadcastPresenceAsync();
                            await PresenceHandler.RequestSubscriptionAsync("jf@draugr.de");



                            this.xmppServerStream.StartAsyncReadLoop();

                            //var imHandler = new ImProtocolHandler(this.xmppServerStream/*this.serverStream*/, runtimeParameters);
                            //await imHandler.EstablishSessionAsync();

                            //await imHandler.SendMessage("agg1n@jabber.ccc.de", "test2");
                        }

                        if (features.Any(f => f.Name == XNames.rosterver_ver))
                        {

                        }
                    }
//                    if(feature)
                }
                else
                {
                    //MAY negotiate voluntary features

                    if (features.Any(f => f is MechanismsFeature))
                    {
                        var saslHandler = new SaslFeatureProtocolHandler(this.serverStream, Mechanisms);
                        
                        var mechanismsFeature = features.OfType<MechanismsFeature>().First();

                        await saslHandler.Handle(mechanismsFeature);

                        await this.RunAsync(jid);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                this.FatalErrorOccurred?.Invoke(this, e);
            }
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
            Log.Logger.Verbose("Restarting stream..");
            //Streams neu aufsetzen, da der XmlReader sonst nicht mit ein evtuellen xml-deklaration klarkommen würde
            this.RecreateStreams(this.serverStream);
            this.xmppServerStream.Reinitialize(this.serverStream);

            await this.WriteInitialStreamHeaderAsync(jid);

            var attributes = await this.ReadResponseStreamHeaderAsync();
            //foreach(var attr in attributes.Where(kvp => kvp.Key.StartsWith("xmlns:")))
            //    namespaces.Add(attr.Key, attr);
//            ValidateInitialStreamHeaderAttributes(attributes)
            Expect(() => attributes.ContainsKey("id"));
            this.streamId = attributes["id"];

            //4.7.2. to //MUST verify the identity of the other entity
        }

        private async Task NegotiateTlsAsync(Jid jid)
        {
            await StartTLSFeatureHandler.BeginNegotiationAsync(this.textWriter);
            var xElem = await this.ReadElementFromStreamAsync();
            if (xElem.Name == XNames.failure)
            {
                //UNDONE If the failure case occurs, the initiating entity MAY attempt to
                //reconnect as explained under Section 3.3.
                Log.Logger.Fatal($"Error: Reply to 'starttls' was '{xElem.Name}'");
                throw new NotExpectedProtocolException(xElem.Name.ToString(), XNames.proceed.ToString());
            }
            else if (xElem.Name == XNames.proceed)
            {
                var sslStream = new SslStream(this.serverStream, false, this.UserCertificateValidationCallback);

                await sslStream.AuthenticateAsClientAsync(jid.Server);
                //UNDONE 5.4.3.3. TLS Success

                this.serverStream = sslStream;
                this.RecreateStreams(sslStream);
                this.xmppServerStream.Reinitialize(sslStream);
            }
            else
            {
                //UNDONE auf localname=="proceed" prüfen
                throw new NotExpectedProtocolException(xElem.Name.ToString(), "proceed");
            }
        }

        private bool UserCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }


        private async Task WriteInitialStreamHeaderAsync(Jid jid)
        {
            Log.Logger.Debug("Writing intial stream header..");

            using (var xmlWriter = XmlWriter.Create(textWriter, new XmlWriterSettings { Async = true, WriteEndDocumentOnClose = false }))
            {
                await xmlWriter.WriteStartDocumentAsync();
                await xmlWriter.WriteStartElementAsync("stream", "stream", "http://etherx.jabber.org/streams");
                await xmlWriter.WriteAttributeStringAsync("", "from", null, jid);
                await xmlWriter.WriteAttributeStringAsync("", "to", null, jid.Server);
                await xmlWriter.WriteAttributeStringAsync("", "version", null, Version);
                await xmlWriter.WriteAttributeStringAsync("xml", "lang", null, "en");
                await xmlWriter.WriteAttributeStringAsync("xmlns", "", null, "jabber:client");
            }
        }

        private async Task<Dictionary<string, string>> ReadResponseStreamHeaderAsync()
        {
            Log.Logger.Debug("Reading response stream header..");

            //var openingTag = await this.xmlReader.ReadOpeningTagAsync();

            //Expect("stream:stream", actual: openingTag.Name);

            //return openingTag.Attributes;

            while (xmlReader.NodeType == XmlNodeType.EndElement)
                await xmlReader.ReadAsync();

            await this.xmlReader.MoveToContentAsync();
            Expect("stream:stream", actual: this.xmlReader.Name);

            return await this.xmlReader.GetAllAttributesAsync();
        }

        //4.3.2. Stream Features Format
        private async Task<IEnumerable<Feature>> ReadStreamFeaturesAsync()
        {
            Log.Logger.Debug("Reading stream features..");

            //var xElem = await this.ReadElementFromStreamAsync();

            //Expect("stream:features", actual: xElem.Name.ToString(), context: xElem);


            //Expect("stream:features", actual: this.xmlReader.Name);
            var xElem = await this.ReadElementFromStreamAsync();
            Expect("features", actual: xElem.Name.LocalName);
            return Features.FromXElement(xElem);
        }

        private async Task<XElement> ReadElementFromStreamAsync()
        {
            var xElem = await this.xmlReader.ReadNextElementAsync();
            //var xmlFragment = await this.xmlReader.ReadElementOrClosingTagAsync();

            //Expect(() => xmlFragment.PartType == XmlPartType.Element);

            //var xElem = XElement.Parse(xmlFragment.RawXml);

            //Log.Logger.Verbose("Read element from stream: " + xElem);

            return xElem;
        }

        private async Task TerminateSessionAsync()
        {
            await this.PresenceHandler.SendUnavailableAsync();

            await this.xmppServerStream.WriteAsync("</stream:stream");
        }
    }
}
