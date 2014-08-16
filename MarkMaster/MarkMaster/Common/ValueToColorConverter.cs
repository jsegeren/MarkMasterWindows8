using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MarkMaster.Common
{
    public class ValueToColorConverter : IValueConverter
    {
        /// <summary>
        /// If set to True, conversion is reversed: True will become Collapsed.
        /// </summary>
        public bool IsReversed { get; set; }

        public object Convert(object value, Type typeName, object parameter, string language)
        {
            double thresholdValue;
            Double.TryParse((string)parameter, out thresholdValue);

            return ((double) value > thresholdValue) ? "#FFB48417" : "#5F37BE";
        }

        public object ConvertBack(object value, Type typeName, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
