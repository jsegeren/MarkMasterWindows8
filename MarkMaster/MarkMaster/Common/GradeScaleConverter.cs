using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MarkMaster.Common
{
    class GradeScaleConverter : IValueConverter
    {
        // This maps 12.0 grade scale values to 4.0 grade scale values (according to official
        // McMaster conversion specifications)
        private Dictionary<UInt16, double> twelveToFourMap = new Dictionary<UInt16, double>() {
            { 12, 4.00 },
            { 11, 3.90 },
            { 10, 3.70 },
            { 9, 3.30 },
            { 8, 3.00 },
            { 7, 2.70 },
            { 6, 2.30 },
            { 5, 2.00 },
            { 4, 1.70 },
            { 3, 1.30 },
            { 2, 1.00 },
            { 1, 0.70 },
            { 0, 0.00 }
        };

        // This maps (minimum) percentage grades to 12.0 grade scale values (according to official
        // McMaster conversion specifications)
        private Dictionary<UInt16, UInt16> percentToTwelveMap = new Dictionary<UInt16, UInt16>() {
            { 90, 12 },
            { 85, 11 },
            { 80, 10 },
            { 77, 9 },
            { 73, 8 },
            { 70, 7 },
            { 67, 6 },
            { 63, 5 },
            { 60, 4 },
            { 57, 3 },
            { 53, 2 },
            { 50, 1 },
            { 0, 0 }
        };

        public double PercentageToGradeScale(double value, string inputParameter)
        {
            int roundedPercentageGrade = (int)Math.Round((double)value, 0);
            UInt16 twelvePointGrade = percentToTwelveMap.Where(grade => grade.Key <= roundedPercentageGrade).OrderByDescending(grade =>
                grade.Key).FirstOrDefault().Value;

            switch (inputParameter)
            {
                case "Twelve":
                    return twelvePointGrade;
                case "Four":
                    return twelveToFourMap[twelvePointGrade];
                default:
                    return 0;
            }
        }

        // This converts the percentage grade to specified grade scale for display
        public object Convert(object value, Type targetType,
            object parameter, string language)
        {
            // Retrieve the format string and use it to format the value
            return PercentageToGradeScale((double)value, (string)parameter).ToString();

        }
        // No need to implement converting back on a one-way binding 
        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}