namespace Serilog.Deduplication
{
    public class DeduplicationLevel
    {
        public bool DeduplicationEnabled { get; set; } = true;  // Default to enabled
        public int DeduplicationWindowMilliseconds { get; set; } = 5000;  // Default window (5 seconds)
    }
}
