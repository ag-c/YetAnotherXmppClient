using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using Serilog;
using Serilog.Filters;

namespace YetAnotherXmppClient.UI
{
    class Program
    {
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Filter.ByIncludingOnly(Matching.WithProperty<bool>("IsXmppStreamContent", b => b))
                .WriteTo.TextWriter(MainViewModel.stringWriter)
                .CreateLogger();

            BuildAvaloniaApp().Start<MainWindow>(() => new MainViewModel());
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                //.UseWin32()
                //.UseAvaloniaModules()
                .UseReactiveUI()
                .LogToDebug();
    }
}
