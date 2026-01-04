using System;
using System.Linq;
using System.Threading;

namespace Cascade.Core.Models
{
    /// <summary>
    /// A message type observed in the system.
    /// </summary>
    public class TopologyMessageType
    {
        private long _timesObserved;

        /// <summary>Full assembly-qualified type name.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Short type name (class name only).</summary>
        public string ShortName => FullName.Split(',')[0].Split('.').LastOrDefault() ?? FullName;

        /// <summary>Number of times this message type has been observed.</summary>
        public long TimesObserved
        {
            get => Interlocked.Read(ref _timesObserved);
            set => Interlocked.Exchange(ref _timesObserved, value);
        }

        /// <summary>When this message type was first observed.</summary>
        public DateTimeOffset FirstSeen { get; set; }

        /// <summary>When this message type was last observed.</summary>
        public DateTimeOffset LastSeen { get; set; }

        /// <summary>
        /// Atomically increments the times observed counter.
        /// </summary>
        /// <returns>The incremented value.</returns>
        public long IncrementTimesObserved() => Interlocked.Increment(ref _timesObserved);
    }
}
