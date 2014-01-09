using MarkMaster.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml;
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
            this.ItemType = itemType;
            this.ItemGrade = itemGrade;
            this.ItemWeight = itemWeight;
        }

        // Backing fields for course item properties
        private string _itemName;
        private string _itemType;
        private double _itemGrade;
        private double _itemWeight;

        // Public properties
        public string UniqueId { get; private set; }
        public string ItemName
        {
            get
            {
                return _itemName;
            }
            set
            {
                if (SetProperty<string>(ref _itemName, value)) { }
            }
        }

        public string ImagePath { get; private set; }
        public string ItemType
        {
            get
            {
                return _itemType;
            }
            set
            {
                if (SetProperty<string>(ref _itemType, value)) { }
            }
        }

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
        private string _courseName;
        private string _courseCode;
        private double _courseGrade;
        private double _courseGoal;
        private UInt16 _courseUnits;

        public string UniqueId { get; private set; }
        public string CourseName
        {
            get
            {
                return _courseName;
            }
            set
            {
                if (SetProperty<string>(ref _courseName, value)) { }
            }
        }
        public string CourseCode
        {
            get
            {
                return _courseCode;
            }

            set
            {
                if (SetProperty<string>(ref _courseCode, value)) { }
            }
        }
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
            if (args.NewItems != null)
            {
                foreach (var newItem in args.NewItems) ((GradesDataItem)newItem).PropertyChanged += RecalculateCourseGrade;
            }

            PropertyChangedEventArgs newArgs = new PropertyChangedEventArgs("ItemGrade");
            RecalculateCourseGrade(sender, newArgs); // Recalculate in case items deleted
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

                // Only consider course items with non-zero (positive) weight
                var itemsNonZeroWeight = Items.Where(item => item.ItemWeight != 0);

                // If filtered result set is empty, avoid zero division error (NaN result!)
                if (itemsNonZeroWeight.Count() == 0)
                {
                    this.CourseGrade = (Double) 0;
                }
                else
                {
                    this.CourseGrade = itemsNonZeroWeight.Select(item => item.ItemWeight * item.ItemGrade).Sum() /
                        itemsNonZeroWeight.Select(item => item.ItemWeight).Sum();
                }
            }
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// GradesDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public class GradesDataSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private int _autoUniqueID = 0; // Private field to maintain next auto-incrementing ID
        private double _sessionalGrade;  // Private field to maintain current sessional average
        private double _sessionalGradeTwelve; // Sessional average in twelve-point McMaster scale
        private double _sessionalGradeFour; // Sessional average in four-point GPA scale
        private UInt16 _sessionalUnits; // Private field to maintain current sessional units
        private bool _isEditingFlag; // Private field indicating whether user is editing fields
        private static GradeScaleConverter gradeScaleConverter = new GradeScaleConverter(); // Private converter member

        public GradesDataSource()
        {
            this.Groups.CollectionChanged += OnCoursesCollectionChanged;
        }

        // Recalculate sessional grade if courses changed
        private void OnCoursesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RecalculateSessionalGrade();
        }

        public int AutoUniqueID
        {
            get
            {
                return _autoUniqueID;
            }
            set
            {
                if (SetProperty<int>(ref _autoUniqueID, value)) {}
            }
        }

        public bool IsEditingFlag
        {
            get
            {
                return _isEditingFlag;
            }
            set
            {
                if (SetProperty<bool>(ref _isEditingFlag, value)) { }
            }
        }

        public UInt16 SessionalUnits
        {
            get
            {
                return _sessionalUnits;
            }
            set
            {
                if (SetProperty<UInt16>(ref _sessionalUnits, value)) { }
            }
        }

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

        public double SessionalGradeFour
        {
            get
            {
                return _sessionalGradeFour;
            }
            set
            {
                if (SetProperty<double>(ref _sessionalGradeFour, value)) { }
            }
        }

        public double SessionalGradeTwelve
        {
            get
            {
                return _sessionalGradeTwelve;
            }
            set
            {
                if (SetProperty<double>(ref _sessionalGradeTwelve, value)) { }
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

        // Public suspension data save mechanism
        public static async Task<bool> SaveDataSourceAsync()
        {
            return await _gradesDataSource.SaveGradesDataAsync();
        }

        //public static async Task<GradesDataGroup> GetGroupAsync(string uniqueId)
        public static GradesDataGroup GetGroup(string uniqueId)
        {
            //await _gradesDataSource.GetGradesDataAsync(); // Comment out to avoid re-parsing JSON file
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

        // Asynchronous static method to save existing grade data structures
        // as serialized JSON to application resource file (inverse of GetGradesDataAsync)
        // To be executed on application suspension
        // TODO should this be called every time a new course, or course item is created?
        private async Task<bool> SaveGradesDataAsync()
        {
            Uri dataUri = new Uri((string)Application.Current.Resources["JsonUriString"]);
            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                (string)Application.Current.Resources["JsonDisplayName"], CreationCollisionOption.ReplaceExisting);

            string jsonString = await JsonConvert.SerializeObjectAsync(_gradesDataSource);
            await FileIO.WriteTextAsync(file, jsonString);
            
            // Determine success based on file availability
            return file.IsAvailable;

        }

        // Asynchronous static method to load the relevant JSON file, parse it, and 
        // populate the relevant data structures
        private async Task GetGradesDataAsync()
        {
            // Ensure grades not already populated (skip if they are)
            if (this._groups.Count != 0)
                return;

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                (string)Application.Current.Resources["JsonDisplayName"], CreationCollisionOption.OpenIfExists);

            string jsonString = await FileIO.ReadTextAsync(file);

            // Check if file just created -> create new instance of grades data source
            if (String.IsNullOrWhiteSpace(jsonString))
            {
                _gradesDataSource = new GradesDataSource();
                CreateNewCourse(); // Create initial course to help guide user
            }
            else
            {
                _gradesDataSource = JsonConvert.DeserializeObject<GradesDataSource>(jsonString);
            }

            RecalculateSessionalGrade(); // Calculate initial sessional grade based on file data
        }

        // Static method to generate blank new course item; returns auto-generated unique ID
        // for subsequent element focus in the list
        public static string CreateNewItem(string courseUniqueID)
        {
            // Set default weight to be remainder of total weight for course (i.e. 100 - sum of existing item weights)
            double defaultItemWeight = 100 - _gradesDataSource.Groups.Where(course => 
                course.UniqueId == courseUniqueID).FirstOrDefault().Items.Select(item => item.ItemWeight).Sum();
            if (defaultItemWeight < 0) {
                defaultItemWeight = 0;
            }

            int itemUniqueID = _gradesDataSource._autoUniqueID++;
            GradesDataItem newItem = new GradesDataItem(
                (itemUniqueID).ToString(),
                (string) Application.Current.Resources["DefaultItemName"],
                (string) Application.Current.Resources["DefaultItemImagePath"],
                (string) Application.Current.Resources["DefaultItemType"],
                (double) Application.Current.Resources["DefaultItemGrade"],
                //(double) Application.Current.Resources["DefaultItemWeight"]
                defaultItemWeight
                );

            ((GradesDataGroup) _gradesDataSource.Groups.Where(course => course.UniqueId == courseUniqueID).FirstOrDefault()).Items.Add(newItem);
            return itemUniqueID.ToString();
        }

        // Static method to delete a course item (from a specific course); returns true if completed successfully
        public static bool RemoveItem(string courseUniqueID, GradesDataItem targetItem)
        {
            return ((GradesDataGroup)_gradesDataSource.Groups.Where(course => 
                course.UniqueId == courseUniqueID).FirstOrDefault()).Items.Remove(targetItem);
        }

        // Static method to delete an entire course; returns true if completely successfully
        public static bool RemoveCourse(GradesDataGroup targetCourse)
        {
            return _gradesDataSource.Groups.Remove(targetCourse);
        }
        
        // Static method to delete multiple courses; true if all complete successfully
        public static bool RemoveCourses(List<object> targetCourses)
        {
            bool isSuccess = true;
            foreach (GradesDataGroup targetCourse in targetCourses)
            {
                isSuccess &= RemoveCourse(targetCourse);
            }
            return isSuccess;
        }

        // Static method to generate blank new course; returns auto-generated unique ID
        // for subsequent access to the course
        public static string CreateNewCourse()
        {
            int courseUniqueID = _gradesDataSource._autoUniqueID++;
            GradesDataGroup group = new GradesDataGroup(
                (courseUniqueID).ToString(),
                (string) Application.Current.Resources["DefaultCourseName"],
                (string) Application.Current.Resources["DefaultCourseCode"],
                (string)Application.Current.Resources["DefaultCourseImagePath"],
                UInt16.Parse((string) Application.Current.Resources["DefaultCourseUnits"]),
                (double) Application.Current.Resources["DefaultCourseGoal"],
                (double) Application.Current.Resources["DefaultCourseGrade"]
                );

            // Insert placeholder course item
            _gradesDataSource.Groups.Add(group);
            int itemUniqueID = Int16.Parse(CreateNewItem(courseUniqueID.ToString()));

            return (courseUniqueID).ToString();
        }

        // Static method to re-evaluate sessional average over all courses for grades data source object
        public static void RecalculateSessionalGrade()
        {
            //// First, update total number of units
            //_gradesDataSource.SessionalUnits = (UInt16) _gradesDataSource.Groups.Select(course => (double)course.CourseUnits).Sum();
            //// Retrieve sessional grade via dot product of units * grades (all courses), divided by total of units
            //_gradesDataSource.SessionalGrade = _gradesDataSource.Groups.Select(course => course.CourseUnits * course.CourseGrade).Sum() /
            //    (double) _gradesDataSource.SessionalUnits;

            // Reset the accumulators
            _gradesDataSource.SessionalUnits = 0; 
            _gradesDataSource.SessionalGrade = 0;
            _gradesDataSource.SessionalGradeFour = 0;
            _gradesDataSource.SessionalGradeTwelve = 0;
            foreach (GradesDataGroup course in _gradesDataSource.Groups)
            {
                _gradesDataSource.SessionalUnits += course.CourseUnits;
                _gradesDataSource.SessionalGrade += course.CourseGrade * course.CourseUnits;
                _gradesDataSource.SessionalGradeFour +=
                    gradeScaleConverter.PercentageToGradeScale(course.CourseGrade, "Four") * course.CourseUnits;
                _gradesDataSource.SessionalGradeTwelve +=
                    gradeScaleConverter.PercentageToGradeScale(course.CourseGrade, "Twelve") * course.CourseUnits;
            }
            
            // Avoid divide by zero issues (i.e. NaN results) - just default to 0 for now
            if (_gradesDataSource.SessionalUnits == 0)
            {
                _gradesDataSource.SessionalGrade = 0;
                _gradesDataSource.SessionalGradeFour = 0;
                _gradesDataSource.SessionalGradeTwelve = 0;
            }
            else
            {
                _gradesDataSource.SessionalGrade /= _gradesDataSource.SessionalUnits;
                _gradesDataSource.SessionalGradeFour /= _gradesDataSource.SessionalUnits;
                _gradesDataSource.SessionalGradeTwelve /= _gradesDataSource.SessionalUnits;
            }
        }

        // Static method to switch editing flag on/off
        public static bool SwitchIsEditingFlag()
        {
            _gradesDataSource.IsEditingFlag = !_gradesDataSource.IsEditingFlag;
            return _gradesDataSource.IsEditingFlag;
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
}