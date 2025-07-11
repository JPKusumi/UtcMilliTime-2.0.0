UtcMilliTime

UtcMilliTime is a C# time component (software-defined clock) that yields Unix time milliseconds (`Int64`) timestamps, similar to JavaScript's `Date.now()`. It synchronizes with NTP servers and is cross-platform for .NET 8.0, supporting async `Main`. Mock-friendly via the `ITime` interface.

On NuGet at: https://www.nuget.org/packages/UtcMilliTime/

Versions
- 2.0.0: .NET 8.0, cross-platform, Task-based async APIs, `CreateAsync` for async `Main`, `ToIso8601String` for ISO-8601 conversion. Recommended for modern .NET.
- 1.0.1: .NET Standard 2.0 (Windows-only, .NET Framework 4.6.1+, .NET Core 2.0+). Available for legacy projects.

Overview
UtcMilliTime provides `Int64` timestamps (milliseconds since 1/1/1970 UTC, excluding leap seconds), avoiding the Year 2038 problem with 64-bit integers. It initializes with device time and syncs with NTP servers (default: `pool.ntp.org`) when permitted, ignoring user-changeable device time thereafter. Supports ISO-8601 string conversion via `ToIso8601String`.

Installation
```
dotnet add package UtcMilliTime --version 2.0.0
```
For legacy projects:
```
dotnet add package UtcMilliTime --version 1.0.1
```
Usage
```
using UtcMilliTime;

    ITime Time = Clock.Time;
    Time.SuppressNetworkCalls = false; // Enable NTP sync
    var timestamp = Time.Now; // Int64 timestamp
    string iso = timestamp.ToIso8601String(); // 2025-07-10T13:00:00.123Z
```
Supporting Async Main

```
static async Task Main(string[] args)
{
    var clock = await Clock.CreateAsync("pool.ntp.org");
    Console.WriteLine($"Synchronized: {clock.Synchronized}, Time: {clock.Now}, ISO: {clock.Now.ToIso8601String()}");
}
```
Features

• Unix Time Milliseconds: `Time.Now` returns milliseconds since 1/1/1970 UTC.
• NTP Synchronization: Syncs with NTP when `SuppressNetworkCalls = false`.
• ISO-8601 Conversion: `ToIso8601String(true)` for seconds (2025-07-10T13:00:00Z), or without parameter for milliseconds.
• Cross-Platform: Runs on Windows, Linux, macOS with .NET 8.0.

We should emphasize this line—
```
Time.SuppressNetworkCalls = false;
```
By default, the clock initializes with device time and leaves the network alone. The above line of code "gives permission" for UtcMilliTime to use the network for synchronization, via Network Time Protocol (NTP) to network time. The setting is durable for the rest of runtime. (The line needs only to execute once.)

With that permission, and subject to connectivity, the clock will synchronize itself to network time.

NetworkTimeAcquired Event

```
Time.NetworkTimeAcquired += (sender, e) => Console.WriteLine($"Synced with {e.Server}, Skew: {e.Skew}ms");
```
Notes

Silent Failure: `SelfUpdateAsync` fails silently if connectivity is absent. Check `Time.Synchronized` for a `true` or `false` value — success in synchronizing.
Leap Seconds: Clock advances during leap seconds, appearing 1 second ahead. Call `Time.SelfUpdateAsync()` to resync.
Performance: Use `Time.Now` for maximum performance; `ToIso8601String` is slower due to DateTime.

Upgrading from 1.0.1

Version 2.0.0: Targets .NET 8.0, cross-platform, adds CreateAsync. Public API unchanged.

Version 1.0.1: Windows-only, .NET Standard 2.0. Remains available for legacy projects.

Migration: Update to .NET 8.0+, then you can use `await Clock.CreateAsync()` in async Main.

Technical Details

Uses `Environment.TickCount64` for cross-platform uptime and `DateTime.UtcNow` for device time. Now is calculated as `device_boot_time + device_uptime`.

License

MIT License
