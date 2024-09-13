using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StratoDomainDDNSChanger.Core
{
    class StringToBoolConverter
    {

        public static object ConvertToBool(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString().ToLower() == "true";
        }

        public static object ConvertToString(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "false";
            return ((bool)value) ? "true" : "false";
        }
    }
}
