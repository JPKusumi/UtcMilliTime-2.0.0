using System.Diagnostics;
using System.Net.Sockets;

namespace UtcMilliTime
{
    public class NTPCallState
    {
        public bool priorSyncState;
        public byte[] buffer = new byte[Constants.bytes_per_buffer];
        public short methodsCompleted;
        public Socket? socket;
        public Stopwatch? latency;
        public Stopwatch? timer;
        public string serverResolved = string.Empty;

        public NTPCallState()
        {
            latency = Stopwatch.StartNew();
            buffer[0] = 0x1B;
        }

        public void OrderlyShutdown()
        {
            if (timer?.IsRunning == true)
            {
                timer.Stop();
            }
            timer = null;

            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch { }
                socket = null;
            }

            if (latency?.IsRunning == true)
            {
                latency.Stop();
            }
            latency = null;
        }
    }
}