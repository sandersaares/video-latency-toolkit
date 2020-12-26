using System;
using System.Collections.Concurrent;

namespace Vltk.Common
{
    /// <summary>
    /// Allows us to get some frames per second estimate of data processing rate.
    /// </summary>
    /// <remarks>
    /// Thread-safe.
    /// </remarks>
    public sealed class FpsCounter
    {
        /// <summary>
        /// Calculate the average processing rate over this many seconds.
        /// </summary>
        private static readonly TimeSpan CalculateOver = TimeSpan.FromSeconds(1);

        public double MovingAverageFps
        {
            get
            {
                Prune();

                return _entries.Count;
            }
        }

        public void AddEvent()
        {
            _entries.Enqueue(DateTimeOffset.UtcNow);
        }

        private readonly ConcurrentQueue<DateTimeOffset> _entries = new();

        private void Prune()
        {
            while (_entries.TryPeek(out var oldest) && oldest + CalculateOver < DateTimeOffset.UtcNow)
                _entries.TryDequeue(out _);
        }
    }
}
