using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EyeFocus.UI.Converters
{
    public class IconKeyToGeometryConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string key && !string.IsNullOrEmpty(key))
            {
                if (System.Windows.Application.Current?.TryFindResource(key) is Geometry geom)
                {
                    return geom;
                }
            }

            return System.Windows.Application.Current?.TryFindResource("IconEye") as Geometry;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
