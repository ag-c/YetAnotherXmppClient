using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml;
using Serilog;

namespace YetAnotherXmppClient.UI
{
    public class App : Application
    {
        public override void Initialize()
        {
            //SerilogLogger.Initialize(new LoggerConfiguration()
            //    .MinimumLevel.Verbose()
            //    .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
            //    .CreateLogger());
            AvaloniaXamlLoader.Load(this);
        }
    }
}
