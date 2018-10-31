using System;
using System.Collections.Generic;
using System.Text;

namespace yoban.Mqtt.ControlPacket
{
    public sealed class ConnectAck
    {
        public const byte PacketType = 0b0000_0010;
        public bool SessionPresent { get; set; }
        public byte ReturnCode { get; set; }
    }
}
