using System;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Vltk.Common;
using ZXing;
using ZXing.Presentation;

namespace Vltk.Generator.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromMilliseconds(1);

            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
        }

        private readonly FpsCounter _fps = new FpsCounter();

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var writer = new BarcodeWriterGeometry
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = (int)((FrameworkElement)SignalPresenter.Parent).ActualHeight,
                    Width = (int)((FrameworkElement)SignalPresenter.Parent).ActualWidth,
                    Margin = 0
                }
            };

            var image = writer.Write(new SignalPayload
            {
                TicksUtc = DateTimeOffset.UtcNow.Ticks
            }.Serialize());

            SignalPresenter.Data = image;

            _fps.AddEvent();
            FpsLabel.Text = ((int)_fps.MovingAverageFps).ToString(CultureInfo.InvariantCulture);
            LastUpdateLabel.Text = DateTimeOffset.UtcNow.ToString(VltkConstants.TimestampFormat);
        }

        private readonly DispatcherTimer _timer = new DispatcherTimer();

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _timer.Start();
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }
    }
}
