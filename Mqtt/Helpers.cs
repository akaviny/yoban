using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace yoban.Mqtt
{
    internal static class Helpers
    {
        public static Task<IPHostEntry> GetHostEntryAsync(string hostName)
            => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, hostName, null);
        
        public static Task ConnectAsync(this Socket socket, IPAddress ipAddress, int port)
        {
            var tcs = new TaskCompletionSource<bool>(socket);
            socket.BeginConnect(ipAddress, port, iar =>
            {
                var t = (TaskCompletionSource<bool>)iar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try
                {
                    s.EndConnect(iar);
                    t.TrySetResult(true);
                }
                catch (Exception exc) { t.TrySetException(exc); }
            }, tcs);
            return tcs.Task;
        }
        public static async Task ReadBytesAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            var numRead = 0;
            while (numRead != count)
            {
                numRead += await stream.ReadAsync(buffer, numRead + offset, count - numRead).ConfigureAwait(false);
            }
        }
    }
}
