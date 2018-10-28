using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace yoban.Mqtt
{
    public class SocketConnection : INetworkConnection
    {
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private Socket _socket;
        public SocketConnection(IPAddress ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }
        public Task ConnectAsync()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            return _socket.ConnectAsync(_ipAddress, _port);
        }
        public Task<int> ReadAsync(byte[] buffer, int offset, int size) => _socket.ReceiveBytesAsync(buffer, offset, size);
        public Task<int> WriteAsync(byte[] buffer, int offset, int size) => _socket.SendBytesAsync(buffer, offset, size);
    }
}
