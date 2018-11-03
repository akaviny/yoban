using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var (remainingLength, remainingLengthBuffer) = EncodeRemainingLength(variableHeaderSize + payloadSize);
            var fixedHeaderSize = 1 + remainingLength; // 1 => holds packet type in high nibble, reserved in low
            var bufferSize = fixedHeaderSize + variableHeaderSize + payloadSize;
            var buffer = new byte[bufferSize];
            var index = 0;

            // Fixed header
            // - Packet type            
            buffer[index++] = CombineNibbles(Connect.PacketType, Empty);
            // - Remaining length
            Buffer.BlockCopy(remainingLengthBuffer, 0, buffer, index, remainingLength);
            index += remainingLength;

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
            var remainingLength = await DecodeRemainingLengthAsync(stream);
            if (remainingLength != 2) throw new InvalidOperationException("Invalid number of remaining bytes");
            var buffer = new byte[remainingLength];
            var numRead = await stream.ReadAsync(buffer, 0, remainingLength);
            return new ConnectAck
            {
                SessionPresent = Convert.ToBoolean(buffer[0] & 0x01),
                ReturnCode = buffer[1]
            };
        }
        internal static async Task WriteSubscribeAsync(this Stream stream, Subscribe subscribe)
        {
            // Determine packet size
            var variableHeaderSize = WordSize;
            var payloadSize = subscribe.GetPayloadSize();
            var (remainingLength, remainingLengthBuffer) = EncodeRemainingLength(variableHeaderSize + payloadSize);
            var fixedHeaderSize = 1 + remainingLength; // 1 => holds packet type in high nibble, reserved in low
            var bufferSize = fixedHeaderSize + variableHeaderSize + payloadSize;
            var buffer = new byte[bufferSize];
            var index = 0;

            // Fixed header
            // - Packet type            
            buffer[index++] = CombineNibbles(Subscribe.PacketType, Bit1);
            // - Remaining length
            Buffer.BlockCopy(remainingLengthBuffer, 0, buffer, index, remainingLength);
            index += remainingLength;

            // Variable header
            // - Message Id
            Buffer.BlockCopy(subscribe.PacketId.GetBigEndianBytes(), 0, buffer, index, WordSize);
            index += WordSize;

            // Payload
            // - Subscriptions
            subscribe.Subscriptions.ForEach(subscription =>
            {
                index = EncodeString(subscription.TopicFilter, buffer, index);
                buffer[index++] = (byte)subscription.RequestedQoS;
            });

            await stream.WriteAsync(buffer, 0, index).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        internal static async Task<SubscribeAck> ReadSubscribeAckAsync(this Stream stream)
        {
            var remainingLength = await DecodeRemainingLengthAsync(stream);
            var buffer = new byte[remainingLength];
            var numRead = await stream.ReadAsync(buffer, 0, remainingLength);
            return new SubscribeAck
            {
                PacketId = buffer.FromBigEndianBytes(),
                ReturnCodes = new ArraySegment<byte>(buffer, WordSize, remainingLength - WordSize).ToArray()
            };
        }
        internal static async Task<Publish> ReadPublishAsync(this Stream stream, byte lowNibble)
        {
            var remainingLength = await DecodeRemainingLengthAsync(stream);
            var publish = new Publish
            {
                Dup = (lowNibble & Bit3) == Bit3,
                QoS = (QoS)((lowNibble & (Bit2 | Bit1)) >> 1),
                Retain = (lowNibble & Bit0) == Bit0
            };
            var (topicNameLength, topicName) = await stream.DecodeStringAsync().ConfigureAwait(false);
            var payloadLength = remainingLength - topicNameLength;
            if (publish.QoS > QoS.AtMostOnce)
            {
                var packetId = await stream.DecodeShortAsync().ConfigureAwait(false);
                payloadLength -= WordSize;
            }
            var buffer = new byte[payloadLength];
            var numRead = await stream.ReadAsync(buffer, 0, payloadLength).ConfigureAwait(false);
            publish.Message = buffer;
            return publish;
        }
        internal static async Task WriteDisconnectAsync(this Stream stream)
        {
            await stream.WriteAsync(new byte[] { CombineNibbles(Disconnect.PacketType, Empty) }, 0, 1).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        private static int GetLength(this Will will)
        {
            if (will == null) return 0;
            var length = will.Topic.TryGetUTF8ByteCount();
            length += will.Message.TryGetUTF8ByteCount();
            return length;
        }
        private static int GetPayloadSize(this Connect connect) 
            => connect.ClientId.TryGetUTF8ByteCount() + connect.Will.GetLength() + connect.Username.TryGetUTF8ByteCount() + connect.Password.TryGetUTF8ByteCount();
        private static int GetPayloadSize(this Subscribe subscribe)
            => subscribe.Subscriptions.Select(subscription => subscription.TopicFilter).Aggregate(0, (count, topicFilter) => count + topicFilter.TryGetUTF8ByteCount()) + subscribe.Subscriptions.Count;
        private static (int length, byte[] buffer) EncodeRemainingLength(int length)
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
        private static async Task<int> DecodeRemainingLengthAsync(Stream stream)
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
            return remainingLength;
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
        private static byte[] GetBigEndianBytes(this int value) => GetBigEndianBytes((short)value);
        private static byte[] GetBigEndianBytes(this short value)
        {
            if (IsLittleEndian)
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);
                return bytes;
            }
            return BitConverter.GetBytes(value);
        }
        private static short FromBigEndianBytes(this byte[] bytes, int offset = 0)
        {
            if (IsLittleEndian)
            {
                var copy = new byte[WordSize];
                Buffer.BlockCopy(bytes, offset, copy, 0, WordSize);
                Array.Reverse(copy);
                return BitConverter.ToInt16(copy, 0);
            }
            return BitConverter.ToInt16(bytes, offset);
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
        private static async Task<(int count, string value)> DecodeStringAsync(this Stream stream)
        {
            var length = await stream.DecodeShortAsync();
            var buffer = new byte[length];
            await stream.ReadAsync(buffer, 0, length);
            return (length + LengthPrefixSize, Encoding.UTF8.GetString(buffer));
        }
        private static async Task<short> DecodeShortAsync(this Stream stream)
        {
            var buffer = new byte[LengthPrefixSize];
            var numRead = await stream.ReadAsync(buffer, 0, LengthPrefixSize).ConfigureAwait(false);
            return FromBigEndianBytes(buffer);
        }
        private static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var item in sequence)
                action(item);
        }
        private static byte CombineNibbles(byte high, byte low) => (byte)((high << 4) | low);
        private static bool IsLittleEndian => BitConverter.IsLittleEndian;
        private static int TryGetUTF8ByteCount(this string value, int lengthPrefix = LengthPrefixSize) => value == null ? 0 : Encoding.UTF8.GetByteCount(value) + lengthPrefix;        
        private const int WordSize = 2;
        private const int LengthPrefixSize = WordSize;
        private const int ConnectFlagsSize = 1;
        private const int KeepAliveSize = WordSize;
        private const byte Empty = 0b0000_0000;
        private const byte Bit0 = 0b0000_0001;
        private const byte Bit1 = 0b0000_0010;
        private const byte Bit2 = 0b0000_0100;
        private const byte Bit3 = 0b0000_1000;
    }
}
