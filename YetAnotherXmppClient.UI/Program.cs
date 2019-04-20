using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Serilog;
using Serilog.Filters;
using YetAnotherXmppClient.Persistence;
using YetAnotherXmppClient.UI.View;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI
{
    class Program
    {
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                //.Filter.ByIncludingOnly(Matching.WithProperty<bool>("IsXmppStreamContent", b => b))
                .WriteTo.TextWriter(MainWindowViewModel.LogWriter)
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Hour)
                .CreateLogger();

            using (var context = new DatabaseContext())
            {
                context.Database.EnsureCreated();
            }

            BuildAvaloniaApp().Start<MainWindow>(() => new MainWindowViewModel());
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
