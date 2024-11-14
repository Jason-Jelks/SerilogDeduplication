using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Serilog.Deduplication.Extensions
{
    public static class DeduplicationApplicationBuilderExtensions
    {
        public static IServiceCollection AddDeduplication(this IServiceCollection services, IConfiguration configuration)
        {
            // Load deduplication settings from the configuration
            var deduplicationSettings = DeduplicationSettings.LoadFromConfiguration(configuration);

            // Register DeduplicationFilter as a singleton
            services.AddSingleton(new DeduplicationFilter(deduplicationSettings));

            return services;
        }
    }
}
