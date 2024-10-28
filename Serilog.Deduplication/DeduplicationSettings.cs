using Microsoft.Extensions.Configuration;

namespace Serilog.Deduplication
{
    public class DeduplicationSettings
    {
        public DeduplicationLevel Error { get; set; } = new DeduplicationLevel();       // Deduplication settings for Error level
        public DeduplicationLevel Warning { get; set; } = new DeduplicationLevel();     // Deduplication settings for Warning level
        public DeduplicationLevel Information { get; set; } = new DeduplicationLevel(); // Deduplication settings for Information level
        public DeduplicationLevel Debug { get; set; } = new DeduplicationLevel();       // Deduplication settings for Debug level

        public static DeduplicationSettings LoadFromConfiguration(IConfiguration configuration)
        {
            var settings = new DeduplicationSettings();
            configuration.GetSection("Logging:Deduplication").Bind(settings);
            return settings;
        }
    }
}
