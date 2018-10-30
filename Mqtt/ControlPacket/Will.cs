namespace yoban.Mqtt.ControlPacket
{
    public sealed class Will
    {
        public QoS QoS { get; set; }
        public string Topic { get; set; }
        public bool Retain { get; set; }
        public string Message { get; set; }
    }
}
