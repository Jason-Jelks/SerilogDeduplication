using Microsoft.Extensions.Configuration;

namespace Serilog.Deduplication
{
    public class DeduplicationSettings
    {
        public DeduplicationLevel Error { get; set; } = new DeduplicationLevel();
        public DeduplicationLevel Warning { get; set; } = new DeduplicationLevel();
        public DeduplicationLevel Information { get; set; } = new DeduplicationLevel();
        public DeduplicationLevel Debug { get; set; } = new DeduplicationLevel();
        public DeduplicationLevel Verbose { get; set; } = new DeduplicationLevel();

        // Pruning configuration
        public int PruneIntervalMilliseconds { get; set; } = 60000;  // Default: prune every 60 seconds
        public int CacheExpirationMilliseconds { get; set; } = 300000;  // Default: remove entries older than 5 minutes

        // New properties for configurable deduplication key
        public List<string> KeyProperties { get; set; } = new(); // Default to "Code"
        public bool IncludeMessageTemplate { get; set; } = true; // Default to true.

        public static DeduplicationSettings LoadFromConfiguration(IConfiguration configuration)
        {
            var settings = new DeduplicationSettings();
            configuration.GetSection("Logging:Deduplication").Bind(settings);
            return settings;
        }
    }
}
