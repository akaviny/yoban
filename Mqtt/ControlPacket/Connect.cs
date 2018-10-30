using System;
using System.IO;
using System.Linq;

namespace yoban.Mqtt.ControlPacket
{
    public sealed class Connect
    {
        public const byte PacketType = 0b0000_0001;
        public Connect()
        {
            // Default to v3.1.1
            Protocol = Protocol.V3_1_1;
            CleanSession = true;
            KeepAliveInterval = TimeSpan.FromSeconds(600);
        }
        public Protocol Protocol { get; set; }
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public Will Will { get; set; }
        public bool CleanSession { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }        
    }
}
