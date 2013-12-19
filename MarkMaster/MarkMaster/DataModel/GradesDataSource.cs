using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace MarkMaster.Data
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class GradesDataItem
    {
        public GradesDataItem(String uniqueId, String itemName, String imagePath, String itemType, Double itemGrade, Double itemWeight)
        {
            this.UniqueId = uniqueId;
            this.ItemName = itemName;
            this.ImagePath = imagePath;
            this.ItemGrade = itemGrade;
            this.ItemWeight = itemWeight;
        }

        public string UniqueId { get; private set; }
        public string ItemName { get; private set; }
        public string ImagePath { get; private set; }
        public string ItemType { get; private set; }
        public double ItemGrade { get; private set; }
        public double ItemWeight { get; private set; }

        public override string ToString()
        {
            return this.ItemName;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class GradesDataGroup
    {
        public GradesDataGroup(String uniqueId, String courseName, String courseCode, String imagePath, 
            UInt16 courseUnits, Double courseGoal, Double courseGrade)
        {
            this.UniqueId = uniqueId;
            this.CourseName = courseName;
            this.CourseCode = courseCode;
            this.ImagePath = imagePath;
            this.CourseUnits = courseUnits;
            this.CourseGoal = courseGoal;
            this.CourseGrade = courseGrade;
            this.Items = new ObservableCollection<GradesDataItem>();
        }

        public string UniqueId { get; private set; }
        public string CourseName { get; private set; }
        public string CourseCode { get; private set; }
        public string ImagePath { get; private set; }
        public UInt16 CourseUnits { get; private set; }
        public double CourseGoal { get; private set; }
        public double CourseGrade { get; private set; }
        public ObservableCollection<GradesDataItem> Items { get; private set; }

        public override string ToString()
        {
            return this.CourseCode;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// GradesDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class GradesDataSource
    {
        private static GradesDataSource _gradesDataSource = new GradesDataSource();

        private ObservableCollection<GradesDataGroup> _groups = new ObservableCollection<GradesDataGroup>();
        public ObservableCollection<GradesDataGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task<IEnumerable<GradesDataGroup>> GetGroupsAsync()
        {
            await _gradesDataSource.GetGradesDataAsync();

            return _gradesDataSource.Groups;
        }

        public static async Task<GradesDataGroup> GetGroupAsync(string uniqueId)
        {
            await _gradesDataSource.GetGradesDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _gradesDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<GradesDataItem> GetItemAsync(string uniqueId)
        {
            await _gradesDataSource.GetGradesDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _gradesDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetGradesDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/GradesData.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                GradesDataGroup group = new GradesDataGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["CourseName"].GetString(),
                                                            groupObject["CourseCode"].GetString(),
                                                            groupObject["ImagePath"].GetString(),
                                                            UInt16.Parse(groupObject["CourseUnits"].GetString()),
                                                            Double.Parse(groupObject["CourseGoal"].GetString()),
                                                            Double.Parse(groupObject["CourseGrade"].GetString()));

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    group.Items.Add(new GradesDataItem(itemObject["UniqueId"].GetString(),
                                                       itemObject["ItemName"].GetString(),
                                                       itemObject["ImagePath"].GetString(),
                                                       itemObject["ItemType"].GetString(),
                                                       Double.Parse(itemObject["ItemGrade"].GetString()),
                                                       Double.Parse(itemObject["ItemWeight"].GetString())));
                }
                this.Groups.Add(group);
            }
        }
    }
}