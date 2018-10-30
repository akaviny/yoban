using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace yoban.Mqtt
{
    /// <summary>
    /// Default <see cref="INetworkConnection"/> implementation using a <see cref="NetworkStream"/> as the transport.
    /// </summary>
    public class NetworkConnection : INetworkConnection
    {
        private Socket _socket;
        private NetworkStream _stream;
        public NetworkConnection(string hostName, int port)
        {
            HostName = hostName;
            Port = port;
        }
        public string HostName { get; private set; }
        public int Port { get; private set; }
        public async Task<Stream> ConnectAsync()
        {
            var hostEntry = await AsyncExtensions.GetHostEntryAsync(HostName);
            var ipAddress = hostEntry?.AddressList?.First(ip => ip != null);
            if (ipAddress == null)
            {
                throw new InvalidOperationException("Invalid hostname");
            }
            _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await _socket.ConnectAsync(ipAddress, Port);
            _stream = new NetworkStream(_socket);
            return _stream;
        }
    }
}
