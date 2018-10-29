using System.IO;
using System.Threading.Tasks;

namespace yoban.Mqtt.ControlPacket
{
    internal static class StreamExtensions
    {
        // Connect => ConnectAck
        internal static Task WriteConnectAsync(this Stream stream, Connect connect)
        {
            return Task.CompletedTask;
        }
        internal static Task<ConnectAck> ReadConnectAckAsync(this Stream stream)
        {
            return Task.FromResult(new ConnectAck());
        }
    }
}
