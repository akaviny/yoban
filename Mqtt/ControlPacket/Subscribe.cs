using System.Collections.Generic;

namespace yoban.Mqtt.ControlPacket
{
    public sealed class Subscribe
    {
        public const byte PacketType = 0b0000_1000;
        public short PacketId { get; set; }
        public IList<Subscription> Subscriptions { get; set; }
    }
}
