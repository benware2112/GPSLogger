using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Store;
using Windows.Devices.Geolocation;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MappingUtilites;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls.Maps;
using System.Threading.Tasks;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.Popups;
using GPXLibU;
using Windows.Services.Maps;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GPSLogger
{
  public sealed partial class MainPage : Page 
  {
    GPXLibU.GPXLibUC1 GPX;
    DispatcherTimer timer;
    DispatcherTimer timerForReplayGPX;
    Stopwatch logTimer;
    Geolocator geolocator;

    LicenseChangedEventHandler licenseChangeHandler = null;
    private LicenseInformation licenseInformation;

    //      LocationIcon locationIcon;
    private bool gpsRunning;
    public object state { get; set; }
    public int savedDataCount { get; private set; }


    private Windows.Storage.Streams.IRandomAccessStreamReference thumbnail;
    private DisplayRequest dispRequest;
    private DateTime startTimeGPS;
    private DateTime startTimeTrip;
    private DateTimeOffset timeForNextReplayedPosition;
    private TimeSpan tripTime;
    private bool dataSaved = false;
    private int pausedCount = 0;
    FileSavePicker savePicker;
    StorageFile file;
		FileOpenPicker loadPicker;
		bool newSavePicker;
    private SimpleOrientationSensor orientation;
    private int versionLimit = 1;
    private double initialZoomLevel = 15.0f;
    private double zoomLevel;
    private double widthWide;
    private double heightWide;
    private double widthHigh;
    private double heightHigh;
    private double width;
    private double height;
    private bool orientationWide = true;
    private int debugMessage = 0; // false; // true; // 
    private bool proApp = false; //true; // 
    private MapPolyline routeLineAll_1;
    ////    private MapPolyline routeLineAll_2;
    //private MapPolyline routeLineLast10_1;
    //private MapPolyline routeLineLast10_2;
    private int whichRouteLineActiveAll;
    private int whichRouteLineActiveLast10;
    private int mapErrorCount = 0;
    private int whichRoute = 0;
    private int showTraceCount = 0;
    private int showLineNow = 2;
    private ApplicationDataContainer localSettings;
    private int interval = 1;
		private string pickedFilename;
    private MapIcon lastIcon;
    private bool isTimeForNewPosition;
    private int replayPosCount;
    private bool firstPoll = true;
    private int refreshPositionIndicator;
    private int maxRefreshPositionIndicator = 1;
    private double currentHeading;
    private bool showFullGPSDataLog;
    private int lastSavedIndexFromMapControl = 0;
    private int tickMultiplier = 1;//1000000;
    private bool fileRunning;
    private bool realTime;
    private bool drawWholePath;
    private bool rotateMapWithHeading;

    public MainPage()
    {
      this.InitializeComponent();
      // Activate a display-required request. If successful, the screen is 
      // guaranteed not to turn off automatically due to user inactivity.
      dispRequest = new DisplayRequest();
      dispRequest.RequestActive();

   //   InitializeLicense();

      if (timer == null)
      {
        timer = new DispatcherTimer();
        timer.Tick += TimerEventHandler;
        timer.Interval = new TimeSpan(0, 0, 0, 1);
        timer.Start();
      }
      if (timerForReplayGPX == null)
      {
        timerForReplayGPX = new DispatcherTimer();
        timerForReplayGPX.Tick += TimerEventHandlerReplayGPX;
        //      timerForReplayGPX.Interval = new TimeSpan(0, 0, 0, 1);
        timerForReplayGPX.Interval = new TimeSpan(1000000); // 50000);
      }

      localSettings = ApplicationData.Current.LocalSettings;
      loadLocalSettings();

      GPX = new GPXLibUC1();
			ButtonStartLogging.Visibility = Visibility.Visible;
			//ButtonStopLogging.Visibility = Visibility.Collapsed;

      showSettingsItems(false);
      showLogItems(false);

      showFullGPSDataLog = false; // true; // 

      textBlockMessage.Visibility = Visibility.Collapsed;  // Visibility.Visible; // 
      CheckBoxRunUnderLockScreen.Visibility = Visibility.Collapsed;
      //    ButtonViewLogs.Visibility = Visibility.Collapsed;
      TextBoxGPSList.Text = "";
      TextBoxGPSList.Visibility = Visibility.Collapsed;
      realTime = false; //  true;
      //     this.TextBoxGPSList.Size = new .Margin.Top = 129;
      TextBlockAppVersion.Text = String.Format("Version: {0}", GetApplicationVersion(versionLimit));

      savedDataCount = 0;
      proApp = true;
      if (proApp)
      {
 //       MyAdControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBlockMessageGoPro.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      }
      else
      {
 //       MyAdControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
        TextBlockMessageGoPro.Visibility = Windows.UI.Xaml.Visibility.Visible;
        TextBlockMessageGoPro.Text = "Don't want the ads?\r\nClick \"Download GPS Logger Pro\" button to get the Pro App\r\n\"GPS Logger Pro\" only $1.49 USD from the Windows Store\r\n";
 //       TextBlockMessageGoPro.Text = "Watch for Windows 10 version.\r\nWith offline maps!\r\nComing soon!";
      }

      TextBlockGradeLabel.Visibility = Visibility.Collapsed;
      TextBlockGrade.Visibility = Visibility.Collapsed;

      textBlockMessage1.Visibility = Visibility.Collapsed;
      textBlockMessage.Visibility = Visibility.Collapsed;
      textBlockZoomLevel.Visibility = Visibility.Collapsed;

      //    MyMap.Margin = Thickness.Top =  ( 250);// Margin. = (250,98,10,10);
    }

    void InitializeLicense()
    {
      // Initialize the license info for use in the app that is uploaded to the Store.
      // uncomment for release
      licenseInformation = CurrentApp.LicenseInformation;

      // Initialize the license info for testing.
      // comment the next line for release
      //licenseInformation = CurrentAppSimulator.LicenseInformation;

      // Register for the license state change event.
      licenseInformation.LicenseChanged += new LicenseChangedEventHandler(licenseChangedEventHandler);
      ReloadLicense();
    }
    void licenseChangedEventHandler()
    {
      ReloadLicense(); // code is in next steps
    }
    void ReloadLicense()
    {
      if (licenseInformation.IsActive)
      {
        if (licenseInformation.IsTrial)
        {
          appStatus(false);
        }
        else
        {
          appStatus(true);
        }
      }
      else
      {
        // A license is inactive only when there's an error.
      }
    }

    private void appStatus(bool v)
    {
      proApp = v;
      if (v)
      {
        buttonRemoveAds.Visibility = Visibility.Collapsed;
    //    TextBlockMessageGoPro.Visibility = Visibility.Collapsed;
        MyAdControl.Visibility = Visibility.Collapsed;
      }
      else
      {
        buttonRemoveAds.Visibility = Visibility.Visible;
        TextBlockMessageGoPro.Visibility = Visibility.Visible;
        MyAdControl.Visibility = Visibility.Visible;
      }
    }

    private void ConvertTrial_Click(object sender, RoutedEventArgs e)
    {
   //   BuyProApp();
    }

    /// <summary>
    /// Invoked when attempting to convert trial to full
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void buttonRemoveAds_Click(object sender, RoutedEventArgs e)
    {
      downloadProApp();
       //reviewMyApp();
      //searchForMyApp();
 //     popupRemoveAdsDialog();

    }
    private void buttonRateMyApp_Click(object sender, RoutedEventArgs e)
    {
      reviewMyApp();
    }
    private async void searchForMyApps()
    {
      string keyword = "Ben Byer";
      var uri = new Uri(string.Format(@"ms-windows-store:Publisher?name={0}", keyword));
      await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private async void searchForMyApp()
    {
      string keyword = "GPS-GPX Logger Pro";
      var uri = new Uri(string.Format(@"ms-windows-store:search?keyword={0}", keyword));
   //   uri = new Uri(string.Format(@"ms-windows-store:Publisher?name={0}", keyword));
      await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private async void reviewMyApp()
    {
      string appid = "29608BenByer.GPS-GPXLogger";
      appid = "29608BenByer.GPS-GPXLogger_sat5knwjgbh50";  // Windows 8.1 ad supported app
      appid = "29608BenByer.GPS-GPXLoggerPro_sat5knwjgbh50";  // Windows 8.1 paid app
      appid = "29608BenByer.GPS-Logger_sat5knwjgbh50";  // Windows 10 ad supported app
      var uri = new Uri(string.Format("ms-windows-store:Review?PFN={0}", appid));  // OurPackageFamilyName
      await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private async void downloadProApp()
    {
      string PFN = "29608BenByer.GPS-LoggerPro_sat5knwjgbh50";
      var uri = new Uri(string.Format("ms-windows-store://pdp/?PFN={0}", PFN));
      //var = new Uri(string.Format("ms-windows-store://pdp/?ProductId={0}", ProductId));//var uri = new Uri(string.Format(@"ms-windows-store:search?keyword={0}", keyword));
      await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private async void popupRemoveAdsDialog()
    {
      if (!licenseInformation.ProductLicenses["GPSProApp"].IsActive)
      {
        appStatus(false);
        try
        {
          // The customer doesn't own this feature, so 
          // show the purchase dialog.
          MessageDialog md;

          md = new MessageDialog("Do you wish to purchase this app and remove advertising?.\r\n\r\nClick Ok to approve a one-time purchase from the Windows App Store of $1.49 USD");
          md.Commands.Add(new UICommand("Ok", async (UICommandInvokedHandler) =>
          {

            //          await CurrentAppSimulator.RequestProductPurchaseAsync("GPSProApp");
            //LicenseInformation licenseInformation = CurrentAppSimulator.LicenseInformation;
            LicenseInformation licenseInformation = CurrentApp.LicenseInformation;
            //      NotifyUser("Buying the full license...", NotifyType.StatusMessage);
            if (licenseInformation.IsTrial)
            {
              try
              {
    //            await CurrentAppSimulator.RequestAppPurchaseAsync(false);
                await CurrentApp.RequestAppPurchaseAsync(false);
                if (!licenseInformation.IsTrial && licenseInformation.IsActive)
                {
                  appStatus(true);
                }
                else
                {
                  appStatus(false);
                  errorMessage(String.Format("IsTrial {0}  IsActive {1}", licenseInformation.IsTrial, licenseInformation.IsActive));
                }
              }
              catch (Exception ex)
              {
                errorMessage(ex.Message);
                appStatus(false);
              }
            }
            else
            {
              appStatus(true);
            }

            ReloadLicense();
          }));
          md.Commands.Add(new UICommand("Cancel"));
          await md.ShowAsync();

          //Check the license state to determine if the in-app purchase was successful.
        }
        catch (Exception ex)
        {
          errorMessage(ex.Message);
          // The in-app purchase was not completed because 
          // an error occurred.
        }
      }
      else
      {
        proApp = true;
      }
    }

    private void errorMessage(string message)
    {
      TextBlockMessageGoPro.Text = message;
      //  MessageDialog md;

      //md = new MessageDialog(message);
      //md.Commands.Add(new UICommand("Ok", (UICommandInvokedHandler) =>
      //{

      //}));
      //md.Commands.Add(new UICommand("Cancel"));
      //md.ShowAsync();
    }

    private void savelLocalSettings()
    {
      localSettings.Values["Units"] = (bool)RadioButtonImperial1.IsChecked;
      localSettings.Values["FileType"] = (bool)RadioButtonCSV1.IsChecked;
      localSettings.Values["ReportInterval"] = TextBoxSetInterval.Text;
      localSettings.Values["ShowMap"] = (bool)CheckBoxShowMap1.IsChecked;
      localSettings.Values["ColourScheme"] = (bool)CheckBoxColourScheme.IsChecked;
      localSettings.Values["ShowLatLong"] = (bool)CheckBoxShowLatLong.IsChecked;
      localSettings.Values["Rotate"] = (bool)CheckBoxRotate.IsChecked;
    }

    public string GetApplicationVersion(int num)
    {
      var ver = Windows.ApplicationModel.Package.Current.Id.Version;
      String version = String.Format("{0}", ver.Major);
      if (num > 0)
      {
        version += String.Format(".{0}", ver.Minor);
      }
      if (num > 1)
      {
        version += String.Format(".{0}", ver.Build);
      }
      //if (num > 2)
      //{
      //  version += String.Format(".{0}", ver.Revision);
      //}
      return version;
    }

    private void loadLocalSettings()
    {
      if (!localSettings.Values.ContainsKey("Units"))
      {
        // if the setting doesn't exist, probably wise to create it here.
        // setting the default to "false", but you can change to true if that makes more sense.
        localSettings.Values.Add("Units", false);
        RadioButtonMetric1.IsChecked = true;
        RadioButtonImperial1.IsChecked = false;
      }
      else
      {
        // read the value of the setting here.  
        // If we just created it, it should default to false (see above)
        if ((bool)localSettings.Values["Units"] == false) // units.METRIC))
        {
          RadioButtonMetric1.IsChecked = true;
          RadioButtonImperial1.IsChecked = false;
        }
        else
        {
          RadioButtonImperial1.IsChecked = true;
          RadioButtonMetric1.IsChecked = false;
        }
      }
      if (!localSettings.Values.ContainsKey("FileType"))
      {
        localSettings.Values.Add("FileType", false);
        RadioButtonCSV1.IsChecked = false;
        RadioButtonGPX1.IsChecked = true;
      }
      else
      {
        if ((bool)localSettings.Values["FileType"] == false)
        {
          RadioButtonCSV1.IsChecked = false;
          RadioButtonGPX1.IsChecked = true;
        }
        else
        {
          RadioButtonCSV1.IsChecked = true;
          RadioButtonGPX1.IsChecked = false;
        }
      }

      if (!localSettings.Values.ContainsKey("ReportInterval"))
      {
        localSettings.Values.Add("ReportInterval", 1);
      }
      else
      {
        TextBoxSetInterval.Text = localSettings.Values["ReportInterval"].ToString();
      }

      if (!localSettings.Values.ContainsKey("ShowMap"))
      {
        localSettings.Values.Add("ShowMap", false);

        MyMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBoxGPSList.Visibility = Windows.UI.Xaml.Visibility.Visible;
      }
      else
      {
        if ((bool)localSettings.Values["ShowMap"] == false)
        {
          CheckBoxShowMap1.IsChecked = false;
        }
        else
        {
          CheckBoxShowMap1.IsChecked = true;
        }
   //     ShowMap(0);
      }

      if (!localSettings.Values.ContainsKey("ShowLatLong"))
      {
        localSettings.Values.Add("ShowLatLong", true);
        CheckBoxShowLatLong.IsChecked = true;
        showLatLongTextBoxes(Visibility.Visible);
      }
      else
      {
        if ((bool)localSettings.Values["ShowLatLong"] == true)
        {
          CheckBoxShowLatLong.IsChecked = true;
          showLatLongTextBoxes(Visibility.Visible);
        }
        else
        {
          CheckBoxShowLatLong.IsChecked = false;
          showLatLongTextBoxes(Visibility.Collapsed);
        }
      }

      if (!localSettings.Values.ContainsKey("ColourScheme"))
      {
        localSettings.Values.Add("ColourScheme", false);
        CheckBoxColourScheme.IsChecked = false;
        MyMap.ColorScheme = MapColorScheme.Light;
      }
      else
      {
        if ((bool)localSettings.Values["ColourScheme"] == true)
        {
          CheckBoxColourScheme.IsChecked = true;
          MyMap.ColorScheme = MapColorScheme.Dark;
        }
        else
        {
          CheckBoxColourScheme.IsChecked = false;
          MyMap.ColorScheme = MapColorScheme.Light;
        }
      }

      if (!localSettings.Values.ContainsKey("Rotate"))
      {
        localSettings.Values.Add("Rotate", true);
        CheckBoxRotate.IsChecked = true;
        MyMap.Heading = currentHeading;
      }
      else
      {
        if ((bool)localSettings.Values["Rotate"] == true)
        {
          CheckBoxRotate.IsChecked = true;
          MyMap.Heading = currentHeading;
        }
        else
        {
          CheckBoxRotate.IsChecked = false;
          MyMap.Heading = 0;
        }
      }
      rotateMapWithHeading = (bool)CheckBoxRotate.IsChecked;
      //showLatLong();
    }

    private void showLatLongTextBoxes(Visibility show)
    {
      TextBlockLatLabel.Visibility = show;
      TextBlockLongLabel.Visibility = show;
      TextBlockLatitude.Visibility = show;
      TextBlockLongitude.Visibility = show;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      //MyMap.Center =
      //new Geopoint(new BasicGeoposition()
      //{
      //    Latitude = 47.604,
      //    Longitude = -122.329
      //});
      MyMap.ZoomLevel = 1;
      MyMap.LandmarksVisible = true;
      MyMap.BusinessLandmarksVisible = true;
  //    MyMap.ColorScheme = MapColorScheme.Dark;
  //    MyMap.
      base.OnNavigatedTo(e);
    }

    private void TimerEventHandler(object sender, object e)
    {
      DateTime now = DateTime.Now;
      TextBlockTime.Text = now.ToLocalTime().ToString(); // String.Format("{0:MMM dd, yyyy  HH:mm:ss}", now);

      if (GPX.fakeGPSData)
      {
        //if (locationIcon == null)
        //{
        //  locationIcon = new LocationIcon();
        //}
        //fakeDisplayPosition();
      }
      else
      {
        GPSUpdate(now);
      }
    }

    private void GPSUpdate(DateTime now)
    {
      GPX.UpdateLastFixSeconds();
      if (startTimeTrip.Ticks > 1000)
      {
        if (tripTime.Ticks == 0)
        {
          tripTime = now - startTimeTrip;
        }
        else
        {
          if (gpsRunning || fileRunning)
          {
            tripTime = tripTime.Add(TimeSpan.FromSeconds(1));
          }
        }

        //      if (logTimer)
        //         TextBlockTripTime.Text = String.Format("{0:00}:{1:00}:{2:00}"// tripTime.Hours, tripTime.Minutes, tripTime.Seconds);
        if (logTimer != null)
        {
          TimeSpan ts = logTimer.Elapsed;
          TextBlockTripTime.Text = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
          //          TextBlockTripTime.Text = String.Format("{0}", logTimer.Elapsed); // tripTime.Hours, tripTime.Minutes, tripTime.Seconds);
        }
        else
        {
          TextBlockTripTime.Text = "0";
        }
      }
      if (gpsRunning)
      {
        updateGPSScreenData();
      }
      if (savedDataCount > 0 && now.Second == 0) // && showFullGPSDataLog)
      {
        if (dataSaved && gpsRunning)
        {
          saveTheData();
        }
      }
    }

    private void updateGPSScreenData()
    {
      String str;
      BasicGeopositionExtended currpos = GPX.GetCurrentLocation();
      double speed = 0;
      double KMH_MPHConversion = 1;
      double metresOrFeet = 1;
      double metresOrMiles = 1;

      KMH_MPHConversion = GPX.GetMetresPerSecondToKMH_MPHConversion((bool)RadioButtonMetric1.IsChecked);
      metresOrFeet = GPX.GetMetresToFeetConversion((bool)RadioButtonImperial1.IsChecked);
      metresOrMiles = GPX.GetMetresToMilesConversion((bool)RadioButtonImperial1.IsChecked);
      try
      {
        if (currpos != null)
        {
          if (currpos.Speed != null)
          {
            speed = (double)(currpos.Speed * KMH_MPHConversion);
            if (Double.IsNaN(speed))
            {
              speed = 0.0;
            }
          }
        }

        try
        {
          TextBlockSpeed.Text = String.Format("{0:0.0}", speed) + getUnits(1);
          TextBlockMaxSpeed.Text = String.Format("{0:0.0}", GPX.MaxSpeed * KMH_MPHConversion) + getUnits(1);
          TextBlockLatitude.Text = String.Format("{0:0.000000}", currpos.pos.Latitude);
          TextBlockLongitude.Text = String.Format("{0:0.000000}", currpos.pos.Longitude);
          TextBlockAltitude.Text = String.Format("{0:0.0}", currpos.pos.Altitude * metresOrFeet) + getUnits(2);
          TextBlockHeading.Text = GPX.GetFlightDirection((int)currpos.Heading);
          currentHeading = (double)currpos.Heading;
          TextBlockMaxElevation.Text = String.Format("{0:0.0}", GPX.MaxElevation * metresOrFeet) + getUnits(2);
          if (logTimer != null)
          {
            TimeSpan ts = logTimer.Elapsed;
            TextBlockTripTime.Text = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            //           TextBlockTripTime.Text = String.Format("{0:00}:{1:00}:{2:00}", tripTime.Hours, tripTime.Minutes, tripTime.Seconds);
          }
        }
        catch (Exception ex)
        {
          //if (ex.Message.Contains("Object reference"))
          //{
          //  TextBlockTripTime.Text = "0";
          //}
          //else
          //{
          //  TextBlockTripTime.Text = ex.Message;
          //}
        }
        TextBlockGrade.Text = GPX.getGrade();
        TextBlockMinElevation.Text = String.Format("{0:0.0}", GPX.MinElevation * metresOrFeet) + getUnits(2);
        TextBlockPositionCount.Text = GPX.positionsCount.ToString();
        TextBlockLastFixTime.Text = String.Format("Last Fix: {0}", GPX.GetLastFixTime());
        //       if ()
        //          GPX.DistanceTravelled = 13610;
        if (GPX.DistanceTravelled < 1609)
        {
          String units = getUnits(2);
          if (units == " m")
          {
            if (GPX.DistanceTravelled > 1000)
            {
              TextBlockDistance.Text = String.Format("{0:0.00} {1}", GPX.GetDistanceTravelled() / metresOrMiles, getUnits(3));
            }
            else
            {
              TextBlockDistance.Text = String.Format("{0} {1}", GPX.GetDistanceTravelled() / metresOrMiles, units);
            }
          }
          else
          {
            if (GPX.DistanceTravelled < 1609)
            {
              TextBlockDistance.Text = String.Format("{0:0.0} {1}", GPX.DistanceTravelled * metresOrFeet, units);
            }
            else
            {
              TextBlockDistance.Text = String.Format("{0:0.0} {1}", GPX.GetDistanceTravelled() * metresOrFeet, units);
            }
          }
        }  
        else
        {
          TextBlockDistance.Text = String.Format("{0:0.00} {1}", GPX.GetDistanceTravelled() * metresOrMiles, getUnits(3));
        }

        str = "Current Position";
        PositionStatus status = geolocator.LocationStatus;
        str += "\r\nStatus: " + GPX.GetStatusString(status);
        str += "\r\nPositionSource: " + currpos.geoPosition.Coordinate.PositionSource.ToString();
        str += "\r\nLatitude: " + String.Format("{0:0.000000}", currpos.pos.Latitude);
        str += "\r\nLongitude: " + String.Format("{0:0.000000}", currpos.pos.Longitude);
        str += "\r\nPos Accuracy: " + String.Format("{0:0.0}", currpos.geoPosition.Coordinate.Accuracy * metresOrFeet) + getUnits(2);
        str += "\r\nAltitude: " + String.Format("{0:0.0}", currpos.pos.Altitude) + getUnits(2);
        str += "\r\nAlt Accuracy: " + String.Format("{0:0.0}", (currpos.geoPosition.Coordinate.AltitudeAccuracy * metresOrFeet)) + getUnits(2);
        str += "\r\nAlt Reference: " + currpos.geoPosition.Coordinate.Point.AltitudeReferenceSystem.ToString();
        str += "\r\nSpeed: " + String.Format("{0:0.0}", speed) + getUnits(1);
        str += "\r\nHeading: " + String.Format("{0:0.0}", currpos.Heading);
        if (currpos.geoPosition.CivicAddress != null)
        {
          str += "\r\nCity: " + currpos.geoPosition.CivicAddress.City;
          str += "\r\nProv/State: " + currpos.geoPosition.CivicAddress.State;
          str += "\r\nPostal/Zip Code: " + currpos.geoPosition.CivicAddress.PostalCode;
        }
        str += "\r\nTimestamp: " + currpos.Timestamp.ToString();

        TextBoxPositionData.Text = str;
        //          TextBoxPositionData2.Text = str;

        // GPX.positionString;
        if (showFullGPSDataLog)
        {
          //TextBoxGPSList.Text = GPX.GetCSVData();
        }
      }
      catch (Exception ex)
      {

      }
    }

    private void Button_ClearDistance(object sender, RoutedEventArgs e)
    {
      MessageDialog md;

      md = new MessageDialog("Are you sure you want to reset all the trip data?\r\n\r\nThis will not clear the log data or the path trace.");
      md.Commands.Add(new UICommand("Yes", (UICommandInvokedHandler) =>
      {
        debugMessage = 0;
        GPX.ClearData(1);
        GPX.ClearData(2);
        GPX.ClearData(3);
        GPX.ClearData(4);
        startTimeTrip = DateTime.Now;
        tripTime = new TimeSpan(0);
        logTimer = new Stopwatch();
        logTimer.Start();
      }));
      md.Commands.Add(new UICommand("No"));
      md.ShowAsync();
    }

    //private void DrawTrace()
    private async void onGeolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
    {
      // Need to set map view on UI thread.
      await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
      {
        BasicGeopositionExtended extPos = GPX.GetBasicGeopositionExtendedFromGeoposition(args.Position);

        GPX.AddBasicGeoPositionToArray(extPos);

        gpsRunning = true;
        fileRunning = false;

        displayCurrentPositionIconOnMap(extPos);

        drawLivePath();

        updateGPSScreenData();
        //       processGPSPosition(extPos);
        String str;// = String.Format("{0}  {1}  {2}", pos.Coordinate.Point.Position.Latitude, pos.Coordinate.Point.Position.Longitude, pos.Coordinate.Timestamp);
        str = String.Format("GPS device running.\r\n\r\nCount: {0}  Lat: {1:0.000000}  Long: {2:0.000000}  Alt: {3:0.0}  Time: {4}  Speed: {5:0.0}\r\n", GPX.positionsCount, extPos.pos.Latitude, extPos.pos.Longitude,
                                                                                                           extPos.pos.Altitude, extPos.Timestamp.ToString(), extPos.Speed);
        str = String.Format("GPS device running.\r\n\r\nCount: {0}  Lat: {1:0.000000}  Long: {2:0.000000}  PrevLat: {3:0.000000}  PrevLong: {4:0.000000}\r\n", 
            GPX.positionsCount, GPX.Latitude, GPX.Longitude, GPX.PrevLatitude, GPX.PrevLongitude);
        //  str = String.Format("Count: {0}", GPX.positionsListUnSaved.Count);
        TextBoxGPSList.Text = str;
        textBlockMessage1.Text = str;

      }));
    }

    private void displayCurrentPositionIconOnMap(BasicGeopositionExtended pos)
    {
      if (pos != null)
      {
        if (gpsRunning || fileRunning)
        {
          if (pos != null)
          {
            GPX.PositionChanged(pos, showFullGPSDataLog); // args.Position);
          }
        }

        refreshPositionIndicator--;
        if (firstPoll || refreshPositionIndicator == 0)
        {
          MyMap.Center = new Geopoint(new BasicGeoposition()
          {
            Latitude = pos.pos.Latitude,
            Longitude = pos.pos.Longitude
          });
          if (firstPoll)
          {
            zoomLevel = MyMap.ZoomLevel = initialZoomLevel;
          }
        }

        if (refreshPositionIndicator <= 0)
        {
          refreshPositionIndicator = maxRefreshPositionIndicator;
        }
        firstPoll = false;
        if (GPX.positionsCount > 0 || GPX.positionsListReplay.Count > 0)
        {
          zoomLevel = MyMap.ZoomLevel;
        }
        else
        {
          zoomLevel = MyMap.ZoomLevel = initialZoomLevel;
        }

        var image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Circle.png"));
        Geopoint bp = new Geopoint(pos.pos);
        var icon = new MapIcon
        {
          Location = bp,
          NormalizedAnchorPoint = new Point(0.5, 0.5),
          Image = image,
          ZIndex = 5
        };
        if (lastIcon != null)
        {
          MyMap.MapElements.Remove(lastIcon);
        }
        MyMap.MapElements.Add(icon);
        lastIcon = icon;
      }
    }

    //private void displayTextFields(Geoposition pos, BasicGeoposition basicPos)
    //{
    //  double speed = 0.0F;
    //  double KMH_MPHConversion = 1;
    //  double metresOrFeet = 1;
    //  double metresOrMiles = 1;

    //  KMH_MPHConversion = GPX.GetMetresPerSecondToKMH_MPHConversion((bool)RadioButtonMetric1.IsChecked);
    //  metresOrFeet = GPX.GetMetresToFeetConversion((bool)RadioButtonImperial1.IsChecked);
    //  metresOrMiles = GPX.GetMetresToMilesConversion((bool)RadioButtonImperial1.IsChecked);
    //  if (pos != null)
    //  {
    //    speed = (double)(pos.Coordinate.Speed * KMH_MPHConversion);
    //    TextBlockHeading.Text = GPX.GetFlightDirection((int)pos.Coordinate.Heading);
    //    currentHeading = (double)pos.Coordinate.Heading;
    //    if (!double.IsNaN(currentHeading))
    //    {
    //      if (rotateMapWithHeading)
    //      {
    //        MyMap.Heading = currentHeading;
    //      }
    //      else
    //      {
    //        MyMap.Heading = 0;
    //      }
    //    }
    //  }
    //  if (double.IsNaN(speed))
    //  {
    //    speed = 0;
    //  }
    //  TextBlockSpeed.Text = String.Format("{0:0.0}", speed) + getUnits(1); //.ToString();
    //                                                                       //TextBlockHeading.Text = pos.Coordinate.Heading.ToString();
    //  TextBlockLatitude.Text = basicPos.Latitude.ToString();
    //  TextBlockLongitude.Text = basicPos.Longitude.ToString();
    //  TextBlockPositionCount.Text = GPX.positionsList.Count.ToString();

    //}

    private string getUnits(int p)
  {
    if (p == 1)
    {
      if ((bool)RadioButtonMetric1.IsChecked)
        return " kmh";
      if ((bool)RadioButtonImperial1.IsChecked)
        return " mph";
    }
    if (p == 2)
    {
      if ((bool)RadioButtonMetric1.IsChecked)
        return " m";
      if ((bool)RadioButtonImperial1.IsChecked)
        return " ft";
    }
    if (p == 3)
    {
      if ((bool)RadioButtonMetric1.IsChecked)
        return " km";
      if ((bool)RadioButtonImperial1.IsChecked)
        return " mi";
    }
    return "";
    }

    private void Button_StartLogging(object sender, RoutedEventArgs e)
    {
      if ((string)ButtonStartLogging.Content == "Start" || (string)ButtonStartLogging.Content == "Resume")
      {
        ButtonStartLogging.Content = "Running";
        if (!gpsRunning)
        {
          ButtonSave.IsEnabled = true;
          ButtonClear.IsEnabled = true;
          ButtonClearDistance.IsEnabled = true;

          if (pausedCount == 0)
          {
            startTimeTrip = startTimeGPS = DateTime.Now;
            logTimer = new Stopwatch();

            //   logTimer.Tick += OnTimerTick;
          }
          logTimer.Start();
          gpsRunning = true;
          TextBoxGPSList.Text = "Getting GPS Ready\r\n\r\nPlease wait...";
          TextBlockFileSaved.Text = "File not saved.";
          TextBlockLastFixTime.Text = "Initializing...";

          initGeolocator();

          //    ButtonStartLogging.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
          //ButtonStopLogging.Visibility = Windows.UI.Xaml.Visibility.Visible;

          firstPoll = true;
          zoomLevel = 20;
        } 
      }
      else
      {
        if ((string)ButtonStartLogging.Content == "Pause")
        {
          //        ButtonStartLogging.Visibility = Windows.UI.Xaml.Visibility.Visible;
          ButtonStartLogging.Content = "Resume";

          if (logTimer != null)
          {
            logTimer.Stop();
          }
          pausedCount++;
          gpsRunning = false;
          geolocator = null;
        }
      }
      //		DrawPath(sender, e);
    }

    private void initGeolocator()
    {
      interval = Convert.ToInt16(TextBoxSetInterval.Text);
      geolocator = new Geolocator();
      geolocator.DesiredAccuracy = PositionAccuracy.High;
      geolocator.MovementThreshold = 1;
      geolocator.ReportInterval = (uint)(interval * 1000);
      geolocator.DesiredAccuracyInMeters = 1;

      geolocator.PositionChanged += new Windows.Foundation.TypedEventHandler<Geolocator, PositionChangedEventArgs>(onGeolocator_PositionChanged);
    }

    private async void buttonStart_Click(object sender, RoutedEventArgs e)
		{      
		}

    private async void loadPath()
    {
      if (loadPicker == null)
      {
        loadPicker = new FileOpenPicker();
      }
      loadPicker.FileTypeFilter.Add(".gpx");
      loadPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder; //.DocumentsLibrary;

      StorageFile file = await loadPicker.PickSingleFileAsync();

      if (file != null)
      {
        pickedFilename = file.Path;
        GPX.CurrentFileTrace = await FileIO.ReadTextAsync(file);
        textBlockMessage.Text = pickedFilename;// GPX.CurrentTrace;
      }
      else
      {
        textBlockMessage.Text = "Cancelled";
      }
    }

    private async void DrawRealTimePathFromFile(object sender, RoutedEventArgs e)
    {
      var strokeColor = Colors.Blue;
      int count = 0;

      zoomLevel = MyMap.ZoomLevel = initialZoomLevel;
      var fullTrace = PointList.GetLinesFromFile(GPX.CurrentFileTrace);

      var points = fullTrace.ElementAt(0);
      for (int x = 1; x < points.Points.Count; x++)
      {
        var thePoint1 = points.Points[x - 1];
        var thePoint2 = points.Points[x];
        for (int a = 0; a < 2000; a++)
          for (int b = 0; b < 1500; b++)
          //           for (int c = 0; c < 1000000; c++)
          //for (int d = 0; d < 1000000; d++)
          //  for (int f = 0; f < 1000000; f++)
          {
            a = a;
            b = b;
            // c = c;
          }
        DrawPathBetweenTwoPoints(thePoint1, thePoint2);
  //      Sleep(100);
      }
    }

    private void DrawPathBetweenTwoPoints(Geopoint p1, Geopoint p2)
    {
      var strokeColor = Colors.Blue;
      int count = 0;
      var pathPoints = PointList.GetSegment(p1, p2);// FromString(s1, s2);

      if (p1 != null & p2 != null)
      {
        count = DrawTheTrace(strokeColor, pathPoints);
      }
    }

    private async void DrawWholePathFromFile(object sender, RoutedEventArgs e)
    {
      DrawWholePathFromFile();
    }

    private void DrawWholePathFromFile()
    {
      var strokeColor = Colors.Blue;
      int count = 0;

      zoomLevel = MyMap.ZoomLevel = initialZoomLevel;
      var fullTrace = PointList.GetLinesFromFile(GPX.CurrentFileTrace);
      count = DrawTheTrace(strokeColor, fullTrace);
 //     var ex = MapExtensions.GetViewArea(MyMap);
    }

    private async void drawLivePath() //object sender, RoutedEventArgs e)
    {
      var strokeColor = Colors.Blue;
      int count = 0;
      var pathPoints = PointList.GetSegment(GPX);
      if (GPX.positionsCount > 1)
      {
        textBlockZoomLevel.Text = MyMap.MapElements.Count.ToString();
        count = DrawTheTrace(strokeColor, pathPoints);
      }
    }

    private void buttonLoad_Click(object sender, RoutedEventArgs e)
		{
      if ((string)buttonLoad.Content == "Browse...")
      {
        loadPath();
        buttonLoad.Content = "Draw";
        drawWholePath = false;
        if ((bool)RadioButtonFullTrace.IsChecked)
        {
          drawWholePath = true;
        }
        if ((bool)RadioButtonRealTime.IsChecked)
        {
          realTime = true;
        }
        if ((bool)RadioButtonFast.IsChecked)
        {
          realTime = false;
        }
      }
      else
      {
        if (!String.IsNullOrEmpty(GPX.CurrentFileTrace))
        {
          firstPoll = true;
          MyMap.ZoomLevel = 15;

          processLoadedFile();
          logTimer = new Stopwatch();
          logTimer.Start();

          if (drawWholePath)
          {
            DrawWholePathFromFile();
          }
          else
          {
            timerForReplayGPX.Start();
          }

          buttonLoad.Content = "Browse...";
          GPX.fakeGPSData = true;
          ButtonSave.IsEnabled = true;
          ButtonClearDistance.IsEnabled = true;
          ButtonClear.IsEnabled = true;
          isTimeForNewPosition = true;

          showLogItems(false);
        }
      }      //	DrawPath();
    }

    private void TimerEventHandlerReplayGPX(object sender, object e)
    {

      BasicGeoposition bg = new BasicGeoposition();

      if (isTimeForNewPosition)
      {
        timerForReplayGPX.Stop();

        BasicGeopositionExtended thisPos;
        long nextPosTime = 0;
        thisPos = getNextPosition(ref nextPosTime);
        if (thisPos != null)
        {
          if (nextPosTime == 1443093683)
          {
            nextPosTime = nextPosTime;
          }

          GPX.AddBasicGeoPositionToArray(thisPos);

          gpsRunning = false;
          fileRunning = true;
          displayCurrentPositionIconOnMap(thisPos);
          drawLivePath();
          updateGPSScreenData();

          //       processGPSPosition(extPos);
          String str;// = String.Format("{0}  {1}  {2}", pos.Coordinate.Point.Position.Latitude, pos.Coordinate.Point.Position.Longitude, pos.Coordinate.Timestamp);
          str = String.Format("Count: {0}  Lat: {1:0.000000}  Long: {2:0.000000}  Alt: {3:0.0}  Time: {4}  Speed: {5:0.0}\r\n", GPX.positionsCount, thisPos.pos.Latitude, thisPos.pos.Longitude,
                                                                                                             thisPos.pos.Altitude, thisPos.Timestamp.ToString(), thisPos.Speed);
          //  str = String.Format("Count: {0}", GPX.positionsListUnSaved.Count);
          TextBoxGPSList.Text = str;

          //      isTimeForNewPosition = false;
          timerForReplayGPX = new DispatcherTimer();
          timerForReplayGPX.Tick += TimerEventHandlerReplayGPX;
          //      timerForReplayGPX.Interval = new TimeSpan(0, 0, 0, 1);
          if (realTime)
          {
            tickMultiplier = 10000000;
          }
          else
          {
            tickMultiplier = 1;
          }
          timerForReplayGPX.Interval = new TimeSpan((nextPosTime - thisPos.Timestamp) * tickMultiplier); // (0, 0, (int)(nextPosTime - thisPos.Timestamp)); // 50000);
          timerForReplayGPX.Start();
          //     processGPSPosition(pos);
        }
        //       ProcessPosition(pos);
      }
    }

    private BasicGeopositionExtended getNextPosition(ref long nextPositionTime)
    {
      BasicGeopositionExtended geo = null;// new Geoposition();

      geo = GPX.GetNextPositionForReplay(ref nextPositionTime); 

      return geo;
    }

    private void processLoadedFile()
    {
      int count = 0;
      //    List<Geopoint> geoPointList = PointList.GetLinesfromFileIntoList(GPX.CurrentFileTrace);
      GPX.GetBasicGeopositionExtendedFromString();

      count = count;
        ////String[] segments = GPX.CurrentFileTrace.Split()
    }

    private int DrawTheTrace(Color strokeColor, IEnumerable<PointList> fullTrace)
    {
      double lat = 49.90;
      double lon = -119.45;
      int count = 0;
      int points = 0;
  //    fullTrace[0].Points
      foreach (var dataObject in fullTrace)
      {
        var shape = new MapPolyline
        {
          StrokeThickness = 2,
          StrokeColor = strokeColor,
          StrokeDashed = false,
          ZIndex = 4,
          Path = new Geopath(dataObject.Points.Select(p => p.Position))
        };
        count++;
        MyMap.MapElements.Add(shape);
        lat = dataObject.Points[dataObject.Points.Count - 1].Position.Latitude;
        lon = dataObject.Points[dataObject.Points.Count - 1].Position.Longitude;
        points = dataObject.Points.Count;
      }
      MyMap.ZoomLevel = zoomLevel;
      if (count > 1 || points > 2)
      {
        MyMap.Center = new Geopoint(new BasicGeoposition()
        {
          Latitude = lat,
          Longitude = lon
        });
      }
      return count;
    }

    private void Button_Save(object sender, RoutedEventArgs e)
    {
      saveTheData();
    }
    private void saveTheData()
    {
      String GPXString = "";
      RadioButtonCSV1.IsEnabled = false;
      RadioButtonGPX1.IsEnabled = false;
      RadioButtonGPX1.IsEnabled = false;
      if ((bool)RadioButtonGPX1.IsChecked)
      {
        if (showFullGPSDataLog)
        {
          GPXString = GPX.GetNewGPXData();
        }
        else
        {
   //       GPXString = getGPXStringFromMapControl();
          GPXString = getGPXStringFromDataArray();
        }
      }
      else
      {
        GPXString = GPX.GetCSVData();
      }

      savedDataCount++;

      if (GPXString.Length > 0)
      {
        saveClickUseFileSavePicker(GPXString);
      }
      //         String filename = "D:\\Temp\\test5.csv";
      //      saveClickManual(filename);
    }

    private string getGPXStringFromDataArray()
    {
      string data = "";
      TextBoxGPSList.Text = String.Format("GPX.positionsListUnSaved.Count: {0}\r\n", GPX.positionsListUnSaved.Count);
      string positionsString = GPX.GetUnsavedPositions();
      TextBoxGPSList.Text += positionsString;
      data = GPX.GetNewGPXData(savedDataCount, positionsString);

      return data;
    }

    private string getGPXStringFromMapControl()
    {
      string data = "";
      string str = "";
      int index = 0;
      var lines = MyMap.MapElements.ToArray();

      for (int x = lastSavedIndexFromMapControl; x < lines.Count(); x++)
      {
        index = x;
        if (lines[x].GetType() == typeof(MapPolyline))
        {
          MapPolyline pp = (MapPolyline)lines[x];
          str += String.Format("{0},{1},{2},{3},{4},{5}\r\n", pp.Path.Positions[1].Latitude, pp.Path.Positions[1].Longitude, pp.Path.Positions[1].Altitude, 0, 0, 0); // MyMap.MapElements[x].ToString();
          //Geoposition curpos = GPX.GetCurrentLocation();
          //str += String.Format("{0},{1},{2},{3},{4},{5}\r\n", curpos.Coordinate.Point.Position.Latitude, curpos.Coordinate.Point.Position.Longitude, curpos.Coordinate.Point.Position.Altitude, 
          //                                                    curpos.Coordinate.Timestamp, curpos.Coordinate.Speed, GPX.GetDistance()); // MyMap.MapElements[x].ToString();
        }
        else
        {
          index = index;
        }
      }
      data = GPX.GetNewGPXData(lastSavedIndexFromMapControl, str);
      lastSavedIndexFromMapControl = index;

      return data;
    }

    private string getCSVData(string positionsString)
    {
      String str = "";

      str = positionsString;

      return str;
    }

    async void saveClickUseFileSavePicker(String saveData)
    {
      if (savePicker == null || newSavePicker)
      {
        savePicker = new FileSavePicker();

        savePicker.SuggestedStartLocation = PickerLocationId.ComputerFolder; //.DocumentsLibrary;


        // Default extension if the user does not select a choice explicitly from the dropdown
        if (saveData.Contains("xml"))
        {
          savePicker.DefaultFileExtension = ".gpx";
        // Dropdown list of file types the user can save the file as
          savePicker.FileTypeChoices.Add("GPX File", new List<string>() { ".gpx" });
          savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });

        }
        else
        {
          savePicker.DefaultFileExtension = ".csv";
          // Dropdown list of file types the user can save the file as
          savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });
          savePicker.FileTypeChoices.Add("GPX File", new List<string>() { ".gpx" });
        }
      }
      savePicker.SuggestedFileName = String.Format("{0}{1}", startTimeGPS, savePicker.DefaultFileExtension);
      savePicker.SuggestedFileName = savePicker.SuggestedFileName.Replace(":", "-");
      if (file == null || newSavePicker)
      {
        dataSaved = true;
        try
        {
          file = await savePicker.PickSaveFileAsync();
        }
        catch (Exception ex)
        {
        }
      }
      if (file != null)
      {
        // Application now has read/write access to the saved file

   //     await Windows.Storage.FileIO.WriteTextAsync(file, saveData);
        await FileIO.AppendTextAsync(file, saveData);
        if (file != null)
        {
          TextBlockFileSaved.Text = String.Format("File saved: {0}", file.Path); //.SuggestedFileName);
          newSavePicker = false;
        }                //         status.Text = "Your File Successfully Saved";
      }
      else
      {
        TextBlockFileSaved.Text = String.Format("File not saved:");
        dataSaved = false;

        //          status.Text = "File was not returned";
      }
      //      await filePicker.PickSaveFileAsync();
    }

    private void Button_Clear(object sender, RoutedEventArgs e)
    {
      clearData();
    }

    private void clearData()
    {
      MessageDialog md;

      md = new MessageDialog("Are you sure you want to clear all the data?");
      md.Commands.Add(new UICommand("Yes", (UICommandInvokedHandler) =>
      {
        saveTheData();

        //       changecount = 0;
        //     positionsString = "";
        GPX.ClearData();
        startTimeTrip = startTimeGPS = DateTime.Now;

        TextBoxGPSList.Text = "";
        TextBoxPositionData.Text = "";
        TextBlockPositionCount.Text = "0";

        newSavePicker = true;
        //     savePicker = null;
        file = null;
        dataSaved = false;

        logTimer = new Stopwatch();
        logTimer.Start();

        RadioButtonCSV1.IsEnabled = true;
        RadioButtonGPX1.IsEnabled = true;

        TextBlockFileSaved.Text = "File not saved.";

        MyMap.MapElements.Clear();

      }));
      md.Commands.Add(new UICommand("No"));
      md.ShowAsync();
    }

    private async void Button_Settings(object sender, RoutedEventArgs e)
    {
      showSettingsItems(true);
    }

    private void showSettingsItems(bool hideOrShow)
    {
      if (hideOrShow) // Show settings stuff
      {
        ItemsControlSettings.Visibility = Windows.UI.Xaml.Visibility.Visible;
        ItemsControlSettings2.Visibility = Windows.UI.Xaml.Visibility.Visible;
        TextBoxSettingsContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;

        showTopRowButtonsVisible(false);
        TextBlockShowMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        TextBoxGPSList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBoxPositionData.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
  //      TextBoxPositionData2.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        CheckBoxShowMap1.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        MyMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      }
      else
      {
        ItemsControlSettings.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        ItemsControlSettings2.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBoxSettingsContainer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        //  = true;

        showTopRowButtonsVisible(true);
        CheckBoxShowMap1.Visibility = Windows.UI.Xaml.Visibility.Visible;
    //    TextBlockFileSaved.Visibility = Windows.UI.Xaml.Visibility.Visible;
        TextBlockShowMap.Visibility = Windows.UI.Xaml.Visibility.Visible;

        ShowMap(0);
      }
    }
    private void showLogItems(bool hideOrShow)
    {
      if (hideOrShow) // Show logs stuff
      {
        //        ItemsControlLogs.Visibility = Windows.UI.Xaml.Visibility.Visible;
        TextBoxLogContainer.Visibility = Visibility.Visible;
        ItemsControlLogFiles.Visibility = Visibility.Visible;
        showTopRowButtonsVisible(false);
        TextBoxGPSList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBoxPositionData.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        MyMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      }
      else
      {
        TextBoxLogContainer.Visibility = Visibility.Collapsed;
        ItemsControlLogFiles.Visibility = Visibility.Collapsed;

        showTopRowButtonsVisible(true);

        MyMap.Visibility = Windows.UI.Xaml.Visibility.Visible;
        ShowMap(0);
      }
    }

    private void Button_CloseLogFilesClick(object sender, RoutedEventArgs e)
    {
      showLogItems(false);
    }

    private async void ShowMap(int which)
    {
      if ((bool)CheckBoxShowMap1.IsChecked)
      {
        MyMap.Visibility = Windows.UI.Xaml.Visibility.Visible;
        TextBoxGPSList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBoxPositionData.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
//        showWide(orientationWide);// //widthWide > heightWide);
        localSettings.Values["ShowMap"] = true;
      }
      else
      {
        MyMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
  //      TextBoxGPSList.Visibility = Windows.UI.Xaml.Visibility.Visible;

        if (orientationWide)
        {
          TextBoxPositionData.Visibility = Windows.UI.Xaml.Visibility.Visible;
  //        TextBoxPositionData2.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        else
        {
          TextBoxPositionData.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
    //      TextBoxPositionData2.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
        localSettings.Values["ShowMap"] = false;
      }
    }

    private void showTopRowButtonsVisible(bool visible)
    {
      if (visible)
      {
        ButtonStartLogging.Visibility = Windows.UI.Xaml.Visibility.Visible;
        ButtonSave.Visibility = Windows.UI.Xaml.Visibility.Visible;
        ButtonClear.Visibility = Windows.UI.Xaml.Visibility.Visible;
        TextBlockFileSaved.Visibility = Windows.UI.Xaml.Visibility.Visible;
        ButtonSettings1.Visibility = Windows.UI.Xaml.Visibility.Visible;
        ButtonViewLogs.Visibility = Windows.UI.Xaml.Visibility.Visible;
        CheckBoxShowMap1.Visibility = Windows.UI.Xaml.Visibility.Visible;
        TextBlockShowMap.Visibility = Windows.UI.Xaml.Visibility.Visible;
      }
      else
      {
        ButtonStartLogging.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        ButtonSave.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        ButtonClear.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBlockFileSaved.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        ButtonSettings1.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        ButtonViewLogs.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        CheckBoxShowMap1.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBlockShowMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      }
    }

    private async void ButtonViewLogsClick(object sender, RoutedEventArgs e)
    {
      showLogItems(true);
    }

    private void RadioButtonGPX1_Click(object sender, RoutedEventArgs e)
    {
      localSettings.Values["FileType"] = (bool)RadioButtonCSV1.IsChecked;
    }

    private void RadioButtonCSV1_Click(object sender, RoutedEventArgs e)
    {
      localSettings.Values["FileType"] = (bool)RadioButtonCSV1.IsChecked;
    }

    private void RadioButtonMetric1_Click(object sender, RoutedEventArgs e)
    {
      GPX.metresOrImperial = (bool)RadioButtonImperial1.IsChecked;
      localSettings.Values["Units"] = (bool)RadioButtonImperial1.IsChecked;
    }

    private void RadioButtonImperial1_Click(object sender, RoutedEventArgs e)
    {
      GPX.metresOrImperial = (bool)RadioButtonImperial1.IsChecked;
      localSettings.Values["Units"] = (bool)RadioButtonImperial1.IsChecked;
    }

    private void CheckBoxShowLatLong_Click(object sender, RoutedEventArgs e)
    {
      if ((bool)CheckBoxShowLatLong.IsChecked)
      {
        showLatLongTextBoxes(Visibility.Visible);
      }
      else
      {
        showLatLongTextBoxes(Visibility.Collapsed);
      } 
    }

    private void Button_CloseSettingsClick(object sender, RoutedEventArgs e)
    {
      showSettingsItems(false);
      savelLocalSettings();
 //     interval = Convert.ToInt16(TextBoxSetInterval.Text);
 //   localSettings.Values["ReportInterval"] = TextBoxSetInterval.Text;
 // localSettings.Values["ShowLatLong"] = CheckBoxShowLatLong.IsChecked;

    }

    private void CheckBoxShowMap1_Click(object sender, RoutedEventArgs e)
    {
      ShowMap(1);
    }

    private void TextBlockShowMap_Tapped(object sender, TappedRoutedEventArgs e)
    {
      ShowMap(1);
    }

    private void CheckBoxColourScheme_Click(object sender, RoutedEventArgs e)
    {
      if (CheckBoxColourScheme.IsChecked == true)
      {
        MyMap.ColorScheme = MapColorScheme.Dark;
      }
      else
      {
        MyMap.ColorScheme = MapColorScheme.Light;
      }
    }

    private void CheckBoxRotate_Click(object sender, RoutedEventArgs e)
    {
      rotateMapWithHeading = (bool)CheckBoxRotate.IsChecked;
      if (CheckBoxRotate.IsChecked == true)
      {
        MyMap.Heading = currentHeading;
      }
      else
      {
        MyMap.Heading = 0; 
      }
    }

    private void TextBlockAppVersion_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
      debugMessage++;
      if (versionLimit < 3)
      {
        versionLimit = 3;
        //          debugMessage = true;
      //  if (debugMessage > 5)
      //  {
      //    txtOrientation.Visibility = Windows.UI.Xaml.Visibility.Visible;
      //    TextBlockMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
      //    TextBlockMessage3.Visibility = Windows.UI.Xaml.Visibility.Visible;
      //    CheckBoxFakeData.Visibility = Windows.UI.Xaml.Visibility.Visible;
      //  }
      //}
      //else
      //{
      //  versionLimit = 2;
      //  txtOrientation.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      //  TextBlockMessage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      //  TextBlockMessage3.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      //  CheckBoxFakeData.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      //  //        debugMessage = false;
      }

      TextBlockAppVersion.Text = String.Format("Version: {0}", GetApplicationVersion(versionLimit));
      if (proApp)
      {
        TextBlockAppVersion.Text += String.Format(" Pro");
      }
    }

    private void Button_DownloadMaps_Click(object sender, RoutedEventArgs e)
    {
      MessageDialog md;

      md = new MessageDialog("Note: This will take you to Settings->System->Offline maps section.\r\n\r\nYou MUST close GPS Logger in order to download the new maps.");
      md.Commands.Add(new UICommand("Ok", (UICommandInvokedHandler) =>
      {
        MapManager.ShowDownloadedMapsUI();
      }));
      md.Commands.Add(new UICommand("Cancel"));
      md.ShowAsync();
    }

 
  }
}
