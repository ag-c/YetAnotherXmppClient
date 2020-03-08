using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using YetAnotherXmppClient.Core.StanzaParts;

namespace YetAnotherXmppClient.UI.Converter
{
    public class PresenceShowToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PresenceShow show)
            {
                return show switch
                    {
                        PresenceShow.Other => new SolidColorBrush(Colors.Gray),
                        PresenceShow.away => new SolidColorBrush(Colors.DarkOrange),
                        PresenceShow.chat => new SolidColorBrush(Colors.Green),
                        PresenceShow.dnd => new SolidColorBrush(Colors.Magenta),
                        PresenceShow.xa => new SolidColorBrush(Colors.Red),
                        _ => new SolidColorBrush(Colors.Gray)
                    };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
