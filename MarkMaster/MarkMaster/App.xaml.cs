﻿using MarkMaster.Common;
using MarkMaster.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        private string _courseJSONData;

        public string CourseJSONData
        {
            get
            {
                return _courseJSONData;
            }
            set
            {
                _courseJSONData = value;
            }
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

        //        // Then parse the JSON response
        //        var resultItem = JsonConvert.DeserializeObject(CourseJSONData);
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
            //RetrieveCourseData();
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
