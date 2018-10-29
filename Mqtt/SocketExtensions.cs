using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using yoban.Mqtt.ControlPacket;

namespace yoban.Mqtt
{
    public static class SocketExtensions
    {
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
        public static Task<int> ReceiveBytesAsync(this Socket socket, byte[] buffer, int offset, int size)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginReceive(buffer, offset, size, SocketFlags.None, iar =>
            {
                var t = (TaskCompletionSource<int>)iar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try { t.TrySetResult(s.EndReceive(iar)); }
                catch (Exception exc) { t.TrySetException(exc); }
            }, tcs);
            return tcs.Task;
        }
        public static Task<int> SendBytesAsync(this Socket socket, byte[] buffer, int offset, int size)
        {
            // Todo: Check for count on completion; it can be less than requested, so need to schedule another BeginSend
            // with the remaining bytes
            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginSend(buffer, offset, size, SocketFlags.None, iar =>
            {
                var t = (TaskCompletionSource<int>)iar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try { t.TrySetResult(s.EndSend(iar)); }
                catch (Exception exc) { t.TrySetException(exc); }
            }, tcs);
            return tcs.Task;
        }

    }
}
