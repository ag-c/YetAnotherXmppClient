using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using Splat;
using YetAnotherXmppClient.Infrastructure;
using YetAnotherXmppClient.Infrastructure.Events;
using YetAnotherXmppClient.UI.View;

namespace YetAnotherXmppClient.UI.ViewModel
{
    public class MainWindowViewModel : ReactiveObject, IScreen, IEventHandler<StreamNegotiationCompletedEvent>
    {
        private static MainWindowViewModel instance;
        public static DebugTextWriterDecorator LogWriter = new DebugTextWriterDecorator(new StringWriter(), _ => instance?.RaisePropertyChanged(nameof(LogText)));

        private XmppClient xmppClient = new XmppClient();
        public RoutingState Router { get; }

        public string LogText => LogWriter.Decoratee.ToString();


        public MainWindowViewModel()
        {
            instance = this;

            this.Router = new RoutingState();

            Locator.CurrentMutable.Register(() => new LoginView(), typeof(IViewFor<LoginViewModel>));
            Locator.CurrentMutable.Register(() => new MainView(), typeof(IViewFor<MainViewModel>));

            this.xmppClient.Disconnected += (sender, args) => this.NavigateToLoginView();
            //Observable.FromEventPattern(
            //    eh => this.xmppClient.Disconnected += eh,
            //    eh => this.xmppClient.Disconnected -= eh)
            //    .ObserveOn(RxApp.MainThreadScheduler)
            //    .Subscribe(_ => )

            this.xmppClient.RegisterHandler<StreamNegotiationCompletedEvent>(this, publishLatestEventToNewHandler: true);

            this.NavigateToLoginView();
        }

        private void NavigateToLoginView()
        {
            Dispatcher.UIThread.InvokeAsync(() => this.Router.Navigate.Execute(new LoginViewModel(this.xmppClient, LogWriter)));
        }

        private Task NavigateToMainViewAsync()
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var vm = new MainViewModel(this.xmppClient, LogWriter);
                    //vm.LogoutCommand.Subscribe(_ => Console.WriteLine(""));
                    vm.OnLogoutRequested = this.LogoutAsync;
                    this.Router.Navigate.Execute(vm);
                });
        }

        Task IEventHandler<StreamNegotiationCompletedEvent>.HandleEventAsync(StreamNegotiationCompletedEvent evt)
        {
            return this.NavigateToMainViewAsync();
        }

        private async Task LogoutAsync()
        {
            await this.xmppClient.ShutdownAsync();
            this.NavigateToLoginView();
        }
    }
}
