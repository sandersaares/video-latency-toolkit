using DashTimeserver.Client;
using Koek;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vltk.Common;

namespace Vltk.Interpreter.Pipe
{
    /// <summary>
    /// Processes signals extracted from a video stream and interprets them to achieve an understanding of video latency.
    /// </summary>
    /// <remarks>
    /// Thread-safe.
    /// 
    /// On error, an event is raised for tracing and the interpreter keeps on trying.
    /// </remarks>
    public sealed class SignalInterpreter : IAsyncDisposable
    {
        /// <summary>
        /// We expect time sync to be fast and will ignore (and retry) if it takes longer than this.
        /// </summary>
        private static readonly TimeSpan SyncTimeout = TimeSpan.FromSeconds(10);

        public SignalInterpreter(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async ValueTask DisposeAsync()
        {
            if (_dashTimeSource != null)
                await _dashTimeSource.DisposeAsync();

            if (_ntpTimeSource != null)
                await _ntpTimeSource.DisposeAsync();
        }

        public sealed class ResultEventArgs : EventArgs
        {
            public TimeSpan Latency { get; }

            public ResultEventArgs(TimeSpan latency) => Latency = latency;
        }

        /// <summary>
        /// Raised when any error occurs during signal processing.
        /// Errors are ignored and retries performed whenever possible.
        /// </summary>
        public event ErrorEventHandler Error;

        /// <summary>
        /// Raised when the video stream latency has been determined.
        /// </summary>
        public event EventHandler<ResultEventArgs> LatencyUpdated;

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly object _lock = new();

        // If not null, we are using time synchronization via an external timeserver (either DASH or NTP).
        // If null, we assume external synchronization.
        private Uri _timeserverUrl;

        // Used to differentiate synchronization attempts with different URLs.
        // Results arriving with a different cookie are out of date and discarded.
        private object _timeserverCookie = new();

        // Set once synchronization completes.
        // Remains set until timeserver URL is changed.
        private SynchronizedTimeSource _dashTimeSource;

        // Set once synchronization completes.
        // Remains set until timeserver URL is changed.
        private NtpTimeSource _ntpTimeSource;

        // Whichever one of the above time sources is set, for ease of use.
        private ITimeSource _timeSource;

        // If we have a timeserver URL, we can wait on this to ensure that it is available.
        private Task<bool> _synchronizeTask;

        /// <summary>
        /// Adds a payload to be analyzed. Events are asynchronously raised if and when the analysis completes.
        /// </summary>
        public void Add(SignalPayload payload)
        {
            Task.Run(async delegate
            {
                Uri url = null;

                if (!string.IsNullOrEmpty(payload.TimeserverUrl) && !Uri.TryCreate(payload.TimeserverUrl, UriKind.Absolute, out url))
                {
                    var e = Error;
                    e?.Invoke(this, new ErrorEventArgs(new NotSupportedException($"Invalid timeserver URL in signal: " + payload.TimeserverUrl)));
                    return;
                }

                bool shouldProcess = await UpdateSynchronizedTimeAsync(url, out var cookie);

                if (!shouldProcess)
                    return;

                lock (_lock)
                {
                    if (_timeserverCookie != cookie)
                        return; // This signal has been superseded already because noather signal changed our timeserver.

                    var referenceTime = DateTimeOffset.UtcNow;

                    if (_timeSource != null)
                        referenceTime = _timeSource.GetCurrentTime();

                    var originalTimestamp = new DateTimeOffset(payload.TicksUtc, TimeSpan.Zero);
                    var latency = referenceTime - originalTimestamp;

                    var e = LatencyUpdated;
                    e?.Invoke(this, new ResultEventArgs(latency));
                }
            });
        }

        /// <returns>True to process the signal that triggered synchronization. False to ignore the signal.</returns>
        private Task<bool> UpdateSynchronizedTimeAsync(Uri url, out object cookie)
        {
            lock (_lock)
            {
                if (_timeserverUrl == url)
                {
                    cookie = _timeserverCookie;
                    return _synchronizeTask!;
                }

                _timeserverCookie = new();

                // If we had existing time source, dispose in the background.
                if (_dashTimeSource != null)
                    Task.Run(_dashTimeSource.DisposeAsync);

                if (_ntpTimeSource != null)
                    Task.Run(_ntpTimeSource.DisposeAsync);

                _timeserverUrl = url;
                _dashTimeSource = null;
                _ntpTimeSource = null;
                _timeSource = null;
                cookie = _timeserverCookie;

                _synchronizeTask = SynchronizeAsync(_timeserverCookie);

                if (_timeserverUrl != null)
                    return _synchronizeTask;
            }

            return Task.FromResult(true);
        }

        /// <returns>True to process the signal that triggered synchronization. False to ignore the signal.</returns>
        private async Task<bool> SynchronizeAsync(object cookie)
        {
            using var timeout = new CancellationTokenSource(SyncTimeout);

            try
            {
                if (_timeserverUrl.Scheme == "ntp")
                {
                    var source = new NtpTimeSource(_timeserverUrl, NullLoggerFactory.Instance.CreateLogger<NtpTimeSource>());

                    await source.FirstSyncPerformed.WaitAsync(timeout.Token);

                    lock (_lock)
                    {
                        if (_timeserverCookie != cookie)
                            return false; // What we did does not matter anymore.

                        _ntpTimeSource = source;
                        _timeSource = source;
                        return true;
                    }
                }
                else
                {
                    var sts = await SynchronizedTimeSource.CreateAsync(_timeserverUrl, _httpClientFactory, timeout.Token);

                    lock (_lock)
                    {
                        if (_timeserverCookie != cookie)
                            return false; // What we did does not matter anymore.

                        _dashTimeSource = sts;
                        _timeSource = new DashTimeSource(sts);
                        return true;
                    }
                }
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                lock (_lock)
                {
                    if (_timeserverCookie != cookie)
                        return false; // What we did does not matter anymore.

                    var e = Error;
                    e?.Invoke(this, new ErrorEventArgs(new TimeoutException("Clock synchronization timed out.")));
                    return false;
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    if (_timeserverCookie != cookie)
                        return false; // What we did does not matter anymore.

                    var e = Error;
                    e?.Invoke(this, new ErrorEventArgs(ex));
                    return false;
                }
            }
        }
    }
}
