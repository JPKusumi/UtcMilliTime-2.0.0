# UtcMilliTime

UtcMilliTime is a C# time component (software-defined clock) that yields Unix time milliseconds (`Int64`) timestamps, similar to JavaScript's `Date.now()`. It synchronizes with NTP servers and is cross-platform for .NET 8.0, supporting async `Main`. Mock-friendly via the `ITime` interface.

On NuGet at: https://www.nuget.org/packages/UtcMilliTime/  
On GitHub at: https://github.com/JPKusumi/UtcMilliTime

## Versions
- **2.0.0**: .NET 8.0, cross-platform, Task-based async APIs, `CreateAsync` for async `Main`, `ToIso8601String` for ISO-8601 conversion. Recommended for modern .NET.
- **1.0.1**: .NET Standard 2.0 (Windows-only, .NET Framework 4.6.1+, .NET Core 2.0+). Available for legacy projects.

## Overview
UtcMilliTime provides `Int64` timestamps (milliseconds since 1/1/1970 UTC, excluding leap seconds), avoiding the Year 2038 problem with 64-bit integers. It initializes with device time and syncs with NTP servers (default: `pool.ntp.org`) when permitted, ignoring user-changeable device time thereafter. Supports ISO-8601 string conversion via `ToIso8601String`.

**Note**: UtcMilliTime uses a singleton pattern—the clock is shared across the app. All accesses (static or via `CreateAsync`) refer to the same instance after initialization.

## Installation
```
dotnet add package UtcMilliTime --version 2.0.0
```
For legacy projects:
```
dotnet add package UtcMilliTime --version 1.0.1
```
## Usage
By default, the clock initializes with device time and leaves the network alone.
```
using UtcMilliTime;
  
  ITime Time = Clock.Time; // Shorthand for repeated access to the singleton
  Time.SuppressNetworkCalls = false; // Enable NTP sync (durable for runtime; execute once)
  var timestamp = Time.Now; // Int64 timestamp
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
Version 2.0.0: Targets .NET 8.0, adds cross-platform support and `CreateAsync`. Public API unchanged (static `Clock.Time.Now` still works as a singleton).

Migration: Update to .NET 8.0+. Static usage remains the same; for async Main, use `await Clock.CreateAsync()`—it returns the shared clock for consistency.

### Technical Details
Uses `Environment.TickCount64` for cross-platform uptime and `DateTime.UtcNow` for device time. Now is calculated as `device_boot_time + device_uptime`. The clock is a singleton to ensure consistent time across the app.

### License
MIT License
