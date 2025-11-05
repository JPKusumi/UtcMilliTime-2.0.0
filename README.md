# UtcMilliTime

UtcMilliTime is a C# time component (software-defined clock) that yields Unix time milliseconds (`Int64`) timestamps, similar to JavaScript's `Date.now()`. It synchronizes with NTP servers and is cross-platform for .NET 8 + .NET 10+, supporting async `Main`. Mock-friendly via the `ITime` interface.

On NuGet at: https://www.nuget.org/packages/UtcMilliTime/  
On GitHub at: https://github.com/JPKusumi/UtcMilliTime

## Versions
- **2.1.0**: Ready for .NET 10; still good for .NET 8+. Accuracy: 1ms. (Shifted to high resolution precise time.)
- **2.0.0**: First update in six years went cross-platform. Good for .NET 8+. Accuracy: +/-8ms.
- **1.0.1**: .NET Standard 2.0 (Windows-only, .NET Framework 4.6.1+, .NET Core 2.0+). Accuracy: +/-8ms due to Windows message pump.

## Overview
UtcMilliTime provides `Int64` timestamps (milliseconds since 1/1/1970 UTC, excluding leap seconds), avoiding the Year 2038 problem with 64-bit integers. It initializes with device time and syncs with NTP servers (default: `pool.ntp.org`) when permitted, ignoring user-changeable device time thereafter. Supports ISO-8601 string conversion via `ToIso8601String`.

**Note**: UtcMilliTime uses a singleton pattern—the clock is shared across the app. All accesses (static or via `CreateAsync`) refer to the same instance after initialization.

## Installation
```
dotnet add package UtcMilliTime --version 2.1.0
```
For legacy projects:
```
dotnet add package UtcMilliTime --version 1.0.1
```
## Usage
By default, the clock initializes with device time and leaves the network alone.
```
using UtcMilliTime;
  
  ITime time = Clock.Time; // Shorthand for repeated access to the singleton
  time.SuppressNetworkCalls = false; // Enable NTP sync (durable for runtime; execute once)
  var timestamp = time.Now; // Int64 timestamp
  string iso = timestamp.ToIso8601String(); // 2025-07-10T13:00:00.123Z
```
**Important**: `SuppressNetworkCalls = false` grants permission for NTP synchronization. The clock starts with device time; after permission and connectivity, it self-updates to network time. This setting persists for the app's lifetime and must be set explicitly (defaults to true to avoid unintended network use).

With permission, and subject to connectivity, the clock will synchronize itself to network time.

### Supporting Async Main
For async initialization in contexts like `async Main` (returns the shared clock instance):
```
static async Task Main(string[] args)
{
    var clock = await Clock.CreateAsync();
    clock.SuppressNetworkCalls = false; // Enable sync (triggers SelfUpdateAsync if indicated)
    Console.WriteLine($"Synchronized: {clock.Synchronized}, Time: {clock.Now}, ISO: {clock.Now.ToIso8601String()}");
    // For custom server: await clock.SelfUpdateAsync("custom.ntp.org");
}
```
**Note**: `CreateAsync` initializes and returns the singleton clock (using device time). Synchronization happens only after setting `SuppressNetworkCalls = false` (via the setter's logic) or manual `SelfUpdateAsync` calls. This ensures no unintended network traffic.

### NetworkTimeAcquired Event
Subscribe to events on the shared instance:
```
Clock.Time.NetworkTimeAcquired += (sender, e) => Console.WriteLine($"Synced with {e.Server}, Skew: {e.Skew}ms");
```
### Notes  
- **Silent Failure**: `SelfUpdateAsync` fails silently if connectivity is absent. Check `Synchronized` for success.  
- **Leap Seconds**: Clock advances during leap seconds, appearing 1 second ahead. Call `SelfUpdateAsync()` to resync.  
- **Performance**: Use `Now` for maximum performance; `ToIso8601String` is slower due to `DateTime`.

### Upgrading from 1.0.1
Version 2.1.0: Ready for .NET 10; still good for .NET 8+. Accuracy: 1ms. (Shifted to high resolution precise time.) Public API unchanged (static `Clock.Time.Now` still works as a singleton).

<<<<<<< HEAD
Migration: Update to .NET 8.0+. Static usage remains the same; for async Main, use `await Clock.CreateAsync()`—it returns the shared clock for consistency.
=======
Migration: Static usage remains the same; for async Main use `await Clock.CreateAsync()`—it returns the shared clock.
>>>>>>> 97f9163 (v2.1.0 Ready for .NET 10; still good for .NET 8+. Shifted to high resolution precise time.)

### Technical Details
Calculates with `Stopwatch.GetTimestamp` for high resolution uptime and `DateTime.UtcNow` for device time. Now is calculated as `device_boot_time + GetHighResUptime`. The clock is a singleton to ensure consistent time across the app.

### License
MIT License
