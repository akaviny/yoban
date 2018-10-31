using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace yoban.Mqtt.ControlPacket
{
    internal static class Extensions
    {

        // Connect => ConnectAck
        internal static async Task WriteConnectAsync(this Stream stream, Connect connect)
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
            index = EncodeString(connect.Protocol.Name, buffer, index);
            // - Protocol level
            buffer[index++] = connect.Protocol.Level;
            // - Connect flags
            buffer[index++] = connect.GetConnectFlags();
            // - Keep alive
            Buffer.BlockCopy(((int)connect.KeepAliveInterval.TotalSeconds).GetBigEndianBytes(), 0, buffer, index, KeepAliveSize);
            index += KeepAliveSize;

            // Payload
            // - Client Identifier            
            index = EncodeString(connect.ClientId, buffer, index);
            // - Will Topic
            index = EncodeString(connect.Will?.Topic, buffer, index);
            // - Will Message
            index = EncodeString(connect.Will?.Message, buffer, index);
            // - User Name
            index = EncodeString(connect.Username, buffer, index);
            // - Password
            index = EncodeString(connect.Password, buffer, index);
            
            await stream.WriteAsync(buffer, 0, index).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        internal static async Task<ConnectAck> ReadConnectAckAsync(this Stream stream)
        {
            var remainingLengthDecoding = await DecodeVariableLengthAsync(stream);
            if (remainingLengthDecoding.remainingLength != 2) throw new InvalidOperationException("Invalid number of remaining bytes");
            return new ConnectAck
            {
                SessionPresent = Convert.ToBoolean(remainingLengthDecoding.buffer[0] & 0x01),
                ReturnCode = remainingLengthDecoding.buffer[1]
            };
        }
        internal static async Task WriteDisconnectAsync(this Stream stream)
        {
            await stream.WriteAsync(new byte[] { CombineNibbles(Disconnect.PacketType, Reserved) }, 0, 1).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
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
            var buffer = new byte[4];
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
        private static async Task<(int remainingLength, byte[] buffer)> DecodeVariableLengthAsync(Stream stream)
        {
            // Taken directly from mqtt v3.1.1 section 2.2.3
            const int termination = 128 * 128 * 128;
            var multiplier = 1;
            var remainingLength = 0;
            var next = new byte[1];
            int numRead = 0;
            do
            {
                numRead = await stream.ReadAsync(next, 0, 1).ConfigureAwait(false);
                if (numRead == 0) throw new InvalidOperationException("Stream read is empty");
                remainingLength += (next[0] & 127) * multiplier;
                multiplier *= 128;
                if (multiplier == termination) throw new InvalidOperationException("Malformed remaining length");
            } while ((next[0] & 128) != 0);
            var buffer = new byte[remainingLength];
            numRead = await stream.ReadAsync(buffer, 0, remainingLength);
            if (numRead == 0 || numRead != remainingLength) throw new InvalidOperationException("Stream read error");
            return (remainingLength, buffer);
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
        private static int EncodeString(string value, byte[] buffer, int index)
        {
            if (string.IsNullOrWhiteSpace(value)) return index;
            var bytes = Encoding.UTF8.GetBytes(value);
            Buffer.BlockCopy(bytes.Length.GetBigEndianBytes(), 0, buffer, index, LengthPrefixSize);
            index += LengthPrefixSize;
            Buffer.BlockCopy(bytes, 0, buffer, index, bytes.Length);
            return index + bytes.Length;
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
