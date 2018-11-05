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
    public sealed class MqttSecureClient : MqttClientBase
    {
        private readonly X509Certificate2 _clientCertificate;
        public MqttSecureClient(INetworkConnection networkConnection, X509Certificate2 clientCertificate)
            : base(networkConnection) => _clientCertificate = clientCertificate;
        public override async Task ConnectAsync(Connect connect)
        {
            var stream = await NetworkConnection.ConnectAsync().ConfigureAwait(false);
            var sslStream = new SslStream(stream, false, null, null);
            var clientCertificates = new X509CertificateCollection(new X509Certificate[] { _clientCertificate });
            await sslStream.AuthenticateAsClientAsync(NetworkConnection.HostName, clientCertificates, SslProtocols.Tls12, true).ConfigureAwait(false);
            await ConnectCoreAsync(connect, sslStream).ConfigureAwait(false);
        }
    }
    public sealed class MqttClient : MqttClientBase
    {
        public MqttClient(INetworkConnection networkConnection)
            : base(networkConnection) { }
        public override async Task ConnectAsync(Connect connect)
        {
            var stream = await NetworkConnection.ConnectAsync().ConfigureAwait(false);
            await ConnectCoreAsync(connect, stream).ConfigureAwait(false);
        }
    }

    public abstract class MqttClientBase
    {
        private Stream _stream;
        private Task _listener;
        private CancellationTokenSource _cts;
        protected MqttClientBase(INetworkConnection networkConnection) => NetworkConnection = networkConnection;
        public abstract Task ConnectAsync(Connect connect);
        public Task SubscribeAsync(Subscribe subscribe) => _stream.WriteSubscribeAsync(subscribe);
        public Task PublishAsync(Publish publish) => _stream.WritePublishAsync(publish);
        public async Task DisconnectAsync()
        {
            await _stream.WriteDisconnectAsync().ConfigureAwait(false);
            _cts.Cancel();
            try
            {
                await _listener;
                Console.WriteLine("Finished listening");
            }
            catch {  }
        }
        protected INetworkConnection NetworkConnection { get; private set; }
        protected Task ConnectCoreAsync(Connect connect, Stream stream)
        {
            _stream = stream;
            _cts = new CancellationTokenSource();
            _listener = StartListeningAsync();
            return _stream.WriteConnectAsync(connect);
        }
        private async Task StartListeningAsync()
        {
            var nextByte = new byte[1];
            var readCount = 0;
            var finished = false;
            using (_cts.Token.Register(() => _stream.Dispose()))
            {
                while (!finished)
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
                        else
                        {
                            Console.WriteLine($"Invalid number of bytes read: {readCount}");
                            finished = true;
                        }
                    }
                    catch
                    {
                        finished = true;
                    }
                }
            }
            
        }
    }
}
