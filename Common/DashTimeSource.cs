using DashTimeserver.Client;
using Koek;
using System;

namespace Vltk.Common
{
    public sealed class DashTimeSource : ITimeSource
    {
        public DashTimeSource(SynchronizedTimeSource source)
        {
            Source = source;
        }

        public SynchronizedTimeSource Source { get; }

        public DateTimeOffset GetCurrentTime() => Source.GetCurrentTime();
    }
}
