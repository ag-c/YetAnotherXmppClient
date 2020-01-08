using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ReactiveUI;
using YetAnotherXmppClient.UI.ViewModel;

namespace YetAnotherXmppClient.UI.View
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public static MainWindow Instance { get; private set; }

        public RoutedViewHost RoutedViewHost => this.FindControl<RoutedViewHost>("routedViewHost");
        public ScrollViewer LogScrollViewer => this.FindControl<ScrollViewer>("logScrollViewer");


        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.ViewModel = new MainWindowViewModel();
            this.WhenActivated(d =>
                {
                    d(this.OneWayBind(this.ViewModel, x => x.Router, x => x.RoutedViewHost.Router));
                    this.ViewModel.WhenAnyValue(vm => vm.LogText)
                        .Subscribe(async _ => await Dispatcher.UIThread.InvokeAsync(() => this.LogScrollViewer.ScrollToEnd()));
                });
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    static class ScrollViewerExtensions
    {
        public static void ScrollToEnd(this ScrollViewer scrollViewer)
        {
            scrollViewer.Offset = new Vector(0, scrollViewer.Extent.Height);
        }
    }
}
