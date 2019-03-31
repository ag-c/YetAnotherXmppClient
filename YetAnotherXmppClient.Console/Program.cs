using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Serilog.Core;
using YetAnotherXmppClient.Core;

namespace YetAnotherXmppClient.Console
{
    using Serilog;
    using System;
    using System.Net.Sockets;
    using System.Xml.Linq;


    class Program
    {
        static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();


            //var strWriter = new StringWriter();
            //using (var writer = XmlWriter.Create(strWriter, new XmlWriterSettings { Async = true, WriteEndDocumentOnClose = false}))
            //{
            //    await writer.WriteStartDocumentAsync();
            //    await writer.WriteStartElementAsync("stream", "stream", "http://etherx.jabber.org/streams");
            //    await writer.WriteAttributeStringAsync("", "from", null, jid);
            //    await writer.WriteAttributeStringAsync("", "to", null, jid.Server);
            //    await writer.WriteAttributeStringAsync("", "version", null, "1.0");
            //    await writer.WriteAttributeStringAsync("xml", "lang", null, "en");
            //    await writer.WriteAttributeStringAsync("xmlns", "", null, "jabber:client");

            //    //                await writer.WriteEndDocumentAsync();
            //                    await writer.FlushAsync();
            //}

            //var initialStreamHeader = await ProtocolHandler.GenerateInitialStreamHeaderAsync(jid);


            //yetanotherxmppuser@jabber.de
            //yetanotherxmppuser@wiuwiu.de
            var jid = new Jid("yetanotherxmppuser@wiuwiu.de/DefaultResource");

            try
            {
                var xmppClient = new XmppClient();
                xmppClient.OnSubscriptionRequestReceived = requestingJid =>
                {
                    Debugger.Break();
                    return true;
                };
                //xmppClient.RosterUpdated += (sender, items) => Debugger.Break();
                xmppClient.OnMessageReceived += (senderJid, text) => Debugger.Break();
                await xmppClient.StartAsync(jid, "gehe1m");

                Console.ReadLine();
            }
            catch (NotExpectedProtocolException ex)
            {
                Log.Logger.Fatal($"{ex.ThrownBy}: Expected {ex.Expected} but got {ex.Actual}");
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal($"Exception thrown: {ex}");
            }


            Console.ReadLine();
        }

    }
}
