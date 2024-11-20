namespace Nearform.AspNetCore.SlowDown;

internal static class Constants
{
    public const string LimitHeader = "X-Slow-Down-Limit";
    public const string RemainingHeader = "X-Slow-Down-Remaining";
    public const string DelayHeader = "X-Slow-Down-Delay";
    
    public const string ConfigurationKey = "SlowDown";
}