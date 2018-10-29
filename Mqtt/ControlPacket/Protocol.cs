namespace yoban.Mqtt.ControlPacket
{
    public sealed class Protocol
    {
        public static Protocol V3_1_1 = new Protocol
        {
            Name = "MQTT",
            Level = 0x04
        };
        public string Name { get; set; }
        public byte Level { get; set; }
    }
}
