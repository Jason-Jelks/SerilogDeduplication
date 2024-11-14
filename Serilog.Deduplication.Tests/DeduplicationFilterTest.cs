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

        private DeduplicationFilter CreateDeduplicationFilter(
            bool includeMessageTemplate = false,
            List<string>? keyProperties = null)
        {
            var deduplicationSettings = new DeduplicationSettings
            {
                KeyProperties = keyProperties ?? new List<string>(),
                IncludeMessageTemplate = includeMessageTemplate
            };
            return new DeduplicationFilter(deduplicationSettings);
        }

        private LogEvent CreateLogEvent(string messageTemplateText, Dictionary<string, string>? properties = null)
        {
            var propertiesList = new List<LogEventProperty>();
            if (properties != null)
            {
                foreach (var (key, value) in properties)
                {
                    propertiesList.Add(new LogEventProperty(key, new ScalarValue(value)));
                }
            }

            return new LogEvent(
                timestamp: DateTimeOffset.UtcNow,
                level: LogEventLevel.Information,
                exception: null,
                messageTemplate: new MessageTemplate(messageTemplateText, new List<MessageTemplateToken>()),
                properties: propertiesList);
        }

        [Fact]
        public void GetKey_NoKeyPropertiesAndNoMessageTemplate_ShouldReturnEmptyString()
        {
            // Arrange
            var deduplicationFilter = CreateDeduplicationFilter(includeMessageTemplate: false);

            var logEvent = CreateLogEvent("Test message");

            // Act
            var key = deduplicationFilter.GetKey(logEvent);

            // Assert
            Assert.Equal(string.Empty, key);
        }

        [Fact]
        public void GetKey_NoKeyPropertiesAndIncludeMessageTemplate_ShouldReturnMessageTemplate()
        {
            // Arrange
            var deduplicationFilter = CreateDeduplicationFilter(includeMessageTemplate: true);

            var logEvent = CreateLogEvent("Test message");

            // Act
            var key = deduplicationFilter.GetKey(logEvent);

            // Assert
            Assert.Equal("Test message", key);
        }

        [Fact]
        public void GetKey_WithKeyPropertiesOnly_ShouldReturnKeyProperty()
        {
            // Arrange
            var deduplicationFilter = CreateDeduplicationFilter(keyProperties: new List<string> { "Code" });

            var logEvent = CreateLogEvent("Test message", new Dictionary<string, string> { { "Code", "123" } });

            // Act
            var key = deduplicationFilter.GetKey(logEvent);

            // Assert
            Assert.Equal("123", key);
        }

        [Fact]
        public void GetKey_WithKeyPropertiesAndIncludeMessageTemplate_ShouldReturnKeyPropertyAndMessageTemplate()
        {
            // Arrange
            var deduplicationFilter = CreateDeduplicationFilter(
                includeMessageTemplate: true,
                keyProperties: new List<string> { "Code" });

            var logEvent = CreateLogEvent("Test message", new Dictionary<string, string> { { "Code", "123" } });

            // Act
            var key = deduplicationFilter.GetKey(logEvent);

            // Assert
            Assert.Equal("123-Test message", key);
        }

        [Fact]
        public void GetKey_MultipleKeyPropertiesAndIncludeMessageTemplate_ShouldReturnCompositeKey()
        {
            // Arrange
            var deduplicationFilter = CreateDeduplicationFilter(
                includeMessageTemplate: true,
                keyProperties: new List<string> { "Code", "Source", "DeviceName" });

            var logEvent = CreateLogEvent("Test message",
                new Dictionary<string, string>
                {
                    { "Code", "123" },
                    { "Source", "App" },
                    { "DeviceName", "Device1" }
                });

            // Act
            var key = deduplicationFilter.GetKey(logEvent);

            // Assert
            Assert.Equal("123-App-Device1-Test message", key);
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
