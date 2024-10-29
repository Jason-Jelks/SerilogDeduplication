using Serilog.Events;
using Serilog.Parsing;
using System.Collections.Generic;

namespace Serilog.Deduplication.Tests
{
    public static class TestHelper
    {
        public static LogEvent CreateLogEvent(string message, string code)
        {
            return new LogEvent(
                timestamp: DateTimeOffset.UtcNow,
                level: LogEventLevel.Information,
                exception: null,
                messageTemplate: new MessageTemplate(message, new List<MessageTemplateToken>()),
                properties: new List<LogEventProperty>
                {
                    new LogEventProperty("Code", new ScalarValue(code))
                });
        }
    }
}
