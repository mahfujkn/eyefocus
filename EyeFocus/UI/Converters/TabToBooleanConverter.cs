using System;
using System.Globalization;
using System.Windows.Data;

namespace EyeFocus.UI.Converters
{
    public class TabToBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string currentTab && parameter is string targetTab)
            {
                return string.Equals(currentTab, targetTab, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string targetTab)
            {
                return targetTab;
            }
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
