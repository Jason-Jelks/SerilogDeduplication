using Xunit;
using Serilog.Deduplication;
using Serilog.Events;
using Moq;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Parsing;

namespace Serilog.Deduplication.Tests
{
    public class DeduplicationFilterTests
    {
        [Fact]
        public void IsEnabled_ShouldReturnTrue_WhenDeduplicationIsDisabled()
        {
            // Arrange
            var deduplicationSettings = new DeduplicationSettings
            {
                Error = new DeduplicationLevel { DeduplicationEnabled = false }
            };
            var deduplicationFilter = new DeduplicationFilter(deduplicationSettings);

            var logEvent = CreateLogEvent(LogEventLevel.Error, "This is an error log");

            // Act
            var result = deduplicationFilter.IsEnabled(logEvent);

            // Assert
            Assert.True(result);  // Deduplication is disabled, so it should always log
        }

        [Fact]
        public void IsEnabled_ShouldReturnFalse_WhenLogIsDuplicate()
        {
            // Arrange
            var deduplicationSettings = new DeduplicationSettings
            {
                Error = new DeduplicationLevel { DeduplicationEnabled = true, DeduplicationWindowMilliseconds = 10000 }
            };
            var deduplicationFilter = new DeduplicationFilter(deduplicationSettings);

            var logEvent = CreateLogEvent(LogEventLevel.Error, "This is an error log");

            // First call, log should be allowed
            deduplicationFilter.IsEnabled(logEvent);

            // Act
            var result = deduplicationFilter.IsEnabled(logEvent);  // Second call within window

            // Assert
            Assert.False(result);  // This is a duplicate log within the deduplication window
        }

        [Fact]
        public void IsEnabled_ShouldReturnTrue_WhenLogIsNotDuplicate()
        {
            // Arrange
            var deduplicationSettings = new DeduplicationSettings
            {
                Error = new DeduplicationLevel { DeduplicationEnabled = true, DeduplicationWindowMilliseconds = 1000 }
            };
            var deduplicationFilter = new DeduplicationFilter(deduplicationSettings);

            var logEvent = CreateLogEvent(LogEventLevel.Error, "This is an error log");

            // First call, log should be allowed
            deduplicationFilter.IsEnabled(logEvent);

            // Wait for the deduplication window to expire
            System.Threading.Thread.Sleep(1500);

            // Act
            var result = deduplicationFilter.IsEnabled(logEvent);  // Second call after window expires

            // Assert
            Assert.True(result);  // Log is allowed because the deduplication window has expired
        }

        // Helper method to create a mock LogEvent
        private LogEvent CreateLogEvent(LogEventLevel level, string message)
        {
            return new LogEvent(
                timestamp: DateTimeOffset.UtcNow,
                level: level,
                exception: null,
                messageTemplate: new MessageTemplate(message, new List<MessageTemplateToken>()),
                properties: new List<LogEventProperty>
                {
                    new LogEventProperty("Code", new ScalarValue("123"))
                });
        }
    }
}
