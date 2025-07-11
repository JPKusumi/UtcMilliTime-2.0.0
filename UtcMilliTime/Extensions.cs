using System;
using System.Threading.Tasks;

namespace UtcMilliTime;

#pragma warning disable RECS0165 // Async methods should return a Task (async void)

/// <summary>
/// Extension methods for working with UtcMilliTime (64-bit Unix timestamps in UTC milliseconds).
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// Converts a UtcMilliTime timestamp to an ISO-8601 string.
    /// </summary>
    /// <param name="timestamp">The UtcMilliTime value.</param>
    /// <param name="suppressMilliseconds">If true, omits milliseconds.</param>
    /// <returns>String like "2019-08-10T22:08:14.102Z".</returns>
    public static string ToIso8601String(this long timestamp, bool suppressMilliseconds = false)
    {
        long ticks = (timestamp + Constants.dotnet_to_unix_milliseconds) * Constants.dotnet_ticks_per_millisecond;
        var dateTime = new DateTime(ticks, DateTimeKind.Utc);
        return suppressMilliseconds
            ? dateTime.ToString(Constants.iso_8601_without_milliseconds)
            : dateTime.ToString(Constants.iso_8601_with_milliseconds);
    }

    /// <summary>
    /// Converts a DateTime to UtcMilliTime (truncates fractional ms).
    /// </summary>
    public static long ToUtcMilliTime(this DateTime given) => (given.ToUniversalTime().Ticks / Constants.dotnet_ticks_per_millisecond) - Constants.dotnet_to_unix_milliseconds;

    /// <summary>
    /// Converts a DateTimeOffset to UtcMilliTime (truncates fractional ms).
    /// </summary>
    public static long ToUtcMilliTime(this DateTimeOffset given) => given.ToUnixTimeMilliseconds();

    /// <summary>
    /// Converts a TimeSpan interval to UtcMilliTime (truncates fractional ms).
    /// </summary>
    public static long ToUtcMilliTime(this TimeSpan given) => (long)given.TotalMilliseconds;

    /// <summary>
    /// Converts UnixTimeSeconds to UtcMilliTime (multiplies by 1000).
    /// </summary>
    public static long ToUtcMilliTime(this long unixtimeSeconds) => unixtimeSeconds * 1000;

    /// <summary>
    /// Truncates UtcMilliTime to UnixTimeSeconds (divides by 1000).
    /// </summary>
    public static long ToUnixTime(this long timestamp) => timestamp / 1000;

    /// <summary>
    /// Extracts the millisecond part (0-999) from a UtcMilliTime timestamp.
    /// </summary>
    public static short MillisecondPart(this long timestamp) => (short)(timestamp % 1000);

    /// <summary>
    /// Converts UtcMilliTime to a UTC DateTime.
    /// </summary>
    public static DateTime ToUtcDateTime(this long timestamp) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp);

    /// <summary>
    /// Converts UtcMilliTime to a local DateTime.
    /// </summary>
    public static DateTime ToLocalDateTime(this long timestamp) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp).ToLocalTime();

    /// <summary>
    /// Converts UtcMilliTime to a DateTimeOffset (UTC, offset 0).
    /// </summary>
    public static DateTimeOffset ToDateTimeOffset(this long timestamp) => new(timestamp.ToUtcDateTime());

    /// <summary>
    /// Converts a UtcMilliTime interval to a TimeSpan (or from 1970 if absolute).
    /// </summary>
    public static TimeSpan ToTimeSpan(this long interval) => new(interval * Constants.dotnet_ticks_per_millisecond);

    public static int IntervalDays(this long interval)
    {
        return TimeSpan.FromMilliseconds(interval).Days;
    }

    public static int IntervalHoursPart(this long interval)
    {
        return TimeSpan.FromMilliseconds(interval).Hours;
    }

    public static int IntervalMinutesPart(this long interval)
    {
        return TimeSpan.FromMilliseconds(interval).Minutes;
    }

    public static int IntervalSecondsPart(this long interval)
    {
        return TimeSpan.FromMilliseconds(interval).Seconds;
    }

    public static int IntervalMillisecondsPart(this long interval)
    {
        return TimeSpan.FromMilliseconds(interval).Milliseconds;
    }

    /// <summary>
    /// Safely fire-and-forget an async task with optional exception handling.
    /// </summary>
    public static async void SafeFireAndForget(this Task task, bool continueOnCapturedContext = true, Action<Exception>? onException = null)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex) when (onException != null)
        {
            onException(ex);
        }
    }

    /// <summary>
    /// Decomposes a UtcMilliTime interval into days, hours, minutes, and seconds.
    /// </summary>
    /// <returns>A struct with the decomposed parts.</returns>
    public static IntervalParts GetIntervalParts(this long interval)
    {
        int days = (int)(interval / Constants.day_milliseconds);
        long remainder = interval % Constants.day_milliseconds;
        int hours = (int)(remainder / Constants.hour_milliseconds);
        remainder %= Constants.hour_milliseconds;
        int minutes = (int)(remainder / Constants.minute_milliseconds);
        remainder %= Constants.minute_milliseconds;
        int seconds = (int)(remainder / Constants.second_milliseconds);
        return new IntervalParts(days, hours, minutes, seconds);
    }

    public readonly record struct IntervalParts(int Days, int Hours, int Minutes, int Seconds);
}

#pragma warning restore RECS0165