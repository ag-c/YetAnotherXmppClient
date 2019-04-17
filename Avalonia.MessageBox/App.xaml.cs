using Avalonia;
using Avalonia.Markup.Xaml;
using System;

namespace StarDebris.Avalonia.MessageBox
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
   }
}