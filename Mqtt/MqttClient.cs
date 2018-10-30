using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using yoban.Mqtt.ControlPacket;

namespace yoban.Mqtt
{
    public sealed class MqttSecureClient
    {
        private readonly MqttClientImpl _mqtt;
        private SslStream _sslStream;
        private readonly X509Certificate2 _clientCertificate;
        public MqttSecureClient(INetworkConnection networkConnection, X509Certificate2 clientCertificate)
        {
            _mqtt = new MqttClientImpl(networkConnection);
            _clientCertificate = clientCertificate;
        }
        public async Task ConnectAsync(Connect connect)
        {
            var stream = await _mqtt.NetworkConnection.ConnectAsync().ConfigureAwait(false);
            _sslStream = new SslStream(stream, false, null, null);
            var clientCertificates = new X509CertificateCollection(new X509Certificate[] { _clientCertificate });
            await _sslStream.AuthenticateAsClientAsync(_mqtt.NetworkConnection.HostName, clientCertificates, SslProtocols.Tls12, true).ConfigureAwait(false);
            await _mqtt.ConnectAsync(connect, _sslStream).ConfigureAwait(false);            
        }
    }
    public sealed class MqttClient
    {
        private MqttClientImpl _mqtt;
        public MqttClient(INetworkConnection networkConnection) => _mqtt = new MqttClientImpl(networkConnection);
        public async Task ConnectAsync(Connect connect)
        {
            var stream = await _mqtt.NetworkConnection.ConnectAsync().ConfigureAwait(false);
            await _mqtt.ConnectAsync(connect, stream).ConfigureAwait(false);
        }
    }

    internal sealed class MqttClientImpl
    {
        private Stream _stream;
        private Task _listener;
        internal MqttClientImpl(INetworkConnection networkConnection) => NetworkConnection = networkConnection;
        internal INetworkConnection NetworkConnection { get; private set; }
        internal async Task ConnectAsync(Connect connect, Stream stream)
        {
            _stream = stream;
            _listener = StartListeningAsync();
            await _stream.WriteConnectAsync(connect).ConfigureAwait(false);
        }
        private async Task StartListeningAsync()
        {
            var fixedHeader = new byte[1];
            var readCount = 0;
            while (true)
            {
                try
                {
                    readCount = await _stream.ReadAsync(fixedHeader, 0, 1).ConfigureAwait(false);
                    if (readCount == 1)
                    {
                        Console.WriteLine($"Received {fixedHeader[0]}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }
    }
}
