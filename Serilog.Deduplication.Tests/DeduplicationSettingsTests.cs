using Xunit;
using Microsoft.Extensions.Configuration;
using Serilog.Deduplication;
using System.Collections.Generic;

namespace Serilog.Deduplication.Tests
{
    public class DeduplicationSettingsTests
    {
        [Fact]
        public void LoadFromConfiguration_ShouldLoadCorrectSettings()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                {"Logging:Deduplication:Error:DeduplicationEnabled", "false"},
                {"Logging:Deduplication:Error:DeduplicationWindowMilliseconds", "10000"},
                {"Logging:Deduplication:Information:DeduplicationEnabled", "true"},
                {"Logging:Deduplication:Information:DeduplicationWindowMilliseconds", "3000"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Act
            var deduplicationSettings = DeduplicationSettings.LoadFromConfiguration(configuration);

            // Assert
            Assert.False(deduplicationSettings.Error.DeduplicationEnabled);
            Assert.Equal(10000, deduplicationSettings.Error.DeduplicationWindowMilliseconds);

            Assert.True(deduplicationSettings.Information.DeduplicationEnabled);
            Assert.Equal(3000, deduplicationSettings.Information.DeduplicationWindowMilliseconds);
        }
    }
}
