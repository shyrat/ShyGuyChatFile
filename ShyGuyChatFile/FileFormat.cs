using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShyGuy.ChatFile
{
    internal static class FileFormat
    {
        /// <summary>First dword of the file - used to identify the file is indeed a ShyGuy chat archive</summary>
        public static readonly uint MagicHeader = 0xf09f91ac;

        /// <summary>Current version - increment to indicate the presence of backwards-compatible changes</summary>
        public static readonly uint FileVersion = 1;

        /// <summary>Last dword of the file - used to ensure the file wasn't truncated</summary>
        public static readonly uint EndOfFileMarker = 0x53687943;

        /// <summary>Sentinel value to indicate when the message ID cannot be stored in a UUID</summary>
        public static readonly Guid ExtendedMessageId = new Guid(MagicHeader, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        /// <summary>Size of the fixed header for each message</summary>
        public static readonly uint HeaderSize = 16 + 2 + 4 + 4;

        /// <summary>Bias for the timestamp</summary>
        public static readonly DateTime DateEpoch = DateTime.FromBinary(-8589708388854775808L); // 2009/01/01 in UTC

        [Flags]
        public enum Flags : ushort
        {
            Unknown = (1 << 0),
            Text = (2 << 0),
            Picture = (3 << 0),
            Location = (4 << 0),
            Blocked = (5 << 0),
            Unblocked = (6 << 0),

            DeliveryNotStarted = (0 << 4),
            DeliveryPending = (1 << 4),
            DeliveryServerTcpAck = (2 << 4),
            DeliveryServerXmppSMAck = (3 << 4),
            DeliveryServerXmppCMReceived = (4 << 4),
            DeliveryServerXmppCMDisplayed = (5 << 4),
            DeliveryServerXmppCMAck = (6 << 4),
            DeliveryTcpFailed = (10 << 4),
            DeliveryXmppError = (11 << 4),

            IsSentByMe = (1 << 8),
            HasExtendedId = (2 << 8),
            IsViewed = (4 << 8),
            IsStarred = (8 << 8),

            IsArchived = (1 << 12),
            IsDeleted = (2 << 12),
        }
    }
}
