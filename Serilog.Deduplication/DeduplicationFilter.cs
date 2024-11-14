using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace Serilog.Deduplication
{
    public class DeduplicationFilter : ILogEventFilter, IDisposable
    {
        private readonly DeduplicationSettings _settings;
        private readonly ConcurrentDictionary<string, DateTime> _logCache = new();
        private Timer _pruneTimer;

        public DeduplicationFilter(DeduplicationSettings settings)
        {
            _settings = settings;
            _pruneTimer = new Timer(PruneCache, null, _settings.PruneIntervalMilliseconds, _settings.PruneIntervalMilliseconds);
        }

        public bool IsEnabled(LogEvent logEvent)
        {
            // Get deduplication settings for the current log level
            var deduplicationLevel = GetDeduplicationLevelForLogLevel(logEvent.Level);

            // If deduplication is disabled for this level, always allow the log entry
            if (!deduplicationLevel.DeduplicationEnabled)
            {
                return true;
            }

            // Generate the deduplication key
            var logKey = GetKey(logEvent);

            // If the deduplication key is empty or null, allow the log entry
            if (string.IsNullOrEmpty(logKey))
            {
                return true;  // No deduplication applied, so allow the log to proceed
            }

            // Check if the log entry should be deduplicated
            if (_logCache.TryGetValue(logKey, out var lastLoggedTime))
            {
                var timeSinceLastLog = DateTime.UtcNow - lastLoggedTime;
                if (timeSinceLastLog.TotalMilliseconds < deduplicationLevel.DeduplicationWindowMilliseconds)
                {
                    return false;  // Skip this log as a duplicate within the deduplication window
                }
            }

            // Cache the current log entry
            _logCache[logKey] = DateTime.UtcNow;
            return true;
        }

        public string GetKey(LogEvent logEvent)
        {
            // Collect parts of the key based on KeyProperties and MessageTemplate setting
            var keyParts = new List<string>();

            // Add each configured property to the key if it exists in the log event
            if (_settings.KeyProperties.Any())
            {
                foreach (var prop in _settings.KeyProperties)
                {
                    if (logEvent.Properties.TryGetValue(prop, out var value))
                    {
                        // Extract raw string from ScalarValue if applicable
                        if (value is ScalarValue scalarValue && scalarValue.Value != null)
                        {
                            keyParts.Add(scalarValue.Value.ToString());
                        }
                        else
                        {
                            keyParts.Add(value.ToString());
                        }
                    }
                }
            }

            // Add MessageTemplate to key if configured to do so
            if (_settings.IncludeMessageTemplate)
            {
                keyParts.Add(logEvent.MessageTemplate.Text);
            }

            // Join key parts with a separator to form the deduplication key
            return string.Join("-", keyParts);
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
                LogEventLevel.Verbose => _settings.Verbose,
                _ => _settings.Information,  // Default to Information level
            };
        }

        public void Dispose()
        {
            _pruneTimer?.Dispose();
        }
    }
}
