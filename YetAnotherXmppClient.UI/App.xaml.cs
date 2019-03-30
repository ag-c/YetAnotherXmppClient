using Avalonia;
using Avalonia.Markup.Xaml;

namespace YetAnotherXmppClient.UI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
