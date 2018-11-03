using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
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
            var sslStream = new SslStream(stream, false, null, null);
            var clientCertificates = new X509CertificateCollection(new X509Certificate[] { _clientCertificate });
            await sslStream.AuthenticateAsClientAsync(_mqtt.NetworkConnection.HostName, clientCertificates, SslProtocols.Tls12, true).ConfigureAwait(false);
            await _mqtt.ConnectAsync(connect, sslStream).ConfigureAwait(false);            
        }
        public Task SubscribeAsync(Subscribe subscribe) => _mqtt.SubscribeAsync(subscribe);
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
        private CancellationTokenSource _cts;
        internal MqttClientImpl(INetworkConnection networkConnection) => NetworkConnection = networkConnection;
        internal INetworkConnection NetworkConnection { get; private set; }
        internal Task ConnectAsync(Connect connect, Stream stream)
        {
            _stream = stream;
            _cts = new CancellationTokenSource();
            _listener = StartListeningAsync();
            return _stream.WriteConnectAsync(connect);
        }
        internal Task SubscribeAsync(Subscribe subscribe) => _stream.WriteSubscribeAsync(subscribe);
        internal Task DisconnectAsync()
        {
            // Ignore any messages during disconnect
            _cts.Cancel();
            return _stream.WriteDisconnectAsync();
        }
        private async Task StartListeningAsync()
        {
            var nextByte = new byte[1];
            var readCount = 0;
            
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    readCount = await _stream.ReadAsync(nextByte, 0, 1).ConfigureAwait(false);
                    if (readCount == 1)
                    {
                        var highNibble = (byte)((nextByte[0] & 0xF0) >> 4);
                        switch (highNibble)
                        {
                            case ConnectAck.PacketType:
                                var connectAck = await _stream.ReadConnectAckAsync().ConfigureAwait(false);
                                Console.WriteLine("Connected");
                                break;
                            case SubscribeAck.PacketType:
                                var subscribeAck = await _stream.ReadSubscribeAckAsync().ConfigureAwait(false);
                                Console.WriteLine($"ClientId: {subscribeAck.PacketId} => {subscribeAck.ReturnCodes.Aggregate("", (value, code) => value + $" {code}")}");
                                break;
                            case Publish.PacketType:
                                var lowNibble = (byte)(nextByte[0] & 0x0F);
                                var publish = await _stream.ReadPublishAsync(lowNibble).ConfigureAwait(false);
                                Console.WriteLine($"Received message: {Encoding.UTF8.GetString(publish.Message)}");
                                break;
                            default:
                                Console.WriteLine(nextByte[0]);
                                break;
                        }
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
