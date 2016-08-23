using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShyGuy.ChatFile
{
    public enum ChatMessageType
    {
        Unknown,
        Deleted,
        Text,
        Picture,
        Location,
        Block,
        Unblock,
    }

    public enum ChatMessageDeliveryStatus
    {
        NotStarted,
        Sent,

        /// <summary>XMPP socket reported an ACK for the message</summary>
        ServerTcpAck,
        /// <summary>Received an XMPP XEP-0198 ACK for the message</summary>
        ServerXmppSMAck,
        /// <summary>Received an XMPP XEP-0333 Received for the message</summary>
        ServerXmppCMReceived,
        /// <summary>Received an XMPP XEP-0333 Displayed for the message</summary>
        ServerXmppCMDisplayed,
        /// <summary>Received an XMPP XEP-0333 ACK for the message</summary>
        ServerXmppCMAck,
        /// <summary>XMPP socket failed at the TCP layer</summary>
        TcpFailed,
        /// <summary>XMPP server returned a message with type='error' on the message ID</summary>
        XmppError,
    }

    public class ChatMessage
    {
        /// <summary>A unique ID for this message used by the XMPP server.
        /// Conventionally a UUID, but not required.</summary>
        public string MessageId { get; set; }

        /// <summary>
        /// The content type of the message (text, picture, etc.)
        /// </summary>
        public ChatMessageType PayloadType { get; set; }

        /// <summary>
        /// True if the user sent this message, else false if the user received this message.
        /// </summary>
        public bool IsSentByMe { get; set; }
        
        /// <summary>
        /// The ID of the account that received the message.
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// The ID of the account that sent the message.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// True if the user has added a star to this message (for messages he likes)
        /// </summary>
        public bool IsStarred { get; set; }

        /// <summary>
        /// True if the user has marked the message as read
        /// </summary>
        public bool IsViewed { get; set; }

        /// <summary>
        /// True if the user has marked the message as Archived
        /// </summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// The timestamp at which this message was sent, in UTC
        /// </summary>
        public DateTime? SentTimeUtc { get; set; }

        /// <summary>
        /// The timestamp at which this message was received (if known)
        /// </summary>
        public DateTime? ReceivedTimeUtc { get; set; }

        /// <summary>
        /// Indicates whether the message was sent successfully (not valid for received messages)
        /// </summary>
        public ChatMessageDeliveryStatus DeliveryStatus { get; set; }

        /// <summary>
        /// For Text or Picture messages, a string. For Location messages, a <c ref="Location2D"/>.  Otherwise null.
        /// </summary>
        public object Payload { get; set; }
    }
}
