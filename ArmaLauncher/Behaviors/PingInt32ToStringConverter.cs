using System;
using System.Globalization;
using System.Windows.Data;

namespace ArmaLauncher.Behaviors
{
    public class PingInt32ToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "*";

            string returnValue;

            if(System.Convert.ToInt32(value) == 88888)
                returnValue = "MaxPing";
            else if(System.Convert.ToInt32(value) == 99999)
                returnValue = "Timeout";
            else
                returnValue = System.Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture);

            return returnValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}