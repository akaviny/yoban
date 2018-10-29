using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace yoban.Mqtt
{
    public class SocketConnection : INetworkConnection
    {
        private Socket _socket;
        private NetworkStream _stream;
        public SocketConnection(string hostName, int port)
        {
            HostName = hostName;
            Port = port;
        }
        public string HostName { get; private set; }
        public int Port { get; private set; }
        public async Task<Stream> ConnectAsync()
        {
            var ipAddress = IPAddress.Parse(HostName);
            _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await _socket.ConnectAsync(ipAddress, Port);
            _stream = new NetworkStream(_socket);
            return _stream;
        }
    }
}
