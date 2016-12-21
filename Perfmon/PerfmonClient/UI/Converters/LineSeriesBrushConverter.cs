using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PerfmonClient.UI.Converters
{
    public class LineSeriesBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var strokeBrush = (SolidColorBrush) values[0];
            return strokeBrush.Color;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var color = (Color) value;

            SolidColorBrush strokeBrush = new SolidColorBrush(color);
            strokeBrush.Freeze();
            SolidColorBrush fillBrush = new SolidColorBrush(color);
            fillBrush.Opacity = 0.15;
            fillBrush.Freeze();

            return new object[] { strokeBrush, fillBrush };
        }
    }
}
