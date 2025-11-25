using System;
using System.Globalization;
using System.Windows.Data;

namespace LibraLibraryManagementSystem.Controls
{
    public class FontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double height = (double)value;
            double factor = System.Convert.ToDouble(parameter);
            return Math.Max(12, height * factor); // minimum font size of 12
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
