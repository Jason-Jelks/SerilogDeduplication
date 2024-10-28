using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace Serilog.Deduplication
{
    public class DeduplicationFilter : ILogEventFilter
    {
        private readonly ConcurrentDictionary<string, DateTime> _logCache = new ConcurrentDictionary<string, DateTime>();
        private readonly int _deduplicationWindowMs;

        public DeduplicationFilter(int deduplicationWindowMs)
        {
            _deduplicationWindowMs = deduplicationWindowMs;
        }

        public bool IsEnabled(LogEvent logEvent)
        {
            // Define the deduplication key (e.g., using "Code", "Message", etc.)
            var logKey = $"{logEvent.Properties["Code"]}-{logEvent.MessageTemplate.Text}";

            if (_logCache.TryGetValue(logKey, out var lastLoggedTime))
            {
                var timeSinceLastLog = DateTime.UtcNow - lastLoggedTime;
                if (timeSinceLastLog.TotalMilliseconds < _deduplicationWindowMs)
                {
                    return false;  // Skip this log entry as it's a duplicate
                }
            }

            _logCache[logKey] = DateTime.UtcNow;  // Update cache with the new log
            return true;  // Allow the log entry
        }
    }
}
