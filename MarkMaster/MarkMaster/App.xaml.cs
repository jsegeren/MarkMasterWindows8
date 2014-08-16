using MarkMaster.Common;
using MarkMaster.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Split App template is documented at http://go.microsoft.com/fwlink/?LinkId=234228

namespace MarkMaster
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {

        // Shared data - global across app pages
        // Department names -> course code 
        private Dictionary<String, HashSet<McMasterCourse>> _departmentToCoursesMap;
        public Dictionary<String, HashSet<McMasterCourse>> DepartmentToCoursesMap
        {
            get
            {
                return _departmentToCoursesMap;
            }
            set
            {
                _departmentToCoursesMap = value;
            }
        }

        private Dictionary<McMasterCourse, String> _courseToNameMap;
        public Dictionary<McMasterCourse, String> CourseToNameMap
        {
            get
            {
                return _courseToNameMap;
            }
            set
            {
                _courseToNameMap = value;
            }
        }

        /**
         * Method to manually retrieve course data from the McMaster registar website directly
         **/
        private async void RetrieveCourseData()
        {
            HttpClient httpClient = new HttpClient();
            Uri sourceDataUrl = new Uri("https://adweb.cis.mcmaster.ca/mtt/U201409.html");
            HttpResponseMessage httpResponse = await httpClient.GetAsync(sourceDataUrl);
            DepartmentToCoursesMap = new Dictionary<string, HashSet<McMasterCourse>>();
            CourseToNameMap = new Dictionary<McMasterCourse, string>();

            if (httpResponse.StatusCode == HttpStatusCode.Ok)
            {
                string responseString = await httpResponse.Content.ReadAsStringAsync();
                responseString.ToString();

                // Now parse the raw HTML
                IList<String> departmentDataList = responseString.FindElements("<p>&nbsp;</p>", 
                    new List<String>() { "</table></td></tr><tr><td class=label colspan=10>" });
                foreach (String departmentData in departmentDataList)
                {
                    String departmentName = departmentData.Split(new string[] { "</td>" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                    IList<String> courseDataList = departmentData.FindElements("<td", new List<String>() { "</table></td></tr><tr>" });
                    foreach (String courseData in courseDataList)
                    {
                        string[] courseMetaList = courseData.Split(new string[] { "&nbsp;" }, StringSplitOptions.RemoveEmptyEntries);
                        String courseCode = courseMetaList[1].Trim();
                        String courseName = courseMetaList[3].Trim();
                        McMasterCourse newCourse = new McMasterCourse(departmentName, courseCode);
                        
                        // Update the department -> courses map
                        if (!DepartmentToCoursesMap.ContainsKey(departmentName))
                        {
                            DepartmentToCoursesMap.Add(departmentName, new HashSet<McMasterCourse>(){ newCourse });
                        }
                        else
                        {
                            DepartmentToCoursesMap[departmentName].Add(newCourse);
                        }

                        // Update the course -> course name map
                        // Note: we are assuming that the first name associated with a course is correct (i.e. that there is no conflict)
                        if (!CourseToNameMap.ContainsKey(newCourse))
                        {
                            CourseToNameMap.Add(newCourse, courseName);
                        }

                        //IList<String> elementList = course.FindElements("<td", new List<string>() { "</td>" });
                        //foreach (String element in elementList)
                        //{
                        //    string[] elementContentList = element.Split(new string[] { ">" }, StringSplitOptions.RemoveEmptyEntries);
                        //    if (elementContentList.Length > 1) {
                        //        elementContentList[1].ToString();
                        //    }
                        //    element.ToString();
                            //Regex regex = new Regex(@".[>]")
                            //Regex regex = new Regex(@"\s\d[A-Z]\w\d$", RegexOptions.IgnoreCase);
                        //}
                    }
                }

            }

            int x = DepartmentToCoursesMap.Count;
            int y = CourseToNameMap.Count;
        }

        //private async void RetrieveCourseData()
        //{
        //    // First retrieve the JSON from the timetable generating website (credits to TODO who?)
        //    HttpClient hyperTextClient = new HttpClient();
        //    Uri courseDataURL = new Uri("http://www.timetablegenerator.com/data/mcmaster_data.json");
        //    HttpResponseMessage hyperTextResponse = await hyperTextClient.GetAsync(courseDataURL);

        //    if (hyperTextResponse.StatusCode == HttpStatusCode.Ok)
        //    {
        //        CourseJSONData = await hyperTextResponse.Content.ReadAsStringAsync();

        //        // Then parse the JSON response dynamically (i.e. without declaring classes)
        //        dynamic resultItem = JsonConvert.DeserializeObject(CourseJSONData);
        //        Debug.WriteLine("First course: {0}", resultItem.courses[0].ToString());
        //    }
        //    else
        //    {
        //        throw new Exception(hyperTextResponse.StatusCode.ToString() + " " + hyperTextResponse.ReasonPhrase);
        //    }

        //    return;

        //}


        /// <summary>
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active

            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                //Associate the frame with a SuspensionManager key                                
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //Something went wrong restoring state.
                        //Assume there is no state and continue
                    }
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(ItemsPage)))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();

            // Perform initial course data retrieval / parsing
            // TODO fix and finish implementing this
            RetrieveCourseData();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            
            // First, make sure grades data is properly serialized, saved
            await GradesDataSource.SaveDataSourceAsync();

            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
