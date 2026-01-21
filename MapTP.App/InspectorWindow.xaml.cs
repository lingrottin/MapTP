using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Graphics.Printing3D;

namespace MapTP.App
{
    /// <summary>
    /// InspectorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InspectorWindow : Window
    {
        private int width;
        private int height;
        private double DpiRatio;
        public delegate void _SendScreenArea(int X, int Y, int eX, int eY);
        public _SendScreenArea SendScreenArea;

        public delegate void _TriggerEvent(object sender, RoutedEventArgs e);
        public _TriggerEvent MainWindow_Stop;
        public _TriggerEvent MainWindow_Start;
        public _TriggerEvent MainWindow_Turtle;

        private readonly int X, Y, eX, eY;

        public InspectorWindow(int X, int Y, int eX, int eY)
        {
            InitializeComponent();
            this.X = X;
            this.Y = Y;
            this.eX = eX;
            this.eY = eY;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.LocationChanged += OnTransformChanged;
            this.SizeChanged += OnTransformChanged;
            this.DpiRatio = ScreenManager.GetDpiRatio(this);
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            this.Top = X / DpiRatio;
            this.Left = Y / DpiRatio;
            this.Width = eX / DpiRatio - X / DpiRatio;
            this.Height = eY / DpiRatio - Y / DpiRatio;
        }

        private void OnTransformChanged(object sender, EventArgs e)
        {
            this.width = (int)this.ActualWidth;
            this.height = (int)this.ActualHeight;
            var x = (int)(this.Left * DpiRatio);
            var y = (int)(this.Top * DpiRatio);
            var ex = (int)((this.Left + this.width) * DpiRatio);
            var ey = (int)((this.Top + this.height) * DpiRatio);
            if (x >= 0 && y >= 0 && ex <= ScreenManager.GetScreenWidth() && ey <= ScreenManager.GetScreenHeight())
            {
                TitleBar.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xff, 0xff, 0xff));
                SendScreenArea(x, y, ex, ey);
            }
            else
            {
                TitleBar.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xff, 0x00, 0x00));
            }
        }

        private void OnTitleBarMouseDown(object sender, RoutedEventArgs e)
        {
            this.DragMove();
        }

        private void CloseGrid_MouseEnter(object sender, RoutedEventArgs e)
        {
            CloseGrid.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        }

        private void CloseGrid_MouseLeave(object sender, RoutedEventArgs e)
        {
            CloseGrid.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x11, 0x22, 0x55));
        }

        private void CloseGrid_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            if (MainWindow_Start != null)
            {
                MainWindow_Start(sender, e);
            }
            this.StopButton.Visibility = Visibility.Visible;
            this.StartButton.Visibility = Visibility.Collapsed;
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            if (MainWindow_Stop != null)
            {
                MainWindow_Stop(sender, e);
            }
            this.StopButton.Visibility = Visibility.Collapsed;
            this.StartButton.Visibility = Visibility.Visible;
        }

    }
}
