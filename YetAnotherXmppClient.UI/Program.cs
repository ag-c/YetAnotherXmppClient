using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using Serilog;

namespace YetAnotherXmppClient.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            //var vm = new MainViewModel();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TextWriter(MainViewModel.stringWriter)
                .CreateLogger();

            BuildAvaloniaApp().Start<MainWindow>(() => new MainViewModel());
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                //.UseWin32()
                //.UseAvaloniaModules()
                .LogToDebug();
    }
}
