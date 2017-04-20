using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SecondaryViewsHelpers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App21
{
    internal class ProjectionViewPageInitializationData
    {
        public CoreDispatcher MainDispatcher;
        public ViewLifetimeControl ProjectionViewPageControl;
        public int MainViewId;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        MainPage rootPage;
        CoreDispatcher thisDispatcher;
        public SecondaryViewsHelpers.ViewLifetimeControl ProjectionViewPageControl;
        int thisViewId = 0;
        public MainPage()
        {
            this.InitializeComponent();

            // This is a static public property that allows downstream pages to get a handle to the MainPage instance
            // in order to call methods that are in this class.
            Current = this;
            rootPage = MainPage.Current;

            thisDispatcher = Window.Current.Dispatcher;

            thisViewId = ApplicationView.GetForCurrentView().Id;
        }

        //private async void ProjectionManager_ProjectionDisplayAvailableChanged(object sender, object e)
        //{
        //    await thisDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //    {
        //        if (ProjectionManager.ProjectionDisplayAvailable)
        //        {
        //            StartProjecting(null);
        //        }
        //    });
        //}

        private async void transitionBtn_Click(object sender, RoutedEventArgs e)
        {
            // Use the device selector query of the ProjectionManager to list wired/wireless displays
            String projectorSelectorQuery = ProjectionManager.GetDeviceSelector();

            // Calling the device API to find devices based on the device query
            //DeviceInformationCollection outputDevices = await DeviceInformation.FindAllAsync(projectorSelectorQuery);
            //DeviceInformation selectedDevice = outputDevices[0];

            // Start projecting to the selected display
            StartProjecting(null);
        }

        private async void StartProjecting(DeviceInformation selectedDisplay)
        {
            // If projection is already in progress, then it could be shown on the monitor again
            // Otherwise, we need to create a new view to show the presentation
            if (this.ProjectionViewPageControl == null)
            {
                // First, create a new, blank view
                await CoreApplication.CreateNewView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // ViewLifetimeControl is a wrapper to make sure the view is closed only
                    // when the app is done with it
                    this.ProjectionViewPageControl = ViewLifetimeControl.CreateForCurrentView();

                    // Assemble some data necessary for the new page
                    var initData = new ProjectionViewPageInitializationData();
                    initData.MainDispatcher = thisDispatcher;
                    initData.ProjectionViewPageControl = this.ProjectionViewPageControl;
                    initData.MainViewId = thisViewId;

                    // Display the page in the view. Note that the view will not become visible
                    // until "StartProjectingAsync" is called
                    var rootFrame = new Frame();
                    rootFrame.Navigate(typeof(ProjectionViewPage), initData);
                    Window.Current.Content = rootFrame;

                    // The call to Window.Current.Activate is required starting in Windos 10.
                    // Without it, the view will never appear.
                    Window.Current.Activate();
                });
            }

            try
            {
                // Start/StopViewInUse are used to signal that the app is interacting with the
                // view, so it shouldn't be closed yet, even if the user loses access to it
                rootPage.ProjectionViewPageControl.StartViewInUse();

                // Show the view on a second display that was selected by the user
                if (selectedDisplay != null)
                {
                    await ProjectionManager.StartProjectingAsync(rootPage.ProjectionViewPageControl.Id, thisViewId, selectedDisplay);
                }
                else
                {
                    await ProjectionManager.StartProjectingAsync(rootPage.ProjectionViewPageControl.Id, thisViewId);
                }

                rootPage.ProjectionViewPageControl.StopViewInUse();
            }
            catch (InvalidOperationException)
            {
                System.Diagnostics.Debug.WriteLine("Start projection failed");
            }
        }
    }
}
