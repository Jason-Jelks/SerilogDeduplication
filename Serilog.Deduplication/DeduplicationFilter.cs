using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;

namespace Serilog.Deduplication
{
    public class DeduplicationFilter : ILogEventFilter
    {
        private readonly DeduplicationSettings _settings;
        private readonly ConcurrentDictionary<string, DateTime> _logCache = new ConcurrentDictionary<string, DateTime>();

        public DeduplicationFilter(DeduplicationSettings settings)
        {
            _settings = settings;
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
    }
}
