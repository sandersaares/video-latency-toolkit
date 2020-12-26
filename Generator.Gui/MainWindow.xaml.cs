using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Vltk.Common;
using ZXing;

namespace Vltk.Generator.Gui
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromMilliseconds(1);

            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;

            TimeserverUrl.Text = GeneratorSettings.Current.TimeserverUrl?.ToString();

            if (!string.IsNullOrWhiteSpace(TimeserverUrl.Text))
            {
                // Only wire up the URL if the URL is valid.
                if (Uri.TryCreate(TimeserverUrl.Text, UriKind.Absolute, out var url))
                    TrueTime.TimeserverUrl = url;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var writer = new ZXing.Presentation.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = (int)((FrameworkElement)ImagePresenter.Parent).ActualHeight,
                    Width = (int)((FrameworkElement)ImagePresenter.Parent).ActualWidth,
                    Margin = 0
                }
            };

            WriteableBitmap? image = null;

            if (TrueTime.TimeserverUrl == null)
            {
                image = writer.Write(new SignalPayload
                {
                    TicksUtc = DateTimeOffset.UtcNow.Ticks,
                }.Serialize());
            }
            else
            {
                var time = TrueTime.TrueTime;

                // If a timeserver is set, we only present an image when we have true time.
                if (time != null)
                {
                    image = writer.Write(new SignalPayload
                    {
                        TicksUtc = time.Value.Ticks,
                        TimeserverUrl = TrueTime.TimeserverUrl.ToString()
                    }.Serialize());
                }
            }

            ImagePresenter.Source = image;

            Fps.AddEvent();
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

        private void ApplyTimeserverButton_Click(object sender, RoutedEventArgs e)
        {
            GeneratorSettings.Current.TimeserverUrl = TimeserverUrl.Text;
            GeneratorSettings.Current.Save();

            if (string.IsNullOrWhiteSpace(TimeserverUrl.Text))
            {
                TrueTime.TimeserverUrl = null;
                return;
            }

            if (!Uri.TryCreate(TimeserverUrl.Text, UriKind.Absolute, out var url))
            {
                MessageBox.Show("Not a valid URL.", "Timeserver URL", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TrueTime.TimeserverUrl = url;
        }
    }
}
