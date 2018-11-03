namespace yoban.Mqtt.ControlPacket
{
    public sealed class SubscribeAck
    {
        public const byte PacketType = 0b0000_1001;
        public short PacketId { get; set; }
        public byte[] ReturnCodes { get; set; }
    }
}
