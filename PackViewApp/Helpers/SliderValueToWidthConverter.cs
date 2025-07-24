using System;
using System.Globalization;
using System.Windows.Data;

namespace PackViewApp.Helpers
{
    public class SliderValueToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                // Slider max assumed 100, adjust if needed
                return val * 4; // since slider width is 400, 4 * value gives pixel width
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}