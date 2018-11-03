namespace yoban.Mqtt.ControlPacket
{
    public sealed class Publish
    {
        public const byte PacketType = 0b0000_0011;
        public bool Dup { get; set; }
        public QoS QoS { get; set; }
        public bool Retain { get; set; }
        public string TopicName { get; set; }
        public short PacketId { get; set; }
        public byte[] Message { get; set; }
    }
}
