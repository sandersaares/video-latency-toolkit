using DashTimeserver.Client;
using Koek;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
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
        /// The URL of the timeserver to use for establishing clock sync.
        /// Can point to either:
        /// 1) An MPEG-DASH compatible timeserver (xs:datetime format via HTTP URL).
        /// 2) An NTP timeserver (ntp://ntp.example.com URL).
        /// 
        /// If null, time sync is disabled.
        /// </summary>
        public Uri TimeserverUrl
        {
            get => _timeserverUrl;
            set
            {
                if (_timeserverUrl == value)
                    return;

                _cookie = new();

                if (_dashTimeSource != null)
                    Task.Run(_dashTimeSource.DisposeAsync);

                if (_ntpTimeSource != null)
                    Task.Run(_ntpTimeSource.DisposeAsync);

                _timeserverUrl = value;
                _syncError = null;
                _dashTimeSource = null;
                _ntpTimeSource = null;
                _timeSource = null;

                if (_timeserverUrl != null)
                    Task.Run(() => SynchronizeAsync(_cookie));
            }
        }

        /// <summary>
        /// Returns the current true time based on the synchronization result (or null if unknown).
        /// </summary>
        public DateTimeOffset? TrueTime => _timeSource?.GetCurrentTime();

        private Uri _timeserverUrl;

        // Used to differentiate synchronization attempts with different URLs.
        private object _cookie;

        // Set once sync is established. Null while syncing or if not using DASH.
        private SynchronizedTimeSource _dashTimeSource;

        // Null if not using NTP.
        private NtpTimeSource _ntpTimeSource;

        // Whichever one of the above is to be used. Null if still syncing.
        private ITimeSource _timeSource;

        // Set if sync fails to be established.
        private string _syncError;

        private async Task SynchronizeAsync(object cookie)
        {
            using var timeout = new CancellationTokenSource(SyncTimeout);

            try
            {
                if (_timeserverUrl.Scheme == "ntp")
                {
                    var source = new NtpTimeSource(_timeserverUrl, NullLoggerFactory.Instance.CreateLogger<NtpTimeSource>());

                    await source.FirstSyncPerformed.WaitAsync(timeout.Token);

                    if (_cookie != cookie)
                        return; // What we did does not matter anymore.

                    _ntpTimeSource = source;
                    _timeSource = source;
                }
                else
                {
                    var sts = await SynchronizedTimeSource.CreateAsync(_timeserverUrl, new DummyHttpClientFactory(), timeout.Token);

                    if (_cookie != cookie)
                        return; // What we did does not matter anymore.

                    _dashTimeSource = sts;
                    _timeSource = new DashTimeSource(sts);
                }
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

        private void OnTimerTick(object sender, EventArgs e)
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
