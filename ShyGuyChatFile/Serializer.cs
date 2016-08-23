using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShyGuy.ChatFile
{
    public static class Serializer
    {
        /// <summary>
        /// Given an account-counterparty tuple, creates a filename suitable for a chat history archive
        /// </summary>
        /// <param name="accountId">The user's account ID</param>
        /// <param name="counterpartyId">The counterparty's account ID</param>
        /// <returns>A filename with extension</returns>
        public static string CreateFilename(string accountId, string counterpartyId)
        {
            if (accountId == null)
                throw new ArgumentNullException(nameof(accountId));
            if (counterpartyId == null)
                throw new ArgumentNullException(nameof(counterpartyId));

            return $"g_{accountId}~{counterpartyId}.shyguy-buddy";
        }

        /// <summary>
        /// Converts some chat messages into the ShyGuy chat file format
        /// </summary>
        /// <param name="messages">Zero or more messages to convert</param>
        /// <returns>An opaque binary stream that should be written to the chat file</returns>
        public static Stream Serialize(IEnumerable<ChatMessage> messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            var headerStream = new MemoryStream();
            try
            {
                using (var header = new BinaryWriter(headerStream, Encoding.UTF8, leaveOpen: true))
                using (var trailerStream = new MemoryStream())
                using (var trailer = new BinaryWriter(trailerStream, Encoding.UTF8, leaveOpen: true))
                {
                    header.Write(FileFormat.MagicHeader);
                    header.Write(FileFormat.FileVersion);
                    header.Write((uint)messages.Count());
                    header.Write(0u); // reserved
                    header.Write(0u); // reserved

                    foreach (var message in messages)
                    {
                        SerializeMessage(header, trailer, message);
                    }

                    if (trailerStream.Length > 0)
                    {
                        trailer.Flush();
                        trailerStream.Seek(0, SeekOrigin.Begin);
                        trailerStream.CopyTo(headerStream);
                    }

                    header.Write(FileFormat.EndOfFileMarker);
                    header.Flush();
                }

                headerStream.Seek(0, SeekOrigin.Begin);
                var result = headerStream;
                headerStream = null;
                return result;
            }
            finally
            {
                if (headerStream != null)
                {
                    headerStream.Dispose();
                }
            }
        }

        private static void SerializeMessage(BinaryWriter header, BinaryWriter trailer, ChatMessage message)
        {
            var flags = default(FileFormat.Flags);

            Guid idAsGuid;
            if (Guid.TryParse(message.MessageId, out idAsGuid) && idAsGuid != FileFormat.ExtendedMessageId)
            {
                header.Write(idAsGuid.ToByteArray());
            }
            else
            {
                header.Write(FileFormat.ExtendedMessageId.ToByteArray());
                flags |= FileFormat.Flags.HasExtendedId;
                trailer.Write(message.MessageId);
            }

            if (message.PayloadType == ChatMessageType.Deleted)
            {
                flags |= FileFormat.Flags.IsDeleted;
            }
            else
            {
                switch (message.PayloadType)
                {
                    case ChatMessageType.Unknown:
                        flags |= FileFormat.Flags.Unknown;
                        break;
                    case ChatMessageType.Text:
                        flags |= FileFormat.Flags.Text;
                        trailer.Write((string)message.Payload);
                        break;
                    case ChatMessageType.Picture:
                        flags |= FileFormat.Flags.Picture;
                        trailer.Write((string)message.Payload);
                        break;
                    case ChatMessageType.Location:
                        flags |= FileFormat.Flags.Location;
                        var location = (Location2D)message.Payload;
                        trailer.Write((float)location.Latitude);
                        trailer.Write((float)location.Longitude);
                        break;
                    case ChatMessageType.Block:
                        flags |= FileFormat.Flags.Blocked;
                        break;
                    case ChatMessageType.Unblock:
                        flags |= FileFormat.Flags.Unblocked;
                        break;
                    default:
                        throw new ArgumentException($"Invalid PayloadType {message.PayloadType}", nameof(message));
                }

                if (message.IsViewed)
                    flags |= FileFormat.Flags.IsViewed;
                if (message.IsStarred)
                    flags |= FileFormat.Flags.IsStarred;

                flags |= GetSerializedDeliveryStatusFlag(message.DeliveryStatus);
            }

            if (message.IsSentByMe)
                flags |= FileFormat.Flags.IsSentByMe;

            if (message.IsArchived)
                flags |= FileFormat.Flags.IsArchived;

            header.Write((ushort)flags);

            if (message.PayloadType != ChatMessageType.Deleted)
            {
                header.Write(DateTimeToUint(message.SentTimeUtc ?? default(DateTime)));
                header.Write(DateTimeToUint(message.ReceivedTimeUtc ?? default(DateTime)));
            }
            else
            {
                header.Write(DateTimeToUint(default(DateTime)));
                header.Write(DateTimeToUint(default(DateTime)));
            }
        }

        private static uint DateTimeToUint(DateTime timestamp)
        {
            if (timestamp.Ticks == 0)
                return 0;

            var secondsSinceEpoch = (timestamp.Ticks - FileFormat.DateEpoch.Ticks) / 10000000.0;
            if (secondsSinceEpoch < 1)
                secondsSinceEpoch = 1;

            return (uint)Math.Round(secondsSinceEpoch, 0);
        }

        private static FileFormat.Flags GetSerializedDeliveryStatusFlag(ChatMessageDeliveryStatus status)
        {
            switch (status)
            {
                case ChatMessageDeliveryStatus.Sent:
                    return FileFormat.Flags.DeliveryPending;
                case ChatMessageDeliveryStatus.XmppError:
                    return FileFormat.Flags.DeliveryXmppError;
                case ChatMessageDeliveryStatus.ServerTcpAck:
                    return FileFormat.Flags.DeliveryServerTcpAck;
                case ChatMessageDeliveryStatus.ServerXmppSMAck:
                    return FileFormat.Flags.DeliveryServerXmppSMAck;
                case ChatMessageDeliveryStatus.ServerXmppCMReceived:
                    return FileFormat.Flags.DeliveryServerXmppCMReceived;
                case ChatMessageDeliveryStatus.ServerXmppCMDisplayed:
                    return FileFormat.Flags.DeliveryServerXmppCMDisplayed;
                case ChatMessageDeliveryStatus.ServerXmppCMAck:
                    return FileFormat.Flags.DeliveryServerXmppCMAck;
                case ChatMessageDeliveryStatus.TcpFailed:
                    return FileFormat.Flags.DeliveryTcpFailed;
                case ChatMessageDeliveryStatus.NotStarted:
                    return FileFormat.Flags.DeliveryNotStarted;
                default:
                    throw new ArgumentException($"Unsupported delivery status: {status}", nameof(status));
            }
        }

    }
}
