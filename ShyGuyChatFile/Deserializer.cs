using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShyGuy.ChatFile
{
    public static class Deserializer
    {
        private struct ParseState
        {
            public string AccountId;
            public string CounterpartyId;

            public long LastTimestampTicks;
        }

        private static Regex _filenameParser = new Regex(@"^g_(\d+)~(\d+).shyguy-buddy$", RegexOptions.Compiled);
        public static bool ParseFilename(string filename, out string accountId, out string counterpartyId)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));
            
            var match = _filenameParser.Match(Path.GetFileName(filename));
            if (match.Groups.Count != 3)
            {
                accountId = null;
                counterpartyId = null;
                return false;
            }

            accountId = match.Groups[1].Value;
            counterpartyId = match.Groups[2].Value;
            return true;
        }

        public static IEnumerable<ChatMessage> Deserialize(string accountId, string counterpartyId, Stream archiveContents)
        {
            if (accountId == null)
                throw new ArgumentNullException(nameof(accountId));
            if (counterpartyId == null)
                throw new ArgumentNullException(nameof(counterpartyId));
            if (archiveContents == null)
                throw new ArgumentNullException(nameof(archiveContents));

            byte[] array;
            using (var reader = new BinaryReader(archiveContents))
            {
                // This is a little kludgy -- I'm not an expert in .NET streams APIs!
                array = reader.ReadBytes((int)archiveContents.Length);
            }

            var state = new ParseState
            {
                AccountId = accountId,
                CounterpartyId = counterpartyId,
            };

            using (var headerStream = new MemoryStream(array, writable: false))
            using (var reader = new BinaryReader(headerStream, Encoding.UTF8, leaveOpen: true))
            {
                if (FileFormat._magic != reader.ReadUInt32())
                    throw new ShyGuyFileFormatException($"File is not a ShyGuy chat archive. Expected file header 0x{FileFormat._magic:x}.");

                var version = reader.ReadUInt32();

                var numMessages = reader.ReadUInt32();

                var reserved1 = reader.ReadUInt32();
                var reserved2 = reader.ReadUInt32();

                var result = new ChatMessage[numMessages];
                var trailerOffset = (int)((uint)headerStream.Position + FileFormat._headerSize * numMessages);

                using (var trailerStream = new MemoryStream(array, trailerOffset, array.Length - trailerOffset, writable: false))
                using (var trailer = new BinaryReader(trailerStream, Encoding.UTF8, leaveOpen: true))
                {
                    for (var i = 0; i < numMessages; i++)
                    {
                        result[i] = DeserializeMessage(reader, trailer, state);
                    }
                }

                return result;
            }
        }

        private static ChatMessage DeserializeMessage(BinaryReader header, BinaryReader trailer, ParseState state)
        {
            var message = new ChatMessage();

            var guid = new Guid(header.ReadBytes(16));
            var flags = (FileFormat.Flags)header.ReadUInt16();
            var type = (FileFormat.Flags)((ushort)flags & 0xF);

            var id = flags.HasFlag(FileFormat.Flags.HasExtendedId) ? trailer.ReadString() : guid.ToString();

            if (flags.HasFlag(FileFormat.Flags.IsDeleted))
            {
                message.PayloadType = ChatMessageType.Deleted;
            }
            else
            {
                switch (type)
                {
                    case FileFormat.Flags.Unknown:
                        message.PayloadType = ChatMessageType.Unknown;
                        break;
                    case FileFormat.Flags.Text:
                        message.PayloadType = ChatMessageType.Text;
                        message.Payload = trailer.ReadString();
                        break;
                    case FileFormat.Flags.Picture:
                        message.PayloadType = ChatMessageType.Picture;
                        message.Payload = trailer.ReadString();
                        break;
                    case FileFormat.Flags.Location:
                        message.PayloadType = ChatMessageType.Location;
                        message.Payload = new Location2D(trailer.ReadSingle(), trailer.ReadSingle());
                        break;
                    case FileFormat.Flags.Blocked:
                        message.PayloadType = ChatMessageType.Block;
                        break;
                    case FileFormat.Flags.Unblocked:
                        message.PayloadType = ChatMessageType.Unblock;
                        break;
                    default:
                        throw new ShyGuyFileFormatException($"Unrecognized message type {type} from flags 0x{flags:x}");
                }
            }

            message.MessageId = id;

            if (flags.HasFlag(FileFormat.Flags.IsSentByMe))
                message.IsSentByMe = true;
            if (flags.HasFlag(FileFormat.Flags.IsStarred))
                message.IsStarred = true;
            if (flags.HasFlag(FileFormat.Flags.IsViewed))
                message.IsViewed = true;
            if (flags.HasFlag(FileFormat.Flags.IsArchived))
                message.IsArchived = true;

            message.Sender = message.IsSentByMe ? state.AccountId : state.CounterpartyId;
            message.Recipient = message.IsSentByMe ? state.CounterpartyId : state.AccountId;

            message.SentTimeUtc = UintToDateTime(header.ReadUInt32(), ref state.LastTimestampTicks);
            message.ReceivedTimeUtc = UintToDateTime(header.ReadUInt32(), ref state.LastTimestampTicks);

            message.DeliveryStatus = GetDeliveryStatus(flags);

            return message;
        }

        
        private static DateTime UintToDateTime(uint value, ref long previousTicks)
        {
            if (value == 0)
                return default(DateTime);

            var ticks = (long)value * 10000000 + FileFormat._epoch.Ticks;

            // Force messages to be ordered correctly
            if (previousTicks == ticks)
                ticks = ticks + 1;
            previousTicks = ticks;

            return new DateTime(ticks);
        }

        private static ChatMessageDeliveryStatus GetDeliveryStatus(FileFormat.Flags flags)
        {
            var deliveryStatus = (FileFormat.Flags)((ushort)flags & 0xf0);
            switch (deliveryStatus)
            {
                case FileFormat.Flags.DeliveryNotStarted:
                    return ChatMessageDeliveryStatus.NotStarted;
                case FileFormat.Flags.DeliveryServerTcpAck:
                    return ChatMessageDeliveryStatus.ServerTcpAck;
                case FileFormat.Flags.DeliveryServerXmppSMAck:
                    return ChatMessageDeliveryStatus.ServerXmppSMAck;
                case FileFormat.Flags.DeliveryServerXmppCMReceived:
                    return ChatMessageDeliveryStatus.ServerXmppCMReceived;
                case FileFormat.Flags.DeliveryServerXmppCMDisplayed:
                    return ChatMessageDeliveryStatus.ServerXmppCMDisplayed;
                case FileFormat.Flags.DeliveryServerXmppCMAck:
                    return ChatMessageDeliveryStatus.ServerXmppCMAck;
                case FileFormat.Flags.DeliveryTcpFailed:
                    return ChatMessageDeliveryStatus.TcpFailed;
                case FileFormat.Flags.DeliveryXmppError:
                    return ChatMessageDeliveryStatus.XmppError;
                case FileFormat.Flags.DeliveryPending:
                    return ChatMessageDeliveryStatus.Sent;
                default:
                    return ChatMessageDeliveryStatus.Sent;
            }
        }
    }
}
