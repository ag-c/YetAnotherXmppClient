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
            var vm = new MainViewModel();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.TextWriter(vm.stringWriter)
                .CreateLogger();

            BuildAvaloniaApp().Start<MainWindow>(() => vm);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();
    }
}
