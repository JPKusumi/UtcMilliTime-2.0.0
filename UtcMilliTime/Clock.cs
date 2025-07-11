﻿using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Diagnostics;  // For Stopwatch
using System.Net;          // For Dns and IPEndPoint
using System.Net.Sockets;  // For Socket, AddressFamily, SocketType, and ProtocolType

namespace UtcMilliTime
{
    public sealed class Clock : ITime
    {
        private static readonly Lazy<Clock> instance = new Lazy<Clock>(() => new Clock());
        public static Clock Time => instance.Value;

        private static bool successfully_synced;
        private static bool suppress_network_calls = true;
        private static bool Indicated => !suppress_network_calls && !successfully_synced && NetworkInterface.GetIsNetworkAvailable();
        private static long device_uptime => Environment.TickCount64;
        private static long device_boot_time;
        private static NTPCallState? ntpCall;

        public bool Initialized => device_boot_time != 0;
        public bool SuppressNetworkCalls
        {
            get => suppress_network_calls;
            set
            {
                if (value != suppress_network_calls)
                {
                    suppress_network_calls = value;
                    if (Indicated)
                    {
                        SelfUpdateAsync().SafeFireAndForget(false);
                    }
                }
            }
        }
        public bool Synchronized => successfully_synced;
        public long DeviceBootTime => device_boot_time;
        public long DeviceUpTime => device_uptime;
        public long DeviceUtcNow => GetDeviceTime();
        public long Now => device_boot_time + device_uptime;
        public long Skew { get; private set; }
        public string DefaultServer { get; set; } = Constants.fallback_server;
        public event EventHandler<NTPEventArgs>? NetworkTimeAcquired;

        private Clock()
        {
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            Initialize();
            if (Indicated)
            {
                SelfUpdateAsync().SafeFireAndForget(false);
            }
        }

        public static Task<Clock> CreateAsync()
        {
            var clock = Time; // Ensure lazy singleton init (sync, device time)
            return Task.FromResult(clock); // Return immediately; no await needed yet
        }
        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (Indicated)
            {
                SelfUpdateAsync().SafeFireAndForget(false);
            }
        }

        private void Initialize()
        {
            device_boot_time = GetDeviceTime() - device_uptime;
            successfully_synced = false;
            Skew = 0;
        }

        private static long GetDeviceTime() => DateTime.UtcNow.Ticks / Constants.dotnet_ticks_per_millisecond - Constants.dotnet_to_unix_milliseconds;

        public async Task SelfUpdateAsync(string ntpServerHostName = Constants.fallback_server)
        {
            if (ntpCall != null)
            {
                return;
            }

            ntpCall = new NTPCallState();
            try
            {
                Initialize();
                if (!Initialized || !Indicated)
                {
                    return;
                }

                ntpServerHostName = ntpServerHostName == Constants.fallback_server && !string.IsNullOrEmpty(DefaultServer)
                    ? DefaultServer
                    : ntpServerHostName;

                ntpCall.serverResolved = ntpServerHostName;
                var addresses = await Dns.GetHostAddressesAsync(ntpServerHostName).ConfigureAwait(false);
                var ipEndPoint = new IPEndPoint(addresses[0], Constants.udp_port_number);

                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    ReceiveTimeout = Constants.three_seconds
                };

                ntpCall.socket = socket; // Assign for NTPCallState compatibility
                ntpCall.timer = Stopwatch.StartNew();
                await socket.ConnectAsync(ipEndPoint).ConfigureAwait(false);
                ntpCall.methodsCompleted += 1;

                await socket.SendAsync(ntpCall.buffer.AsMemory(0, Constants.bytes_per_buffer)).ConfigureAwait(false);
                ntpCall.methodsCompleted += 1;

                await socket.ReceiveAsync(ntpCall.buffer.AsMemory(0, Constants.bytes_per_buffer)).ConfigureAwait(false);
                ntpCall.methodsCompleted += 1;
                ntpCall.timer.Stop();

                long halfRoundTrip = ntpCall.timer.ElapsedMilliseconds / 2;
                const byte serverReplyTime = 40;
                ulong intPart = BitConverter.ToUInt32(ntpCall.buffer, serverReplyTime);
                ulong fractPart = BitConverter.ToUInt32(ntpCall.buffer, serverReplyTime + 4);
                intPart = SwapEndianness(intPart);
                fractPart = SwapEndianness(fractPart);
                var milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
                long timeNow = (long)milliseconds - Constants.ntp_to_unix_milliseconds + halfRoundTrip;

                if (timeNow <= 0)
                {
                    successfully_synced = false;
                    return;
                }

                device_boot_time = timeNow - device_uptime;
                Skew = timeNow - GetDeviceTime();
                successfully_synced = ntpCall.methodsCompleted == 3;
                ntpCall.latency.Stop();

                if (successfully_synced && !ntpCall.priorSyncState && NetworkTimeAcquired != null)
                {
                    NTPEventArgs args = new NTPEventArgs(ntpCall.serverResolved, ntpCall.latency.ElapsedMilliseconds, Skew);
                    NetworkTimeAcquired.Invoke(this, args);
                }
            }
            catch (Exception)
            {
                successfully_synced = false;
            }
            finally
            {
                ntpCall?.OrderlyShutdown();
                ntpCall = null;
            }
        }

        private static uint SwapEndianness(ulong x) => (uint)(((x & 0x000000ff) << 24) +
            ((x & 0x0000ff00) << 8) +
            ((x & 0x00ff0000) >> 8) +
            ((x & 0xff000000) >> 24));
    }
}