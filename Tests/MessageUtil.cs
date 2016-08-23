using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShyGuy.ChatFile.Tests
{
    internal static class MessageUtil
    {
        public static void VerifyMessagesEqual(ChatMessage a, ChatMessage b)
        {
            Assert.AreEqual(a.MessageId, b.MessageId, nameof(ChatMessage.MessageId));
            Assert.AreEqual(a.PayloadType, b.PayloadType, nameof(ChatMessage.PayloadType));
            Assert.AreEqual(a.IsSentByMe, b.IsSentByMe, nameof(ChatMessage.IsSentByMe));
            Assert.AreEqual(a.Recipient, b.Recipient, nameof(ChatMessage.Recipient));
            Assert.AreEqual(a.Sender, b.Sender, nameof(ChatMessage.Sender));
            Assert.AreEqual(a.IsArchived, b.IsArchived, nameof(ChatMessage.IsArchived));

            if (a.PayloadType != ChatMessageType.Deleted)
            {
                Assert.AreEqual(a.IsStarred, b.IsStarred, nameof(ChatMessage.IsStarred));
                Assert.AreEqual(a.IsViewed, b.IsViewed, nameof(ChatMessage.IsViewed));
                Assert.AreEqual(a.DeliveryStatus, b.DeliveryStatus, nameof(ChatMessage.DeliveryStatus));

                // Timestamps lose up to 1 second of precision, so we have to verify them specially
                VerifyTimestamps(a.SentTimeUtc, b.SentTimeUtc);
                VerifyTimestamps(a.ReceivedTimeUtc, b.ReceivedTimeUtc);
            }

            VerifyPayload(a.Payload, b.Payload);
        }

        private static void VerifyPayload(object a, object b)
        {
            if (a == null || a is string)
            {
                Assert.AreEqual(a, b, nameof(ChatMessage.Payload));
            }
            else if (a is Location2D)
            {
                Assert.IsTrue(b is Location2D);
                var locationA = (Location2D)a;
                var locationB = (Location2D)b;

                var deltaLatitude = Math.Abs(locationA.Latitude - locationB.Latitude);
                var deltaLongitude = Math.Abs(locationA.Longitude - locationB.Longitude);

                var epsilon = 0.0001;
                Assert.IsTrue(deltaLatitude < epsilon, $"Too much variation in latitude: {locationA.Latitude} vs {locationB.Latitude}");
                Assert.IsTrue(deltaLongitude < epsilon, $"Too much variation in longitude: {locationA.Longitude} vs {locationB.Longitude}");
            }
            else
            {
                Assert.Fail($"Unknown payload type {a.GetType()}");
            }
        }

        private static void VerifyTimestamps(DateTime? a, DateTime? b)
        {
            if (a == null || a == default(DateTime))
            {
                Assert.IsTrue(b == null || b == default(DateTime));
            }
            else
            {
                Assert.IsNotNull(b);

                var ticksA = a.Value.Ticks;
                var ticksB = b.Value.Ticks;

                var delta = Math.Abs(ticksA - ticksB);
                Assert.IsTrue(delta < 2 * 1000 * 1000 * 10, $"Timestamp skew is too great. Timestamps are: {a} and {b}");
            }
        }
    }
}
