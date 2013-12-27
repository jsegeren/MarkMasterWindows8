using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Course item data model -> describes properties of individual item
    /// One or more items can be included in a course (i.e. course is a collection of course items)
    /// </summary>
    public class GradesDataItem : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public GradesDataItem(String uniqueId, String itemName, String imagePath, String itemType, Double itemGrade, Double itemWeight)
        {
            this.UniqueId = uniqueId;
            this.ItemName = itemName;
            this.ImagePath = imagePath;
            this.ItemGrade = itemGrade;
            this.ItemWeight = itemWeight;
        }

        // Backing fields for course item properties
        private double _itemGrade;
        private double _itemWeight;

        // Public properties
        public string UniqueId { get; private set; }
        public string ItemName { get; private set; }
        public string ImagePath { get; private set; }
        public string ItemType { get; private set; }
        public double ItemGrade
        {
            get
            {
                return _itemGrade;
            }
            set
            { // Implement for two-way binding; re-evalulate only if value actually changed
                if (SetProperty<double>(ref _itemGrade, value)) { }
            }
        }
        public double ItemWeight
        {
            get
            {
                return _itemWeight;
            }
            set
            { // Implement for two-way binding; re-evalulate only if value actually changed
                if (SetProperty<double>(ref _itemWeight, value)) { }
            }
        }

        public override string ToString()
        {
            return this.ItemName;
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value; OnPropertyChanged(propertyName); return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// Data model for academic course -> encompasses properties and course items 
    /// </summary>
    public class GradesDataGroup : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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

            // Add event to recalculate course grade if any item changes
            this.Items.CollectionChanged += OnItemsCollectionChanged;
        }
        private double _courseGrade;
        private double _courseGoal;
        private UInt16 _courseUnits;

        public string UniqueId { get; private set; }
        public string CourseName { get; private set; }
        public string CourseCode { get; private set; }
        public string ImagePath { get; private set; }
        public UInt16 CourseUnits
        {
            get
            {
                return _courseUnits;
            }
            set
            {
                if (SetProperty<UInt16>(ref _courseUnits, value)) {
                    GradesDataSource.RecalculateSessionalGrade();
                }
            }
        }
        public double CourseGoal
        {
            get
            {
                return _courseGoal;
            }
            set
            {
                if (SetProperty<double>(ref _courseGoal, value)) { }
            }
        }

        public double CourseGrade
        {
            get
            {
                return _courseGrade;
            }
            set
            { // Implement for two-way binding; re-evalulate only if value actually changed
                if (SetProperty<double>(ref _courseGrade, value)) {
                    GradesDataSource.RecalculateSessionalGrade(); // Update sessional average
                }
            }
        }
        public ObservableCollection<GradesDataItem> Items { get; private set; }

        public override string ToString()
        {
            return this.CourseCode;
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value; OnPropertyChanged(propertyName); return true;
        }

        protected void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            foreach (var newItem in args.NewItems) ((GradesDataItem)newItem).PropertyChanged += RecalculateCourseGrade;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Subscribed function (event handler) which updates the course grade upon change in item weight or grade
        // Also calls function to update sessional average
        void RecalculateCourseGrade(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "ItemWeight" || args.PropertyName == "ItemGrade")
            {
                //double oldCourseGrade = this.CourseGrade; // Store previous course grade for reference
                // Retrieve course grade via dot product of weights * grades (all items), divided by total of weights
                this.CourseGrade = Items.Select(item => item.ItemWeight * item.ItemGrade).Sum() /
                    Items.Select(item => item.ItemWeight).Sum();
            }
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// GradesDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class GradesDataSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private int _autoUniqueID = 0; // Private field to maintain next auto-incrementing ID
        private double _sessionalGrade;  // Private field to maintain current sessional average

        public double SessionalGrade
        {
            get
            {
                return _sessionalGrade;
            }
            set
            {
                if (SetProperty<double>(ref _sessionalGrade, value)) { }
            }
        }

        private static GradesDataSource _gradesDataSource = new GradesDataSource();

        private ObservableCollection<GradesDataGroup> _groups = new ObservableCollection<GradesDataGroup>();
        public ObservableCollection<GradesDataGroup> Groups
        {
            get { return this._groups; }
        }

        //public static async Task<IEnumerable<GradesDataGroup>> GetGroupsAsync()
        public static async Task<GradesDataSource> GetDataSourceAsync()
        {
            await _gradesDataSource.GetGradesDataAsync();

            //return _gradesDataSource.Groups;
            return _gradesDataSource;
        }

        public static async Task<GradesDataGroup> GetGroupAsync(string uniqueId)
        //public static GradesDataGroup GetGroup(string uniqueId)
        {
            await _gradesDataSource.GetGradesDataAsync(); // Comment out to avoid re-parsing JSON file
            // Simple linear search is acceptable for small data sets
            var matches = _gradesDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<GradesDataItem> GetItemAsync(string uniqueId)
        //public static GradesDataItem GetItem(string uniqueId)
        {
            await _gradesDataSource.GetGradesDataAsync(); // Comment out to avoid re-parsing JSON file
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

            RecalculateSessionalGrade(); // Calculate initial sessional grade based on file data
        }

        // Static method to generate blank new course; returns auto-generated unique ID
        // for subsequent access to the course
        public static string CreateNewCourse()
        {
            int courseUniqueID = _gradesDataSource._autoUniqueID++;
            GradesDataGroup group = new GradesDataGroup((courseUniqueID).ToString(),
                                                         "New Course",
                                                         String.Empty,
                                                         String.Empty,
                                                         (UInt16)3,
                                                         (Double)50,
                                                         (Double)0);
            // Insert placeholder course item
            int itemUniqueID = _gradesDataSource._autoUniqueID++;
            group.Items.Add(new GradesDataItem((itemUniqueID).ToString(),
                             "New Item",
                             String.Empty,
                             String.Empty,
                             (Double)0,
                             (Double)0));

            _gradesDataSource.Groups.Add(group);
            return (courseUniqueID).ToString();
        }

        // Static method to re-evaluate sessional average over all courses for grades data source object
        public static void RecalculateSessionalGrade()
        {
            // Retrieve sessional grade via dot product of units * grades (all courses), divided by total of units
            _gradesDataSource._sessionalGrade = _gradesDataSource.Groups.Select(course => course.CourseUnits * course.CourseGrade).Sum() /
                _gradesDataSource.Groups.Select(course => (double)course.CourseUnits).Sum();
        }

        // Static method to retrieve/access current 

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value; OnPropertyChanged(propertyName); return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}