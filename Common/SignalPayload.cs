using System;
using System.Text.Json;

namespace Vltk.Common
{
    /// <summary>
    /// This is the payload we embed into the visual signal.
    /// </summary>
    public sealed class SignalPayload
    {
        /// <summary>
        /// The current timestamp in 100ns ticks, UTC timezone.
        /// </summary>
        /// <remarks>
        /// Assumes clock synchronization via unspecified mechanism between generator and interpreter (e.g. by virtue of running both on the same PC).
        /// </remarks>
        public long TicksUtc { get; set; }

        /// <summary>
        /// Deserializes from a string that has been retrieved from visual channel.
        /// </summary>
        public static SignalPayload Deserialize(string s) => JsonSerializer.Deserialize<SignalPayload>(s) ?? throw new NotSupportedException("Payload deserialized to null.");

        /// <summary>
        /// Serializes to a string, suitable for encoding into visual channel.
        /// </summary>
        /// <returns></returns>
        public string Serialize() => JsonSerializer.Serialize(this);
    }
}
