using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Vltk.Common;
using ZXing;
using Point = System.Drawing.Point;

namespace Vltk.Interpreter.Gui
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

            Reset();
        }

        private IntPtr _targetWindow;

        private const int MovingAverageOverCount = 30;
        private readonly Queue<double> _movingAverageItems = new Queue<double>();

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var rect = WindowManagement.GetWindowRect(_targetWindow);

                if (rect == null)
                {
                    MessageArea.Text = "Target window not found.";
                    LatencyLabel.Text = "";
                    return;
                }

                using var bitmap = new Bitmap(rect.Value.Width, rect.Value.Height);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(rect.Value.TopLeft, Point.Empty, rect.Value.Size);

                var reader = new BarcodeReader
                {
                    AutoRotate = false,
                    TryInverted = false,
                    Options = new ZXing.Common.DecodingOptions
                    {
                        PossibleFormats = new[]
                        {
                            BarcodeFormat.QR_CODE
                        }
                    }
                };

                var result = reader.Decode(bitmap);

                Fps.AddEvent();

                if (result == null)
                {
                    // The detection is not perfect - sometimes it just fails to read the image. That's fine - just skip such iteration.
                }
                else
                {
                    var payload = SignalPayload.Deserialize(result.Text);

                    if (payload.TimeserverUrl != null)
                    {
                        if (Uri.TryCreate(payload.TimeserverUrl, UriKind.Absolute, out var url))
                        {
                            TrueTime.TimeserverUrl = url;
                        }
                        else
                        {
                            // It might really be invalid or the parse might have failed (it is not perfect).
                            // Ignore such a case - if it is a broken parse, we'll fix it next parse. Otherwise, user should notice lack of time sync.
                        }
                    }
                    else
                    {
                        TrueTime.TimeserverUrl = null;
                    }

                    DateTimeOffset? referenceTime = DateTimeOffset.UtcNow;

                    if (TrueTime.TimeserverUrl != null)
                        referenceTime = TrueTime.TrueTime; // Might be null (if still syncing).

                    if (referenceTime != null)
                    {
                        var originalTimestamp = new DateTimeOffset(payload.TicksUtc, TimeSpan.Zero);
                        var diff = referenceTime.Value - originalTimestamp;

                        var value = diff.TotalMilliseconds;
                        _movingAverageItems.Enqueue(value);
                    }
                }

                while (_movingAverageItems.Count > MovingAverageOverCount)
                    _movingAverageItems.Dequeue();

                if (_movingAverageItems.Any())
                {
                    var avg = _movingAverageItems.Average();
                    MessageArea.Text = "";
                    LatencyLabel.Text = $"{avg:N0} ms";
                }
                else
                {
                    MessageArea.Text = "No timing signal detected";
                    LatencyLabel.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageArea.Text = ex.Message;
            }
        }

        private readonly DispatcherTimer _timer = new DispatcherTimer();

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // We only start the timer when something is selected from among the window candidates.
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }

        private void Reset()
        {
            _timer.Stop();
            MessageArea.Text = "";
            LatencyLabel.Text = "";
            _movingAverageItems.Clear();

            TrueTime.TimeserverUrl = null;

            WindowCandidates.SelectedItem = null;
            WindowCandidates.ItemsSource = WindowManagement.GetOpenWindows();
        }

        private void WindowCandidates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var window = (WindowManagement.NativeWindow)WindowCandidates.SelectedItem;

            if (window == null)
                return;

            _targetWindow = window.Handle;
            _timer.Start();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }
    }
}
