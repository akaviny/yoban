using System;

namespace yoban.Mqtt.ControlPacket
{
    public sealed class Connect
    {
        public Protocol Protocol { get; set; }
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public Will Will { get; set; }
        public bool CleanSession { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }
    }
}
