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
using System.Xml.XPath;
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


            //yetanotherxmppuser@jabber.de
            //yetanotherxmppuser@wiuwiu.de
            //yetanotherxmppuser@sum7.eu
            //yetanotherxmppuser@xmpp.is
            var jid = new Jid("yetanotherxmppuser@jabber.de/DefaultResource");

            try
            {
                var xmppClient = new XmppClient();
                //await xmppClient.RegisterAsync("draugr.de");
                xmppClient.SubscriptionRequestReceived = requestingJid =>
                {
                    Debugger.Break();
                    return Task.FromResult(true);
                };
                //xmppClient.RosterUpdated += (sender, items) => Debugger.Break();
                xmppClient.MessageReceived += (chatSession, text) => Debugger.Break();
                await xmppClient.StartAsync(jid, "***");

                Console.ReadLine();
            }
            catch (NotExpectedProtocolException ex)
            {
                Log.Fatal($"{ex.ThrownBy}: Expected {ex.Expected} but got {ex.Actual}");
            }
            catch (Exception ex)
            {
                Log.Fatal($"Exception thrown: {ex}");
            }


            Console.ReadLine();
        }

    }
}
