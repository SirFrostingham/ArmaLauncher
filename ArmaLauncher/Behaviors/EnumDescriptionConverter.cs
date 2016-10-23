using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using ArmaLauncher.Helpers;

namespace ArmaLauncher.Behaviors
{
    public class EnumDescriptionConverter : IValueConverter
    {
        //From Binding Source
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Enum)) throw new ArgumentException("Value is not an Enum");
            return (value as Enum).GetDescription();
        }

        //From Binding Target
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string)) throw new ArgumentException("Value is not a string");
            foreach (var item in Enum.GetValues(targetType))
            {
                var asString = (item as Enum).GetDescription();
                if (asString == (string)value)
                {
                    return item;
                }
            }
            throw new ArgumentException("Unable to match string to Enum description");
        }
    }
}