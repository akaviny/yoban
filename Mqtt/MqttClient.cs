using System.Threading.Tasks;
using yoban.Mqtt.ControlPacket;

namespace yoban.Mqtt
{
    public class MqttClient
    {
        private readonly INetworkConnection _networkConnection;
        public MqttClient(INetworkConnection networkConnection)
        {
            _networkConnection = networkConnection;
        }
        public async Task ConnectAsync(ConnectOptions options = null)
        {
            await _networkConnection.ConnectAsync().ConfigureAwait(false);
            var connect = new Connect(options ?? new ConnectOptions());
            await _networkConnection.WriteControlPacket(connect).ConfigureAwait(false);
        }
    }
}
