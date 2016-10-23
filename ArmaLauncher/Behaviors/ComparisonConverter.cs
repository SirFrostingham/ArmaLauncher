using System;
using System.Windows.Data;

namespace ArmaLauncher.Behaviors
{
    public class ComparisonConverter : IValueConverter
    {
        public int GreenGreaterThanOrEqualTo { get; set; }
        public int GreenLessThanOrEqualTo { get; set; }

        public int YellowGreaterThanOrEqualTo { get; set; }
        public int YellowLessThanOrEqualTo { get; set; }

        public int RedGreaterThanOrEqualTo { get; set; }
        public int RedLessThanOrEqualTo { get; set; }

        public int GrayGreaterThanOrEqualTo { get; set; }
        public int GrayLessThanOrEqualTo { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = string.Empty;

            if (value == null)
                return "gray";

            var test = System.Convert.ToInt32(value);

            if ((test >= GreenGreaterThanOrEqualTo) && (test <= GreenLessThanOrEqualTo))
                result = "green";

            if ((test >= YellowGreaterThanOrEqualTo) && (test <= YellowLessThanOrEqualTo))
                result = "yellow";

            if ((test >= RedGreaterThanOrEqualTo) && (test <= RedLessThanOrEqualTo))
                result = "red";

            if ((test >= GrayGreaterThanOrEqualTo) && (test <= GrayLessThanOrEqualTo))
                result = "gray";

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    } 
}