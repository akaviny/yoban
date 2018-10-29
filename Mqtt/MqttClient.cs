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
        private readonly X509Certificate2 _clientCertificate;
        public MqttSecureClient(INetworkConnection networkConnection, X509Certificate2 clientCertificate)
        {
            _mqtt = new MqttClientImpl(networkConnection);
            _clientCertificate = clientCertificate;
        }
        public async Task ConnectAsync(Connect connect)
        {
            var stream = await _mqtt.NetworkConnection.ConnectAsync().ConfigureAwait(false);
            var sslStream = new SslStream(stream);
            var clientCertificates = new X509CertificateCollection(new X509Certificate2[] { _clientCertificate });
            await sslStream.AuthenticateAsClientAsync(_mqtt.NetworkConnection.HostName, clientCertificates, SslProtocols.Tls12, true);
            await _mqtt.ConnectAsync(connect, sslStream);
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
        internal MqttClientImpl(INetworkConnection networkConnection) => NetworkConnection = networkConnection;
        internal INetworkConnection NetworkConnection { get; private set; }
        internal Task ConnectAsync(Connect connect, Stream stream)
        {
            _stream = stream;
            return _stream.WriteConnectAsync(connect);
        }
    }
}
