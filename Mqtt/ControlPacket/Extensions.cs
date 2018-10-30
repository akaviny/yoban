using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace yoban.Mqtt.ControlPacket
{
    internal static class Extensions
    {

        // Connect => ConnectAck
        internal static Task WriteConnectAsync(this Stream stream, Connect connect)
        {            
            // Determine packet size
            var variableHeaderSize = connect.Protocol.Size + LengthPrefixSize + ConnectFlagsSize + KeepAliveSize;
            var payloadSize = connect.GetPayloadSize();
            var remainingLengthEncoding = EncodeVariableLength(variableHeaderSize + payloadSize);
            var fixedHeaderSize = 1 + remainingLengthEncoding.length; // 1 => holds packet type in high nibble, reserved in low
            var bufferSize = fixedHeaderSize + variableHeaderSize + payloadSize;
            var buffer = new byte[bufferSize];
            var index = 0;

            // Fixed header
            // - Packet type            
            buffer[index++] = CombineNibbles(Connect.PacketType, Reserved);
            // - Remaining length
            Buffer.BlockCopy(remainingLengthEncoding.buffer, 0, buffer, index, remainingLengthEncoding.length);
            index += remainingLengthEncoding.length;

            // Variable header
            // - Protocol name
            var protocolNameBuffer = Encoding.UTF8.GetBytes(connect.Protocol.Name);
            Buffer.BlockCopy(protocolNameBuffer.Length.GetBigEndianBytes(), 0, buffer, index, LengthPrefixSize);
            index += LengthPrefixSize;            
            Buffer.BlockCopy(protocolNameBuffer, 0, buffer, index, protocolNameBuffer.Length);
            index += protocolNameBuffer.Length;
            // - Protocol level
            buffer[index++] = connect.Protocol.Level;
            // - Connect flags
            buffer[index++] = connect.GetConnectFlags();
            // - Keep alive
            Buffer.BlockCopy(((int)connect.KeepAliveInterval.TotalSeconds).GetBigEndianBytes(), 0, buffer, index, KeepAliveSize);
            index += KeepAliveSize;

            // Payload
            // - Client Identifier            
            var clientIdBuffer = Encoding.UTF8.GetBytes(connect.ClientId);
            Buffer.BlockCopy(clientIdBuffer.Length.GetBigEndianBytes(), 0, buffer, index, LengthPrefixSize);
            index += LengthPrefixSize;
            Buffer.BlockCopy(clientIdBuffer, 0, buffer, index, clientIdBuffer.Length);
            index += clientIdBuffer.Length;
            // - Will Topic
            if (connect.Will != null)
            {
                if (!string.IsNullOrWhiteSpace(connect.Will.Topic))
                {
                    var topicBuffer = Encoding.UTF8.GetBytes(connect.Will.Topic);
                    Buffer.BlockCopy(topicBuffer.Length.GetBigEndianBytes(), 0, buffer, index, LengthPrefixSize);
                    index += LengthPrefixSize;
                    Buffer.BlockCopy(topicBuffer, 0, buffer, index, topicBuffer.Length);
                    index += topicBuffer.Length;
                }
                if (!string.IsNullOrWhiteSpace(connect.Will.Message))
                {
                    var messageBuffer = Encoding.UTF8.GetBytes(connect.Will.Message);
                    Buffer.BlockCopy(messageBuffer.Length.GetBigEndianBytes(), 0, buffer, index, LengthPrefixSize);
                    index += LengthPrefixSize;
                    Buffer.BlockCopy(messageBuffer, 0, buffer, index, messageBuffer.Length);
                    index += messageBuffer.Length;
                }
            }
            // - User Name
            if (!string.IsNullOrWhiteSpace(connect.Username))
            {
                var usernameBuffer = Encoding.UTF8.GetBytes(connect.Username);
                Buffer.BlockCopy(usernameBuffer.Length.GetBigEndianBytes(), 0, buffer, index, LengthPrefixSize);
                index += LengthPrefixSize;
                Buffer.BlockCopy(usernameBuffer, 0, buffer, index, usernameBuffer.Length);
                index += usernameBuffer.Length;
            }
            // - Password
            if (!string.IsNullOrWhiteSpace(connect.Password))
            {
                var passwordBuffer = Encoding.UTF8.GetBytes(connect.Password);
                Buffer.BlockCopy(passwordBuffer.Length.GetBigEndianBytes(), 0, buffer, index, LengthPrefixSize);
                index += LengthPrefixSize;
                Buffer.BlockCopy(passwordBuffer, 0, buffer, index, passwordBuffer.Length);
                index += passwordBuffer.Length;
            }
            Console.WriteLine($"Connect packet size => {index}");
            return stream.WriteAsync(buffer, 0, index);
        }
        internal static Task<ConnectAck> ReadConnectAckAsync(this Stream stream)
        {
            return Task.FromResult(new ConnectAck());
        }
        private static int GetLength(this Will will)
        {
            if (will == null) return 0;
            var length = will.Topic.TryGetUTF8ByteCount();
            length += will.Message.TryGetUTF8ByteCount();
            return length;
        }
        private static int GetPayloadSize(this Connect connect) => 
            connect.ClientId.TryGetUTF8ByteCount() + connect.Will.GetLength() + connect.Username.TryGetUTF8ByteCount() + connect.Password.TryGetUTF8ByteCount();
        private static (int length, byte[] buffer) EncodeVariableLength(int length)
        {
            // Taken directly from mqtt v3.1.1 section 2.2.3
            byte[] buffer = new byte[4];
            var value = 0;
            var index = 0;
            do
            {
                value = length % 128;
                length /= 128; // Note, rounding is intentional...
                if (length > 0) value |= 128;
                buffer[index++] = (byte)value;
            }
            while (length > 0);
            return (index, buffer);
        }        
        private static byte GetConnectFlags(this Connect connect)
        {
            byte flags = 0b0000_0000;
            if (connect.CleanSession) flags |= 1 << 1;
            if (connect.Will != null)
            {
                flags |= 1 << 2;
                flags |= (byte)((byte)connect.Will.QoS << 3);
                if (connect.Will.Retain) flags |= 1 << 5;
            }
            if (!string.IsNullOrWhiteSpace(connect.Password)) flags |= 1 << 6;
            if (!string.IsNullOrWhiteSpace(connect.Username)) flags |= 1 << 7;
            return flags;
        }
        // BitConverter.GetBytes uses the host for endianness; typically, this is little endian, so create a specific endian converter
        private static byte[] GetBigEndianBytes(this int value)
        {
            if (IsLittleEndian)
            {
                var bytes = BitConverter.GetBytes((short)value);
                Array.Reverse(bytes);
                return bytes;
            }
            return BitConverter.GetBytes(value);
        }       
        private static byte CombineNibbles(byte high, byte low) => (byte)((high << 4) | low);
        private static bool IsLittleEndian => BitConverter.IsLittleEndian;
        private static int TryGetUTF8ByteCount(this string value, int lengthPrefix = LengthPrefixSize) => value == null ? 0 : Encoding.UTF8.GetByteCount(value) + lengthPrefix;        
        private const int WordSize = 2;
        private const int LengthPrefixSize = WordSize;
        private const int ConnectFlagsSize = 1;
        private const int KeepAliveSize = WordSize;
        private const byte Reserved = 0b0000_0000;
    }
}
