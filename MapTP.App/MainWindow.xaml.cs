using MapTP.App.Touchpad;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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

        public void ReceiveScreenMapArea(int scsx, int scsy, int scex, int scey)
        {
            Scsx.Text = scsx.ToString();
            Scex.Text = scex.ToString();
            Scsy.Text = scsy.ToString();
            Scey.Text = scey.ToString();
            this.StartButtonClick(new object(), new RoutedEventArgs());
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void About_Click(object sender, RoutedEventArgs e)
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
            WindowState = WindowState.Minimized;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
            System.Windows.Application.Current.Shutdown();
        }

        private void OnLaunchInspectorClick(object sender, RoutedEventArgs e)
        {
            MapAreaWindow w = new MapAreaWindow()
            {
                sendArea = ReceiveScreenMapArea
            };
            w.Show();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Closing += OnWindowCloses;
            ptpExists = Touchpad.Handler.Exists();

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
                bool success;
                if (_targetSource != null)
                    success = Touchpad.Handler.RegisterInput(_targetSource.Handle);
                else success = false;
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
            
            
            if (!(Environment.OSVersion.Version > new Version(10, 0, 17763)))
            {
                this.Background = Brushes.White;
            }
            else if (Environment.OSVersion.Version.Build >= 22000) // Mica
            {

                // Get PresentationSource
                PresentationSource presentationSource = PresentationSource.FromVisual((Visual)sender);

                // Subscribe to PresentationSource's ContentRendered event
                presentationSource.ContentRendered += (s,ev)=>OnRendered(PresentationSource.FromVisual((Visual)sender) as HwndSource);
                

            }
            else
            {
                var WalterlvCompositor = new WalterlvBlurManager(this)
                {
                    Color = Color.FromArgb(0x1f, 0x87, 0xce, 0xfa),
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
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            started = false;
            StopButton.Visibility = Visibility.Collapsed;
            StartButton.Visibility = Visibility.Visible;
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
                calibrated = false;
            }
            else
            {
                using (XmlReader reader = XmlReader.Create(filePath))
                {
                    config = (Config)ConfigXmlSerializer.Deserialize(reader);
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
                ReceiveTouchpadSize(config.TouchpadSizeX, config.TouchpadSizeY);
                ConfigScreenMapUpdate(config);
                ConfigTouchpadMapUpdate(config);
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
            config.tpsx = tpsx != null ? tpsx:0 ;
            config.tpsy = tpsy != null ? tpsy:0;
            config.tpex = tpex != null ? tpex:0;
            config.tpey = tpey != null ? tpey : 0;
            config.scsx = scsx != null ? scsx:0;
            config.scsy = scsy != null ? scsy : 0;
            config.scex = scex != null ? scex : 0;
            config.scey = scey != null ? scey : 0;
            config.TouchpadSizeX = TouchpadSizeX != null ? TouchpadSizeX:0;
            config.TouchpadSizeY = TouchpadSizeY != null ? TouchpadSizeY:0;

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
            switch (msg)
            {
                case Touchpad.Handler.WM_INPUT:
                    foreach (TouchpadContact x in Touchpad.Handler.ParseInput(lParam))
                    {
                        if (x.ContactId == 0) // limiting ContactId to 0 is to read the first finger
                        {
                            InputX = x.X;
                            InputY = x.Y;

                            if (started)
                            {
                                try
                                {
                                    int X, Y;
                                    X = (tpsx <= x.X ? 
                                            (tpex >= x.X ? 
                                                (int)Math.Floor((((decimal)(x.X - tpsx) / tpgx * scgx) + scsx) / ScreenSizeX * 65535)
                                            : (int)Math.Floor((decimal)scex/ScreenSizeX*65535) )
                                        : (int)Math.Floor((decimal)scsx / ScreenSizeX * 65535) );
                                    Y = (tpsy <= x.Y ?
                                            (tpey >= x.Y ? 
                                                (int)Math.Floor((((decimal)(x.Y - tpsy) / tpgy * scgy) + scsy) / ScreenSizeY * 65535)
                                            : (int)Math.Floor((decimal)scey / ScreenSizeY * 65535) )
                                        : (int)Math.Floor((decimal)scsy / ScreenSizeY * 65535) );
                                    mouseProcessor.MoveCursor(X, Y);

                                }
                                catch (Exception e)
                                {
                                    HandyControl.Controls.MessageBox.Show(e.ToString());
                                }
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

        public Config(int scsx, int tpsx, int scsy, int tpsy, int scex, int tpex, int tpey, int scey, int touchpadSizeX, int touchpadSizeY)
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
        }
        public Config() : this(0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
        { }
    }
}