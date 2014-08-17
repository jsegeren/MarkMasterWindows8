using MarkMaster.Common;
using MarkMaster.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Split Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234234

namespace MarkMaster
{
    /// <summary>
    /// A page that displays a group title, a list of items within the group, and details for
    /// the currently selected item.
    /// </summary>
    public sealed partial class SplitPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private String subjectSearchString;
        private String previousTextBoxString;

        private Dictionary<UIElement, UIElement> readToEditUIEements;
        private Dictionary<UIElement, UIElement> editToReadUIElements;

        // TODO autosort these alphabetically when populating the combobox
        private static HashSet<String> itemTypeValues = new HashSet<String> { "Lab", "Assignment", "Tutorial", "Test", "Final", "Paper", "Project", "Presentation", "Other" };

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public SplitPage()
        {
            this.InitializeComponent();

            // Setup the navigation helper
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            // Setup the logical page navigation components that allow
            // the page to only show one pane at a time.
            this.navigationHelper.GoBackCommand = new RelayCommand(() => this.GoBack(), () => this.CanGoBack());
            this.itemListView.SelectionChanged += ItemListView_SelectionChanged;

            // Start listening for Window size changes 
            // to change from showing two panes to showing a single pane
            // TODO fix and finish this
            //Window.Current.SizeChanged += Window_SizeChanged;
            this.InvalidateVisualState();

            // Set up the map of read-only -> edit UI elements
            readToEditUIEements = new Dictionary<UIElement, UIElement>()
            {
                { pageTitle, pageTitleEdit },
                { courseCodePanel, courseCodeEditPanel },
                { courseGoal, courseGoalEdit },
                { itemTitle, itemTitleEdit },
                { itemSubtitle, itemSubtitleEditCombo }
            };
            editToReadUIElements = new Dictionary<UIElement, UIElement>()
            {
                { pageTitleEdit, pageTitle },
                { courseCodeEditPanel, courseCodePanel },
                { courseGoalEdit, courseGoal },
                { itemTitleEdit, itemTitle },
                { itemSubtitleEditCombo, itemSubtitle }
            };
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // Set up elements of view model
            GradesDataSource gradesDataSource = (GradesDataSource)await GradesDataSource.GetDataSourceAsync();
            GradesDataGroup group = GradesDataSource.GetGroup((String)e.NavigationParameter);
            this.DefaultViewModel["Group"] = group;
            this.DefaultViewModel["Items"] = group.Items;
            this.DefaultViewModel["IsEditingFlag"] = gradesDataSource.IsEditingFlag;

            // Populate the course code combobox with cached list of subjects
            var courseSubjectFile = await
                Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets\\Subjects_McMaster_Jan_2014.csv");
            IList<String> courseSubjectList = await FileIO.ReadLinesAsync(courseSubjectFile);

            // Decide whether to use scraped course data or cached version
            if (((App)(App.Current)).DepartmentToCoursesMap != null)
            {
                List<String> departmentList = ((App)(App.Current)).DepartmentToCoursesMap.Keys.ToList();
                departmentList.Sort((x, y) => string.Compare(x, y)); // In-place alpha sort
                departmentNameEdit.ItemsSource = departmentList;
                courseCodeEditCombo.Visibility = Visibility.Visible;
                courseCodeEdit2.Visibility = Visibility.Collapsed;
            }
            else
            {
                departmentNameEdit.ItemsSource = courseSubjectList;
                courseCodeEditCombo.Visibility = Visibility.Collapsed;
                courseCodeEdit2.Visibility = Visibility.Visible;
            }

            // Populate the item types combo box
            List<String> itemTypeValuesList = itemTypeValues.ToList();
            itemTypeValuesList.Sort((x, y) => string.Compare(x, y)); // In-place alpha sort
            itemSubtitleEditCombo.ItemsSource = itemTypeValuesList;

            if (e.PageState == null)
            {
                this.itemListView.SelectedItem = null;
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null)
                {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }
            }
            else
            {
                // Restore the previously saved state associated with this page
                if (e.PageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null)
                {
                    var selectedItem = await GradesDataSource.GetItemAsync((String)e.PageState["SelectedItem"]);
                    this.itemsViewSource.View.MoveCurrentTo(selectedItem);
                }
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            if (this.itemsViewSource.View != null)
            {
                var selectedItem = (Data.GradesDataItem)this.itemsViewSource.View.CurrentItem;
                if (selectedItem != null) e.PageState["SelectedItem"] = selectedItem.UniqueId;
            }
        }

        #region Logical page navigation

        // The split page isdesigned so that when the Window does have enough space to show
        // both the list and the dteails, only one pane will be shown at at time.
        //
        // This is all implemented with a single physical page that can represent two logical
        // pages.  The code below achieves this goal without making the user aware of the
        // distinction.

        private const int MinimumWidthForSupportingTwoPanes = 768;

        /// <summary>
        /// Invoked to determine whether the page should act as one logical page or two.
        /// </summary>
        /// <returns>True if the window should show act as one logical page, false
        /// otherwise.</returns>
        private bool UsingLogicalPageNavigation()
        {
            return Window.Current.Bounds.Width < MinimumWidthForSupportingTwoPanes;
        }

        /// <summary>
        /// Invoked with the Window changes size
        /// </summary>
        /// <param name="sender">The current Window</param>
        /// <param name="e">Event data that describes the new size of the Window</param>
        private void Window_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            this.InvalidateVisualState();
        }

        /// <summary>
        /// Invoked when an item within the list is selected.
        /// </summary>
        /// <param name="sender">The GridView displaying the selected item.</param>
        /// <param name="e">Event data that describes how the selection was changed.</param>
        private void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();

            // Prevent no item from being selected i.e. disable deselection
            // (Assumes only one course item was previously selected, i.e. not multiple items at a time!)
            if (this.itemListView.SelectedItem == null)
            {
                this.itemListView.SelectedIndex = this.itemListView.Items.IndexOf(e.RemovedItems.FirstOrDefault());
            }
        }

        private bool CanGoBack()
        {
            if (this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null)
            {
                return true;
            }
            else
            {
                return this.navigationHelper.CanGoBack();
            }
        }
        private void GoBack()
        {
            if (this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null)
            {
                // When logical page navigation is in effect and there's a selected item that
                // item's details are currently displayed.  Clearing the selection will return to
                // the item list.  From the user's point of view this is a logical backward
                // navigation.
                this.itemListView.SelectedItem = null;
            }
            else
            {
                this.navigationHelper.GoBack();
            }
        }

        private void InvalidateVisualState()
        {
            var visualState = DetermineVisualState();
            VisualStateManager.GoToState(this, visualState, false);
            this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Invoked to determine the name of the visual state that corresponds to an application
        /// view state.
        /// </summary>
        /// <returns>The name of the desired visual state.  This is the same as the name of the
        /// view state except when there is a selected item in portrait and snapped views where
        /// this additional logical page is represented by adding a suffix of _Detail.</returns>
        private string DetermineVisualState()
        {
            if (!UsingLogicalPageNavigation())
                return "PrimaryView";

            // Update the back button's enabled state when the view state changes
            var logicalPageBack = this.UsingLogicalPageNavigation() && this.itemListView.SelectedItem != null;

            return logicalPageBack ? "SinglePane_Detail" : "SinglePane";
        }

        #endregion

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void EnableEditingMode(bool enableFlag)
        {
            foreach (UIElement readElement in readToEditUIEements.Keys)
            {
                if (readElement != itemSubtitle)
                {
                    readElement.Visibility = (enableFlag) ? Visibility.Collapsed : Visibility.Visible;
                    readToEditUIEements[readElement].Visibility = (enableFlag) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void OnAddItemClick(object sender, RoutedEventArgs e)
        {
            // Add new item to current course
            string uniqueID = GradesDataSource.CreateNewItem(((GradesDataGroup)this.DefaultViewModel["Group"]).UniqueId);
            itemListView.SelectedIndex = itemListView.Items.Count - 1;

            itemTitleEdit.Focus(FocusState.Programmatic); // Set the user input focus to the new item label
        }

        private void OnRemoveItemClick(object sender, RoutedEventArgs e)
        {
            GradesDataSource.RemoveItem(((GradesDataGroup)this.DefaultViewModel["Group"]).UniqueId, (GradesDataItem)itemListView.SelectedItem);

            if (itemListView.Items.Count == 0)
            {
                string uniqueID = GradesDataSource.CreateNewItem(((GradesDataGroup)this.DefaultViewModel["Group"]).UniqueId);
            }

            itemListView.SelectedIndex = itemListView.Items.Count - 1; // Make sure some item is still selected
        }

        // Also update course code read-only content
        private void departmentNameEdit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;

            List<String> courseCodeList = new List<String>();
            try
            {
                foreach (McMasterCourse course in ((App)(App.Current)).DepartmentToCoursesMap[(string)e.AddedItems[0]])
                {
                    courseCodeList.Add(course.CourseCode);
                }
            }
            catch (NullReferenceException nullException) { }

            courseCodeList.Sort((x, y) => string.Compare(x, y)); // In-place alpha sort
            courseCodeEditCombo.ItemsSource = courseCodeList;
        }

        private void courseCodeEdit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Auto-populate course name, units based on department and course code
            try
            {
                McMasterCourse currentMacCourse = ((GradesDataGroup)this.DefaultViewModel["Group"]).MacCourse;
                String currentMacCourseCode = ((GradesDataGroup)this.DefaultViewModel["Group"]).MacCourse.CourseCode;

                ushort currentMacCourseUnits;
                ushort.TryParse(currentMacCourseCode[currentMacCourseCode.Length - 1].ToString(), out currentMacCourseUnits);
                ((GradesDataGroup)this.DefaultViewModel["Group"]).CourseName = ((App)(App.Current)).CourseToNameMap[currentMacCourse];
                ((GradesDataGroup)this.DefaultViewModel["Group"]).CourseUnits = currentMacCourseUnits;
            }
            catch (NullReferenceException nullException) { }
        }

        private void courseCode_SelectionMade(object sender, object e)
        {
            courseCodeEditPanel.Visibility = Visibility.Collapsed;
            courseCodePanel.Visibility = Visibility.Visible;
        }

        private void comboBox_SelectionMade(object sender, object e)
        {
            ((UIElement)sender).Visibility = Visibility.Collapsed;
            editToReadUIElements[(UIElement)sender].Visibility = Visibility.Visible;
        }

        // TODO fix this -> need to subscribe to key up on underlying scrollviewer; allow more complex searches
        //private void courseCodeEdit_KeyUp(object sender, KeyRoutedEventArgs e)
        //{
        //subjectSearchString = String.Join(String.Empty, e.Key.ToString());
        //var subjectMatch = ((ComboBox)sender).Items.FirstOrDefault(subject => 
        //    ((string)subject).StartsWith(subjectSearchString, StringComparison.CurrentCultureIgnoreCase));
        //if (subjectMatch != null)
        //{
        //    ((ComboBox)sender).SelectedItem = subjectMatch;
        //    ((ComboBox)sender).IsDropDownOpen = true;
        //}
        //courseCodeEdit2_KeyDown(sender, e);
        //}

        private void itemWeightValueEdit_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            defocus_KeyDown(sender, e);

            if (!(char.IsDigit((char)e.Key) || (Char.Equals((char)e.Key, (char)190) && !((TextBox)sender).Text.Contains('.'))))
            {
                e.Handled = true;
                return;
            }

            // Selected text means an overwrite will occur; no need to check for bound errors
            if (String.IsNullOrWhiteSpace(((TextBox)sender).SelectedText))
            {
                double nextValue;
                if (Char.Equals((char)e.Key, (char)190))
                {
                    nextValue = Double.Parse((string)((TextBox)sender).Text.ToString() + ".");
                }
                else
                {
                    nextValue = Double.Parse((string)((TextBox)sender).Text.ToString() + Convert.ToChar(e.Key));
                }

                if (nextValue > 100.0 || nextValue < 0.0)
                {
                    e.Handled = true;
                }
            }

        }

        private void itemListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            BottomAppBar.IsOpen = true;

            int item = 0;
            Double verticalCoordinate = e.GetPosition((UIElement)sender).Y;

            ListView listView = sender as ListView;
            if (sender is ListView)
            {
                listView.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Size listViewSize = listView.DesiredSize;
                item = (int)(verticalCoordinate / listViewSize.Height * listView.Items.Count);
                item = item >= listView.Items.Count ? listView.Items.Count - 1 : item;
            }

            var tappedItem = listView.Items[item];
            listView.SelectedItem = tappedItem;
        }

        private void pageTitle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TextBlockTapped(pageTitle, pageTitleEdit);
        }

        private void TextBlockTapped(UIElement readBlock, UIElement editBox)
        {
            readBlock.Visibility = Visibility.Collapsed;
            editBox.Visibility = Visibility.Visible;
            if (editBox is TextBox)
            {
                ((TextBox)editBox).Focus(FocusState.Programmatic);
            }
            else if (editBox is ComboBox)
            {
                ((ComboBox)editBox).Focus(FocusState.Programmatic);
            }

            // TODO clean this up; force course code edit to lose focus
            //courseCodeEdit_LostFocus(null, null);
        }

        private void TextBoxLostFocus(UIElement readBlock, UIElement editBox)
        {
            readBlock.Visibility = Visibility.Visible;
            editBox.Visibility = Visibility.Collapsed;

            // Simple hack fix to hide virtual keyboard on textbox defocus
            if (editBox is TextBox)
            {
                ((TextBox)editBox).IsEnabled = false;
                ((TextBox)editBox).IsEnabled = true;
            }

        }

        private void OnTextBoxFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.SelectAll();
            previousTextBoxString = textBox.Text;
        }


        private void pageTitleEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxLostFocus(pageTitle, pageTitleEdit);
            if (String.IsNullOrWhiteSpace(pageTitleEdit.Text))
            {
                pageTitleEdit.Text = previousTextBoxString;
            }
        }

        private void defocus_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Escape)
            {
                EnableEditingMode(false);
            }
        }

        private void AlphaEditBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            defocus_KeyDown(sender, e);
        }

        private void courseCode_Tapped(object sender, TappedRoutedEventArgs e)
        {
            courseCodePanel.Visibility = Visibility.Collapsed;
            courseCodeEditPanel.Visibility = Visibility.Visible;
        }

        private void courseCodeEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxLostFocus(courseCodePanel, courseCodeEditPanel);
        }

        private void courseCodeEdit2_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Escape)
            {
                courseCodeEdit_LostFocus(sender, null);
            }

            else if (!(char.IsLetterOrDigit((char)e.Key)))
            {
                e.Handled = true;
            }
        }

        private void pageTitleEdit_GotFocus(object sender, RoutedEventArgs e)
        {
            OnTextBoxFocus(sender, e);
            courseCodeEdit_LostFocus(sender, e);
        }

        private void Slider_GotFocus(object sender, RoutedEventArgs e)
        {
            courseCodeEdit_LostFocus(sender, e);
        }

        private void itemTitle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TextBlockTapped(itemTitle, itemTitleEdit);
        }

        private void itemSubtitle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TextBlockTapped(itemSubtitle, itemSubtitleEditCombo);
        }

        private void itemTitleEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            itemTitleEdit.Text = (String.IsNullOrWhiteSpace(itemTitleEdit.Text)) ?
                "New Item Name" : itemTitleEdit.Text;
            TextBoxLostFocus(itemTitle, itemTitleEdit);
        }

        private void courseGoal_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TextBlockTapped(courseGoal, courseGoalEdit);
        }

        private void courseGoalEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxLostFocus(courseGoal, courseGoalEdit);
            if (String.IsNullOrWhiteSpace(courseGoalEdit.Text))
            {
                courseGoalEdit.Text = previousTextBoxString;
            }
        }

        private void itemWeightValue_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TextBlockTapped(itemWeightValue, itemWeightValueEdit);
        }

        private void itemWeightValueEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxLostFocus(itemWeightValue, itemWeightValueEdit);
            if (String.IsNullOrWhiteSpace(itemWeightValueEdit.Text))
            {
                itemWeightValueEdit.Text = previousTextBoxString;
            }
        }

        private void itemGradeValueEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxLostFocus(itemGradeValue, itemGradeValueEdit);
            itemGradeCheckbox.Visibility = Visibility.Visible;
            if (String.IsNullOrWhiteSpace(itemGradeValueEdit.Text))
            {
                itemGradeValueEdit.Text = previousTextBoxString;
            }
        }

        private void itemGradeValue_Tapped(object sender, TappedRoutedEventArgs e)
        {
            itemGradeCheckbox.Visibility = Visibility.Collapsed;
            TextBlockTapped(itemGradeValue, itemGradeValueEdit);
        }

        private void defocus_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(e.OriginalSource is TextBlock))
            {
                EnableEditingMode(false);
            }
        }

        // TODO - Use data binding instead? Check if weight change causes the total item weight
        // associated with the course exceeds 100. This case is allowed for courses incorporating
        // bonus components; however, warning should be displayed to user in case accidental
        private void ItemWeight_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (((GradesDataGroup)this.DefaultViewModel["Group"]).Items.Select(item => item.ItemWeight).Sum() > 100)
            {
                itemWeightWarning.Visibility = Visibility.Visible;
            }
            else
            {
                itemWeightWarning.Visibility = Visibility.Collapsed;
            }
            ResetCourseGradeColor();
            Crossfade.Begin();
        }

        // TODO - Use data binding instead? Set color of course grade based on comparison with course goal
        private void CourseGoal_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ResetCourseGradeColor();
            Crossfade.Begin();
        }

        private void ResetCourseGradeColor()
        {
            GradesDataGroup currentCourse = (GradesDataGroup)this.DefaultViewModel["Group"];
            if (currentCourse.CourseGrade < currentCourse.CourseGoal)
            {
                courseGrade.Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 132, 23));
            }
            else
            {
                courseGrade.Foreground = new SolidColorBrush(Color.FromArgb(255, 95, 55, 190));
            }
        }

        private void itemGradeCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Ensure edit box becomes invisible
            itemGradeValueEdit.Visibility = Visibility.Collapsed;
            Crossfade.Begin();
        }

        private void itemGradeCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            Crossfade.Begin();
        }

    }
}