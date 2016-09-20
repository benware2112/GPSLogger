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
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.ExtendedExecution;
//using System.IO.Ports

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GPSLogger
{
  public sealed partial class MainPage : Page
  {
    GPXLibUC1 GPX;

    DispatcherTimer timer;
    DispatcherTimer timerForReplayGPX;
    Stopwatch logTimer;
    Geolocator geolocator;
    private ApplicationDataContainer localSettings;

    LicenseChangedEventHandler licenseChangeHandler = null;

    public int savedDataCount { get; private set; }

    private string[] filePatterns;

    public enum IconTypes
    {
      Home = 0,
      Work,
      School,
      Pin,
      Point,
      IconTypeCount
    }

    private enum LocalSettings
    {
      Units,
      FileType,
      ReportInterval,
      MovementThreshold,
      DesiredAccuracyInMeters,
      ShowMap,
      ColourScheme,
      ShowLatLong,
      ShowMinMax,
      Rotate,
      LastCentrePoint,
      Traffic,
      MapStyle,
      RunUnderLockScreen,
      ReplaySpeed,
      ReplayType,
      DistanceToNearestPin,
      FilePattern,
      GarminBasecamp,
      LocalSettingsCount
    }

    private IRandomAccessStreamReference thumbnail;
    private DisplayRequest dispRequest;
    private DateTime startTimeGPS;
    private DateTime startTimeTrip;
    private DateTimeOffset timeForNextReplayedPosition;
    private TimeSpan tripTime;
    FileSavePicker savePicker;
    StorageFile file;
    FileOpenPicker loadPicker;
    string PlacemarksText;
    bool newSavePicker;
    private SimpleOrientationSensor orientation;
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
    private uint interval = 1;
    private int movementThreshold = 1;
    private uint desiredAccuracyInMeters = 1;
    private bool hideDesiredAccuracy = true;
    private int replayPosCount;
    private bool tryingToSaveAFile;

    private string pickedFilename;
    private string lastCentrePoint;
    private string timeString;  // also used for debugging

    private double currentHeading;
    private double initialZoomLevel = 15.0f;
    private double zoomLevel;
    //private double widthWide;
    //private double heightWide;
    //private double widthHigh;
    //private double heightHigh;
    //private double width;
    //private double height;

    private int versionLimit = 3;
    //private int lastSavedIndexFromMapControl = 0;
    private int tickMultiplier = 1;//1000000;
    private int lastZoomMyMap = -1;
    private int refreshPositionIndicator;
    private int maxRefreshPositionIndicator = 1;
    private int debugMessage = 0; // false; // true; // 
    private int pausedCount = 0;
    private int selectedMapTypeIndex;
    private int selectedFilePatternIndex;

    private bool proApp = false; //true; // 
    private bool orientationWide = true;
    private bool lastOrientation = true;
    private bool showFullGPSDataLog;
    private bool gpsRunning;
    private bool fileRunning;
    private bool realTime;
    private bool preloadTrace;
    private bool drawWholePath;
    private bool rotateMapWithHeading;
    private bool isTimeForNewPosition;
    private bool firstPoll = true;
    private bool firstMapDisplay = true;
    private bool dataSaved = false;
    private bool pointsMyMapVisible;
    private bool showPlacemarkItems = true; //false;
    private bool showDestinationItems = true; //false;
    private bool firstVisibleMyMap;

    public static MainPage CurrentMainPage;
    private LicenseInformation licenseInformation;
    private MapIcon lastIcon;
    private MapIcon iconHome;
    private MapIcon iconWork;
    private MapIcon iconSchool;
    private MapIcon iconPin;
    public List<MapIcon> ListOfMapIconPoints;
    private Color liveStrokeColor;
    //    private List<String> DestinationList;
    //    private List<BasicGeopositionExtended> pointsList;

    private BasicGeopositionExtended currPos;
    private BasicGeopositionExtended startPos;
    private BasicGeopositionExtended homeLocation;
    private BasicGeopositionExtended workLocation;
    private BasicGeopositionExtended schoolLocation;
    private List<BasicGeopositionExtended> pinLocations;
    private BasicGeoposition doubleTap;
    private BasicGeoposition tap1;
    private BasicGeoposition tap2;
    //   private BasicGeopositionExtended tapEnd;
    private MapPolyline routeshape;
    private String routeText;

    private String[] pinNames;
    private List<MapPolyline> pathList;
    //    private Points[] points;
    private Points points;
    private string filename;
    private RandomAccessStreamReference imagePin1;
    private RandomAccessStreamReference imagePin3;
    private RandomAccessStreamReference imagePin5;
    private BasicGeoposition res;
    private bool placemarkActive;
    private bool destinationActive;
    private MapLocation mapLocationClicked;
    private MapIcon[] iconLastPin;
    private BasicGeopositionExtended tapTripLocation;
    public List<TripPlanning> tripPlanList;
    private RandomAccessStreamReference tripLocationImage;

    private bool isWindowsPhone;
    private bool settingsOpen;
    private bool placemarksOpen;
    private bool logsOpen;
    private bool directionsOpen;

    private bool debugging;
    private int minuteCount;
    private int minutesToSave;
    private bool addWPToFilename;
    private bool useDateTimeForFilename;
    private bool runInBackground;
    private ExtendedExecutionSession session;

    private bool usetextBlockMessage1ForDebugging = false; //true; //
    private string sessionString;
    private int sessionCount = 0;
    private bool saveEachMapPolylineToMemory = false; //true; 

    //   private static string fileName;

    public MainPage()
    {
      this.InitializeComponent();

      proApp = false;

      // Activate a display-required request. If successful, the screen is 
      // guaranteed not to turn off automatically due to user inactivity.
      dispRequest = new DisplayRequest();
      dispRequest.RequestActive();
      firstVisibleMyMap = true;
      CurrentMainPage = this;

      settingsOpen = false;
      placemarksOpen = false;
      logsOpen = false;
      directionsOpen = false;
      tryingToSaveAFile = false;
      debugging = false; // true; //
      timeString = "Debugging";

      isWindowsPhone = Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons");
      // Show battery and cell strength on Windows Phone
      //      ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow); //.UseVisible);//
      if (isWindowsPhone)
      {
        var view = ApplicationView.GetForCurrentView();
        //view.ShowStandardSystemOverlays();
        view.TryEnterFullScreenMode();
        //      view.ExitFullScreenMode(); 
        //view.ShowStandardSystemOverlays();
      }

      //   InitializeLicense();
      TextBlockSpeed.Text = "Speed: 0";
      TextBlockHeading.Text = "";
      textBlockMessage1.Text = "";
      //TextBlockSpeedPhone.Text = "Speed: 0";
      //TextBlockHeadingPhone.Text = "Heading: ";

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
        timerForReplayGPX.Interval = new TimeSpan(1000000); // 50000);
      }

      localSettings = ApplicationData.Current.LocalSettings;
      loadLocalSettings();

      GPX = new GPXLibUC1();
      GPX.AppType(proApp);
      ButtonStartLogging.Visibility = Visibility.Visible;
      GPX.MetresOrImperial = !(bool)RadioButtonMetric1.IsChecked;

      if (hideDesiredAccuracy)
      {
        TextBlockDesiredAccuracy.Visibility = Visibility.Collapsed;
        TextBoxSetDesiredAccuracy.Visibility = Visibility.Collapsed;
      }
      //ButtonStopLogging.Visibility = Visibility.Collapsed;
      if (Microsoft.Services.Store.Engagement.Feedback.IsSupported)
      {
        feedbackButton.Visibility = Visibility.Visible;
        feedbackButtonTextBlock.Visibility = Visibility.Visible;
      }
      else
      {
        feedbackButton.Visibility = Visibility.Collapsed;
        feedbackButtonTextBlock.Visibility = Visibility.Collapsed;
      }

      //    DestinationList = new List<string>();
      if (isWindowsPhone)
      {
        showTopRowButtonsVisible(false);
        minutesToSave = 2;
      }
      else
      {
        minutesToSave = 1;
      }
      showSettingsItems(false);
      showLogItems(false);
      showPinsItems(false);
      showSetDestinationItems(false);
      radioButtonLogsClicked();
      //if (isWindowsPhone)
      //  showFullGPSDataLog = true; // 
      //else
      showFullGPSDataLog = false; // true; // 
      useDateTimeForFilename = true;
      addWPToFilename = false; // true;

      textBlockMessage.Visibility = Visibility.Collapsed;  // Visibility.Visible; // 
      //CheckBoxRunUnderLockScreen.Visibility = Visibility.Collapsed;
      //    ButtonViewLogs.Visibility = Visibility.Collapsed;
      TextBoxGPSList.Visibility = Visibility.Collapsed;
      realTime = false; //  true;

      //textBlockRoute.Text = "";
      textBoxRoute.Text = "";
      TextBoxGPSList.Text = "";
      TextBlockMaxElevation.Text = "Max:";
      TextBlockMinElevation.Text = "Min:";


      TextBlockAppVersion.Text = String.Format("Version: {0}", GetApplicationVersion(versionLimit));
      //   this.
      GPX.SetAppVersion(GetApplicationVersion(3));

      showGPSItems(true);
      SetGUIItems();

      savedDataCount = 0;

      initializeAdvertising();

      //TextBlockGradeLabel.Visibility = Visibility.Collapsed;
      TextBlockGrade.Visibility = Visibility.Collapsed;

      if ((bool)CheckBoxShowDistanceToNearestPin.IsChecked)
      {
        textBlockMessage1.Visibility = Visibility.Visible;
      }
      else
      {
        textBlockMessage1.Visibility = Visibility.Collapsed;
      }
      textBlockMessage.Visibility = Visibility.Collapsed;
      textBlockZoomLevel.Visibility = Visibility.Collapsed;

      appBarButtonPause.Visibility = Visibility.Collapsed;
      appBarButtonResume.Visibility = Visibility.Collapsed;
      appBarButtonStopGPS.Visibility = Visibility.Collapsed;

      //ButtonViewPins.Visibility = Visibility.Collapsed;
      //ButtonClosePins.Visibility = Visibility.Collapsed;
      //ButtonCloseDestination.Visibility = Visibility.Collapsed;

      initializeMyMapMethods(); 
      //     tapTripLocation = new BasicGeopositionExtended(new BasicGeoposition());
      //      tapEnd = new BasicGeopositionExtended(new BasicGeoposition());

      //MyMapSetDestination.MapDoubleTapped += MyMapSetDestination_MapDoubleTapped;
      //MyMapSetDestination.GotFocus += MyMapSetDestination_GotFocus;

      //homeLocation.Latitude = 0; // 49.800081;
      //homeLocation.Longitude = 0; // -119.549513;
      //workLocation.Latitude = 0; // 50.116981;
      //workLocation.Longitude = 0; // -119.490639;
      //schoolLocation.Latitude = 0; // 49.984368;
      //schoolLocation.Longitude = 0; // -119.554021;
      //homeLocation.Latitude = 0; // 49.900081;
      //homeLocation.Longitude = 0; // -119.449513;
      //workLocation.Latitude = 0; // 50.016981;
      //workLocation.Longitude = 0; // -119.390639;
      //schoolLocation.Latitude = 0; // 49.884368;
      //schoolLocation.Longitude = 0; // -119.454021;
      filename = "CustomPoints.txt";

      iconLastPin = new MapIcon[50];
      ListOfMapIconPoints = new List<MapIcon>();

      imagePin1 = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/SquareBlack1x1.png", UriKind.RelativeOrAbsolute));
      imagePin3 = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/SquareBlack3x3.png", UriKind.RelativeOrAbsolute));
      imagePin5 = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/SquareBlack5x5.png", UriKind.RelativeOrAbsolute));

      LoadPointsFileAsync();
      // LoadPointsFile();
      InitializeComboBoxes();

      //   SetPointsListFromFile();
      //MyMap.Style = MapStyle.Aerial3DWithRoads;//.Aerial;

      HideShowPinItems(showPlacemarkItems);

      notReadyForPrimeTime();

      minuteCount = minutesToSave;

    }

    private void InitializeComboBoxes()
    {
      comboBoxPinTypes.Items.Add(new ComboBoxItem() { Content = IconTypes.Home });
      comboBoxPinTypes.Items.Add(new ComboBoxItem() { Content = IconTypes.Work });
      comboBoxPinTypes.Items.Add(new ComboBoxItem() { Content = IconTypes.School });
      comboBoxPinTypes.SelectedIndex = 0;

      filePatterns = new string[6];
      filePatterns[0] = "{yyyy}-{mm}-{dd} {hh}-{mm}-{ss}";
      filePatterns[1] = "{mm}-{dd}-{yyyy} {hh}-{mm}-{ss}";
      filePatterns[2] = "{yyyy}-{mm}-{dd}";
      filePatterns[3] = "{mm}-{dd}-{yyyy}";
      filePatterns[4] = "{dd}-{mm}-{yyyy}";
      filePatterns[5] = "{closest pin}-{yyyy}-{mm}-{dd} {hh}-{mm}";

      comboBoxFilePattern.Items.Add(new ComboBoxItem() { Content = filePatterns[0] });
      comboBoxFilePattern.Items.Add(new ComboBoxItem() { Content = filePatterns[1] });
      comboBoxFilePattern.Items.Add(new ComboBoxItem() { Content = filePatterns[2] });
      comboBoxFilePattern.Items.Add(new ComboBoxItem() { Content = filePatterns[3] });
      comboBoxFilePattern.Items.Add(new ComboBoxItem() { Content = filePatterns[4] });
      comboBoxFilePattern.Items.Add(new ComboBoxItem() { Content = filePatterns[5] });

      //startTimeGPS.Month, startTimeGPS.Day, startTimeGPS.Year, startTimeGPS.Hour, startTimeGPS.Minute, startTimeGPS.Second, savePicker.DefaultFileExtension);
      if (proApp)
      {
        comboBoxPinTypes.Items.Add(new ComboBoxItem() { Content = IconTypes.Pin });
        comboBoxPinTypes.Items.Add(new ComboBoxItem() { Content = IconTypes.Point });
        comboBoxPinTypes.SelectedIndex = 4;

        comboBoxMapTypes.Items.Add(new ComboBoxItem() { Content = "Road" });
        comboBoxMapTypes.Items.Add(new ComboBoxItem() { Content = "Terrain" });
        comboBoxMapTypes.Items.Add(new ComboBoxItem() { Content = "Aerial" });
        comboBoxMapTypes.Items.Add(new ComboBoxItem() { Content = "Aerial With Roads" });
        comboBoxMapTypes.SelectedIndex = selectedMapTypeIndex;
        bool extendedOptionsForMapType = false;
        if (extendedOptionsForMapType)
        {
          comboBoxMapTypes.Items.Add(new ComboBoxItem() { Content = "Aerial3D" });
          comboBoxMapTypes.Items.Add(new ComboBoxItem() { Content = "Aerial3DWithRoads" });
        }


      }
      else
      {
        textBlockMapTypes.Visibility = Visibility.Collapsed;
        comboBoxMapTypes.Visibility = Visibility.Collapsed;
      }

      comboBoxFilePattern.SelectedIndex = selectedFilePatternIndex;
    }

    private void initializeMyMapMethods()
    {
      MyMap.ZoomLevelChanged += MyMap_ZoomLevelChanged;
      MyMap.RightTapped += MyMap_RightTapped;
      MyMap.MapDoubleTapped += MyMap_MapDoubleTapped;
      MyMap.GotFocus += MyMap_GotFocus;
      MyMap.MapTapped += MyMap_MapTapped;
      MyMap.PointerMoved += MyMap_PointerMoved;
    }

    private void MyMap_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
      string str = e.Pointer.PointerDeviceType.ToString();
      Debug.WriteLine(str);
    }

    #region Advertising
    private void initializeAdvertising()
    {
      AdMediatorWindows.AdSdkError += AdMediatorWindows_AdSdkError;
      AdMediatorWindows.AdSdkEvent += AdMediatorWindows_AdSdkEvent;
      AdMediatorWindows.AdMediatorError += AdMediatorWindows_AdMediatorError;
      AdMediatorWindows.AdMediatorFilled += AdMediatorWindows_AdMediatorFilled;
      //     setAdStatus(proApp);
      if (proApp)
      {
        AdMediatorWindows.Visibility = Visibility.Collapsed;
        //AdMediatorMobile.Visibility = Visibility.Collapsed;
        TextBlockMessageGoPro.Visibility = Visibility.Collapsed;
        buttonRemoveAds.Visibility = Visibility.Collapsed;
        TextBlockMessageGoPro.Text = "";
        GoPro.Header = "";
        TextBlockMessageAboutMyAppLine1.Text = "GPS-GPX Logger Pro.";
      }
      else
      {
        AdMediatorWindows.Visibility = Visibility.Visible;
        if (isWindowsPhone)
        {
          //AdMediatorMobile.Visibility = Visibility.Visible;

          AdMediatorWindows.Height = 50;
          AdMediatorWindows.Width = 300;
          AdMediatorWindows.Margin = new Thickness(0, 50, 0, 0);
          //         TextBlockMessageAboutMyAppLine1.Text = "GPS-GPX Logger Pro.";
        }
        else
        {
          AdMediatorWindows.Margin = new Thickness(250, 0, 0, 0);
          AdMediatorWindows.Height = 90;
          AdMediatorWindows.Width = 728;
        }
        TextBlockMessageGoPro.Visibility = Visibility.Visible;
        TextBlockMessageGoPro.Text = "Reasons to get the Pro App\r\nNo advertising\r\nMore custom placemarks\r\nPlease download \"GPS Logger Pro\"";
        buttonRemoveAds.Visibility = Visibility.Visible;
        CheckBoxTraffic.Visibility = Visibility.Collapsed;
        TextBlockMessageAboutMyAppLine1.Text = "GPS-GPX Logger.";
        //        ButtonSetDestination.Visibility = Visibility.Collapsed;
      }
    }

    private void AdMediatorWindows_AdMediatorFilled(object sender, Microsoft.AdMediator.Core.Events.AdSdkEventArgs e)
    {
      string str;

      str = String.Format("{0}  {1}", e.EventName, e.Name);

      Debug.WriteLine(String.Format("AdMediatorFilled {0}  {1}", DateTime.Now.ToLocalTime(), str));
    }

    private void AdMediatorWindows_AdMediatorError(object sender, Microsoft.AdMediator.Core.Events.AdMediatorFailedEventArgs e)
    {
      string str = e.Error.Message;
      Debug.WriteLine(String.Format("AdMediatorError {0}  {1}", DateTime.Now.ToLocalTime(), str));
    }

    private void AdMediatorWindows_AdSdkEvent(object sender, Microsoft.AdMediator.Core.Events.AdSdkEventArgs e)
    {
      string str;

      str = String.Format("{0}  {1}", e.EventName, e.Name);

      Debug.WriteLine(String.Format("AdSdkEvent {0}  {1}", DateTime.Now.ToLocalTime(), str));
    }

    private void AdMediatorWindows_AdSdkError(object sender, Microsoft.AdMediator.Core.Events.AdFailedEventArgs e)
    {
      string str;

      str = String.Format("{0}  {1}  {2}", e.Name, e.EventName, e.ErrorDescription);
      if (e.Error != null)
      {
        str = e.Error.Message;
      }

      Debug.WriteLine(String.Format("AdSdkError {0}  {1}", DateTime.Now.ToLocalTime(), str));
    }
    #endregion

    private void notReadyForPrimeTime()
    {
      appBarButtonDestination.Visibility = Visibility.Collapsed;

      //buttonImportPlacemarks.Visibility = Visibility.Collapsed;
      //buttonExportPlacemarks.Visibility = Visibility.Collapsed;
      CheckBoxFullScreenMap.Visibility = Visibility.Collapsed;
    }

    private void HideShowPinItems(bool visible)
    {
      if (visible)
      {
        buttonSetPlacemark.Visibility = Visibility.Visible;
        comboBoxPinTypes.Visibility = Visibility.Visible;
        TextBoxPinText.Visibility = Visibility.Visible;
        TextBlockMapClickLocation.Visibility = Visibility.Visible;
      }
      else
      {
        buttonSetPlacemark.Visibility = Visibility.Collapsed;
        comboBoxPinTypes.Visibility = Visibility.Collapsed;
        TextBoxPinText.Visibility = Visibility.Collapsed;
        TextBlockMapClickLocation.Visibility = Visibility.Collapsed;
      }
    }

    private async void LoadPointsFileAsync()
    {
      points = new Points();
      await points.LoadFile(filename);

      int g = 0;
    }

    private async void buttonLoadPoints_Click(object sender, RoutedEventArgs e)
    {
      await points.ReadFromFile();
    }

    #region Map Events
    private async void MyMap_MapTapped(MapControl sender, MapInputEventArgs args)
    {
      Debug.WriteLine(" MyMap_MapTapped {0} {1}", args.Location.Position.Latitude, args.Location.Position.Longitude);

      BasicGeopositionExtended tapped = new BasicGeopositionExtended(args.Location.Position);

      TextBoxLatText.Text = String.Format("{0:0.000000}", tapped.pos.Latitude);
      TextBoxLonText.Text = String.Format("{0:0.000000}", tapped.pos.Longitude);
      tap2 = tap1;
      tap1 = args.Location.Position;
      if (destinationActive)
      {
        if (tripPlanList == null)
        {
          tripPlanList = new List<TripPlanning>();
        }
        tapTripLocation = new BasicGeopositionExtended(tap1);

        string str = String.Format("{0},{1}", tapTripLocation.pos.Latitude, tapTripLocation.pos.Longitude);
        Geopoint hintPoint = null;
        if (homeLocation != null)
        {
          hintPoint = new Geopoint(homeLocation.pos);
        }
        mapLocationClicked = await GetLocationFromPointOnMap(str, hintPoint, false);
        TextBoxDestinationLocation.Text = mapLocationClicked.Address.FormattedAddress;

        if (tripPlanList.Count < 1)
        {
          tripLocationImage = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/DestinationStart.png"));
        }
        else
        {
          tripLocationImage = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/DestinationEnd.png"));
        }
        SetPinLocationIcon(tap1, "1", tripLocationImage, tripPlanList.Count);
      }

      if (placemarkActive)
      {
        if (tap1.Latitude == tap2.Latitude && tap1.Longitude == tap2.Longitude && tap1.Altitude == tap2.Altitude)
        {
          SetPinLocationIcon(tap1);
          SetPinLocationIcon(tap1);
        }
        SetPinLocationIcon(tap1, "1", 1);
        buttonSetPlacemark.IsEnabled = true;
        comboBoxPinTypes.IsEnabled = true;
        TextBoxPinText.IsEnabled = true;

        lastCentrePoint = String.Format("{0}~{1}", tap1.Latitude, tap1.Longitude);
      }
    }

    private void MyMap_MapDoubleTapped(MapControl sender, MapInputEventArgs args)
    {
      Debug.WriteLine("MapDoubleTapped {0} {1}", args.Location.Position.Latitude, args.Location.Position.Longitude);
      if (!isWindowsPhone)
      {
        TextBlockMapClickLocation.Text = String.Format("Map Tapped {0:0.000000} {1:0.000000}", args.Location.Position.Latitude, args.Location.Position.Longitude);
      }
      doubleTap = args.Location.Position;
    }


    private void MyMap_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
    }

    private void MyMap_GotFocus(object sender, RoutedEventArgs e)
    {
      ShowPointsFromFile(0);
    }

    private async void MyMap_ZoomLevelChanged(MapControl sender, object args)
    {
      int zoom = (int)MyMap.ZoomLevel;
      if (zoom != lastZoomMyMap)
      {
        SetIconBecauseZoomLevelChanged(0, true);
      }
      lastZoomMyMap = zoom;
    }

    private void SetIconBecauseZoomLevelChanged(int whichMap, bool clear)
    {
      switch (whichMap)
      {
        case 0:
          pointsMyMapVisible = false;

          RemovePlacemarksFromMap(whichMap);
          ShowPointsFromFile(whichMap);
          if (firstVisibleMyMap)
          {
            firstVisibleMyMap = false;

            BasicGeopositionExtended MapLocation = new BasicGeopositionExtended(lastCentrePoint);

            if (homeLocation != null)
            {
              MapLocation = homeLocation;
            }
            MyMap.Center = new Geopoint(new BasicGeoposition()
            {
              Latitude = MapLocation.pos.Latitude,
              Longitude = MapLocation.pos.Longitude
            });
            MyMap.ZoomLevel = 5;
          }

          Debug.WriteLine(String.Format("MyMap.ZoomLevel {0}\r\nMyMap.MapElements.Count {1}", MyMap.ZoomLevel, MyMap.MapElements.Count));
          if (points != null)
          {
            if (points.lines != null)
            {
              Debug.WriteLine(String.Format("p.lines.Count {0}", points.lines.Count));
            }
          }
          break;

      }
    }

    private void RemoveFromPointsList()
    {
      try
      {
        if (points != null)
        {
          while (points.lines.Count > 0)
          {
            points.lines.RemoveAt(0);
          }
        }
      }
      catch (Exception ex)
      {
        string str = ex.Message;
      }
    }

    private void RemovePlacemarksFromMap(int whichMap)
    {
      int count = 0;
      switch (whichMap)
      {
        case 0:
          for (int x = 0; x < ListOfMapIconPoints.Count; x++)
          {
            MapIcon MapIcon1 = ListOfMapIconPoints[x];
            if (MyMap.MapElements.Contains(MapIcon1))
            {
              MyMap.MapElements.Remove(MapIcon1);
              ListOfMapIconPoints.Remove(MapIcon1);
              x--;
            }
          }
          break;
      }
    }

    private bool isSingularIcon(string icon)
    {
      if (icon == IconTypes.Home.ToString())
        return true;
      if (icon == IconTypes.Work.ToString())
        return true;
      if (icon == IconTypes.School.ToString())
        return true;
      //if (MyMap.MapElements.Contains(mapIcon))
      //  return true;
      return false;
    }

    #endregion

    private void Grid_LostFocus(object sender, RoutedEventArgs e)
    {
      Geopoint centerPoint = MyMap.Center;
      lastCentrePoint = String.Format("{0}~{1}", centerPoint.Position.Latitude, centerPoint.Position.Longitude);
      savelLocalSettings();


    }

    private void Grid_Unloaded(object sender, RoutedEventArgs e)
    {
      savelLocalSettings();
    }

    private string getReplayType()
    {
      if (RadioButtonRealTime.IsChecked == true)
      {
        return "2";
      }
      if (RadioButtonFullTrace.IsChecked == true)
      {
        return "3";
      }
      return "1";
    }

    private string getReplaySpeed()
    {
      if ((bool)radioButtonFast.IsChecked)
        return "2";
      if ((bool)radioButtonFaster.IsChecked)
        return "3";
      if ((bool)radioButtonFastest.IsChecked)
        return "4";
      return "1";
    }

    private void loadLocalSettings()
    {
      if (!localSettings.Values.ContainsKey(LocalSettings.Units.ToString()))
      {
        // if the setting doesn't exist, probably wise to create it here.
        // setting the default to "false", but you can change to true if that makes more sense.
        localSettings.Values.Add(LocalSettings.Units.ToString(), false);
        RadioButtonMetric1.IsChecked = true;
        RadioButtonImperial1.IsChecked = false;
      }
      SetUnitsRadioButtons();

      if (!localSettings.Values.ContainsKey(LocalSettings.FileType.ToString()))
      {
        localSettings.Values.Add(LocalSettings.FileType.ToString(), false);
        RadioButtonCSV1.IsChecked = false;
        RadioButtonGPX1.IsChecked = true;
      }
      if ((bool)localSettings.Values[LocalSettings.FileType.ToString()] == false)
      {
        RadioButtonCSV1.IsChecked = false;
        RadioButtonGPX1.IsChecked = true;
      }
      else
      {
        RadioButtonCSV1.IsChecked = true;
        RadioButtonGPX1.IsChecked = false;
      }

      if (!localSettings.Values.ContainsKey(LocalSettings.ReportInterval.ToString()))
      {
        localSettings.Values.Add(LocalSettings.ReportInterval.ToString(), interval);
      }
      TextBoxSetInterval.Text = localSettings.Values[LocalSettings.ReportInterval.ToString()].ToString();

      if (!localSettings.Values.ContainsKey(LocalSettings.MovementThreshold.ToString()))
      {
        localSettings.Values.Add(LocalSettings.MovementThreshold.ToString(), movementThreshold);
      }
      TextBoxSetMovementThreshold.Text = localSettings.Values[LocalSettings.MovementThreshold.ToString()].ToString();

      if (!localSettings.Values.ContainsKey(LocalSettings.DesiredAccuracyInMeters.ToString()))
      {
        localSettings.Values.Add(LocalSettings.DesiredAccuracyInMeters.ToString(), desiredAccuracyInMeters);
      }
      TextBoxSetDesiredAccuracy.Text = localSettings.Values[LocalSettings.DesiredAccuracyInMeters.ToString()].ToString();
      
      if (!localSettings.Values.ContainsKey(LocalSettings.ShowMap.ToString()))
      {
        localSettings.Values.Add(LocalSettings.ShowMap.ToString(), true);

        MyMap.Visibility = Visibility.Visible;
        TextBoxGPSList.Visibility = Visibility.Collapsed;
      }
      if ((bool)localSettings.Values[LocalSettings.ShowMap.ToString()] == false)
      {
        CheckBoxShowMap1.IsChecked = false;
        //         CheckBoxShowMap1Phone.IsChecked = false;
      }
      else
      {
        CheckBoxShowMap1.IsChecked = true;
        //         CheckBoxShowMap1Phone.IsChecked = true;
      }
        //     ShowMap(0);

      if (!localSettings.Values.ContainsKey(LocalSettings.ShowLatLong.ToString()))
      {
        localSettings.Values.Add(LocalSettings.ShowLatLong.ToString(), true);
        CheckBoxShowLatLong.IsChecked = true;
        showLatLongTextBoxes(Visibility.Visible);
      }
      if ((bool)localSettings.Values[LocalSettings.ShowLatLong.ToString()] == true)
      {
        CheckBoxShowLatLong.IsChecked = true;
        showLatLongTextBoxes(Visibility.Visible);
      }
      else
      {
        CheckBoxShowLatLong.IsChecked = false;
        showLatLongTextBoxes(Visibility.Collapsed);
      }

      if (!localSettings.Values.ContainsKey(LocalSettings.ShowMinMax.ToString()))
      {
        localSettings.Values.Add(LocalSettings.ShowMinMax.ToString(), true);
        CheckBoxShowMinMax.IsChecked = true;
        showMinMaxTextBoxes(Visibility.Visible);
      }
      else
      {
        if ((bool)localSettings.Values[LocalSettings.ShowMinMax.ToString()] == true)
        {
          CheckBoxShowMinMax.IsChecked = true;
          showMinMaxTextBoxes(Visibility.Visible);
        }
        else
        {
          CheckBoxShowMinMax.IsChecked = false;
          showMinMaxTextBoxes(Visibility.Collapsed);
        }
      }

      if (!localSettings.Values.ContainsKey(LocalSettings.ColourScheme.ToString()))
      {
        localSettings.Values.Add(LocalSettings.ColourScheme.ToString(), false);
        CheckBoxColourScheme.IsChecked = false;
        MyMap.ColorScheme = MapColorScheme.Light;
      }
      else
      {
        if ((bool)localSettings.Values[LocalSettings.ColourScheme.ToString()] == true)
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

      if (!localSettings.Values.ContainsKey(LocalSettings.Rotate.ToString()))
      {
        localSettings.Values.Add(LocalSettings.Rotate.ToString(), true);
        CheckBoxRotate.IsChecked = true;
        MyMap.Heading = currentHeading;
      }
      else
      {
        if ((bool)localSettings.Values[LocalSettings.Rotate.ToString()] == true)
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

      if (!localSettings.Values.ContainsKey(LocalSettings.RunUnderLockScreen.ToString()))
      {
        localSettings.Values.Add(LocalSettings.RunUnderLockScreen.ToString(), true);
        CheckBoxRunUnderLockScreen.IsChecked = true;
      }
      else
      {
        if ((bool)localSettings.Values[LocalSettings.RunUnderLockScreen.ToString()] == true)
        {
          CheckBoxRunUnderLockScreen.IsChecked = true;
        }
        else
        {
          CheckBoxRunUnderLockScreen.IsChecked = false;
        }
      }
      runInBackground = (bool)CheckBoxRunUnderLockScreen.IsChecked;

      if (!localSettings.Values.ContainsKey(LocalSettings.LastCentrePoint.ToString()))
      {
        lastCentrePoint = "40~-100";
        localSettings.Values.Add(LocalSettings.LastCentrePoint.ToString(), lastCentrePoint);
      }
      lastCentrePoint = localSettings.Values[LocalSettings.LastCentrePoint.ToString()].ToString();

      if (!localSettings.Values.ContainsKey(LocalSettings.Traffic.ToString()))
      {
        localSettings.Values.Add(LocalSettings.Traffic.ToString(), false);
        CheckBoxTraffic.IsChecked = false;
        MyMap.TrafficFlowVisible = false;
      }
      if ((bool)localSettings.Values[LocalSettings.Traffic.ToString()] == true)
      {
        CheckBoxTraffic.IsChecked = true;
        MyMap.TrafficFlowVisible = true;
      }
      else
      {
        CheckBoxTraffic.IsChecked = false;
        MyMap.TrafficFlowVisible = false;
      }

      if (!localSettings.Values.ContainsKey(LocalSettings.MapStyle.ToString()))
      {
        selectedMapTypeIndex = 0;
        localSettings.Values.Add(LocalSettings.MapStyle.ToString(), selectedMapTypeIndex);
      }
      selectedMapTypeIndex = (int)localSettings.Values[LocalSettings.MapStyle.ToString()];
      if (selectedMapTypeIndex < 0)
      {
        selectedMapTypeIndex = 0;
      }

      if (!localSettings.Values.ContainsKey(LocalSettings.ReplaySpeed.ToString()))
      {
        radioButtonSlow.IsChecked = true;
        radioButtonFast.IsChecked = false;
        radioButtonFaster.IsChecked = false;
        radioButtonFastest.IsChecked = false;
        localSettings.Values.Add(LocalSettings.ReplaySpeed.ToString(), "1");
      }
      SetReplaySpeedRadioButtons();

      if (!localSettings.Values.ContainsKey(LocalSettings.ReplayType.ToString()))
      {
        localSettings.Values.Add(LocalSettings.ReplayType.ToString(), "1");
        RadioButtonAccelerated.IsChecked = true;
        RadioButtonFullTrace.IsChecked = false;
        RadioButtonRealTime.IsChecked = false;
      }
      SetReplayTypeRadioButtons();

      if (!localSettings.Values.ContainsKey(LocalSettings.DistanceToNearestPin.ToString()))
      {
        localSettings.Values.Add(LocalSettings.DistanceToNearestPin.ToString(), false);
        CheckBoxShowDistanceToNearestPin.IsChecked = false;
      }
      else
      {
        CheckBoxShowDistanceToNearestPin.IsChecked = ((bool)localSettings.Values[LocalSettings.DistanceToNearestPin.ToString()] == true);
      }
      if ((bool)CheckBoxShowDistanceToNearestPin.IsChecked)
      {
        textBlockMessage1.Visibility = Visibility.Visible;
      }
      else
      {
        textBlockMessage1.Visibility = Visibility.Collapsed;
      }


      if (!localSettings.Values.ContainsKey(LocalSettings.FilePattern.ToString()))
      {
        selectedFilePatternIndex = 0;
        localSettings.Values.Add(LocalSettings.FilePattern.ToString(), selectedFilePatternIndex);
      }
      selectedFilePatternIndex = (int)localSettings.Values[LocalSettings.FilePattern.ToString()];
      if (selectedFilePatternIndex < 0)
      {
        selectedFilePatternIndex = 0;
      }

      if (!localSettings.Values.ContainsKey(LocalSettings.GarminBasecamp.ToString()))
      {
        localSettings.Values.Add(LocalSettings.GarminBasecamp.ToString(), false);
      }
      if ((bool)localSettings.Values[LocalSettings.GarminBasecamp.ToString()] == true)
      {
        checkBoxGarminBasecamp.IsChecked = true;
      }
      else
      {
        checkBoxGarminBasecamp.IsChecked = false;
      }

      radioButtonLogsClicked();

      rotateMapWithHeading = (bool)CheckBoxRotate.IsChecked;

      //    MapExtensions.RequestDirections();
    }

    private void savelLocalSettings()
    {
      bool forceClear = false;
      if (forceClear) //false) //localSettings.Values.Count > (int)LocalSettings.LocalSettingsCount)
      {
        localSettings.Values.Clear();
      }
      else
      {
        localSettings.Values[LocalSettings.Units.ToString()] = (bool)RadioButtonImperial1.IsChecked;
        localSettings.Values[LocalSettings.FileType.ToString()] = (bool)RadioButtonCSV1.IsChecked;
        localSettings.Values[LocalSettings.ReportInterval.ToString()] = TextBoxSetInterval.Text;
        localSettings.Values[LocalSettings.MovementThreshold.ToString()] = TextBoxSetMovementThreshold.Text;
        localSettings.Values[LocalSettings.DesiredAccuracyInMeters.ToString()] = TextBoxSetDesiredAccuracy.Text;
        localSettings.Values[LocalSettings.ShowMap.ToString()] = (bool)CheckBoxShowMap1.IsChecked;
        localSettings.Values[LocalSettings.ColourScheme.ToString()] = (bool)CheckBoxColourScheme.IsChecked;
        localSettings.Values[LocalSettings.ShowLatLong.ToString()] = (bool)CheckBoxShowLatLong.IsChecked;
        localSettings.Values[LocalSettings.ShowMinMax.ToString()] = (bool)CheckBoxShowMinMax.IsChecked;
        localSettings.Values[LocalSettings.Rotate.ToString()] = (bool)CheckBoxRotate.IsChecked;
        localSettings.Values[LocalSettings.LastCentrePoint.ToString()] = lastCentrePoint;
        localSettings.Values[LocalSettings.Traffic.ToString()] = (bool)CheckBoxTraffic.IsChecked;
        localSettings.Values[LocalSettings.MapStyle.ToString()] = comboBoxMapTypes.SelectedIndex;
        localSettings.Values[LocalSettings.RunUnderLockScreen.ToString()] = (bool)CheckBoxRunUnderLockScreen.IsChecked;
        localSettings.Values[LocalSettings.ReplaySpeed.ToString()] = getReplaySpeed();
        localSettings.Values[LocalSettings.ReplayType.ToString()] = getReplayType();
        localSettings.Values[LocalSettings.DistanceToNearestPin.ToString()] = (bool)CheckBoxShowDistanceToNearestPin.IsChecked;
        localSettings.Values[LocalSettings.FilePattern.ToString()] = comboBoxFilePattern.SelectedIndex;
        localSettings.Values[LocalSettings.GarminBasecamp.ToString()] = (bool)checkBoxGarminBasecamp.IsChecked;


      }
    }

    private void SetUnitsRadioButtons()
    {
      if ((bool)localSettings.Values[LocalSettings.Units.ToString()] == false) // units.METRIC))
      {
        RadioButtonMetric1.IsChecked = true;
        RadioButtonImperial1.IsChecked = false;
      }
      else
      {
        RadioButtonImperial1.IsChecked = true;
        RadioButtonMetric1.IsChecked = false;
      }
      if (!(bool)RadioButtonFullTrace.IsChecked && !(bool)RadioButtonAccelerated.IsChecked && !(bool)RadioButtonRealTime.IsChecked)
      {
        SetReplayTypeRadioButtons();
        SetReplaySpeedRadioButtons();
      }

    }

    private void SetReplaySpeedRadioButtons()
    {
      string s = localSettings.Values[LocalSettings.ReplaySpeed.ToString()].ToString();
      if (s == "1")
      {
        radioButtonSlow.IsChecked = true;
        radioButtonFast.IsChecked = false;
        radioButtonFaster.IsChecked = false;
        radioButtonFastest.IsChecked = false;
      }

      if (s == "2")
      {
        radioButtonSlow.IsChecked = false;
        radioButtonFast.IsChecked = true;
        radioButtonFaster.IsChecked = false;
        radioButtonFastest.IsChecked = false;
      }
      if (s == "3")
      {
        radioButtonSlow.IsChecked = false;
        radioButtonFast.IsChecked = false;
        radioButtonFaster.IsChecked = true;
        radioButtonFastest.IsChecked = false;
      }
      if (s == "4")
      {
        radioButtonSlow.IsChecked = false;
        radioButtonFast.IsChecked = false;
        radioButtonFaster.IsChecked = false;
        radioButtonFastest.IsChecked = true;
      }
      if (s == "5")
      {

      }
      if (s == "6")
      {
      }       //rad
    }

    private void SetReplayTypeRadioButtons()
    {
      string str = (string)localSettings.Values[LocalSettings.ReplayType.ToString()];
      if (str == "1")
      {
        RadioButtonAccelerated.IsChecked = true;
        RadioButtonFullTrace.IsChecked = false;
        RadioButtonRealTime.IsChecked = false;
      }
      if (str == "2")
      {
        RadioButtonAccelerated.IsChecked = false;
        RadioButtonRealTime.IsChecked = true;
        RadioButtonFullTrace.IsChecked = false;
      }
      if (str == "3")
      {
        RadioButtonAccelerated.IsChecked = false;
        RadioButtonRealTime.IsChecked = false;
        RadioButtonFullTrace.IsChecked = true;
      }
    }

    private async Task ShowPointsFromFile(int whichMap)
    {
      switch (whichMap)
      {
        case 0:
          if (!pointsMyMapVisible && showPlacemarkItems)
          {
            pointsMyMapVisible = true;
            await ShowPointsFile(whichMap);
          }
          break;
      }
    }

    private async Task ShowPointsFile(int whichMap)
    {
      string str;

      if (points != null)
      {
        if (points.lines != null)
        {
          for (int x = 0; x < points.lines.Count; x++)
          {
            SetPointLocationIcon(x, whichMap);
          }
        }
        else
        {
          switch (whichMap)
          {
            case 0:
              if (!String.IsNullOrEmpty(points.strLines))
              {
                string[] lines = points.strLines.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < lines.Length; x++)
                {
                  BasicGeoposition pos = new BasicGeoposition();
                  string[] line = points.strLines.Split(',');
                  string text;

                  pos.Latitude = Convert.ToDouble(line[0]);
                  pos.Longitude = Convert.ToDouble(line[1]);
                  text = line[2];
                  RandomAccessStreamReference image = points.getImage(line[4], MyMap.ZoomLevel);
                  SetPointLocationIcon(pos, text, whichMap, image);
                }
              }
              pointsMyMapVisible = false;
              break;
          }
        }
      }
      else
      {
        pointsMyMapVisible = false;
      }
      str = "";
    }

    ////private BasicGeoposition getBasicGeopositionFromString(string line)
    ////{
    ////  string str = line;
    ////  BasicGeoposition pp = getBasicGeopositionFromString(str);

    ////  return pp;
    ////}

    private void SetPointLocationIcon(int index, int whichMap)
    {
      string[] line = points.lines[index].Split(',');
      string text;

      if (line.Length >= 6)
      {
        if (pinLocations == null)
        {
          pinLocations = new List<BasicGeopositionExtended>();
        }
        BasicGeoposition pos = new BasicGeoposition();
        pos.Latitude = Convert.ToDouble(line[0]);
        pos.Longitude = Convert.ToDouble(line[1]);
        Geopoint pp = new Geopoint(pos);
        BasicGeopositionExtended posex = new BasicGeopositionExtended(pos);
        //   posex.geoPosition = pp;
        posex.Name = line[2];
        posex.PointType = line[4];
        text = line[2];
        if (line[4].ToUpper() == IconTypes.Home.ToString().ToUpper())
        {
          SetHomeLocation(pos);
        }
        if (line[4].ToUpper() == IconTypes.Work.ToString().ToUpper())
        {
          SetWorkLocation(pos);
        }
        if (line[4].ToUpper() == IconTypes.School.ToString().ToUpper())
        {
          SetSchoolLocation(pos);
        }

        addToPinLocations(posex);

        switch (whichMap)
        {
          case 0:
            RandomAccessStreamReference image0 = points.getImage(line[4], MyMap.ZoomLevel);
            SetPointLocationIcon(pos, text, whichMap, image0);
            break;
        }
      }
    }

    private void addToPinLocations(BasicGeopositionExtended posex)
    {
      bool addit = true;
      foreach (BasicGeopositionExtended bge in pinLocations)
      {
        if (posex.Name == bge.Name && posex.PointType == bge.PointType && posex.pos.Latitude == bge.pos.Latitude && posex.pos.Longitude == bge.pos.Longitude)
        {
          addit = false;
          break;
        }
      }
      if (addit)
      {
        pinLocations.Add(posex);
      }
    }

    private void SetHomeLocation(BasicGeoposition pos)
    {
      homeLocation = new BasicGeopositionExtended(pos);
      homeLocation.pos = pos;
    }
    private void SetWorkLocation(BasicGeoposition pos)
    {
      workLocation = new BasicGeopositionExtended(pos);
      workLocation.pos = pos;
    }
    private void SetSchoolLocation(BasicGeoposition pos)
    {
      schoolLocation = new BasicGeopositionExtended(pos);
      schoolLocation.pos = pos;
    }

    private void SetPointLocationIcon(BasicGeoposition pos, string text, int whichMap, RandomAccessStreamReference image)
    {
      MapIcon MapIcon1 = new MapIcon();
      MapIcon1.Image = image;
      MapIcon1.Location = new Geopoint(pos);
      MapIcon1.NormalizedAnchorPoint = new Point(0.5, 0.5);
      MapIcon1.Title = text;
      MapIcon1.ZIndex = 999;
      if (showPlacemarkItems)
      {
        switch (whichMap)
        {
          case 0:
            ListOfMapIconPoints.Add(MapIcon1);
            MyMap.MapElements.Add(MapIcon1);
            if (MyMap.MapElements.Contains(MapIcon1))
            {
              text = text;
            }
            if (ListOfMapIconPoints.Contains(MapIcon1))
            {
              text = text;
            }
            whichMap = whichMap;
            break;
        }
      }
    }

    //private bool isEqual(MapIcon yy, MapIcon mapIcon1)
    //{
    //  if (yy.Title == mapIcon1.Title && yy.Location.Position.Latitude == mapIcon1.Location.Position.Latitude && yy.Location.Position.Longitude == mapIcon1.Location.Position.Longitude)
    //    return true;
    //  return false;
    //}

    private void SetPinLocationIcon(BasicGeoposition pos)
    {
      //     SetPinLocationIcon(pos, "", whichMap);
    }
    private void SetPinLocationIcon(BasicGeoposition pos, string text, int whichIcon)
    {
      RandomAccessStreamReference image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Pin50x50.png"));
      SetPinLocationIcon(pos, text, image, whichIcon);
    }
    private void SetPinLocationIcon(BasicGeoposition pos, string text, RandomAccessStreamReference image, int whichIcon)
    {
      Geopoint bp = new Geopoint(pos);
      iconPin = new MapIcon
      {
        Location = bp,
        NormalizedAnchorPoint = new Point(0.5, 0.5),
        Image = image,
        Title = text,
        ZIndex = 5
      };
      if (showPlacemarkItems)
      {
        if (MyMap.MapElements.Contains(iconLastPin[whichIcon]))
        {
          MyMap.MapElements.Remove(iconLastPin[whichIcon]);
        }
        MyMap.MapElements.Add(iconPin);
        iconLastPin[whichIcon] = iconPin;
      }
    }
    ////private void SetHomeLocationIcon(double zoom, int whichMap)
    ////{
    ////  iconHome = GetTheIconImage(0, zoom);

    ////  if (showPinItems)
    ////  {
    ////    switch (whichMap)
    ////    {
    ////      case 0:
    ////        MyMap.MapElements.Add(iconHome);
    ////        break;
    ////      case 1:
    ////        MyMapForPins.MapElements.Add(iconHome);
    ////        break;
    ////    }
    ////  }
    ////}

    ////private void SetWorkLocationIcon(double zoom, int whichMap)
    ////{
    ////  iconWork = GetTheIconImage(1, zoom);
    ////  if (showPinItems)
    ////  {
    ////    switch (whichMap)
    ////    {
    ////      case 0:
    ////        MyMap.MapElements.Add(iconWork);
    ////        break;
    ////      case 1:
    ////        MyMapForPins.MapElements.Add(iconWork);
    ////        break;
    ////    }
    ////  }
    ////}
    ////private void SetSchoolLocationIcon(double zoom, int whichMap)
    ////{
    ////  iconSchool = GetTheIconImage(2, zoom);

    ////  if (showPinItems)
    ////  {
    ////    switch (whichMap)
    ////    {
    ////      case 0:
    ////        MyMap.MapElements.Add(iconSchool);
    ////        break;
    ////      case 1:
    ////        MyMapForPins.MapElements.Add(iconSchool);
    ////        break;
    ////    }
    ////  }
    ////}

    private MapIcon GetTheIconImage(string title, double zoom, BasicGeoposition p)
    {
      Geopoint bp = new Geopoint(p);
      RandomAccessStreamReference image = points.getImage(title, zoom); //, ref title, ref bp);

      //title = getTitleForPoint(whichType, zoom);
      //bp = getBasicGeopositionForIcon(whichType, zoom);

      if (isValidLocation(bp))
      {
        MapIcon icon = new MapIcon
        {
          Location = bp,
          NormalizedAnchorPoint = new Point(0.5, 0.5),
          Image = image,
          Title = title,
          ZIndex = 1000
        };
        return icon;
      }

      return null;
    }

    private Geopoint getBasicGeopositionForIcon(int whichType, double zoom)
    {
      switch (whichType)
      {
        case 0:
          return new Geopoint(homeLocation.pos);
        case 1:
          return new Geopoint(workLocation.pos);
        case 2:
          return new Geopoint(schoolLocation.pos);
        default:
          return new Geopoint(homeLocation.pos);
      }
    }

    private string getTitleForPoint(int whichType, double zoom)
    {
      switch (whichType)
      {
        case 0:
          if (zoom > 11)
          {
            return homeLocation.Name;
          }
          break;
        case 1:
          if (zoom > 11)
          {
            return workLocation.Name;
          }
          break;
        case 2:
          if (zoom > 11)
          {
            return schoolLocation.Name;
          }
          break;
        default:
          return "";
          break;
      }
      return "";
    }



    private bool isValidLocation(Geopoint bp)
    {
      if (bp.Position.Altitude == 0 && bp.Position.Latitude == 0 && bp.Position.Longitude == 0)
        return false;
      return true;
    }

    private void SetGUIItems()
    {
      int heightAdjust = 100;
      int heightAdjustForAd = 50;
      int distanceBetweenTwoControls = 5;

      buttonLoadPoints.Visibility = Visibility.Collapsed;

      timeString = string.Format("{0} X {1}", Window.Current.Bounds.Width, Window.Current.Bounds.Height);  //  ** Debugging

      if (proApp)
      {
        heightAdjust = 20;
        textBlockPinsMessage.Visibility = Visibility.Collapsed;
        buttonPinsPageRemoveAds.Visibility = Visibility.Collapsed;
        buttonSavePlacemarks.Visibility = Visibility.Collapsed;
      }
      else
      {
        buttonSavePlacemarks.Visibility = Visibility.Collapsed;
      }

      SettingsTitle.Margin = new Thickness(5, 5, 5, 5);

      PlacemarksTitle.Margin = new Thickness(10, 5, 10, 10);
      PlacemarksTitle.Height = Window.Current.Bounds.Height - heightAdjustForAd - heightAdjust;
      PlacemarksContainer.Height = PlacemarksTitle.Height - heightAdjust;
      PlacemarksContainer.Width = PlacemarksTitle.Width - 10;

      // Destination
      DestinationTitle.Margin = new Thickness(10, 5, 0, 0);
      DestinationTitle.Height = Window.Current.Bounds.Height - heightAdjustForAd - heightAdjust;
      DestinationTitle.Width = 230;
      DestinationContainer.Margin = new Thickness(12, 100, 0, 0);
      DestinationContainer.Height = DestinationTitle.Height - heightAdjust;
      DestinationContainer.Width = DestinationTitle.Width - 10;
      if (!isWindowsPhone)
      {
        textBoxRoute.Height = DestinationTitle.Height - 350;
      }
      buttonCalculateRoute.Margin = new Thickness(30, distanceBetweenTwoControls, 0, 0);
      buttonClearRoute.Margin = new Thickness(30, distanceBetweenTwoControls, 0, 0);
      ButtonCloseDestination.Margin = new Thickness(30, distanceBetweenTwoControls, 0, 0);

      if (isWindowsPhone)
      {
        int h = 28;
        heightAdjust = 5;
        ButtonResetTripData.Visibility = Visibility.Collapsed;
        MenuFlyoutResetTripData.Visibility = Visibility.Visible;
        TextBlockHeadingTitle.Margin = new Thickness(2, -10, 2, 2);
        TextBlockHeading.Margin = new Thickness(80, -28, 2, 2);
        textBlockMessage1.Margin = new Thickness(180, 10, 2, 2);
        if (proApp)
        {
          MyMap.Margin = new Thickness(2, 2, 2, 2);
        }
        else
        {
          MyMap.Margin = new Thickness(2, 2, 2, 50);
        }
   //     AdMediatorWindows.Margin = new Thickness(5, 0, 0, 0); 
   //     AdMediatorWindows.Margin = new Thickness(0, 100, 0, 0); // Window.Current.Bounds.Width - 100, 0, 0);
        setGUIItemsFontSizesForPhone();
        if (MyMap.Style == MapStyle.Aerial)
        {
          setGUIItemsColors(Colors.White);
        }
        else
        {
          setGUIItemsColors(Colors.Black);
        }
        //SettingsContainer1.Margin = new Thickness(20, 50, 0, 0);
        //SettingsContainer2.Margin = new Thickness(SettingsContainer1.Width + 2, 50, 0, 0);
        //      RadioButtonMetric1.Margin
        if (orientationWide)
        {
          TextBlockUnits.Margin = new Thickness(220, -92, 0, 0);
          RadioButtonMetric1.Margin = new Thickness(200, -32, 0, 0);
          RadioButtonImperial1.Margin = new Thickness(300, -32, 0, 0);
          ButtonCloseSettingsGPS.Margin = new Thickness(165, -39, 0, 0);
          //       ButtonCloseSettingsFiles.Margin = new Thickness(165, -39, 0, 0);
          //       buttonRateMyApp.Margin = new Thickness(320, -350, 0, 0);
          ButtonCloseSettingsAbout.Margin = new Thickness(165, -39, 0, 0);
          ButtonCloseSettingsMaps.Margin = new Thickness(165, -39, 0, 0);
          TextBlockMessageAboutMyAppLine2.Margin = new Thickness(265, -32, 0, 0);
          TextBlockAppVersion.Margin = new Thickness(265, -24, 0, 0);
          CheckBoxShowMinMax.Margin = new Thickness(300, -32, 0, 0);
          CheckBoxRotate.Margin = new Thickness(300, -32, 0, 0);
          if (proApp)
          {
            CheckBoxShowDistanceToNearestPin.Margin = new Thickness(300, -45, 0, 0);
          }
          else
          {
            CheckBoxShowDistanceToNearestPin.Margin = new Thickness(0, 2, 0, 0);
          }

          RadioButtonAccelerated.Margin = new Thickness(2, 0, 0, 0);
          RadioButtonRealTime.Margin = new Thickness(300, -150, 0, 0);
          RadioButtonFullTrace.Margin = new Thickness(300, -120, 0, 0);
          TextBlockSlowLoad.Margin = new Thickness(320, -100, 0, 0);
          TextBlockSlowLoad.Width = 120;

          ButtonCloseSettingsFiles.Margin = new Thickness(235, -45, 0, 0);

          TextBlockMessageGoPro.Margin = new Thickness(2, 0, 0, 0);
//          buttonRemoveAds.Margin = new Thickness(320, -100, 0, 0);
          ButtonCloseLogFilesWindow.Margin = new Thickness(320, -50, 0, 0);
        }
        else
        {
          TextBlockUnits.Margin = new Thickness(15, 0, 0, 0);
          RadioButtonMetric1.Margin = new Thickness(0, 0, 0, 0);
          RadioButtonImperial1.Margin = new Thickness(80, -32, 0, 0);
          ButtonCloseSettingsGPS.Margin = new Thickness(10, 8, 0, 0);
          ButtonCloseSettingsFiles.Margin = new Thickness(10, 8, 0, 0);
          buttonRateMyApp.Margin = new Thickness(10, 8, 0, 0);
          ButtonCloseSettingsMaps.Margin = new Thickness(10, 8, 0, 0);
          ButtonCloseSettingsAbout.Margin = new Thickness(10, 8, 0, 0);
          TextBlockMessageAboutMyAppLine2.Margin = new Thickness(10, 8, 0, 0);
          TextBlockAppVersion.Margin = new Thickness(10, 8, 0, 0);

          CheckBoxShowMinMax.Margin = new Thickness(0, 2, 0, 0);
          CheckBoxRotate.Margin = new Thickness(0, 2, 0, 0);
          CheckBoxShowDistanceToNearestPin.Margin = new Thickness(0, 2, 0, 0);

          RadioButtonAccelerated.Margin = new Thickness(2, 0, 0, 0);
          RadioButtonRealTime.Margin = new Thickness(2, 2, 0, 0);
          RadioButtonFullTrace.Margin = new Thickness(2, 2, 0, 0);
          TextBlockSlowLoad.Margin = new Thickness(2, 2, 0, 0);
          ButtonCloseLogFilesWindow.Margin = new Thickness(2, 8, 0, 0);

 //         TextBlockMessageGoPro.Margin = new Thickness(2, 2, 0, 0);
 //         buttonRemoveAds.Margin = new Thickness(2, 2, 0, 0);
        }
        LogsContainer.Margin = new Thickness(20, 50, 0, 0);
        LogsTitle.Width = Window.Current.Bounds.Width - 10; //600;
        TextBlockSlowLoad.Width = Window.Current.Bounds.Width - 60;
        radioButtonSlow.Width = Window.Current.Bounds.Width - 30; //600;
        radioButtonSlow.Height = h;
        radioButtonFastest.Width = Window.Current.Bounds.Width - 30;
        radioButtonFastest.Height = h;

        PlacemarksTitle.Width = 170;
        if (proApp)
        {
          PlacemarksTitle.Height = 230;
        }
        else
        {
          PlacemarksTitle.Height = 345;
        }
        PlacemarksContainer.Margin = new Thickness(12, 40, 0, 0);
        PlacemarksContainer.Height = PlacemarksTitle.Height - 2;
        PlacemarksContainer.Width = PlacemarksTitle.Width - 2;

        ButtonClosePlacemarks.Visibility = Visibility.Collapsed;
        try 
        {
          buttonSetPlacemark.Content = "Set";
          buttonSetPlacemark.Width = 50;
          buttonSetPlacemark.Height = h;

          buttonSavePlacemarks.Content = "Save";
          buttonSavePlacemarks.Width = buttonSetPlacemark.Width;
          buttonSavePlacemarks.Margin = new Thickness(buttonSetPlacemark.Width + 8, (h * -1), 0, 0);
          buttonSavePlacemarks.Height = h;

          buttonClearPlacemarks.Content = "Clear";
          buttonClearPlacemarks.Width = buttonSavePlacemarks.Width;
          buttonClearPlacemarks.Margin = new Thickness(buttonSetPlacemark.Width + buttonClearPlacemarks.Width + 12, (h * -1), 0, 0);
          buttonClearPlacemarks.Height = h;
        }
        catch (Exception) { }
      }
      else
      {
        ButtonResetTripData.Visibility = Visibility.Visible;
        MenuFlyoutResetTripData.Visibility = Visibility.Collapsed;
        MyMap.Margin = new Thickness(250, 100, 10, heightAdjust);
        //SettingsPivot.Margin = new Thickness(50, 60, 0, 0);
        //       AdMediatorWindows.Margin = new Thickness(0, Window.Current.Bounds.Height - 100, 0, 0);
        setGUIItemsColors(Colors.White);
        //        SettingsTitle
        //SettingsContainer1.Margin = new Thickness(30, 100, 0, 0);
        //SettingsContainer2.Margin = new Thickness(SettingsContainer1.Width + 100, 100, 0, 0);
        //SettingsContainerAbout.Margin = new Thickness(SettingsContainer1.Width + SettingsContainer2.Width + 200, 100, 0, 0);
        //        SettingsContainer2.Margin = new Thickness(350, 120, 0, 0);
        LogsContainer.Margin = new Thickness(25, 55, 0, 0);
        //       LogsContainer.Height = LogsTitle.Height - heightAdjust;
        PlacemarksContainer.Margin = new Thickness(12, 100, 0, 0);
        PlacemarksTitle.Width = 230;
        LogsTitle.Width = 230;
      }
      LogsTitle.Margin = new Thickness(10, 5, 5, 5);
      LogsTitle.Height = Window.Current.Bounds.Height - heightAdjustForAd - heightAdjust;// 600;

      buttonSetPlacemark.IsEnabled = false;
      comboBoxPinTypes.IsEnabled = false;
      TextBoxPinText.IsEnabled = false;

      GPSItemsContainer.Margin = new Thickness(4, 4, 0, 0);
      //     ItemsControlGPSDataPagePhone.Margin = new Thickness(4, 4, 0, 0);

      //SettingsContainer1.Height = Window.Current.Bounds.Height - heightAdjustForAd - 100;// 600;
      //SettingsContainer2.Height = Window.Current.Bounds.Height - heightAdjustForAd - 100;// 600;
      //SettingsContainerAbout.Height = Window.Current.Bounds.Height - heightAdjustForAd - 100;// 600;

      //      TextBlockAppVersion.Margin = new Thickness(6, SettingsTitle.Height - 550, 0, 0);
      //ButtonCloseSettings.Margin = new Thickness(6, distanceBetweenTwoControls, 0, 0);// SettingsTitle.Height - 550, 0, 0);

      //    PlacemarksContainer

      //LogsTitle.Height = Window.Current.Bounds.Height - heightAdjustForAd - heightAdjust;// 600;
      //LogsTitle.Width = Window.Current.Bounds.Width - 10; //600;230;// 
      //      ButtonCloseLogFilesWindow.Margin = new Thickness(6, LogsTitle.Height - 550, 0, 0);
      //ButtonCloseLogFilesWindow.Margin = new Thickness(6, Window.Current.Bounds.Height - 100, 0, 0);


      SettingsTitle.Height = Window.Current.Bounds.Height - heightAdjustForAd - heightAdjust;// 600;
      SettingsTitle.Width = Window.Current.Bounds.Width - 10; //600;

      TextBoxPositionData.Margin = new Thickness(266, 129, 0, 0);
      TextBoxPositionData.Height = 600;
      TextBoxPositionData.Width = 600;

      buttonSavePlacemarks.IsEnabled = false;

      TextBlockTime.Margin = new Thickness(6, LogsTitle.Height - 550, 0, 0);
      //AdMediatorWindows.Margin = new Thickness(250, LogsTitle.Height - 550, 0, 0);   // Margin = "250,0,0,6"


      textBlockPinsMessage.Margin = new Thickness(2, 2, 0, 0);
      //    buttonPinsPageRemoveAds.Margin = new Thickness(6, PlacemarksTitle.Height - 550, 0, 0);
      if (buttonPinsPageRemoveAds.Visibility == Visibility.Visible)
      {
        ButtonClosePlacemarks.Margin = new Thickness(6, distanceBetweenTwoControls, 0, 0);
      }
      else
      {
        ButtonClosePlacemarks.Margin = new Thickness(6, PlacemarksTitle.Height - 480, 0, 0);
      }
    }

    private void setGUIItemsFontSizesForPhone()
    {
      int phoneFont = 20;
      int phoneFont2 = 14;
      int itemWidth = 100;
      int itemHeight = 10;

      TextBlockSpeed.FontSize = phoneFont;
      TextBlockHeadingTitle.FontSize = phoneFont2;
      TextBlockHeading.FontSize = phoneFont;
      TextBlockMaxSpeed.FontSize = phoneFont2;
      TextBlockDistance.FontSize = phoneFont2;
      TextBlockTripTime.FontSize = phoneFont2;
      TextBlockAltitude.FontSize = phoneFont2;
      TextBlockMaxElevation.FontSize = phoneFont2;
      TextBlockMinElevation.FontSize = phoneFont2;
      TextBlockGrade.FontSize = phoneFont2;
      TextBlockPositionCount.FontSize = phoneFont2;
      TextBlockLatitude.FontSize = phoneFont2;
      TextBlockLongitude.FontSize = phoneFont2;
      //     TextBlockShowMap.FontSize = phoneFont2;
      TextBlockLastFixTime.FontSize = phoneFont2;
      ButtonResetTripData.FontSize = phoneFont2;
      TextBlockTime.Visibility = Visibility.Collapsed;
      TextBlockTime.FontSize = phoneFont2;
      ButtonStartLogging.Visibility = Visibility.Collapsed;

      LogsTitle.FontSize = phoneFont;
      buttonLoad.FontSize = phoneFont2;
      RadioButtonFullTrace.FontSize = phoneFont2;
      RadioButtonRealTime.FontSize = phoneFont2;
      RadioButtonAccelerated.FontSize = phoneFont2;
      radioButtonSlow.FontSize = phoneFont2;
      radioButtonFast.FontSize = phoneFont2;
      radioButtonFaster.FontSize = phoneFont2;
      radioButtonFastest.FontSize = phoneFont2;

      ButtonCloseLogFilesWindow.FontSize = phoneFont2;

      SettingsTitle.FontSize = phoneFont;
      TextBlockFileType.FontSize = phoneFont2;
      RadioButtonGPX1.FontSize = phoneFont2;
      RadioButtonCSV1.FontSize = phoneFont2;
      TextBlockFilePattern.FontSize = phoneFont2;
      comboBoxFilePattern.FontSize = phoneFont2;
      checkBoxGarminBasecamp.FontSize = phoneFont2;
      buttonExportPlacemarks.FontSize = phoneFont2;
      buttonImportPlacemarks.FontSize = phoneFont2;
      
      //TextBlockUnits.FontSize = phoneFont2;
      RadioButtonMetric1.FontSize = phoneFont2;
      RadioButtonImperial1.FontSize = phoneFont2;
      TextBlockGPSUpdate.FontSize = phoneFont2;
      TextBlockSeconds.FontSize = phoneFont2;
      TextBlockMovementThreshold.FontSize = phoneFont2;
      TextBoxSetMovementThreshold.FontSize = phoneFont2;
      TextBlockMovementThreshold2.FontSize = phoneFont2;
      TextBlockMovementThreshold3.FontSize = phoneFont2;
      TextBoxSetInterval.FontSize = phoneFont2;
      TextBoxSetMovementThreshold.FontSize = phoneFont2;
      TextBlockDesiredAccuracy.FontSize = phoneFont2;
      TextBoxSetDesiredAccuracy.FontSize = phoneFont2;
      CheckBoxShowLatLong.FontSize = phoneFont2;
      CheckBoxShowMinMax.FontSize = phoneFont2;
      CheckBoxColourScheme.FontSize = phoneFont2;
      CheckBoxRotate.FontSize = phoneFont2;
      ButtonDownloadMaps.FontSize = phoneFont2;
      TextBlockAppVersion.FontSize = phoneFont2;
      //      ButtonCloseSettings.Visibility = Visibility.Collapsed;
      TextBlockMessageGoPro.FontSize = phoneFont2;
      TextBlockMessageAboutMyAppLine1.FontSize = phoneFont2;
      TextBlockMessageAboutMyAppLine2.FontSize = phoneFont2;
      TextBlockMessageAboutMyAppLine3.FontSize = phoneFont2;
      buttonRemoveAds.FontSize = phoneFont2;

      CheckBoxRunUnderLockScreen.FontSize = phoneFont2;
      CheckBoxTraffic.FontSize = phoneFont2;
      buttonToggleLocation.FontSize = phoneFont2;
      TextBlockMessageRateMyApp.FontSize = phoneFont2;
      buttonRateMyApp.FontSize = phoneFont2;

      TextBlockMapClickLocation.FontSize = phoneFont2;
      TextBoxLatText.FontSize = phoneFont2;
      TextBoxLatText.Height = itemHeight;
      TextBoxLatText.Width = itemWidth;
      TextBoxLonText.FontSize = phoneFont2;
      TextBoxLonText.Height = itemHeight;
      TextBoxLonText.Width = itemWidth;

      phoneFont2 = 10;
      TextBlockLatPlacemark.FontSize = phoneFont2;
      TextBlockLonPlacemark.FontSize = phoneFont2;
      TextBlockIconPlacemark.FontSize = phoneFont2;
      TextBlockTextPlacemark.FontSize = phoneFont2;
      textBlockPinsMessage.FontSize = phoneFont2;
      comboBoxPinTypes.FontSize = phoneFont2;
      //     comboBoxPinTypes.Height = itemHeight;
      comboBoxPinTypes.Width = itemWidth;
      PlacemarksTitle.FontSize = phoneFont;
      TextBoxPinText.FontSize = phoneFont2;
      TextBoxPinText.Height = itemHeight;
      TextBoxPinText.Width = itemWidth;
      buttonSetPlacemark.FontSize = phoneFont2;
      buttonSavePlacemarks.FontSize = phoneFont2;
      buttonClearPlacemarks.FontSize = phoneFont2;
      buttonPinsPageRemoveAds.FontSize = phoneFont2;
      ButtonClosePlacemarks.Visibility = Visibility.Collapsed;
    }



    #region License stuff
    void InitializeLicense()
    {
      // Initialize the license info for use in the app that is uploaded to the Store.
      // uncomment for release
      //licenseInformation = CurrentApp.LicenseInformation;

      // Initialize the license info for testing.
      // comment the next line for release
      licenseInformation = CurrentAppSimulator.LicenseInformation;

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
          //setAdStatus(false);
        }
        else
        {
          //setAdStatus(true);
        }
      }
      else
      {
        // A license is inactive only when there's an error.
      }
    }

    //private void setAdStatus(bool v)
    //{
    //  proApp = v;
    //  if (v)
    //  {
    //    buttonRemoveAds.Visibility = Visibility.Collapsed;
    //    //    TextBlockMessageGoPro.Visibility = Visibility.Collapsed;
    //    AdMediatorWindows.Visibility = Visibility.Collapsed;
    //  }
    //  else
    //  {
    //    buttonRemoveAds.Visibility = Visibility.Visible;
    //    TextBlockMessageGoPro.Visibility = Visibility.Visible;
    //    AdMediatorWindows.Visibility = Visibility.Visible;
    //  }
    //}

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
    }
    private void buttonRateMyApp_Click(object sender, RoutedEventArgs e)
    {
      rateMyApp();
    }
    private async void searchForMyApps()
    {
      string keyword = "Ben Byer";
      var uri = new Uri(string.Format(@"ms-windows-store:Publisher?name={0}", keyword));
      await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private async void searchForMyApp(string keyword)
    {
      var uri = new Uri(string.Format(@"ms-windows-store:search?keyword={0}", keyword));
      await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private async void rateMyApp()
    {
      string appid = "29608BenByer.GPS-GPXLogger";
      appid = "29608BenByer.GPS-LoggerPro_sat5knwjgbh50";  // Windows 10 app
      if (proApp)
      {
        appid = "29608BenByer.GPS-GPXLoggerPro_sat5knwjgbh50";  // Windows 8.1 paid app
      }
      else
      {
        appid = "29608BenByer.GPS-GPXLogger_sat5knwjgbh50";  // Windows 8.1 ad supported app
      }
      var uri = new Uri(string.Format("ms-windows-store:Review?PFN={0}", appid));  // OurPackageFamilyName
      await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private async void downloadProApp()
    {
      string PFN = "29608BenByer.GPS-GPXLoggerPro_sat5knwjgbh50";
      var uri = new Uri(string.Format("ms-windows-store://pdp/?PFN={0}", PFN));
      //var = new Uri(string.Format("ms-windows-store://pdp/?ProductId={0}", ProductId));//var uri = new Uri(string.Format(@"ms-windows-store:search?keyword={0}", keyword));
      await Windows.System.Launcher.LaunchUriAsync(uri);
    }
    #endregion

    private async void popupRemoveAdsDialog()
    {
      if (!licenseInformation.ProductLicenses["GPSProApp"].IsActive)
      {
        //        setAdStatus(false);
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
                  //                setAdStatus(true);
                }
                else
                {
                  //                setAdStatus(false);
                  errorMessage(String.Format("IsTrial {0}  IsActive {1}", licenseInformation.IsTrial, licenseInformation.IsActive));
                }
              }
              catch (Exception ex)
              {
                errorMessage(ex.Message);
                //             setAdStatus(false);
              }
            }
            else
            {
              //           setAdStatus(true);
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
      if (num > 2 && ver.Revision > 0)
      {
        version += String.Format(".{0}", ver.Revision);
      }
      if (proApp)
      {
        version += String.Format(" Pro");
      }

      return version;
    }

    private void showLatLongTextBoxes(Visibility show)
    {
      //TextBlockLatLabel.Visibility = show;
      //TextBlockLongLabel.Visibility = show;
      TextBlockLatitude.Visibility = show;
      TextBlockLongitude.Visibility = show;
    }

    private void showMinMaxTextBoxes(Visibility show)
    {
      TextBlockMaxSpeed.Visibility = show;
      TextBlockMaxElevation.Visibility = show;
      TextBlockMinElevation.Visibility = show;
    }
    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
      try
      {
        if (runInBackground)
        {
 //         if (session == null)
          {
  //          TestDialog("OnNavigatingFrom Start");
            StartLocationExtensionSession();
          }
        }
        else
        {
//          TestDialog("OnNavigatingFrom End");
//          EndExtendedExecution();
        }
      }
      catch (Exception ex)
      { }
      base.OnNavigatingFrom(e);
    }
    protected override void OnLostFocus(RoutedEventArgs e)
    {
      //StartLocationExtensionSession();
//      SessionRevokedDialog("OnLostFocus");
      base.OnLostFocus(e);
    }
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      StartLocationExtensionSession();
//      SessionRevokedDialog("OnNavigatedFrom");

      base.OnNavigatedFrom(e);
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      //MyMap.Center =
      //new Geopoint(new BasicGeoposition()
      //{
      //    Latitude = 47.604,
      //    Longitude = -122.329
      //});
      MyMap.ZoomLevel = 4;
      MyMap.LandmarksVisible = true;
      MyMap.BusinessLandmarksVisible = true;

      //MyMap.Style = MapStyle.Aerial;
      //MyMap.TrafficFlowVisible = true;

      //    MyMap.ColorScheme = MapColorScheme.Dark;

      base.OnNavigatedTo(e);
    }

    private void TimerEventHandler(object sender, object e)
    {
      DateTime now = DateTime.Now;
      if (!debugging)
      {
        timeString = String.Format("{0:MMM dd, yyyy  HH:mm:ss}", now);
      }
      TextBlockTime.Text = timeString;
      GPSUpdate(now);
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

        if (logTimer != null)
        {
          TimeSpan ts = logTimer.Elapsed;
          TextBlockTripTime.Text = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
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
      if (savedDataCount > 0)
      {
        if (isWindowsPhone)
        {
          if (now.Second == 0) // && showFullGPSDataLog)
          {
            minuteCount--;
            if (minuteCount <= 0)
            {
              if (dataSaved && gpsRunning)
              {
                saveTheData(false);
              }
              minuteCount = minutesToSave;
            }
          }
        }
        else
        {
          if (now.Second == 0) // && showFullGPSDataLog)
          {
            if (dataSaved && gpsRunning)
            {
              saveTheData(false);
            }
          }
        }
      }
    }

    private void updateGPSScreenData()
    {
      String str;
      BasicGeopositionExtended currpos = GPX.GetCurrentLocation();
      double speed = 0;
      double KMH_MPHConversion = GPX.GetMetresPerSecondToKMH_MPHConversion((bool)RadioButtonMetric1.IsChecked);
      double metresOrFeet = GPX.GetMetresToFeetConversion((bool)RadioButtonImperial1.IsChecked);
      double metresOrMiles = GPX.GetMetresToMilesConversion((bool)RadioButtonImperial1.IsChecked);

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

        //if (isWindowsPhone)
        //{
        //  DisplayPhoneSpecific(currpos, speed, KMH_MPHConversion, metresOrFeet, metresOrMiles);
        //}
        //else
        {
          DisplayTabletSpecific(currpos, speed, KMH_MPHConversion, metresOrFeet, metresOrMiles);
          if ((bool)CheckBoxShowDistanceToNearestPin.IsChecked)
          {
            if (!usetextBlockMessage1ForDebugging)
            {
              textBlockMessage1.Text = getDistanceToClosestPin(currpos, metresOrFeet, metresOrMiles);
            }
          }
        }

        TextBoxPositionData.Text = GetTextBoxPositionData(currpos, speed, metresOrFeet);
        //          TextBoxPositionData2.Text = str;

        // GPX.positionString;
        if (showFullGPSDataLog)
        {
          //TextBoxGPSList.Text = GPX.GetCSVData();
        }
      }
      catch (Exception ex)
      { }
    }

    //private void DisplayPhoneSpecific(BasicGeopositionExtended currpos, double speed, double kMH_MPHConversion, double metresOrFeet, double metresOrMiles)
    //{
    //  TextBlockSpeedPhone.Text = String.Format("Speed: {0:0.0}", speed) + getUnits(1);
    //  TextBlockMaxSpeedPhone.Text = String.Format("Max: {0:0.0}", GPX.MaxSpeed * kMH_MPHConversion) + getUnits(1);
    //  if (currpos != null)
    //  {
    //    //TextBlockLatitudePhone.Text = String.Format("{0:0.000000}", currpos.pos.Latitude);
    //    //TextBlockLongitudePhone.Text = String.Format("{0:0.000000}", currpos.pos.Longitude);
    //    TextBlockAltitudePhone.Text = String.Format("Elev: {0:0.0}", currpos.pos.Altitude * metresOrFeet) + getUnits(2);
    //    TextBlockHeadingPhone.Text = String.Format("Heading: {0}", GPX.GetFlightDirection((int)currpos.Heading, ref currentHeading));
    //    //       currentHeading = (double)currpos.Heading;
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
    //    if (logTimer != null)
    //    {
    //      TimeSpan ts = logTimer.Elapsed;
    //      TextBlockTripTimePhone.Text = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
    //      //           TextBlockTripTime.Text = String.Format("{0:00}:{1:00}:{2:00}", tripTime.Hours, tripTime.Minutes, tripTime.Seconds);
    //    }
    //    //if (GPX.MaxElevation != -1000.0)
    //    //{
    //    //  TextBlockMaxElevationPhone.Text = String.Format("{0:0.0}", GPX.MaxElevation * metresOrFeet) + getUnits(2);
    //    //}

    //    //if (GPX.MinElevation != 100000)
    //    //{
    //    //  TextBlockMinElevationPhone.Text = String.Format("{0:0.0}", GPX.MinElevation * metresOrFeet) + getUnits(2);
    //    //}
    //  }
    //  //TextBlockPositionCountPhone.Text = GPX.positionsCount.ToString();
    //  //TextBlockLastFixTimePhone.Text = String.Format("Last Fix: {0}", GPX.GetLastFixTime());
    //  //       if ()
    //  //          GPX.DistanceTravelled = 13610;
    //  if (GPX.DistanceTravelled < 1609)
    //  {
    //    String units = getUnits(2);
    //    if (units == " m")
    //    {
    //      if (GPX.DistanceTravelled > 1000)
    //      {
    //        TextBlockDistancePhone.Text = String.Format("{0:0.00} {1}", GPX.GetDistanceTravelled() / metresOrMiles, getUnits(3));
    //      }
    //      else
    //      {
    //        TextBlockDistancePhone.Text = String.Format("{0} {1}", GPX.GetDistanceTravelled() / metresOrMiles, units);
    //      }
    //    }
    //    else
    //    {
    //      if (GPX.DistanceTravelled < 1609)
    //      {
    //        TextBlockDistancePhone.Text = String.Format("{0:0.0} {1}", GPX.DistanceTravelled * metresOrFeet, units);
    //      }
    //      else
    //      {
    //        TextBlockDistancePhone.Text = String.Format("{0:0.0} {1}", GPX.GetDistanceTravelled() * metresOrFeet, units);
    //      }
    //    }
    //  }
    //  else
    //  {
    //    TextBlockDistancePhone.Text = String.Format("{0:0.00} {1}", GPX.GetDistanceTravelled() * metresOrMiles, getUnits(3));
    //  }
    //}

    private string GetTextBoxPositionData(BasicGeopositionExtended currpos, double speed, double metresOrFeet)
    {
      string str;
      PositionStatus status = geolocator.LocationStatus;
      str = "Current Position";
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

      return str;
    }

    private void DisplayTabletSpecific(BasicGeopositionExtended currpos, double speed, double KMH_MPHConversion, double metresOrFeet, double metresOrMiles)
    {
      TextBlockSpeed.Text = String.Format("Speed: {0:0.0}", speed) + getUnits(1);
      TextBlockMaxSpeed.Text = String.Format("Max: {0:0.0}", GPX.MaxSpeed * KMH_MPHConversion) + getUnits(1);
      if (currpos != null)
      {
        TextBlockLatitude.Text = String.Format("Lat: {0:0.000000}", currpos.pos.Latitude);
        TextBlockLongitude.Text = String.Format("Lon: {0:0.000000}", currpos.pos.Longitude);
        TextBlockHeading.Text = String.Format("{0}", GPX.GetFlightDirection((int)currpos.Heading, ref currentHeading));
        //       currentHeading = (double)currpos.Heading;
        if (!double.IsNaN(currentHeading))
        {
          if (rotateMapWithHeading)
          {
            MyMap.Heading = currentHeading;
          }
          else
          {
            MyMap.Heading = 0;
          }
        }
        if (logTimer != null)
        {
          TimeSpan ts = logTimer.Elapsed;
          TextBlockTripTime.Text = String.Format("Time: {0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
          //           TextBlockTripTime.Text = String.Format("{0:00}:{1:00}:{2:00}", tripTime.Hours, tripTime.Minutes, tripTime.Seconds);
        }
        TextBlockGrade.Text = GPX.getGrade();
        TextBlockAltitude.Text = String.Format("Ele: {0:0.0}", currpos.pos.Altitude * metresOrFeet) + getUnits(2);

        if (GPX.MaxElevation != -10000.0)
        {
          TextBlockMaxElevation.Text = String.Format("Max: {0:0.0}", GPX.MaxElevation * metresOrFeet) + getUnits(2);
        }

        if (GPX.MinElevation != 100000)
        {
          TextBlockMinElevation.Text = String.Format("Min: {0:0.0}", GPX.MinElevation * metresOrFeet) + getUnits(2);
        }


      }
      TextBlockPositionCount.Text = String.Format("Pos Count: {0}", GPX.positionsCount.ToString());
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
            TextBlockDistance.Text = String.Format("Dist: {0:0.00} {1}", GPX.GetDistanceTravelled() / metresOrMiles, getUnits(3));
            if (GPX.DistanceTravelled > 2000)
            {
              TextBlockDistance.Text = String.Format("Dist: {0:0.0} {1}", GPX.GetDistanceTravelled() / metresOrMiles, getUnits(3));
            }
          }
          else
          {
            TextBlockDistance.Text = String.Format("Dist: {0} {1}", GPX.GetDistanceTravelled() / metresOrMiles, units);
          }
        }
        else
        {
          if (GPX.DistanceTravelled < 1609)
          {
            TextBlockDistance.Text = String.Format("Dist: {0:0.0} {1}", GPX.DistanceTravelled * metresOrFeet, units);
          }
          else
          {
            TextBlockDistance.Text = String.Format("Dist: {0:0.0} {1}", GPX.GetDistanceTravelled() * metresOrFeet, units);
          }
        }
      }
      else
      {
        TextBlockDistance.Text = String.Format("Dist: {0:0.0} {1}", GPX.GetDistanceTravelled() * metresOrMiles, getUnits(3));
      }
    }

    private void setGUIItemsColors(Color color)
    {
      TextBlockSpeed.Foreground = new SolidColorBrush(color);
      TextBlockHeadingTitle.Foreground = new SolidColorBrush(color);
      TextBlockHeading.Foreground = new SolidColorBrush(color);
      textBlockMessage1.Foreground = new SolidColorBrush(color);
      TextBlockLatitude.Foreground = new SolidColorBrush(color);
      TextBlockLongitude.Foreground = new SolidColorBrush(color);
      TextBlockAltitude.Foreground = new SolidColorBrush(color);
      //TextBlockMaxSpeedTitle.Foreground = new SolidColorBrush(color);
      TextBlockMaxSpeed.Foreground = new SolidColorBrush(color);
      //TextBlockDistanceTitle.Foreground = new SolidColorBrush(color);
      TextBlockDistance.Foreground = new SolidColorBrush(color);
      //TextBlockTriptimeTitle.Foreground = new SolidColorBrush(color);
      TextBlockTripTime.Foreground = new SolidColorBrush(color);
      TextBlockMaxElevation.Foreground = new SolidColorBrush(color);
      TextBlockMinElevation.Foreground = new SolidColorBrush(color);
      TextBlockLastFixTime.Foreground = new SolidColorBrush(color);
      TextBlockPositionCount.Foreground = new SolidColorBrush(color);
      CheckBoxShowMap1.Foreground = new SolidColorBrush(color);
      TextBlockTime.Foreground = new SolidColorBrush(color);
      ButtonResetTripData.Foreground = new SolidColorBrush(color);
    }

    private void Button_ResetTripData(object sender, RoutedEventArgs e)
    {
      resetTripData();
    }

    private void resetTripData()
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
      //if (runInBackground)
      //{
      //var _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
      //{
      //  ProcessPosition(args);
      //});
      //}
      //else
      //{
      if (gpsRunning)
      {
        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
        {
          ProcessPosition(args);
        }));
      }
      else
      {
        int g = GPX.positionsCount;
      }
      //}
      //}));
    }
    private async void StartLocationExtensionSession()
    {
      session = new ExtendedExecutionSession();
      session.Description = "Location Tracker";
      session.Reason = ExtendedExecutionReason.LocationTracking;
      session.Revoked += Session_Revoked; //+= ExtendedExecutionSession_Revoked;
      var result = await session.RequestExtensionAsync();
      //      TestDialog("StartLocationExtensionSession");
//      sessionCount++;
      if (result == ExtendedExecutionResult.Denied)
      {
   //     TestDialog("ExtendedExecutionResult.Denied");
        EndExtendedExecution();
      }
    }
    private async void EndExtendedExecution()
    {
      if (session != null)
      {
        session.Dispose();
        session = null;
        sessionCount += 1000;
      }
    }

    private void Session_Revoked(object sender, ExtendedExecutionRevokedEventArgs args)
    {
      try
      {
  //      sessionString = "Session_Revoked End";
  //      TestDialog(sessionString);
        EndExtendedExecution();
 //       if (runInBackground)
        {
  //        if (session == null)
          {
   //         TestDialog("Session_Revoked Start");
            StartLocationExtensionSession();
          }
        }
      }
      catch (Exception ex)
      { }
    }

    private void TestDialog(string mess)
    {
      if (usetextBlockMessage1ForDebugging)
      {
        textBlockMessage1.Text = mess;
      }
      else
      {
        MessageDialog md;

        md = new MessageDialog(mess);
        md.Commands.Add(new UICommand("Ok", (UICommandInvokedHandler) =>
        {
        }));
        md.ShowAsync();
      }
    }

    private void StopLocationExtensionSession()
    {
      if (session != null)
      {
        session.Dispose();
        session = null;
      }

    }

    private void ProcessPosition(PositionChangedEventArgs args)
    {
      currPos = GPX.GetBasicGeopositionExtendedFromGeoposition(args.Position);

      GPX.AddBasicGeopositionExtendedToArray(currPos);

      gpsRunning = true;
      fileRunning = false;
      if (startPos == null)
      {
        startPos = currPos;
      }
      displayCurrentPositionIconOnMap(currPos);

      drawLivePath();

      double dif = GPX.CalcDistance(currPos, homeLocation);

      updateGPSScreenData();
      //       processGPSPosition(extPos);
      String str;// = String.Format("{0}  {1}  {2}", pos.Coordinate.Point.Position.Latitude, pos.Coordinate.Point.Position.Longitude, pos.Coordinate.Timestamp);
      str = String.Format("GPS device running.\r\n\r\nCount: {0}  Lat: {1:0.000000}  Long: {2:0.000000}  Alt: {3:0.0}  Time: {4}  Speed: {5:0.0}\r\n", GPX.positionsCount, currPos.pos.Latitude, currPos.pos.Longitude,
                                                                                                           currPos.pos.Altitude, currPos.Timestamp.ToString(), currPos.Speed);
      str = String.Format("GPS device running.\r\n\r\nCount: {0}  Lat: {1:0.000000}  Long: {2:0.000000}  PrevLat: {3:0.000000}  PrevLong: {4:0.000000} dist: {5}\r\n",
          GPX.positionsCount, GPX.Latitude, GPX.Longitude, GPX.PrevLatitude, GPX.PrevLongitude, dif);
      //  str = String.Format("Count: {0}", GPX.positionsListUnSaved.Count);
      TextBoxGPSList.Text = str;
      //       textBlockMessage1.Text = getDistanceToClosestPin(extPos);

      Debug.WriteLine(str);
      str = string.Format("Session: {0} {1}", session == null ? "FALSE" : "True", sessionCount);
      //TestDialog(str);
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

        Geopoint bp = new Geopoint(pos.pos);
        RandomAccessStreamReference image;
        if (MyMap.Style == MapStyle.Road || MyMap.Style == MapStyle.Terrain)
        {
          image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/Circle.png"));
        }
        else
        {
          image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/CircleWhite.png"));
        }
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
      startLogging();
    }


    private void startLogging()
    {
      if ((string)ButtonStartLogging.Content == "Start" || (string)ButtonStartLogging.Content == "Resume" || (string)ButtonStartLogging.Content == "Stop")
      {
        if (fileRunning)
        {
          ButtonStartLogging.Content = "Pause";
          appBarButtonPause.Visibility = Visibility.Visible;
          appBarButtonResume.Visibility = Visibility.Collapsed;

          timerForReplayGPX.Start();
        }
        else
        {
          if (!gpsRunning)
          {
            if (!preloadTrace)
            {
              clearDataForced(true);
            }
            ButtonStartLogging.Content = "Stop";
            if (saveEachMapPolylineToMemory)
            {
              pathList = new List<MapPolyline>();
            }

            try
            {
              if (runInBackground)
              {
                if (session == null)
                {
    //               TestDialog("startLogging Start");
                  StartLocationExtensionSession();
                }
              }
              else
              {
  //               TestDialog("startLogging End");
  //               EndExtendedExecution();
              }
            }
            catch (Exception ex)
            { }

            CheckBoxRunUnderLockScreen.IsEnabled = false;
            CheckBoxRunUnderLockScreen.Content = "Run in background (Restart app to change)";

            GPX.GarminBasecamp = (bool)checkBoxGarminBasecamp.IsChecked;
            //          appBarButtonStartGPS.Icon.
            //appBarButtonPause.Visibility = Visibility.Collapsed;
            //appBarButtonResume.Visibility = Visibility.Visible;

            appBarButtonStartGPS.Visibility = Visibility.Collapsed;
            appBarButtonStopGPS.Visibility = Visibility.Visible;
            //ButtonStartLogging.IsEnabled = false;

            MenuFlyoutLogs.IsEnabled = false;
            ButtonViewLogs.IsEnabled = false;
            ButtonSave.IsEnabled = true;
            MenuFlyoutSave.IsEnabled = true;
            ButtonClear.IsEnabled = true;
            MenuFlyoutClear.IsEnabled = true;
            ButtonResetTripData.IsEnabled = true;
            MenuFlyoutResetTripData.IsEnabled = true;
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

            interval = Convert.ToUInt16(TextBoxSetInterval.Text);
            movementThreshold = Convert.ToInt16(TextBoxSetMovementThreshold.Text);
            desiredAccuracyInMeters = Convert.ToUInt16(TextBoxSetDesiredAccuracy.Text);

            GPSLogger.GPSShared.initGeolocator(ref geolocator, interval, movementThreshold, desiredAccuracyInMeters);
            geolocator.PositionChanged += new TypedEventHandler<Geolocator, PositionChangedEventArgs>(onGeolocator_PositionChanged);

            //    ButtonStartLogging.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //ButtonStopLogging.Visibility = Windows.UI.Xaml.Visibility.Visible;

            firstPoll = true;
            zoomLevel = 20;
          }
          else
          {
            ButtonStartLogging.Content = "Start";
            appBarButtonStartGPS.Visibility = Visibility.Visible;
            appBarButtonStopGPS.Visibility = Visibility.Collapsed;
            TextBlockLastFixTime.Text = "GPS Stopped.";
            geolocator = null;
            gpsRunning = false;
            pausedCount = 0;
            MenuFlyoutLogs.IsEnabled = true;
            ButtonViewLogs.IsEnabled = true;
            ButtonSave.IsEnabled = false;
            MenuFlyoutSave.IsEnabled = false;
 //           ButtonClear.IsEnabled = false;
 //           MenuFlyoutClear.IsEnabled = false;
            logTimer.Stop();
   //         newSavePicker = true;
            if (GPX.positionsListUnSaved.Count > 0)
            {
              stoppingGPSWithUnsavedPositions();
            }
            else
            {
              saveTheData(true);
            }
            clearDataForced(false);
          }
        }
      }
      else
      {
        if ((string)ButtonStartLogging.Content == "Pause")
        {
          //        ButtonStartLogging.Visibility = Windows.UI.Xaml.Visibility.Visible;
          ButtonStartLogging.Content = "Resume";
          appBarButtonPause.Visibility = Visibility.Collapsed;
          appBarButtonResume.Visibility = Visibility.Visible;
          if (fileRunning)
          {
            timerForReplayGPX.Stop();
          }
          else
          {
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
    }

    private async void stoppingGPSWithUnsavedPositions()
    {

      if (savePicker == null)
      {
        try
        {
          MessageDialog md;
          string message;

          message = "WARNING!\r\nThere is unsaved GPS data.\r\n\r\nDo you want to save the data before stopping the GPS?";

          md = new MessageDialog(message);
          md.Commands.Add(new UICommand("Yes", async (UICommandInvokedHandler) =>
          {
            await saveTheData(true);
          }));
          md.Commands.Add(new UICommand("No"));
          await md.ShowAsync();

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
        await saveTheData(true);

      }
    }



    private void pauseRunningFile()
    {
      throw new NotImplementedException();
    }

    //  private async void buttonStart_Click(object sender, RoutedEventArgs e)
    //{      
    //}

    private async Task loadPath()
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
        buttonLoad.Content = "Draw";
        textBlockMessage.Text = pickedFilename;// GPX.CurrentTrace;
        TextBlockFileSaved.Text = String.Format("Running saved file: {0}", pickedFilename);

      }
      else
      {
        textBlockMessage.Text = "Cancelled";
        buttonLoad.Content = "Browse Files...";
      }
    }

    private void DrawPathBetweenTwoPoints(Geopoint p1, Geopoint p2)
    {
      Color strokeColor = liveStrokeColor;
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
      Color strokeColor = liveStrokeColor;
      int count = 0;
      int strokeWidth = 2;

      zoomLevel = MyMap.ZoomLevel = initialZoomLevel;
      var fullTrace = PointList.GetLinesFromFile(GPX.CurrentFileTrace);
      if (preloadTrace)
      {
        strokeColor = Colors.Brown;
        strokeWidth = 1;
      }
      count = DrawTheTrace(strokeColor, fullTrace, strokeWidth);
      //     var ex = MapExtensions.GetViewArea(MyMap);
    }

    private async void drawLivePath()
    {
      Color strokeColor = liveStrokeColor;
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
      draw();
    }

    private async void draw()
    {
      drawWholePath = (bool)RadioButtonFullTrace.IsChecked;
      realTime = (bool)RadioButtonRealTime.IsChecked;
      preloadTrace = (bool)RadioButtonPreloadTrace.IsChecked;

      if ((string)buttonLoad.Content == "Browse Files...")
      {
        await loadPath();
        processLoadedFile();
        RadioButtonFullTrace.IsEnabled = true;
      }
      else
      {
        if (!String.IsNullOrEmpty(GPX.CurrentFileTrace))
        {
          if (!preloadTrace && !drawWholePath)
          {
            appBarButtonPause.Visibility = Visibility.Visible;
            appBarButtonResume.Visibility = Visibility.Collapsed;
            appBarButtonStartGPS.Visibility = Visibility.Collapsed;

            RadioButtonFullTrace.IsEnabled = false;
            if (saveEachMapPolylineToMemory)
            {
              pathList = new List<MapPolyline>();
            }
            gpsRunning = false;
            fileRunning = true;
            firstPoll = true;
            MyMap.ZoomLevel = 15;
            ButtonStartLogging.Content = "Pause";
            TextBlockLastFixTime.Visibility = Visibility.Collapsed;

            logTimer = new Stopwatch();
            logTimer.Start();
            GPX.ResetPositionInDataArray();
          }
          if (drawWholePath || preloadTrace)
          {
            DrawWholePathFromFile();
          }
          else
          {
            timerForReplayGPX.Start();
          }

          buttonLoad.Content = "Browse Files...";
          ButtonSave.IsEnabled = false;
          MenuFlyoutSave.IsEnabled = false;
          ButtonResetTripData.IsEnabled = true;
          MenuFlyoutResetTripData.IsEnabled = true;
          ButtonClear.IsEnabled = true;
          MenuFlyoutClear.IsEnabled = true;
          isTimeForNewPosition = true;

          LogsClicked();

          //showLogItems(false);
          //showGPSItems(true);
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

          //          textBlockMessage1.Text = getDistanceToClosestPin(thisPos); 


          GPX.AddBasicGeopositionExtendedToArray(thisPos);

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
          int timespan;
          if (realTime)
          {
            tickMultiplier = 1;// 0000000;
            timespan = (int)((nextPosTime - thisPos.Timestamp) * tickMultiplier);
            if (timespan < 100000)
            {
              tickMultiplier = 10000000;
              timespan = (int)((nextPosTime - thisPos.Timestamp) * tickMultiplier);
            }
            timerForReplayGPX.Interval = new TimeSpan(timespan); // (0, 0, (int)(nextPosTime - thisPos.Timestamp)); // 50000);
          }
          else
          {
            timespan = 1000;
            if ((bool)radioButtonFast.IsChecked)
              timespan = 500;
            if ((bool)radioButtonFaster.IsChecked)
              timespan = 100;
            if ((bool)radioButtonFastest.IsChecked)
              timespan = 1;
            timerForReplayGPX.Interval = new TimeSpan(0, 0, 0, 0, timespan);
            //        tickMultiplier = 1;
          }
          timerForReplayGPX.Start();
        }
      }
    }

    private string getClosestPin(BasicGeopositionExtended cp)
    {
      string str = getDistanceToClosestPin(cp);

      str = str.Remove(0, str.IndexOf("to ") + 3).Trim();

      return str;
    }

    private string getDistanceToClosestPin(BasicGeopositionExtended tp)
    {
      double mof = GPX.GetMetresToFeetConversion((bool)RadioButtonImperial1.IsChecked);
      double mom = GPX.GetMetresToMilesConversion((bool)RadioButtonImperial1.IsChecked);

      return getDistanceToClosestPin(tp, mof, mom);
    }
    private string getDistanceToClosestPin(BasicGeopositionExtended thisPos, double metresOrFeet, double metresOrMiles)
    {
      string str = "";
      double dif = 0;
      double difdif = 100000000;

      foreach (BasicGeopositionExtended bge in pinLocations)
      {
        dif = GPX.CalcDistance(thisPos, bge);
        if (dif < difdif)
        {
          difdif = dif;

          if ((bool)RadioButtonMetric1.IsChecked)
          {
            if (dif > 1000)
            {
              metresOrMiles = 1000;
              str = String.Format("{0:0.0}{1} to {2}", dif / metresOrMiles, getUnits(3), bge.Name);
              if (dif > 10000)
              {
                str = String.Format("{0:0}{1} to {2}", dif / metresOrMiles, getUnits(3), bge.Name);
              }
            }
            else
            {
              metresOrMiles = 1;
              str = String.Format("{0}{1} to {2}", dif / metresOrMiles, getUnits(2), bge.Name);
            }
          }
          else
          {
            if (dif < 1609)
            {
              str = String.Format("{0:0}{1} to {2}", dif * metresOrFeet, getUnits(2), bge.Name);
            }
            else
            {
              dif /= 1000;
              str = String.Format("{0:0.0}{1} to {2}", dif * metresOrMiles, getUnits(3), bge.Name);
              if (dif > 50)
              {
                str = String.Format("{0:0}{1} to {2}", dif * metresOrMiles, getUnits(3), bge.Name);
              }
            }
          }

          str = str;
          //if ((bool)RadioButtonImperial1.IsChecked)
          //{
          //  dif *= metresOrFeet;
          //  if (dif < 5280)
          //  {
          //    str = string.Format("{0:0} ft to {1}", dif, bge.Name);
          //  }
          //  else
          //  {
          //    str = string.Format("{0:0.0} mi to {1}", dif, bge.Name);
          //  }
          //}


        }
      }

      return str;
    }

    private void RadioButtonLogs_Click(object sender, RoutedEventArgs e)
    {
      radioButtonLogsClicked();
    }

    private void radioButtonLogsClicked()
    {
      if (isWindowsPhone)
      {
        TextBlockSlowLoad.Text = "";
      }
      else
      {
        TextBlockSlowLoad.Text = "(Note: Large files may load very slow)";
      }

      //      SetReplayTypeRadioButtons();
      //      SetReplaySpeedRadioButtons();
      if ((bool)RadioButtonFullTrace.IsChecked)
      {
        TextBlockSlowLoad.Text = "(Note: Large files may load very slow)";
      }
      if ((bool)RadioButtonAccelerated.IsChecked)
      {
        radioButtonSlow.IsEnabled = true;
        radioButtonFast.IsEnabled = true;
        radioButtonFaster.IsEnabled = true;
        radioButtonFastest.IsEnabled = true;
        realTime = false;
      }
      else
      {
        radioButtonSlow.IsEnabled = false;
        radioButtonFast.IsEnabled = false;
        radioButtonFaster.IsEnabled = false;
        radioButtonFastest.IsEnabled = false;
      }

      if ((bool)RadioButtonRealTime.IsChecked)
      {
        realTime = true;
      }

      savelLocalSettings();
    }
    private BasicGeopositionExtended getNextPosition(ref long nextPositionTime)
    {
      BasicGeopositionExtended geo = null;

      geo = GPX.GetNextPositionForReplay(ref nextPositionTime);

      return geo;
    }

    private void processLoadedFile()
    {
      int count = 0;

      GPX.GetBasicGeopositionExtendedFromString();
    }
    private int DrawTheTrace(Color strokeColor, IEnumerable<PointList> fullTrace)
    {
      return DrawTheTrace(strokeColor, fullTrace, 2);
    }
    private int DrawTheTrace(Color strokeColor, IEnumerable<PointList> fullTrace, double strokeThickness)
    {
      double lat = 49.90;
      double lon = -119.45;
      int count = 0;
      int points = 0;

      foreach (var dataObject in fullTrace)
      {
        var shape = new MapPolyline
        {
          StrokeThickness = strokeThickness,
          StrokeColor = strokeColor,
          StrokeDashed = false,
          ZIndex = 4,
          Path = new Geopath(dataObject.Points.Select(p => p.Position))
        };
        count++;
        if (saveEachMapPolylineToMemory) 
        {
          pathList.Add(shape);
        }

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
      saveTheData(false);
    }
  
    private async Task saveTheData(bool force)
    {
      String GPXString = "";
      if (gpsRunning || force) //!fileRunning)
      {
        RadioButtonCSV1.IsEnabled = false;
        RadioButtonGPX1.IsEnabled = false;
        if ((bool)RadioButtonGPX1.IsChecked)
        {
          if (showFullGPSDataLog)// || GPX.GarminBasecamp)
          {
            GPXString = GPX.GetAllGPXData();
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

        if (force)
        {
          GPXString += "</trk>\r\n</gpx>\r\n";
        }

        if (GPXString.Length > 0)
        {
          saveClickUseFileSavePicker(GPXString);
        }
      }
  //    return true;
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

    private string getCSVData(string positionsString)
    {
      String str = "";

      str = positionsString;

      return str;
    }

    async void saveClickUseFileSavePicker(String saveData)
    {
      if ((savePicker == null || newSavePicker) && !tryingToSaveAFile)
      {
        savePicker = new FileSavePicker();

        savePicker.SuggestedStartLocation = PickerLocationId.ComputerFolder; //.DocumentsLibrary;

        // Default extension if the user does not select a choice explicitly from the dropdown
        if (saveData.Contains("xml") || saveData.Contains("trkpt"))
        {
          savePicker.DefaultFileExtension = ".gpx";
          savePicker.FileTypeChoices.Add("GPX File", new List<string>() { ".gpx" });
          savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });

        }
        else
        {
          savePicker.DefaultFileExtension = ".csv";
          savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });
          savePicker.FileTypeChoices.Add("GPX File", new List<string>() { ".gpx" });
        }
      }
      if (useDateTimeForFilename)
      {
 //       ComboBoxItem item = comboBoxFilePattern.Items[g];
        savePicker.SuggestedFileName = getFileNameBasedOnFilePattern();
      }

      savePicker.SuggestedFileName = savePicker.SuggestedFileName.Replace(":", "-");
      savePicker.SuggestedFileName = savePicker.SuggestedFileName.Replace("/", "-");
      if (addWPToFilename)
      {
        if (isWindowsPhone)
        {          
          savePicker.SuggestedFileName = savePicker.SuggestedFileName.Insert(savePicker.SuggestedFileName.IndexOf("."), "WP");
        }
      }
      if ((file == null || newSavePicker) && !tryingToSaveAFile)
      {
        try
        {
          dataSaved = true;

          tryingToSaveAFile = true;
          file = await savePicker.PickSaveFileAsync();
//          bool t = file.IsAvailable;
          if (file == null)
          {
            dataSaved = false;
            tryingToSaveAFile = false;
          }
        }
        catch (Exception ex)
        {
          dataSaved = false;
        }
      }
      if (file != null)
      {
        // Application now has read/write access to the saved file

        //if ((bool)checkBoxGarminBasecamp.IsChecked)//isWindowsPhone ||  && false)
        //{
        //  string s = file.Name;
        //  //if (File.Exists(s))
        //  //{
        //  //  File.Delete(s);
        //  //}
        //  await FileIO.WriteTextAsync(file, saveData);
        //}
        //else
        {
          tryingToSaveAFile = true;
          await saveTheData(file, saveData);

          savedDataCount++;
          GPX.positionsListUnSaved = new List<BasicGeopositionExtended>();

          tryingToSaveAFile = false;
        }
        if (file != null)
        {
          if (isWindowsPhone)
          {
            TextBlockFileSaved.Text = String.Format("File: {0}{1}", file.DisplayName, file.FileType); //.SuggestedFileName);
          }
          else
          {
            TextBlockFileSaved.Text = String.Format("File saved: {0}", file.Path); //.SuggestedFileName);
          }
          newSavePicker = false;
        }                //         status.Text = "Your File Successfully Saved";
      }
      else
      {
        TextBlockFileSaved.Text = String.Format("File not saved:");
        dataSaved = false;
        tryingToSaveAFile = false;
        //          status.Text = "File was not returned";
      }
      //      await filePicker.PickSaveFileAsync();
    }

    private async Task saveTheData(StorageFile file, string saveData)
    {
      await FileIO.AppendTextAsync(file, saveData);
    }

    private string getFileNameBasedOnFilePattern()
    {
      string str;

      switch (comboBoxFilePattern.SelectedIndex)
      {
        case 0:
          //filePatterns[0] = "{yyyy}-{mm}-{dd} {hh}-{mm}-{ss}";
          str = string.Format("{0:0000}-{1:00}-{2:00} {3:00}-{4:00}-{5:00}{6}", startTimeGPS.Year, startTimeGPS.Month, startTimeGPS.Day,
                                                                                startTimeGPS.Hour, startTimeGPS.Minute, startTimeGPS.Second, savePicker.DefaultFileExtension);
          break;

        case 1:
        //filePatterns[1] = "{mm}-{dd}-{yyyy} {hh}-{mm}-{ss}";
          str = string.Format("{0:00}-{1:00}-{2:0000} {3:00}-{4:00}-{5:00}{6}", startTimeGPS.Month, startTimeGPS.Day, startTimeGPS.Year,
                                                                                startTimeGPS.Hour, startTimeGPS.Minute, startTimeGPS.Second, savePicker.DefaultFileExtension);
          break;

        case 2:
          //filePatterns[2] = "{yyyy}-{mm}-{dd}";
          str = string.Format("{0:0000}-{1:00}-{2:00}{3}", startTimeGPS.Year, startTimeGPS.Month, startTimeGPS.Day, savePicker.DefaultFileExtension);
          break;

        case 3:
          //filePatterns[3] = "{mm}-{dd}-{yyyy}";
          str = string.Format("{0:00}-{1:00}-{2:0000}{3}", startTimeGPS.Month, startTimeGPS.Day, startTimeGPS.Year, savePicker.DefaultFileExtension);
          break;

        case 4:
          //filePatterns[3] = "{mm}-{dd}-{yyyy}";
          str = string.Format("{0:00}-{1:00}-{2:0000}{3}", startTimeGPS.Day, startTimeGPS.Month, startTimeGPS.Year, savePicker.DefaultFileExtension);
          break;

        case 5:
          //filePatterns[4] = "{closest placemark}-{yyyy}-{mm}-{dd}";
          if (pinLocations == null)
          {
            str = string.Format("{0:0000}-{1:00}-{2:00} {3:00}-{4:00}-{5:00}{6}", startTimeGPS.Year, startTimeGPS.Month, startTimeGPS.Day,
                                                                                  startTimeGPS.Hour, startTimeGPS.Minute, startTimeGPS.Second, savePicker.DefaultFileExtension);
          }
          else
          {
            str = string.Format("{0} {1:0000}-{2:00}-{3:00} {4:00}-{5:00}{6}", getClosestPin(startPos), startTimeGPS.Year, startTimeGPS.Month, startTimeGPS.Day,
                                                                               startTimeGPS.Hour, startTimeGPS.Minute, savePicker.DefaultFileExtension);
          }
          break;

        default:
          str = string.Format("{0:00}-{1:00}-{2:0000} {3:00}-{4:00}-{5:00}{6}", startTimeGPS.Month, startTimeGPS.Day, startTimeGPS.Year,
                                                                                startTimeGPS.Hour, startTimeGPS.Minute, startTimeGPS.Second, savePicker.DefaultFileExtension);

          break;
      }
      return str;
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
        clearDataForced(true);

      }));
      md.Commands.Add(new UICommand("No"));
      md.ShowAsync();
    }

    private void clearDataForced(bool clearMapTrace)
    {
      saveTheData(false);

      //       changecount = 0;
      //     positionsString = "";
      GPX.ClearData(clearMapTrace);

      //appBarButtonStartGPS.IsEnabled = true;
      //ButtonStartLogging.IsEnabled = true;

      startTimeTrip = startTimeGPS = DateTime.Now;


      newSavePicker = true;
      //     savePicker = null;
      file = null;
      dataSaved = false;

      logTimer = new Stopwatch();

      RadioButtonCSV1.IsEnabled = true;
      RadioButtonGPX1.IsEnabled = true;

      TextBlockFileSaved.Text = "File not saved.";

      if (clearMapTrace)
      {
        logTimer.Start();
        TextBoxGPSList.Text = "";
        TextBoxPositionData.Text = "";
        TextBlockPositionCount.Text = "Pos Count:";

        if (pathList != null)
        {
          for (int g = 0; g < pathList.Count; g++)
          {
            var ele = pathList[g];
            if (MyMap.MapElements.Contains(ele))
            {
              MyMap.MapElements.Remove(ele);
            }
          }
          //for (int f = shapeList.Count - 1; f >= 0; f--)
          //{
          //  int num = shapeList[f];

          //  MyMap.MapElements.RemoveAt(num);
          //}
        }
        else
        {
          MyMap.MapElements.Clear();
        }

        pointsMyMapVisible = false;
        ShowPointsFromFile(0);

        if (saveEachMapPolylineToMemory)
        {
          pathList = new List<MapPolyline>();
        }
        //      MyMap = new MapControl();
        //     MyMap.MapElements.Clear();
      }
    }

    private async void Button_Settings(object sender, RoutedEventArgs e)
    {
      SettingsClicked();
      //showSettingsItems(true);
      //showGPSItems(false);
    }

    private void showGPSItems(bool hideOrShow)
    {
      if (hideOrShow)
      {
        GPSItemsContainer.Visibility = Visibility.Visible;
        TextBlockTime.Visibility = Visibility.Visible;
      }
      else
      {
        GPSItemsContainer.Visibility = Visibility.Collapsed;
        TextBlockTime.Visibility = Visibility.Collapsed;
      }
    }

    private void showSettingsItems(bool hideOrShow)
    {
      if (hideOrShow) // Show settings stuff
      {
        SettingsGrid.Visibility = Visibility.Visible;
        SettingsContainer2.Visibility = Visibility.Visible;
        SettingsContainerAbout.Visibility = Visibility.Visible;
        SettingsTitle.Visibility = Visibility.Visible;
        //buttonLoadPoints.Visibility = Windows.UI.Xaml.Visibility.Visible;

        showTopRowButtonsVisible(false);
        //       TextBlockShowMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        TextBoxGPSList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        TextBoxPositionData.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //      TextBoxPositionData2.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        CheckBoxShowMap1.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        //        MyMap.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
      }
      else
      {
        SettingsGrid.Visibility = Visibility.Collapsed;
        SettingsContainer2.Visibility = Visibility.Collapsed;
        SettingsContainerAbout.Visibility = Visibility.Collapsed;
        SettingsTitle.Visibility = Visibility.Collapsed;
        //buttonLoadPoints.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        //  = true;

        //     showTopRowButtonsVisible(true);
        if (!isWindowsPhone)
        {
          CheckBoxShowMap1.Visibility = Visibility.Visible;
        }
        //    TextBlockFileSaved.Visibility = Windows.UI.Xaml.Visibility.Visible;
        //       TextBlockShowMap.Visibility = Visibility.Visible;

        //        ShowMap(0);
      }
    }
    private void showLogItems(bool hideOrShow)
    {
      if (hideOrShow) // Show logs stuff
      {
        //        ItemsControlLogs.Visibility = Windows.UI.Xaml.Visibility.Visible;
        LogsTitle.Visibility = Visibility.Visible;
        LogsContainer.Visibility = Visibility.Visible;
        //    showTopRowButtonsVisible(false);
        TextBoxGPSList.Visibility = Visibility.Collapsed;
        TextBoxPositionData.Visibility = Visibility.Collapsed;
      }
      else
      {
        LogsTitle.Visibility = Visibility.Collapsed;
        LogsContainer.Visibility = Visibility.Collapsed;
      }
    }

    private void Button_CloseLogFilesClick(object sender, RoutedEventArgs e)
    {
      LogsClicked();
    }

    private async void ShowMap(int which)
    {
      bool show = false;
      if ((bool)CheckBoxShowMap1.IsChecked) // || (bool)CheckBoxShowMap1Phone.IsChecked)
      {
        show = true;
        CheckBoxShowMap1.IsChecked = true;
      }
      else
      {
        CheckBoxShowMap1.IsChecked = false;
      }
      if (show)
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
        if (!isWindowsPhone)
        {
          ButtonStartLogging.Visibility = Visibility.Visible;
          ButtonSave.Visibility = Visibility.Visible;
          ButtonClear.Visibility = Visibility.Visible;
          TextBlockFileSaved.Visibility = Visibility.Visible;
          ButtonSettings1.Visibility = Visibility.Visible;
          ButtonViewLogs.Visibility = Visibility.Visible;
          //          ButtonSetDestination.Visibility = Visibility.Visible;
          CheckBoxShowMap1.Visibility = Visibility.Visible;
        }
        //        TextBlockShowMap.Visibility = Visibility.Visible;
        //     ButtonViewPins.Visibility = Visibility.Visible; 
      }
      else
      {
        ButtonStartLogging.Visibility = Visibility.Collapsed;
        ButtonSave.Visibility = Visibility.Collapsed;
        ButtonClear.Visibility = Visibility.Collapsed;
        TextBlockFileSaved.Visibility = Visibility.Collapsed;
        ButtonSettings1.Visibility = Visibility.Collapsed;
        ButtonViewLogs.Visibility = Visibility.Collapsed;
        //        ButtonSetDestination.Visibility = Visibility.Collapsed;
        CheckBoxShowMap1.Visibility = Visibility.Collapsed;
        CheckBoxShowMap1.IsChecked = true;
        //       TextBlockShowMap.Visibility = Visibility.Collapsed;
        //       ButtonViewPins.Visibility = Visibility.Collapsed;
      }
    }

    private void ButtonSetDestinationClick(object sender, RoutedEventArgs e)
    {
      RouteAndDirectionsClicked();
    }



    private void HideShowDestinationItems(bool visible)
    {
      if (visible)
      {
        ButtonCloseDestination.Visibility = Visibility.Visible;
      }
      else
      {
      }
    }
    private void showSetDestinationItems(bool hideOrShow)
    {
      if (hideOrShow) // Show Destination stuff
      {
        //       PlacemarksContainer.Focus(FocusState.Unfocused); // .Focus(FocusState);
        DestinationContainer.Visibility = Visibility.Visible;
        DestinationTitle.Visibility = Visibility.Visible;
        //       showTopRowButtonsVisible(false);
        destinationActive = true;
        //        placemarkActive = false;

        //        MyMap.Visibility = Visibility.Collapsed;
      }
      else
      {
        DestinationContainer.Visibility = Visibility.Collapsed;
        DestinationTitle.Visibility = Visibility.Collapsed;
        //     showTopRowButtonsVisible(true);
        destinationActive = false;
        //        placemarkActive = false;

        //        MyMap.Visibility = Visibility.Visible;
        ShowMap(0);
      }
    }

    private void showPinsItems(bool hideOrShow)
    {
      if (hideOrShow) // Show pins stuff
      {
        //       PlacemarksContainer.Focus(FocusState.Unfocused); // .Focus(FocusState);
        PlacemarksContainer.Visibility = Visibility.Visible;
        PlacemarksTitle.Visibility = Visibility.Visible;
        //     showTopRowButtonsVisible(false);
        //       destinationActive = false;
        placemarkActive = true;
        //       MyMap.Visibility = Visibility.Collapsed;
      }
      else
      {
        PlacemarksContainer.Visibility = Visibility.Collapsed;
        PlacemarksTitle.Visibility = Visibility.Collapsed;
        //      showTopRowButtonsVisible(true);
        //       destinationActive = false;
        placemarkActive = false;
        //       MyMap.Visibility = Visibility.Visible;
        //       ShowMap(0);
      }
    }

    private async void ButtonViewLogsClick(object sender, RoutedEventArgs e)
    {
      LogsClicked();
      //showLogItems(true);
      //showGPSItems(false);
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
      GPX.MetresOrImperial = !(bool)RadioButtonMetric1.IsChecked;
      localSettings.Values["Units"] = (bool)RadioButtonImperial1.IsChecked;
      SetUnitsRadioButtons();
    }

    private void RadioButtonImperial1_Click(object sender, RoutedEventArgs e)
    {
      GPX.MetresOrImperial = (bool)RadioButtonImperial1.IsChecked;
      localSettings.Values["Units"] = (bool)RadioButtonImperial1.IsChecked;
      SetUnitsRadioButtons();
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
    private void CheckBoxShowMinMax_Click(object sender, RoutedEventArgs e)
    {
      if ((bool)CheckBoxShowMinMax.IsChecked)
      {
        showMinMaxTextBoxes(Visibility.Visible);
      }
      else
      {
        showMinMaxTextBoxes(Visibility.Collapsed);
      }
    }

    private void Button_CloseDestinationsClick(object sender, RoutedEventArgs e)
    {
      RouteAndDirectionsClicked();
    }

    private void Button_ClosePinsClick(object sender, RoutedEventArgs e)
    {
      PlacemarksClicked();
    }

    private void Button_CloseSettingsClick(object sender, RoutedEventArgs e)
    {
      SettingsClicked();
      //showSettingsItems(false);
      //showGPSItems(true);
      //showGPSItems(true);

      //savelLocalSettings();
    }

    //private void CheckBoxShowMapPhone_Click(object sender, RoutedEventArgs e)
    //{
    //  CheckBoxShowMap1.IsChecked = CheckBoxShowMap1Phone.IsChecked;

    //  ShowMap(1);
    //}
    private void CheckBoxShowMap1_Click(object sender, RoutedEventArgs e)
    {
      //      CheckBoxShowMap1Phone.IsChecked = CheckBoxShowMap1.IsChecked;

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
    private void comboBoxMapTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      MapTypeChanged();
    }

    private void MapTypeChanged()
    {
      int index = comboBoxMapTypes.SelectedIndex;
      if (index >= 0)
      {
        CheckBoxColourScheme.IsEnabled = false;
        switch (index)
        {
          case 0:
            MyMap.Style = MapStyle.Road;
            CheckBoxColourScheme.IsEnabled = true;
            liveStrokeColor = Colors.Blue;
            break;
          case 1:
            MyMap.Style = MapStyle.Terrain;
            liveStrokeColor = Colors.Blue;
            break;
          case 2:
            MyMap.Style = MapStyle.Aerial;
            liveStrokeColor = Colors.WhiteSmoke;
            break;
          case 3:
            MyMap.Style = MapStyle.AerialWithRoads;
            liveStrokeColor = Colors.WhiteSmoke;
            break;
          case 4:
            MyMap.Style = MapStyle.Aerial3D;
            liveStrokeColor = Colors.WhiteSmoke;
            break;
          case 5:
            MyMap.Style = MapStyle.Aerial3DWithRoads;
            liveStrokeColor = Colors.WhiteSmoke;
            break;
        }
      }
    }

    private void comboBoxPinTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      int index = comboBoxPinTypes.SelectedIndex;
      if (index >= 0)
      {
        switch (index)
        {
          case 0:
            if (homeLocation == null || String.IsNullOrEmpty(homeLocation.Name))
            {
              TextBoxPinText.Text = IconTypes.Home.ToString(); //"Home";
            }
            else
            {
              TextBoxPinText.Text = homeLocation.Name;
            }
            break;
          case 1:
            if (workLocation == null || String.IsNullOrEmpty(workLocation.Name))
            {
              TextBoxPinText.Text = IconTypes.Work.ToString(); //"Work";
            }
            else
            {
              TextBoxPinText.Text = workLocation.Name;
            }
            break;
          case 2:
            if (schoolLocation == null || String.IsNullOrEmpty(schoolLocation.Name))
            {
              TextBoxPinText.Text = IconTypes.School.ToString(); //"School";
            }
            else
            {
              TextBoxPinText.Text = schoolLocation.Name;
            }
            break;
          default:
            TextBoxPinText.Text = ""; // "<Text Here>";
            break;
        }
      }
    }

    private void buttonSetPlacemark_Click(object sender, RoutedEventArgs e)
    {
      SetNewPlacemark(tap1);

      SetIconBecauseZoomLevelChanged(0, false);
      SetIconBecauseZoomLevelChanged(1, false);

      savelLocalSettings();
      SavePointsFileAsync();
      buttonSavePlacemarks.IsEnabled = true;

    }

    private void SetNewPlacemark(BasicGeoposition tap1)
    {
      string str = ""; ;// = String.Format(",{0},;
      string str2 = "";
      string temp = "";
      string search;
      int index;
      int duplicate = 0;

      search = String.Format(",{0},", TextBoxPinText.Text);
      if (comboBoxPinTypes.SelectedIndex == 0)
      {
        str2 = search = IconTypes.Home.ToString(); //"Home";
      }
      if (comboBoxPinTypes.SelectedIndex == 1)
      {
        str2 = search = IconTypes.Work.ToString(); //"Work";
      }
      if (comboBoxPinTypes.SelectedIndex == 2)
      {
        str2 = search = IconTypes.School.ToString(); //"School";
      }
      if (points.lines == null)
      {
        str = String.Format("{0},{1},{2},{3},{4},", tap1.Latitude, tap1.Longitude, TextBoxPinText.Text, zoomLevel, str2);
        points.lines = new List<string>();
        points.lines.Add(str);
      }

      if (points.lines != null)
      {
        for (int x = 0; x < points.lines.Count; x++)
        {
          str = points.lines[x];
          temp += str + "\r\n";
          if (points.lines[x].Contains(search))
          {
            index = x;
            str = str;
            duplicate = checkForPossibleDuplicatePoint(str, tap1);
            if (duplicate == 2)
            {
              RemoveIconFromMap(0, str);
              RemoveIconFromMap(1, str);
              RemoveFromPointsList(str);
            }
          }
        }
        int zoomLevel = 11;

        switch (comboBoxPinTypes.SelectedIndex)
        {
          case (int)IconTypes.Home:
            str2 = IconTypes.Home.ToString(); //"Home";
            break;
          case (int)IconTypes.Work:
            str2 = IconTypes.Work.ToString(); //"Work";
            break;
          case (int)IconTypes.School:
            str2 = IconTypes.School.ToString(); //"School";
            break;
          case (int)IconTypes.Pin:
            str2 = IconTypes.Pin.ToString(); //"Pin";
            break;
          default:
            str2 = IconTypes.Point.ToString(); //"Point";
            break;
        }
        if (duplicate == 2)
        {
          if (comboBoxPinTypes.SelectedIndex > (int)IconTypes.School)
          {
            duplicate = 0;
          }
        }
        if (duplicate == 0 || duplicate == 2)
        {
          str = String.Format("{0},{1},{2},{3},{4},", tap1.Latitude, tap1.Longitude, TextBoxPinText.Text, zoomLevel, str2);
          points.lines.Add(str);
          //       SavePointsFileAsync();

        }
        SetIconBecauseZoomLevelChanged(0, true);
        SetIconBecauseZoomLevelChanged(1, true);
      }
    }

    private void saveTempFile(string fname, string str)
    {
      points.SaveFile(fname, null, str);
    }

    private void RemoveFromPointsList(string str)
    {
      string[] items = str.Split(',');
      for (int x = 0; x < points.lines.Count; x++)
      {
        if (points.lines[x].Contains(str))
        {
          points.lines.RemoveAt(x);
        }
      }
    }

    private void RemoveIconFromMap(int whichMap, string str)
    {
      string[] items = str.Split(',');
      BasicGeoposition p = new BasicGeoposition();

      switch (whichMap)
      {
        case 0:
          for (int x = 0; x < ListOfMapIconPoints.Count; x++)
          {
            MapIcon MapIcon1 = ListOfMapIconPoints[x];
            if (MapIcon1.Title == items[2])
            {
              if (MyMap.MapElements.Contains(MapIcon1))
              {
                MyMap.MapElements.Remove(MapIcon1);
              }
              if (ListOfMapIconPoints.Contains(MapIcon1))
              {
                ListOfMapIconPoints.Remove(MapIcon1);
              }
            }
          }
          break;
      }
    }

    private int checkForPossibleDuplicatePoint(string str, BasicGeoposition tap1)
    {
      string[] items = str.Split(',');
      BasicGeoposition p = new BasicGeoposition();

      if (items.Length == 6)
      {
        p.Latitude = Convert.ToDouble(items[0]);
        p.Longitude = Convert.ToDouble(items[1]);

        if (String.Format("{0:0.000000}", p.Latitude) == String.Format("{0:0.000000}", tap1.Latitude) &&
            String.Format("{0:0.000000}", p.Longitude) == String.Format("{0:0.000000}", tap1.Longitude) &&
            items[2] == TextBoxPinText.Text)
        {
          return 3;
        }
        if (String.Format("{0:0.000000}", p.Latitude) != String.Format("{0:0.000000}", tap1.Latitude) &&
            String.Format("{0:0.000000}", p.Longitude) != String.Format("{0:0.000000}", tap1.Longitude))
        {
          if (isSingularIcon(items[4]))
            return 2;
          if (items[2] == TextBoxPinText.Text)
            return 0;
        }
        if (String.Format("{0:0.000000}", p.Latitude) == String.Format("{0:0.000000}", tap1.Latitude) &&
            String.Format("{0:0.000000}", p.Longitude) == String.Format("{0:0.000000}", tap1.Longitude))
        {
          return 1;
        }
      }

      return 0;
    }

    private void textBlockMessage_SelectionChanged(object sender, RoutedEventArgs e)
    {

    }

    private void buttonToggleLocation_Click(object sender, RoutedEventArgs e)
    {
      var navigate = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
    }

    private void TheGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      SetGUIItems();
    }

    private void buttonSavePlacemarks_Click(object sender, RoutedEventArgs e)
    {
      SavePointsFileAsync();
    }
    private async void SavePointsFileAsync()
    {
      buttonSavePlacemarks.IsEnabled = false;
      await points.SaveFile(filename, points, null);
    }

    private void buttonClearPlacemarks_Click(object sender, RoutedEventArgs e)
    {
      clearAllPlacemarks(true);
    }
    private async void clearAllPlacemarks(bool prompt)
    {
      try
      {
        // The customer doesn't own this feature, so 
        // show the purchase dialog.
        MessageDialog md;
        string message;

        if (prompt)
        {
          message = "Are you sure you want to remove ALL current placemarks?\r\n\r\nYou will need to recreate them or reload them from file.\r\nGo to the Settings page for Import/Export placemarks.";

          md = new MessageDialog(message);
          md.Commands.Add(new UICommand("Yes", async (UICommandInvokedHandler) =>
          {
            await removeAllPlacemarks();
          }));
          md.Commands.Add(new UICommand("No"));
          await md.ShowAsync();
        }
        else
        {
          await removeAllPlacemarks();
        }
        //Check the license state to determine if the in-app purchase was successful.
      }
      catch (Exception ex)
      {
        errorMessage(ex.Message);
        // The in-app purchase was not completed because 
        // an error occurred.
      }
    }

    private async Task removeAllPlacemarks()
    {
      RemovePlacemarksFromMap(0);
      RemoveFromPointsList();
      buttonSavePlacemarks.IsEnabled = true;
      SavePointsFileAsync();
    }

    private void CheckBoxShowTraffic_Click(object sender, RoutedEventArgs e)
    {
      MyMap.TrafficFlowVisible = (bool)CheckBoxTraffic.IsChecked;
    }

    private async void GetRouteAndDirections(BasicGeopositionExtended startLocation, BasicGeopositionExtended endLocation, int legCount)
    {
      string str = "";
      BasicGeoposition start = new BasicGeoposition();
      start.Latitude = startLocation.pos.Latitude;
      start.Longitude = startLocation.pos.Longitude;
      Geopoint startPoint = new Geopoint(start);

      BasicGeoposition end = new BasicGeoposition();
      end.Latitude = endLocation.pos.Latitude;
      end.Longitude = endLocation.pos.Longitude;
      Geopoint endPoint = new Geopoint(end);

      // Get the route between the points. 
      MapRouteFinderResult routeResult = await MapRouteFinder.GetDrivingRouteAsync(startPoint, endPoint, MapRouteOptimization.Time, MapRouteRestrictions.None);

      if (routeResult.Status == MapRouteFinderStatus.Success)
      {
        // Display summary info about the route. 
        str += String.Format("Estimated time: {0:0.0} min\r\n", routeResult.Route.EstimatedDuration.TotalMinutes);
        str += String.Format("Total distance: {0:0.0} km\r\n\r\n", routeResult.Route.LengthInMeters / 1000);


        // Display the directions. 
        str += String.Format("DIRECTIONS\r\n");

        int count;
        Geopath path = null;
        //    var pp = null;
        bool showDistanceOnLeg = false;
        foreach (MapRouteLeg leg in routeResult.Route.Legs)
        {
          count = routeResult.Route.Legs.Count;
          path = routeResult.Route.Path;
          var pp = path.Positions;
          count = path.Positions.Count;
          foreach (MapRouteManeuver maneuver in leg.Maneuvers)
          {
            str += String.Format("{0}", maneuver.InstructionText);
            if (showDistanceOnLeg)
            {
              str += String.Format(" for {0} m.", maneuver.LengthInMeters);
            }
            str += "\r\n";
          }
        }
        str = str;
        if (path != null && path.Positions.Count > 0)
        {
          int rcount = 0;
          routeshape = getRouteShape(path);
          rcount++;
          MyMap.MapElements.Add(routeshape);
        }
        //MyMap.MapElements.Add(path.Positions);


      }
      else
      {
        str += String.Format("A problem occurred: {0}", routeResult.Status.ToString());
      }
      routeText += str;
      textBoxRoute.Text = routeText;

    }

    private MapPolyline getRouteShape(Geopath path)
    {
      Color strokeColor = Colors.Green;
      MapPolyline route = new MapPolyline
      {
        StrokeThickness = 6,
        StrokeColor = strokeColor,
        StrokeDashed = false,
        ZIndex = 4,
        //              Path = new Geopath(path.Positions.Select(path => path.Position))
        Path = new Geopath(path.Positions) // (dataObject..Select(p => p.Position))
      };

      return route;
    }

    private async void buttonCalculateRoute_Click(object sender, RoutedEventArgs e)
    {
      Geopoint queryHintPoint = null;
      int legCount = 0;
      routeText = "";
      for (int x = 0; x < tripPlanList.Count - 1; x++)
      {
        string str = tripPlanList[legCount].FormattedAddress(tripPlanList[legCount]);//  "1970 Golfview Drive, Kelowna, BC";
        legCount++;
        MapLocation mapLocationFrom = await GetLocationFromPointOnMap(str, queryHintPoint, false);
        str = tripPlanList[legCount].FormattedAddress(tripPlanList[legCount]);
        MapLocation mapLocationTo = await GetLocationFromPointOnMap(str, queryHintPoint, false);

        BasicGeopositionExtended from = new BasicGeopositionExtended(mapLocationFrom.Point.Position);
        BasicGeopositionExtended to = new BasicGeopositionExtended(mapLocationTo.Point.Position);

        GetRouteAndDirections(from, to, legCount);
      }
    }

    async Task<MapLocation> GetLocationFromPointOnMap(string address, Geopoint queryHintPoint, bool centerOnIt)
    {
      var result = await MapLocationFinder.FindLocationsAsync(address, queryHintPoint);
      string locations = "";

      // 50.262311, -119.253682

      // Get the coordinates
      if (result.Status == MapLocationFinderStatus.Success)
      {
        if (result.Locations.Count == 0)
          return null;
        double lat = result.Locations[0].Point.Position.Latitude;
        double lon = result.Locations[0].Point.Position.Longitude;
      }
      if (result.Locations.Count > 1)
      {
        for (int x = 0; x < result.Locations.Count; x++)
        {
          var r = result.Locations[x].Point.Position;
          locations += string.Format("{0}\r\n", result.Locations[x].Address.FormattedAddress);
        }
      }
      else
      {
        if (result.Locations.Count == 0)
          return null;
        res = result.Locations[0].Point.Position;
        locations += string.Format("{0}\r\n", result.Locations[0].Address.FormattedAddress);
      }
      if (centerOnIt)
      {
        Geopoint p = new Geopoint(res);
        MyMap.Center = p;
        MyMap.ZoomLevel = 16;
      }
      //    mapLocation = result.Locations[0];
      string str = result.Locations[0].Address.StreetNumber;

      return result.Locations[0];
    }

    private void buttonClearRoute_Click(object sender, RoutedEventArgs e)
    {
      tripPlanList = new List<TripPlanning>();
      if (MyMap.MapElements.Contains(routeshape))
      {
        MyMap.MapElements.Remove(routeshape);
      }
      for (int x = 0; x < iconLastPin.Length; x++)
      {
        if (MyMap.MapElements.Contains(iconLastPin[x]))
        {
          MyMap.MapElements.Remove(iconLastPin[x]);
        }
      }
      //    textBlockRoute.Text = "";
      textBoxRoute.Text = "";
      routeText = "";
    }

    private void buttonFindLocation_Click(object sender, RoutedEventArgs e)
    {
      findTheLocation();
    }

    private async void findTheLocation()
    {
      if (homeLocation == null)
      {
        homeLocation = new BasicGeopositionExtended(new BasicGeoposition());
      }
      Geopoint p = new Geopoint(homeLocation.pos);
      await GetLocationFromPointOnMap((string)TextBoxDestinationLocation.Text, p, true);
    }

    private void buttonAddLocationToRoute_Click(object sender, RoutedEventArgs e)
    {
      addLocationToTripPlanningList();
    }

    private void addLocationToTripPlanningList()
    {
      //     DestinationList.Add(TextBoxDestinationLocation.Text);
      TripPlanning planPoint = new TripPlanning(tapTripLocation);
      planPoint.SetMapLocation(mapLocationClicked, tripLocationImage);
      planPoint.SetIcon(iconPin);

      tripPlanList.Add(planPoint);

      string str = ""; // "hi\r\nthere\r\nyou\r\nsexy\r\nthing.";
      for (int x = 0; x < tripPlanList.Count; x++)
      {
        str += String.Format("{0}\r\n", planPoint.GetStreetAddress(tripPlanList[x]));
      }
      //   textBlockRoute.Text = str;
      textBoxRoute.Text = str;
      //    putIconOnMap();
    }

    private void appBarButtonSettings_Click(object sender, RoutedEventArgs e)
    {
      SettingsClicked();
    }

    private void SettingsClicked()
    {
      settingsOpen = !settingsOpen;
      logsOpen = false;
      placemarksOpen = false;
      directionsOpen = false;
      showTopRowButtonsVisible(!settingsOpen);
      updateSettingsVisiblity();
      updateLogsVisibilty();
      updatePlacemarksVisibility();
      updateDirectionsVisibility();
      showGPSItems(!settingsOpen);
      if (settingsOpen)
      {
        MyMap.Visibility = Visibility.Collapsed;
        textBlockMessage1.Visibility = Visibility.Collapsed;
        //      SettingsContainerAbout.Margin = new Thickness(SettingsContainer1.Width + SettingsContainer2.Width + 5, 50, 0, 0);
      }
      else
      {
        MyMap.Visibility = Visibility.Visible;
        if ((bool)CheckBoxShowDistanceToNearestPin.IsChecked)
        {
          textBlockMessage1.Visibility = Visibility.Visible;
        }
        else
        {
          textBlockMessage1.Visibility = Visibility.Collapsed;
        }
      }
      GPX.GarminBasecamp = (bool)checkBoxGarminBasecamp.IsChecked;

      SetGUIItems();
    }

    private void updateSettingsVisiblity()
    {
      if (settingsOpen)
      {
        showSettingsItems(true);

        savelLocalSettings();
      }
      else
      {
        showSettingsItems(false);
      }
    }

    private void MenuFlyoutItemLogs_Click(object sender, RoutedEventArgs e)
    {
      LogsClicked();
    }

    private void LogsClicked()
    {
      logsOpen = !logsOpen;
      settingsOpen = false;
      placemarksOpen = false;
      directionsOpen = false;
      showTopRowButtonsVisible(!logsOpen);
      updateLogsVisibilty();
      updateSettingsVisiblity();
      updatePlacemarksVisibility();
      updateDirectionsVisibility();
      showGPSItems(!logsOpen);

      if (!(bool)RadioButtonFullTrace.IsChecked && !(bool)RadioButtonAccelerated.IsChecked && !(bool)RadioButtonRealTime.IsChecked)
      {
        SetReplayTypeRadioButtons();
        SetReplaySpeedRadioButtons();
      }
      if (!(bool)RadioButtonMetric1.IsChecked && !(bool)RadioButtonImperial1.IsChecked)
      {
        SetUnitsRadioButtons();
      }

      if (isWindowsPhone && logsOpen)
      {
        MyMap.Visibility = Visibility.Collapsed;
        textBlockMessage1.Visibility = Visibility.Collapsed;
      }
      else
      {
        MyMap.Visibility = Visibility.Visible;
        if ((bool)CheckBoxShowDistanceToNearestPin.IsChecked)
        {
          textBlockMessage1.Visibility = Visibility.Visible;
        }
        else
        {
          textBlockMessage1.Visibility = Visibility.Collapsed;
        }
      }
    }

    private void updateLogsVisibilty()
    {
      if (logsOpen)
      {
        showLogItems(true);
      }
      else
      {
        showLogItems(false);
        //       HideShowPinItems(showPlacemarkItems);
      }
    }

    private void appBarButtonLogs_Click(object sender, RoutedEventArgs e)
    {
    }

    private void appBarButtonDirections_Click(object sender, RoutedEventArgs e)
    {
      RouteAndDirectionsClicked();
    }

    private void RouteAndDirectionsClicked()
    {
      directionsOpen = !directionsOpen;
      settingsOpen = false;
      logsOpen = false;
      placemarksOpen = false;
      showLogItems(false);

      showTopRowButtonsVisible(!directionsOpen);
      updateDirectionsVisibility();
      updateLogsVisibilty();
      updateSettingsVisiblity();
      updatePlacemarksVisibility();

      showGPSItems(!directionsOpen);
      if (isWindowsPhone && directionsOpen)
      {
        MyMap.Visibility = Visibility.Collapsed;
        textBlockMessage1.Visibility = Visibility.Collapsed;
      }
      else
      {
        MyMap.Visibility = Visibility.Visible;
        if ((bool)CheckBoxShowDistanceToNearestPin.IsChecked)
        {
          textBlockMessage1.Visibility = Visibility.Visible;
        }
        else
        {
          textBlockMessage1.Visibility = Visibility.Collapsed;
        }
      }
    }

    private void updateDirectionsVisibility()
    {
      if (directionsOpen)
      {
        showSetDestinationItems(true);
      }
      else
      {
        showSetDestinationItems(false);
      }
    }

    private void appBarButtonPlacemarks_Click(object sender, RoutedEventArgs e)
    {
      PlacemarksClicked();
    }

    private void PlacemarksClicked()
    {
      placemarksOpen = !placemarksOpen;
      settingsOpen = false;
      logsOpen = false;
      directionsOpen = false;

      showTopRowButtonsVisible(!placemarksOpen);
      updatePlacemarksVisibility();
      updateLogsVisibilty();
      updateSettingsVisiblity();
      updateDirectionsVisibility();
      showGPSItems(!placemarksOpen);
      {
        MyMap.Visibility = Visibility.Visible;
        if ((bool)CheckBoxShowDistanceToNearestPin.IsChecked)
        {
          textBlockMessage1.Visibility = Visibility.Visible;
        }
        else
        {
          textBlockMessage1.Visibility = Visibility.Collapsed;
        }
      }
    }

    private void updatePlacemarksVisibility()
    {
      if (placemarksOpen)
      {
        showPinsItems(true);
      }
      else
      {
        showPinsItems(false);
      }
    }

    private void showThePlacemarkItems(bool v)
    {
      throw new NotImplementedException();
    }

    private void appBarButtonStart_Click(object sender, RoutedEventArgs e)
    {
      startLogging();
    }

    private void appBarButtonSave_Click(object sender, RoutedEventArgs e)
    {
      saveTheData(false);
    }

    private void appBarButtonClear_Click(object sender, RoutedEventArgs e)
    {
      clearData();
    }

    private void MenuFlyoutItemSave_Click(object sender, RoutedEventArgs e)
    {
      saveTheData(false);
    }

    private void MenuFlyoutItemClear_Click(object sender, RoutedEventArgs e)
    {
      clearData();
    }

    private void ThePage_LayoutUpdated(object sender, object e)
    {
      if (Window.Current.Bounds.Height > Window.Current.Bounds.Width)
      {
        orientationWide = false;
      }
      else
      {
        orientationWide = true;
      }
      if (lastOrientation != orientationWide)
      {
        OrientationChanged();
      }
      lastOrientation = orientationWide;
    }

    private void OrientationChanged()
    {
      SetGUIItems();
    }

    private void MenuFlyoutItemResetTrip_Click(object sender, RoutedEventArgs e)
    {
      resetTripData();
    }

    private class ExtensionRevokedEventArgs
    {
    }

    private void checkBoxGarminBasecamp_Click(object sender, RoutedEventArgs e)
    {
      if ((bool)checkBoxGarminBasecamp.IsChecked)
      {
        showGarminBaseCampDialog();
      }
    }
    private async void showGarminBaseCampDialog()
    {
      try
      {
        MessageDialog md;
        string message;

        message = "Important:\r\nBy selecting this option you will be saving data to be compatible with Garmin BaseCamp software.\r\nSpeed and Heading will NOT be saved to the file\r\n\r\nBe sure to 'Stop' the GPS when your trip is done to save ending GPX tags\r\n\r\nAre you sure you want to enable Garmin BaseCamp compatability?";

        md = new MessageDialog(message);
        md.Commands.Add(new UICommand("Yes"));
        md.Commands.Add(new UICommand("No", async (UICommandInvokedHandler) =>
        {
          checkBoxGarminBasecamp.IsChecked = false;
        }));
        await md.ShowAsync();

      }
      catch (Exception ex)
      {
        errorMessage(ex.Message);
        // The in-app purchase was not completed because 
        // an error occurred.
      }
    }

    private async void buttonExportPlacemarks_Click(object sender, RoutedEventArgs e)
    {
      exportPlacemarks();
    }

    private async void exportPlacemarks()
    {
   //   StorageFolder folder = await getFolderName();
      StorageFile file = await getFileName();

      if (filename != null)
      {
        //    string filename = String.Format("Placemarks.xml");
        //   file.
        // Prevent updates to the remote version of the file until
        // we finish making changes and call CompleteUpdatesAsync.
        try
        {
          CachedFileManager.DeferUpdates(file);
        }
        catch (Exception ex)
        { }
        // write to file
        try
        {
          await FileIO.WriteTextAsync(file, GPX.formatXMLForSaveToFile(points, proApp));
        // Let Windows know that we're finished changing the file so
        // the other app can update the remote version of the file.
        // Completing updates may require Windows to ask for user input.
          Windows.Storage.Provider.FileUpdateStatus status =
            await CachedFileManager.CompleteUpdatesAsync(file);
          if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
          {
  //          this.textBlock.Text = "File " + file.Name + " was saved.";
          }
          else
          {
      //      this.textBlock.Text = "File " + file.Name + " couldn't be saved.";
          }
        }
        catch (Exception ex)
        { }

        //     await points.SaveFile(file.Path, points, GPX.formatXMLForSaveToFile(points));
      }
    }

    private static async Task<StorageFile> getFileName()
    {
      var filePicker = new FileSavePicker();

      filePicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
      filePicker.DefaultFileExtension = ".xml";
      filePicker.FileTypeChoices.Add("XML File", new List<string>() { ".xml" });

      StorageFile file = await filePicker.PickSaveFileAsync();


      return file;
    }

    private static async Task<StorageFolder> getFolderName()
    {
      var folderPicker = new FolderPicker();

      folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
      folderPicker.FileTypeFilter.Add("*");

      StorageFolder folder = await folderPicker.PickSingleFolderAsync();
      return folder;
    }

    private void buttonImportPlacemarks_Click(object sender, RoutedEventArgs e)
    {
      //  importPlacemarksFolderPicker();
      importPlacemarks();
      //  importPlacemarksFilePicker();
      
    }
    private async void importPlacemarks()
    {
      importPlacemarksFilePicker();
    }

    private async void importPlacemarksFilePicker()
    {
      var filePicker = new FileOpenPicker();

      filePicker.FileTypeFilter.Add(".xml");
      filePicker.SuggestedStartLocation = PickerLocationId.ComputerFolder; //.DocumentsLibrary;

      StorageFile file = await filePicker.PickSingleFileAsync();
      if (file != null)
      {
        PlacemarksText = await FileIO.ReadTextAsync(file);
        if (PlacemarksText != null)
        {
          clearAllPlacemarks(false);
          setNewPlacemarks(PlacemarksText);
          SavePointsFileAsync();
        }
      }
    }

    private async void importPlacemarksFolderPicker()
    {
      var folderPicker = new FolderPicker();

      folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
      folderPicker.FileTypeFilter.Add("*");

      StorageFolder folder = await folderPicker.PickSingleFolderAsync();
      if (folder != null)
      {
        string filename = String.Format("Placemarks.xml", folder.Path);
        await points.LoadFile(folder, filename);
      }
      string str = points.strPlacemarks;
      setNewPlacemarks(str);
    }

    private void setNewPlacemarks(string str)
    {
      points.strLines = GPX.GetXMLData("Places", str);
      string[] lines = points.strLines.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
      for (int x = 0; x < lines.Length; x++)
      {
        
        if (validDataForImportExport(lines[x]) && !points.lines.Contains(lines[x]))
        {
          points.lines.Add(lines[x]);
        }
      }
     // points.lines = lines;
      pointsMyMapVisible = false;

      RemovePlacemarksFromMap(0);

      ShowPointsFromFile(0);
    }

    private bool validDataForImportExport(string line)
    {
      if (proApp)
      {
        return true;
      }

      string[] items = line.Split(new string[] { "," }, StringSplitOptions.None);
      if (items[4].ToUpper().Contains("HOME") || items[4].ToUpper().Contains("SCHOOL") || items[4].ToUpper().Contains("WORK"))
      {
        return true;
      }
      return false;
    }

    private void CheckBoxFullScreenMap_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void feedbackButton_Click(object sender, RoutedEventArgs e)
    {
      await Microsoft.Services.Store.Engagement.Feedback.LaunchFeedbackAsync();
    }

    private async void feedbackButtonTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
    {
      await Microsoft.Services.Store.Engagement.Feedback.LaunchFeedbackAsync();
    }

    private void TextBoxLatText_TextChanged(object sender, TextChangedEventArgs e)
    {
      Debug.WriteLine(TextBoxLatText.Text);
      placemarkManuallyEntered();

    }

    private void placemarkManuallyEntered()
    {
      BasicGeoposition typedLocation = new BasicGeoposition();
      if (!String.IsNullOrEmpty(TextBoxLatText.Text) && !String.IsNullOrEmpty(TextBoxLonText.Text))
      {
        try
        {
          typedLocation.Latitude = Convert.ToDouble(TextBoxLatText.Text);
          typedLocation.Longitude = Convert.ToDouble(TextBoxLonText.Text);
          string text = "1";
          if (!String.IsNullOrEmpty(TextBoxPinText.Text))
          {
            text = TextBoxPinText.Text;
          }
          SetPinLocationIcon(typedLocation, text, 1);

          buttonSetPlacemark.IsEnabled = true;
          comboBoxPinTypes.IsEnabled = true;
          TextBoxPinText.IsEnabled = true;

          lastCentrePoint = String.Format("{0}~{1}", tap1.Latitude, tap1.Longitude);
        }
        catch (Exception ex)
        { }
      }
    }

    private void TextBoxLonText_TextChanged(object sender, TextChangedEventArgs e)
    {
      Debug.WriteLine(TextBoxLonText.Text);
      placemarkManuallyEntered();
    }

    private void TextBoxPinText_TextChanged(object sender, TextChangedEventArgs e)
    {
      Debug.WriteLine(TextBoxLonText.Text);
      placemarkManuallyEntered();
    }
  }
}
