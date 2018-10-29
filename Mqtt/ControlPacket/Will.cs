namespace yoban.Mqtt.ControlPacket
{
    public sealed class Will
    {
        public bool InUse { get; set; }
        public QoS QoS { get; set; }
        public bool Retain { get; set; }
        public string Message { get; set; }
    }
}
