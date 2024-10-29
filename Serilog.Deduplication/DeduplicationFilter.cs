using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Serilog.Deduplication
{
    public class DeduplicationFilter : ILogEventFilter, IDisposable
    {
        private readonly DeduplicationSettings _settings;
        private readonly ConcurrentDictionary<string, DateTime> _logCache = new ConcurrentDictionary<string, DateTime>();
        private Timer _pruneTimer;

        public DeduplicationFilter(DeduplicationSettings settings)
        {
            _settings = settings;
            // Start the pruning timer
            _pruneTimer = new Timer(PruneCache, null, _settings.PruneIntervalMilliseconds, _settings.PruneIntervalMilliseconds);
        }

        public bool IsEnabled(LogEvent logEvent)
        {
            // Get the deduplication settings for the current log level
            var deduplicationLevel = GetDeduplicationLevelForLogLevel(logEvent.Level);

            // If deduplication is disabled for this level, always allow the log entry
            if (!deduplicationLevel.DeduplicationEnabled)
            {
                return true;
            }

            // Define deduplication key (based on log message and other properties)
            var logKey = $"{logEvent.Properties["Code"]}-{logEvent.MessageTemplate.Text}";

            // Check if the log entry should be deduplicated
            if (_logCache.TryGetValue(logKey, out var lastLoggedTime))
            {
                var timeSinceLastLog = DateTime.UtcNow - lastLoggedTime;
                if (timeSinceLastLog.TotalMilliseconds < deduplicationLevel.DeduplicationWindowMilliseconds)
                {
                    return false;  // Skip this log as it is a duplicate within the deduplication window
                }
            }

            // Cache the current log entry
            _logCache[logKey] = DateTime.UtcNow;
            return true;
        }

        // Pruning logic: removes entries that have been in the cache longer than the configured expiration time
        private void PruneCache(object state)
        {
            var expirationTime = DateTime.UtcNow.AddMilliseconds(-_settings.CacheExpirationMilliseconds);
            var keysToRemove = _logCache
                .Where(kvp => kvp.Value < expirationTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _logCache.TryRemove(key, out _);
            }
        }

        private DeduplicationLevel GetDeduplicationLevelForLogLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Error => _settings.Error,
                LogEventLevel.Warning => _settings.Warning,
                LogEventLevel.Debug => _settings.Debug,
                _ => _settings.Information,  // Default to Information level
            };
        }

        public void Dispose()
        {
            _pruneTimer?.Dispose();
        }
    }
}
