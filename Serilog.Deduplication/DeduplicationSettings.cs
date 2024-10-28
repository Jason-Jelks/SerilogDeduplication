using Microsoft.Extensions.Configuration;

namespace Serilog.Deduplication
{
    public class DeduplicationSettings
    {
        public int DeduplicationWindowMilliseconds { get; set; } = 5000; // Default to 5 seconds

        public static DeduplicationSettings LoadFromConfiguration(IConfiguration configuration)
        {
            // Load settings from the "Logging:Deduplication" section of the configuration
            var settings = new DeduplicationSettings();
            configuration.GetSection("Logging:Deduplication").Bind(settings);
            return settings;
        }
    }
}
