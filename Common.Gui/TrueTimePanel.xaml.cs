using DashTimeserver.Client;
using Prometheus;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Vltk.Common.Gui
{
    public partial class TrueTimePanel : UserControl
    {
        private static readonly TimeSpan SyncTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The URL of the dash-timeserver to use for establishing clock sync.
        /// 
        /// If null, time sync is disabled.
        /// </summary>
        public Uri? TimeserverUrl
        {
            get => _timeserverUrl;
            set
            {
                if (_timeserverUrl == value)
                    return;

                if (_timeSource != null)
                    Task.Run(_timeSource.DisposeAsync);

                _timeserverUrl = value;
                _syncError = null;
                _timeSource = null;
                _cookie = new();

                if (_timeserverUrl != null)
                    Task.Run(() => SynchronizeAsync(_cookie));
            }
        }

        /// <summary>
        /// Returns the current true time based on the synchronization result (or null if unknown).
        /// </summary>
        public DateTimeOffset? TrueTime => _timeSource?.GetCurrentTime();

        private Uri? _timeserverUrl;

        // Used to differentiate synchronization attempts with different URLs.
        private object? _cookie;

        // Set once sync is established. Null while syncing.
        private SynchronizedTimeSource? _timeSource;

        // Set if sync fails to be established.
        private string? _syncError;

        private async Task SynchronizeAsync(object cookie)
        {
            using var timeout = new CancellationTokenSource(SyncTimeout);

            try
            {
                var sts = await SynchronizedTimeSource.CreateAsync(_timeserverUrl, new DummyHttpClientFactory(), timeout.Token);

                if (_cookie != cookie)
                    return; // What we did does not matter anymore.

                _timeSource = sts;
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                if (_cookie != cookie)
                    return; // What we did does not matter anymore.

                _syncError = "Clock sync timeout";
            }
            catch (Exception ex)
            {
                if (_cookie != cookie)
                    return; // What we did does not matter anymore.

                _syncError = ex.Message;
            }
        }

        public TrueTimePanel()
        {
            InitializeComponent();

            _refreshTimer.Tick += OnTimerTick;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                // This is a global registration, so we avoid making it in the designer.
                // Even at runtime it is a bit dirty.
                Metrics.DefaultRegistry.AddBeforeCollectCallback(delegate
                {
                    var trueTime = TrueTime;

                    if (trueTime == null)
                        _trueTimeGauge.Unpublish();
                    else
                        _trueTimeGauge.SetToTimeUtc(trueTime.Value);
                });
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_timeserverUrl == null)
            {
                OutputLabel.Text = "No timeserver set";
            }
            else if (_syncError != null)
            {
                OutputLabel.Text = _syncError;
            }
            else if (_timeSource == null)
            {
                OutputLabel.Text = "Synchronizing clocks...";
            }
            else
            {
                OutputLabel.Text = _timeSource.GetCurrentTime().ToString(VltkConstants.TimestampFormat, CultureInfo.InvariantCulture);
            }
        }

        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.1)
        };

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
        }

        private static readonly Gauge _trueTimeGauge = Metrics.CreateGauge("synchronized_unixtime_seconds", "The true time obtained from the timeserver.");
    }
}
