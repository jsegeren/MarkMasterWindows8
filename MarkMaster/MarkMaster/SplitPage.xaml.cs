using MarkMaster.Common;
using MarkMaster.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
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
            var group = await GradesDataSource.GetGroupAsync((String)e.NavigationParameter);
            GradesDataSource gradesDataSource = (GradesDataSource)await GradesDataSource.GetDataSourceAsync();
            //GradesDataGroup group = GradesDataSource.GetGroup((String)e.NavigationParameter);
            this.DefaultViewModel["Group"] = group;
            this.DefaultViewModel["Items"] = group.Items;
            this.DefaultViewModel["IsEditingFlag"] = gradesDataSource.IsEditingFlag;

            // Populate the combobox with cached list of subjects
            var courseSubjectFile = await
                Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets\\Subjects_McMaster_Jan_2014.csv");
            IList<string> courseSubjects = await FileIO.ReadLinesAsync(courseSubjectFile);
            courseCodeEdit.ItemsSource = courseSubjects;

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

        private void OnEditFieldsClick(object sender, RoutedEventArgs e)
        {
            // TODO Figure out why the view is not aware that the value of the boolean flag has changed
            if (GradesDataSource.SwitchIsEditingFlag())
            {
                pageTitle.Visibility = Visibility.Collapsed;
                pageTitleEdit.Visibility = Visibility.Visible;

                courseCode.Visibility = Visibility.Collapsed;
                courseCodeEditPanel.Visibility = Visibility.Visible;

                courseGoal.Visibility = Visibility.Collapsed;
                courseGoalEdit.Visibility = Visibility.Visible;

                itemTitle.Visibility = Visibility.Collapsed;
                itemTitleEdit.Visibility = Visibility.Visible;

                itemSubtitle.Visibility = Visibility.Collapsed;
                itemSubtitleEdit.Visibility = Visibility.Visible;

                itemWeightValue.Visibility = Visibility.Collapsed;
                itemWeightValueEdit.Visibility = Visibility.Visible;

                itemGradeValue.Visibility = Visibility.Collapsed;
                itemGradeValueEdit.Visibility = Visibility.Visible;
            }

            else
            {
                pageTitle.Visibility = Visibility.Visible;
                pageTitleEdit.Visibility = Visibility.Collapsed;

                courseCode.Visibility = Visibility.Visible;
                courseCodeEditPanel.Visibility = Visibility.Collapsed;

                courseGoal.Visibility = Visibility.Visible;
                courseGoalEdit.Visibility = Visibility.Collapsed;

                itemTitle.Visibility = Visibility.Visible;
                itemTitleEdit.Visibility = Visibility.Collapsed;

                itemSubtitle.Visibility = Visibility.Visible;
                itemSubtitleEdit.Visibility = Visibility.Collapsed;

                itemWeightValue.Visibility = Visibility.Visible;
                itemWeightValueEdit.Visibility = Visibility.Collapsed;

                itemGradeValue.Visibility = Visibility.Visible;
                itemGradeValueEdit.Visibility = Visibility.Collapsed;
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
            GradesDataSource.RemoveItem(((GradesDataGroup)this.DefaultViewModel["Group"]).UniqueId, (GradesDataItem) itemListView.SelectedItem);

            if (itemListView.Items.Count == 0)
            {
                string uniqueID = GradesDataSource.CreateNewItem(((GradesDataGroup)this.DefaultViewModel["Group"]).UniqueId);
            }

            itemListView.SelectedIndex = itemListView.Items.Count - 1; // Make sure some item is still selected
        }

        private void courseCodeEdit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SetCourseCodeInput();
            //courseCodeEdit2.Focus(FocusState.Programmatic); // Progress input focus over to code suffix

            // Reset the search input buffer
            subjectSearchString = String.Empty;
        }

        private void courseCodeEdit2_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.SetCourseCodeInput();
        }

        private void SetCourseCodeInput()
        {
            // Set text value of course code text to string of selected element in course code list (combobox)
            if (courseCodeEdit.SelectedValue != null && !String.IsNullOrEmpty(courseCodeEdit.SelectedValue.ToString()))
            {
                courseCode.Text = courseCodeEdit.SelectedValue.ToString();
            }
            if (!String.IsNullOrEmpty(courseCodeEdit2.Text.ToString()))
            {   
                courseCode.Text = (courseCodeEdit.SelectedValue == null || String.IsNullOrEmpty(courseCodeEdit.SelectedValue.ToString())) ?
                    courseCodeEdit2.Text.ToString() : courseCode.Text + " " + courseCodeEdit2.Text.ToString();
            }
        }

        // TODO fix this -> need to subscribe to key up on underlying scrollviewer; allow more complex searches
        private void courseCodeEdit_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            subjectSearchString = String.Join(String.Empty, e.Key.ToString());
            var subjectMatch = ((ComboBox)sender).Items.FirstOrDefault(subject => 
                ((string)subject).StartsWith(subjectSearchString, StringComparison.CurrentCultureIgnoreCase));
            if (subjectMatch != null)
            {
                ((ComboBox)sender).SelectedItem = subjectMatch;
                ((ComboBox)sender).IsDropDownOpen = true;
            }
            courseCodeEdit2_KeyDown(sender, e);
        }

        private void itemWeightValueEdit_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!(char.IsDigit((char) e.Key) || ( Char.Equals((char) e.Key, (char) 190) && !((TextBox)sender).Text.Contains('.') ))) {
                e.Handled = true;
                return;
            }

            double nextValue;
            if (Char.Equals((char) e.Key, (char) 190)) {
                nextValue = Double.Parse((string) ((TextBox)sender).Text.ToString() + ".");
            }
            else {
                nextValue = Double.Parse((string) ((TextBox)sender).Text.ToString() + Convert.ToChar(e.Key));
            }

            if (nextValue > 100.0 || nextValue < 0.0)
            {
                e.Handled = true;
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
                item = item > listView.Items.Count ? listView.Items.Count - 1 : item;
            }

            var tappedItem = listView.Items[item];
            listView.SelectedItem = tappedItem;
        }

        private void pageTitle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TextBlockTapped(pageTitle, pageTitleEdit);
        }

        private void TextBlockTapped(UIElement readBlock, TextBox editBox)
        {
            readBlock.Visibility = Visibility.Collapsed;
            editBox.Visibility = Visibility.Visible;
            editBox.Focus(FocusState.Programmatic);
        }

        private void TextBoxLostFocus(UIElement readBlock, UIElement editBox)
        {
            readBlock.Visibility = Visibility.Visible;
            editBox.Visibility = Visibility.Collapsed;
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
            //pageTitle.Visibility = Visibility.Visible;
            //pageTitleEdit.Visibility = Visibility.Collapsed;
            if (String.IsNullOrWhiteSpace(pageTitleEdit.Text))
            {
                if (!String.IsNullOrWhiteSpace(previousTextBoxString))
                {
                    pageTitleEdit.Text = previousTextBoxString;
                }
                else
                {
                    pageTitleEdit.Text = "New Course";
                }
            }
        }

        private void pageTitleEdit_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Escape)
            {
                pageTitleEdit_LostFocus(sender, null);
            }
        }

        private void courseCode_Tapped(object sender, TappedRoutedEventArgs e)
        {
            courseCode.Visibility = Visibility.Collapsed;
            courseCodeEditPanel.Visibility = Visibility.Visible;
            courseCodeEdit.Focus(FocusState.Programmatic);
        }

        private void courseCodeEdit_GotFocus(object sender, RoutedEventArgs e)
        {
            OnTextBoxFocus(courseCodeEdit2, e);
        }

        private void courseCodeEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxLostFocus(courseCode, courseCodeEditPanel);
            //courseCode.Visibility = Visibility.Visible;
            //courseCodeEditPanel.Visibility = Visibility.Collapsed;
            var codeStringList = courseCode.Text.Split(' ');
            courseCodeEdit2.Text = (String.IsNullOrWhiteSpace(courseCode.Text) || codeStringList.Length <= 1) ?
                "1A03" : codeStringList.Last();
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
            TextBlockTapped(itemSubtitle, itemSubtitleEdit);
        }

        private void itemTitleEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            itemTitleEdit.Text = (String.IsNullOrWhiteSpace(itemTitleEdit.Text)) ?
                "New Item Name" : itemTitleEdit.Text;
            TextBoxLostFocus(itemTitle, itemTitleEdit);
        }

        private void itemSubtitleEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            itemSubtitleEdit.Text = (String.IsNullOrWhiteSpace(itemSubtitleEdit.Text)) ?
                "New Item Type" : itemSubtitleEdit.Text;
            TextBoxLostFocus(itemSubtitle, itemSubtitleEdit);
        }


    }
}