using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace YetAnotherXmppClient.UI.Converter
{
    public class IsOnlineBooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOnline)
            {
                return isOnline ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
