using Linearstar.Windows.RawInput;
using Microsoft.Toolkit.Uwp.Notifications;
using OSVersionExtension;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace MapTP.App
{
    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// This variable describes if a PTP exists
        /// </summary>
        public bool ptpExists;

        /// <summary>
        /// The HWND is for accepting touchpad inputs
        /// </summary>
        private HwndSource _targetSource;

        /// <summary>
        /// This is for moving the mouse
        /// </summary>
        private MouseProcessor mouseProcessor;

        /// <summary>
        /// This is for sending log messages to the log window
        /// </summary>
        /// <param name="message"></param>
        public delegate void _SendLog(string message);
        private _SendLog SendLog;

        private XmlSerializer ConfigXmlSerializer;
        private Config config;

        public int ScreenSizeX, ScreenSizeY;

        private int inputX, inputY, TouchpadSizeX = 150, TouchpadSizeY = 100;

        private int tpsx, tpsy, tpex, tpey, scsx, scsy, scex, scey, tpgx, tpgy, scgx, scgy;

        /// <summary>
        /// These variables contain the current(last) position of the touchpad input
        /// </summary>
        public int InputX { get => inputX; set => inputX = value; }
        public int InputY { get => inputY; set => inputY = value; }

        private bool _disabled_tp;
        private bool disabled_tp
        {
            get { return _disabled_tp; }
            set
            {
                _disabled_tp = value;
                StartButton.IsEnabled = !(disabled_sc || disabled_tp || !calibrated);
                if ((disabled_sc || disabled_tp || !calibrated)) { StopButtonClick(new object(), new RoutedEventArgs()); }
            }
        }

        private bool _disabled_sc;
        private bool disabled_sc
        {
            get { return _disabled_sc; }
            set
            {
                _disabled_sc = value;
                StartButton.IsEnabled = !(disabled_sc || disabled_tp || !calibrated);
                if ((disabled_sc || disabled_tp || !calibrated)) { StopButtonClick(new object(), new RoutedEventArgs()); }
            }
        }

        private bool _calibrated;
        private bool calibrated
        {
            get { return _calibrated; }
            set
            {
                _calibrated = value;
                StartButton.IsEnabled = !(disabled_sc || disabled_tp || !calibrated);
                if ((disabled_sc || disabled_tp || !calibrated)) { StopButtonClick(new object(), new RoutedEventArgs()); }

            }
        }

        /// <summary>
        /// if mapping is started, this should be true
        /// </summary>
        private bool started;

        /// <summary>
        /// if turtle mode is enabled, this should be true
        /// </summary>
        private bool turtle;

        /// <summary>
        /// This is for accepting touchpad size from the calibration window
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        public void ReceiveTouchpadSize(int X, int Y)
        {
            this.TouchpadSizeX = X;
            this.TouchpadSizeY = Y;
            if (TouchpadSizeX != 0 && TouchpadSizeY != 0)
            {
                TpAreaRect.Width = CalculateRectangle(TouchpadSizeX, TouchpadSizeY);
                TpRectGrid.Width = CalculateRectangle(TouchpadSizeX, TouchpadSizeY);
                TouchpadSizeTB.Text = $"Touchpad size: {TouchpadSizeX}x{TouchpadSizeY}";
            }
            return;
        }

        public void ReceiveScreenArea(int X, int Y, int eX, int eY)
        {
            Scsx.Text = X.ToString();
            Scsy.Text = Y.ToString();
            Scex.Text = eX.ToString();
            Scey.Text = eY.ToString();
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        /*private void TrayButtonClick(object sender, RoutedEventArgs e)
        {
            var t = new Wpf.Ui.Tray.Controls.NotifyIcon();
            t.Register();
            this.Visibility = Visibility.Hidden;
            this.ShowInTaskbar = false;
        }*/

        private void TrayCBClick(object sender, RoutedEventArgs e)
        {
            if (TrayCB.IsChecked.Value)
            {
                TrayIcon.Register();
            }
            else
            {
                TrayIcon.Unregister();
            }
        }

        private void TrayShowWindowClick(object sender, RoutedEventArgs e)
        {
            TrayShowMenuItem.IsEnabled = false;
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
        }

        private void TurtleCBClick(object sender, RoutedEventArgs e)
        {
            this.turtle = TurtleCB.IsChecked.Value;
        }

        private void InspectorButtonClick(object sender, RoutedEventArgs e)
        {
            if (started) StopButtonClick(new object(), new RoutedEventArgs());
            var w = new InspectorWindow(scsx, scsy, scex, scey);

            w.SendScreenArea = ReceiveScreenArea;
            w.MainWindow_Start = StartButtonClick;
            w.MainWindow_Stop = StopButtonClick;
            w.Show();
        }

        private void AboutButtonClick(object sender, RoutedEventArgs e)
        {
            var w = new AboutWindow();
            w.Show();
        }

        private void SuggestButtonClick(object sender, RoutedEventArgs e)
        {
            if (calibrated)
            {
                Scsx.Text = "0";
                Scsy.Text = "0";
                Scex.Text = ScreenSizeX.ToString();
                Scey.Text = ScreenSizeY.ToString();
                Tpsx.Text = "0";
                Tpsy.Text = "0";
                Tpex.Text = TouchpadSizeX.ToString();
                Tpey.Text = ((int)Math.Floor((double)TouchpadSizeX / ScreenSizeX * ScreenSizeY)).ToString();
                if (tpey > TouchpadSizeY)
                {
                    Tpey.Text = TouchpadSizeY.ToString();
                    var width = ((int)Math.Floor((double)TouchpadSizeY / ScreenSizeY * ScreenSizeX));
                    var margin = (TouchpadSizeX - width) / 2;
                    Tpsx.Text = margin.ToString();
                    Tpex.Text = (width + margin).ToString();
                }
            }
            else
            {
                HandyControl.Controls.MessageBox.Show("Please calibrate first!");
            }
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            return;
        }

        private void OnMinButtonClick(object sender, RoutedEventArgs e)
        {
            if (HideCB.IsChecked.Value && TrayCB.IsChecked.Value)
            {
                // Hide to tray
                this.Visibility = Visibility.Hidden;
                this.ShowInTaskbar = false;
                TrayShowMenuItem.IsEnabled = true;
                new ToastContentBuilder()
                       .AddText("MapTP is hidden to the taskbar tray!").Show();
            }
            else WindowState = WindowState.Minimized;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            if (HideCB.IsChecked.Value)
            {
                var r = MessageBox.Show("Are you sure to close MapTP?\n(use minimize to hide MapTP into tray icon)", "Closing MapTP", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r != MessageBoxResult.Yes) return;
            }
            Close();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Closing += OnWindowCloses;
            // ptpExists = Touchpad.Handler.Exists();
            ptpExists = false;
            var devices = RawInputDevice.GetDevices();
            foreach (var device in devices)
            {
                if (device.DeviceType == RawInputDeviceType.Hid)
                {
                    if (device.UsageAndPage == HidUsageAndPage.TouchPad)
                    {
                        ptpExists = true;
                        break;
                    }
                }
            }


            mouseProcessor = new MouseProcessor();

            ScreenSizeX = (int)Math.Floor(ScreenManager.GetScreenWidth());
            ScreenSizeY = (int)Math.Floor(ScreenManager.GetScreenHeight());
            ScAreaRect.Width = CalculateRectangle(ScreenSizeX, ScreenSizeY);
            ScRectGrid.Width = ScAreaRect.Width;
            ScreenSizeTB.Text = $"Screen Size: {ScreenSizeX}x{ScreenSizeY}";

            started = false;

            _targetSource = PresentationSource.FromVisual(this) as HwndSource; // Get the HWND of this window
            _targetSource?.AddHook(WndProc);


            if (ptpExists)
            {
                if (_targetSource != null)
                {
                    // success = Touchpad.Handler.RegisterInput(_targetSource.Handle)
                    RawInputDevice.RegisterDevice(HidUsageAndPage.TouchPad,
                        RawInputDeviceFlags.InputSink, _targetSource.Handle);
                }
                InitConfig();
            }
            else
            {
                label_PtpExists.Visibility = Visibility.Visible;
                MainCardGrid.IsEnabled = false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var version = OSVersion.GetOSVersion().Version;
            if (!(version > new Version(10, 0, 17763)))
            {
                this.Background = Brushes.White;
            }
            else if (version.Build >= 22000) // Mica
            {

                // Get PresentationSource
                PresentationSource presentationSource = PresentationSource.FromVisual((Visual)sender);

                // Subscribe to PresentationSource's ContentRendered event
                presentationSource.ContentRendered += (s, ev) => OnRendered(PresentationSource.FromVisual((Visual)sender) as HwndSource);


            }
            else
            {
                var WalterlvCompositor = new BlurManager(this)
                {
                    Color = Color.FromArgb(0x1f, 0xf0, 0x00, 0x74),
                    IsEnabled = true
                };
                this.WindowChrome.GlassFrameThickness = new Thickness(0, 0, 1, 0);
            }
        }

        /// <summary>
        /// Enable Mica when the window is rendered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRendered(HwndSource src)
        {
            var mica = new Mica(src);
            mica.enable = true;
        }

        /// <summary>
        /// This method is for limiting TextBoxes only to accept numbers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void limitnumber(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9]+");
            e.Handled = re.IsMatch(e.Text);
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            started = true;
            StartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
            TrayWorkingMenuItem.Header = "MapTP is working...";
            TrayStartMenuItem.Header = "Stop";
            TrayStartMenuItem.Click -= StartButtonClick;
            TrayStartMenuItem.Click += StopButtonClick;
            TrayIcon.Icon = new BitmapImage(new Uri("pack://application:,,,/logo.ico"));
            TrayIcon.TooltipText = "MapTP (active)";
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            started = false;
            StopButton.Visibility = Visibility.Collapsed;
            StartButton.Visibility = Visibility.Visible;
            TrayWorkingMenuItem.Header = "MapTP is not working...";
            TrayStartMenuItem.Header = "Start";
            TrayStartMenuItem.Click -= StopButtonClick;
            TrayStartMenuItem.Click += StartButtonClick;
            TrayIcon.Icon = new BitmapImage(new Uri("pack://application:,,,/logo-inactive.ico"));
            TrayIcon.TooltipText = "MapTP (inactive)";
        }

        /// <summary>
        /// This method opens the `touchpad calibration' window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalibrateButtonClick(object sender, RoutedEventArgs e)
        {
            this.StopButtonClick(sender, e); // mapping should stop while calibrating
            CalibrateWindow w = new CalibrateWindow(_targetSource)
            {
                sendSize = ReceiveTouchpadSize
            };
            bool success = (bool)w.ShowDialog();
            if (success) calibrated = true;
        }

        private void AdvancedButtonClick(object sender, RoutedEventArgs e)
        {
            var w = new AdvancedWindow(_targetSource);
            this.SendLog = w.Log;
            w.Show();
        }


        private void OnTouchpadMapUpdate(object sender, RoutedEventArgs e)
        {
            if (!calibrated) return;
            StopButtonClick(sender, e);
            bool notEmpty = Tpsx.Text != "" && Tpsy.Text != "" && Tpex.Text != "" && Tpey.Text != "";
            if (notEmpty)
            {
                tpsx = int.Parse(Tpsx.Text);
                tpsy = int.Parse(Tpsy.Text);
                tpex = int.Parse(Tpex.Text);
                tpey = int.Parse(Tpey.Text);
                tpgx = tpex - tpsx;
                tpgy = tpey - tpsy;
                if (tpsy >= tpey || tpsx >= tpex) { disabled_tp = true; return; }
                else if (tpsx > TouchpadSizeX || tpex > TouchpadSizeX || tpsy > TouchpadSizeY || tpey > TouchpadSizeY) { disabled_tp = true; return; }
                else disabled_tp = false;
                TpMapareaRect.Margin = new Thickness((TpAreaRect.Width * (tpsx / (double)TouchpadSizeX)), (100d * (tpsy / (double)TouchpadSizeY)), 0, 0);
                TpMapareaRect.Width = Math.Floor(TpAreaRect.Width * (tpgx / (double)TouchpadSizeX));
                TpMapareaRect.Height = Math.Floor(100d * (tpgy / (double)TouchpadSizeY));
            }
        }

        private void OnScreenMapUpdate(object sender, RoutedEventArgs e)
        {
            StopButtonClick(sender, e);
            bool notEmpty = Scsx.Text != "" && Scsy.Text != "" && Scex.Text != "" && Scey.Text != "";
            if (notEmpty)
            {
                scsx = int.Parse(Scsx.Text);
                scsy = int.Parse(Scsy.Text);
                scex = int.Parse(Scex.Text);
                scey = int.Parse(Scey.Text);
                scgx = scex - scsx;
                scgy = scey - scsy;
                if (scsy >= scey || scsx >= scex) { disabled_sc = true; return; }
                else if (scsx > ScreenSizeX || scex > ScreenSizeX || scsy > ScreenSizeY || scey > ScreenSizeY) { disabled_sc = true; return; }
                else disabled_sc = false;
                ScMapareaRect.Margin = new Thickness((ScAreaRect.Width * (scsx / (double)ScreenSizeX)), (100d * (scsy / (double)ScreenSizeY)), 0, 0);
                ScMapareaRect.Width = Math.Floor(ScAreaRect.Width * (scgx / (double)ScreenSizeX));
                ScMapareaRect.Height = Math.Floor(100d * (scgy / (double)ScreenSizeY));
            }
        }

        /// <summary>
        /// This function calculates the length of the 2 rectangles respecting the whole screen and touchpad
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns>Calculated X value while keeping the scale and let Y be 100</returns>
        private int CalculateRectangle(int X, int Y)
        {
            return (int)Math.Ceiling(100.0 / Y * X);
        }

        private void InitConfig()
        {
            ConfigXmlSerializer = new XmlSerializer(typeof(Config));
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\cn.enita.MapTP\\config.xml";
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\cn.enita.MapTP\\");
                config = new Config();
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    ConfigXmlSerializer.Serialize(writer, config);
                }
                // load defaults into UI
                LoadSensitivityFromConfig();
                calibrated = false;
            }
            else
            {
                using (XmlReader reader = XmlReader.Create(filePath))
                {
                    try
                    {
                        config = (Config)ConfigXmlSerializer.Deserialize(reader);
                    }
                    catch
                    {
                        File.Delete(filePath);
                        InitConfig();
                        return;
                    }
                }
                if (config.TouchpadSizeX != 0 && config.TouchpadSizeY != 0) calibrated = true;
                else calibrated = false;
                Tpsx.Text = config.tpsx.ToString();
                Tpsy.Text = config.tpsy.ToString();
                Tpex.Text = config.tpex.ToString();
                Tpey.Text = config.tpey.ToString();
                Scsx.Text = config.scsx.ToString();
                Scsy.Text = config.scsy.ToString();
                Scex.Text = config.scex.ToString();
                Scey.Text = config.scey.ToString();
                TurtleCB.IsChecked = config.Turtle;
                turtle = config.Turtle;
                TrayCB.IsChecked = config.Tray;
                HideCB.IsChecked = config.HideToTray;
                ReceiveTouchpadSize(config.TouchpadSizeX, config.TouchpadSizeY);
                ConfigScreenMapUpdate(config);
                ConfigTouchpadMapUpdate(config);
                // Load sensitivity UI/values
                LoadSensitivityFromConfig();
            }
        }


        private void ConfigTouchpadMapUpdate(Config config)
        {
            tpsx = config.tpsx;
            tpsy = config.tpsy;
            tpex = config.tpex;
            tpey = config.tpey;
            tpgx = tpex - tpsx;
            tpgy = tpey - tpsy;
            if (tpsy >= tpey || tpsx >= tpex) { disabled_tp = true; return; }
            else if (tpsx > TouchpadSizeX || tpex > TouchpadSizeX || tpsy > TouchpadSizeY || tpey > TouchpadSizeY) { disabled_tp = true; return; }
            else disabled_tp = false;
            TpMapareaRect.Margin = new Thickness((TpAreaRect.Width * (tpsx / (double)TouchpadSizeX)), (100d * (tpsy / (double)TouchpadSizeY)), 0, 0);
            TpMapareaRect.Width = Math.Floor(TpAreaRect.Width * (tpgx / (double)TouchpadSizeX));
            TpMapareaRect.Height = Math.Floor(100d * (tpgy / (double)TouchpadSizeY));

        }

        private void ConfigScreenMapUpdate(Config config)
        {
            scsx = config.scsx;
            scsy = config.scsy;
            scex = config.scex;
            scey = config.scey;
            scgx = scex - scsx;
            scgy = scey - scsy;
            if (scsy >= scey || scsx >= scex) { disabled_sc = true; return; }
            else if (scsx > ScreenSizeX || scex > ScreenSizeX || scsy > ScreenSizeY || scey > ScreenSizeY) { disabled_sc = true; return; }
            else disabled_sc = false;
            ScMapareaRect.Margin = new Thickness((ScAreaRect.Width * (scsx / (double)ScreenSizeX)), (100d * (scsy / (double)ScreenSizeY)), 0, 0);
            ScMapareaRect.Width = Math.Floor(ScAreaRect.Width * (scgx / (double)ScreenSizeX));
            ScMapareaRect.Height = Math.Floor(100d * (scgy / (double)ScreenSizeY));
        }


        private void SaveConfig()
        {
#pragma warning disable CS0472
            // Explanation: If we try to store config when the program starts, these values
            // WILL be null since they are just initialized.
            config.tpsx = tpsx != null ? tpsx : 0;
            config.tpsy = tpsy != null ? tpsy : 0;
            config.tpex = tpex != null ? tpex : 0;
            config.tpey = tpey != null ? tpey : 0;
            config.scsx = scsx != null ? scsx : 0;
            config.scsy = scsy != null ? scsy : 0;
            config.scex = scex != null ? scex : 0;
            config.scey = scey != null ? scey : 0;
            config.TouchpadSizeX = TouchpadSizeX != null ? TouchpadSizeX : 0;
            config.TouchpadSizeY = TouchpadSizeY != null ? TouchpadSizeY : 0;
#pragma warning restore CS0472
            config.Turtle = TurtleCB.IsChecked.Value;
            config.Tray = TrayCB.IsChecked.Value;
            config.HideToTray = HideCB.IsChecked.Value;
            config.TapTimeMs = TapDurationThresholdMs;
            config.TapMoveXAbs = TapMovementThresholdXAbs;
            config.TapMoveYAbs = TapMovementThresholdYAbs;

            using (StreamWriter writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\cn.enita.MapTP\\config.xml"))
            {
                ConfigXmlSerializer.Serialize(writer, config);
            }
        }

        private void OnWindowCloses(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ptpExists) SaveConfig();
            e.Cancel = false;
            return;
        }

        // Tap/Drag detection state (improves separating discrete taps from drags)
        private bool lastTip;
        private DateTime fingerDownTime;
        private int fingerStartAbsX, fingerStartAbsY; // starting absolute (0..65535) mapped coordinates
        private bool mouseDownSent; // whether we already issued a real left down (drag scenario)
        private int TapDurationThresholdMs = 180; // max duration to still count as a tap before auto-convert to drag
        private int TapMovementThresholdXAbs = 600; // movement threshold in absolute coordinate units (~2% of65535 width)
        private int TapMovementThresholdYAbs = 600; // movement threshold in absolute coordinate units (~2% of65535 width)

        // Allow user to apply sensitivity changes from UI
        private void OnApplySensitivityClick(object sender, RoutedEventArgs e)
        {
            var tapTimeBox = this.FindName("TapTimeBox") as System.Windows.Controls.TextBox;
            var tapMoveXBox = this.FindName("TapMoveXBox") as System.Windows.Controls.TextBox;
            var tapMoveYBox = this.FindName("TapMoveYBox") as System.Windows.Controls.TextBox;

            if (tapTimeBox != null && int.TryParse(tapTimeBox.Text, out var t))
                TapDurationThresholdMs = Math.Max(50, Math.Min(800, t));
            if (tapMoveXBox != null && int.TryParse(tapMoveXBox.Text, out var mx))
                TapMovementThresholdXAbs = Math.Max(50, Math.Min(8000, mx));
            if (tapMoveYBox != null && int.TryParse(tapMoveYBox.Text, out var my))
                TapMovementThresholdYAbs = Math.Max(50, Math.Min(8000, my));

            if (config != null)
            {
                config.TapTimeMs = TapDurationThresholdMs;
                config.TapMoveXAbs = TapMovementThresholdXAbs;
                config.TapMoveYAbs = TapMovementThresholdYAbs;
                SaveConfig();
            }
        }

        // Load sensitivity values from config into fields and UI
        private void LoadSensitivityFromConfig()
        {
            if (config == null) return;
            TapDurationThresholdMs = config.TapTimeMs <= 0 ? 180 : config.TapTimeMs;
            TapMovementThresholdXAbs = config.TapMoveXAbs <= 0 ? 600 : config.TapMoveXAbs;
            TapMovementThresholdYAbs = config.TapMoveYAbs <= 0 ? 600 : config.TapMoveYAbs;

            var tapTimeBox = this.FindName("TapTimeBox") as System.Windows.Controls.TextBox;
            var tapMoveXBox = this.FindName("TapMoveXBox") as System.Windows.Controls.TextBox;
            var tapMoveYBox = this.FindName("TapMoveYBox") as System.Windows.Controls.TextBox;

            if (tapTimeBox != null) tapTimeBox.Text = TapDurationThresholdMs.ToString();
            if (tapMoveXBox != null) tapMoveXBox.Text = TapMovementThresholdXAbs.ToString();
            if (tapMoveYBox != null) tapMoveYBox.Text = TapMovementThresholdYAbs.ToString();
        }

        /// <summary>
        /// This method is for processing touchpad inputs
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_INPUT = 0x00FF;
            switch (msg)
            {
                case WM_INPUT:
                    var data = RawInputData.FromHandle(lParam);
                    if (data is RawInputDigitizerData digitizerData)
                    {
                        foreach (var x in digitizerData.Contacts)
                        {
                            if (x.Identifier == 0) // limiting ContactId(Identifier) to0 is to read the first finger
                            {
                                var curTip = x.IsButtonDown.Value; // finger touching surface
                                if (started)
                                {
                                    try
                                    {
                                        int X, Y;
                                        X = (tpsx <= x.X ?
                                            (tpex >= x.X ?
                                            (int)Math.Floor((((decimal)(x.X - tpsx) / tpgx * scgx) + scsx) / ScreenSizeX * 65535)
                                            : (int)Math.Floor((decimal)scex / ScreenSizeX * 65535))
                                            : (int)Math.Floor((decimal)scsx / ScreenSizeX * 65535));
                                        Y = (tpsy <= x.Y ?
                                            (tpey >= x.Y ?
                                            (int)Math.Floor((((decimal)(x.Y - tpsy) / tpgy * scgy) + scsy) / ScreenSizeY * 65535)
                                            : (int)Math.Floor((decimal)scey / ScreenSizeY * 65535))
                                            : (int)Math.Floor((decimal)scsy / ScreenSizeY * 65535));

                                        // Always move pointer (for visual feedback)
                                        mouseProcessor.MoveCursor(X, Y);

                                        // Enhanced tap vs drag logic only when turtle mode is enabled (click simulation enabled)
                                        if (turtle)
                                        {
                                            if (!lastTip && curTip)
                                            {
                                                // Finger just touched: start possible tap
                                                fingerDownTime = DateTime.UtcNow;
                                                fingerStartAbsX = X;
                                                fingerStartAbsY = Y;
                                                mouseDownSent = false; // not yet a drag
                                            }
                                            else if (lastTip && curTip)
                                            {
                                                // Finger is still down; decide if we should convert to drag
                                                if (!mouseDownSent)
                                                {
                                                    double elapsed = (DateTime.UtcNow - fingerDownTime).TotalMilliseconds;
                                                    int deltaAbsX = Math.Abs(X - fingerStartAbsX);
                                                    int deltaAbsY = Math.Abs(Y - fingerStartAbsY);
                                                    if (elapsed > TapDurationThresholdMs ||
                                                        deltaAbsX > TapMovementThresholdXAbs ||
                                                        deltaAbsY > TapMovementThresholdYAbs)
                                                    {
                                                        // Became a drag: send down now
                                                        mouseProcessor.MouseDown();
                                                        mouseDownSent = true;
                                                    }
                                                }
                                            }
                                            else if (lastTip && !curTip)
                                            {
                                                // Finger lifted
                                                if (!mouseDownSent)
                                                {
                                                    // Treat as discrete tap: emit down+up quickly
                                                    mouseProcessor.MouseDown();
                                                    mouseProcessor.MouseUp();
                                                }
                                                else
                                                {
                                                    // End drag
                                                    mouseProcessor.MouseUp();
                                                }
                                                mouseDownSent = false;
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        HandyControl.Controls.MessageBox.Show(e.ToString());
                                    }
                                }
                                lastTip = curTip; // update last state after processing
                            }
                        }
                    }

                    break;
            }
            return IntPtr.Zero;
        }
    }

    [XmlRoot("maptp-config")]
    public class Config
    {
        [XmlElement("scsx")]
        public int scsx;
        [XmlElement("tpsx")]
        public int tpsx;
        [XmlElement("scsy")]
        public int scsy;
        [XmlElement("tpsy")]
        public int tpsy;
        [XmlElement("scex")]
        public int scex;

        [XmlElement("tpex")]
        public int tpex;
        [XmlElement("tpey")]
        public int tpey;
        [XmlElement("scey")]
        public int scey;

        [XmlElement("touchpad-size-x")]
        public int TouchpadSizeX;
        [XmlElement("touchpad-size-y")]
        public int TouchpadSizeY;

        [XmlElement("tray")]
        public bool Tray;

        [XmlElement("hide-to-tray")]
        public bool HideToTray;

        [XmlElement("turtle")]
        public bool Turtle;

        // New: tap/drag sensitivity options
        [XmlElement("tap-time-ms")]
        public int TapTimeMs;
        [XmlElement("tap-move-x-abs")]
        public int TapMoveXAbs;
        [XmlElement("tap-move-y-abs")]
        public int TapMoveYAbs;

        public Config(int scsx, int tpsx, int scsy, int tpsy, int scex, int tpex, int tpey, int scey, int touchpadSizeX, int touchpadSizeY, bool tray, bool hideToTray, bool turtle,
 int tapTimeMs, int tapMoveXAbs, int tapMoveYAbs)
        {
            this.scsx = scsx;
            this.tpsx = tpsx;
            this.scsy = scsy;
            this.tpsy = tpsy;
            this.scex = scex;
            this.tpex = tpex;
            this.tpey = tpey;
            this.scey = scey;
            TouchpadSizeX = touchpadSizeX;
            TouchpadSizeY = touchpadSizeY;
            Tray = tray;
            HideToTray = hideToTray;
            Turtle = turtle;
            TapTimeMs = tapTimeMs;
            TapMoveXAbs = tapMoveXAbs;
            TapMoveYAbs = tapMoveYAbs;
        }
        public Config() : this(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, true, true, false, 180, 600, 600)
        { }
    }
}