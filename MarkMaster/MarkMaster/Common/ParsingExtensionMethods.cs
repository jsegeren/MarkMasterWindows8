using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime;
using Windows.Foundation.Metadata;


namespace MarkMaster.Common
{
    public sealed class ParseResult
    {
        public string Text { get; set; }
        public int Location { get; set; }
    }

    public static class ParsingExtensionMethods
    {

        #region Public Methods


        /// <summary>
        /// Finds multiple instances of text bounded by text markers
        /// </summary>
        /// <param name="target"></param>
        /// <param name="startMarker"></param>
        /// <param name="endMarkers"></param>
        /// <returns></returns>
        public static IList<string> FindElements(this string target, string startMarker, IList<string> endMarkers)
        {
            var returnList = new List<string>();

            //var returnList = [];

            int totalLength = target.Length;
            int currentPoint = 0;
            //int endPoint;

            var workingString = target;
            var workingStringLength = workingString.Length;

            while (workingString.Length > 1)
            {

                workingString = workingString.Substring(currentPoint);


                if (workingString.Length < startMarker.Length) // || workingString.Length < endMarkers.Length)
                {
                    break;
                }

                var elementResult = workingString.FindElement(startMarker, endMarkers);

                currentPoint = elementResult.Location;

                if (currentPoint == -1)
                {
                    break;
                }

                returnList.Add(elementResult.Text);

                var resultCount = returnList.Count;
            }

            return returnList;

        }


        /// <summary>
        /// Finds a single instance of text bounded by text markers
        /// </summary>
        /// <param name="target"></param>
        /// <param name="startMarker"></param>
        /// <param name="endMarkers"></param>
        /// <returns></returns>
        public static ParseResult FindElement(this string target, string startMarker, IList<string> endMarkers)
        {

            var startPoint = target.IndexOf(startMarker);

            var adjustedstartPoint = startPoint + startMarker.Length;

            var endPoint = FindEndElement(target, endMarkers, adjustedstartPoint);

            var length = (endPoint - adjustedstartPoint);

            if (startPoint != -1 && endPoint != -1)
            {
                var subString = target.Substring(adjustedstartPoint, length);

                var result = subString;

                var parseResult = new ParseResult()
                {
                    Location = endPoint,
                    Text = subString
                };

                return parseResult;

            }

            return new ParseResult() { Text = string.Empty, Location = -1 };
        }

        private static int FindEndElement(string sourceString, IList<string> endMarkers, int adjustedstartPoint)
        {
            foreach (var endMarker in endMarkers)
            {
                int endPoint = sourceString.IndexOf(endMarker, adjustedstartPoint);

                if (endPoint != -1)
                {
                    return endPoint;
                }
            }

            return -1;
        }

        /// <summary>
        /// Removes a string from another string
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="targetString"></param>
        /// <returns></returns>
        public static string Remove(this string sourceString, string targetString)
        {
            string cleanedString = sourceString.Replace(targetString, string.Empty);

            return cleanedString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="targetString"></param>
        /// <returns></returns>
        public static IEnumerable<string> LinesStartingWithString(this string sourceString, string targetString)
        {
            var lines = sourceString.Split('\n').ToList();

            var matchingLines = lines.Where(x => x.StartsWith(targetString));

            return matchingLines;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceString"></param>
        /// <param name="targetString"></param>
        /// <returns></returns>
        public static IEnumerable<string> LinesContainingString(this string sourceString, string targetString)
        {
            var lines = sourceString.Split('\n').ToList();

            var matchingLines = lines.Where(x => x.Contains(targetString));

            return matchingLines;
        }


        #endregion

    }
}