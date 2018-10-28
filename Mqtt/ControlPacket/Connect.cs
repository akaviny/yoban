using System;

namespace yoban.Mqtt.ControlPacket
{
    public sealed class ConnectOptions
    {
        public ConnectOptions()
        {
            ClientId = Guid.NewGuid().ToString();
        }
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public sealed class Connect : ControlPacketBase
    {
        private readonly ConnectOptions _options;
        public Connect(ConnectOptions options) => _options = options;
        public override byte[] ToBytes()
        {
            throw new System.NotImplementedException();
        }
    }
}
