using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MarkMaster.Common
{
    public class DepartmentNameToAbbrevConverter : IValueConverter
    {
        public object Convert(object value, System.Type type, object parameter, string language)
        {
            if (value is string && ((string)value).Contains("(") && ((string)value).Contains(")"))
            {
                return ((string)value).Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries)[1];
            }
            else
            {
                return value;
            }
        }
        public object ConvertBack(object value, System.Type type, object parameter, string language)
        {
            throw new NotImplementedException(); // Not required for one-way bindings only
        }
    }
}
