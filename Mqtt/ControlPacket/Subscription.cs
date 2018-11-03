namespace yoban.Mqtt.ControlPacket
{
    public sealed class Subscription
    {
        public string TopicFilter { get; set; }
        public QoS RequestedQoS { get; set; }
    }
}
