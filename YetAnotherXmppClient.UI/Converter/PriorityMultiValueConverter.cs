using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

namespace YetAnotherXmppClient.UI.Converter
{
    public class PriorityMultiValueConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.FirstOrDefault(o => o != null && o != AvaloniaProperty.UnsetValue);
        }
    }
}
