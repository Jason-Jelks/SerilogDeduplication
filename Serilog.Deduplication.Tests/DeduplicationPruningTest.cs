using System;
using Xunit;
using Serilog.Deduplication;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace Serilog.Deduplication.Tests
{
    public class DeduplicationPruningTests
    {
        [Fact]
        public void PruneCache_RemovesOldEntries_AfterExpirationTime()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                { "Logging:Deduplication:PruneIntervalMilliseconds", "1000" }, // Prune every 1 second for the test
                { "Logging:Deduplication:CacheExpirationMilliseconds", "2000" } // Entries older than 2 seconds will be pruned
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var deduplicationSettings = DeduplicationSettings.LoadFromConfiguration(configuration);
            var deduplicationFilter = new DeduplicationFilter(deduplicationSettings);

            // Act
            var logEvent1 = TestHelper.CreateLogEvent("First log", "Code1");
            deduplicationFilter.IsEnabled(logEvent1);

            Thread.Sleep(1000);  // Wait for 1 second (within expiration time)
            Assert.False(deduplicationFilter.IsEnabled(logEvent1));  // Log should still be deduplicated

            // Wait for more time (total 3 seconds) to exceed the expiration time (2 seconds)
            Thread.Sleep(3000);  // Wait for pruning to be triggered by the timer (prune interval: 1 second)

            // Assert
            Assert.True(deduplicationFilter.IsEnabled(logEvent1));  // The log should have been pruned and allowed again
        }
    }
}
