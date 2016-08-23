using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShyGuy.ChatFile.Tests
{
    [TestClass]
    public class RoundtripTests
    {
        [TestMethod]
        public void VerifyRoundtripMessageTypes()
        {
            var textMessage = GenerateBasicMessage();
            textMessage.PayloadType = ChatMessageType.Text;
            textMessage.Payload = "hello world";

            var pictureMessage = GenerateBasicMessage();
            pictureMessage.PayloadType = ChatMessageType.Picture;
            pictureMessage.Payload = "123456789";

            var locationMessage = GenerateBasicMessage();
            locationMessage.PayloadType = ChatMessageType.Location;
            locationMessage.Payload = new Location2D(1.2345, -9.8765);

            var blockMessage = GenerateBasicMessage();
            blockMessage.PayloadType = ChatMessageType.Block;

            var unblockMessage = GenerateBasicMessage();
            unblockMessage.PayloadType = ChatMessageType.Unblock;

            var deletedMessage = GenerateBasicMessage();
            deletedMessage.PayloadType = ChatMessageType.Deleted;

            var unknownMessage = GenerateBasicMessage();
            unknownMessage.PayloadType = ChatMessageType.Unknown;

            VerifyRoundtripMessages(new ChatMessage[] 
            {
                textMessage,
                pictureMessage,
                locationMessage,
                blockMessage,
                unblockMessage,
                deletedMessage,
                unknownMessage,
            });
        }

        [TestMethod]
        public void VerifyEmptyBlock()
        {
            VerifyRoundtripMessages(new ChatMessage[0]);
        }

        [TestMethod]
        public void VerifyLargeNumberOfMessages()
        {
            var n = 10000;
            var array = new ChatMessage[n];

            _rand = new Random(0x1234567);

            for (var i = 0; i < n; i++)
            {
                array[i] = GenerateRandomMessage();
            }

            VerifyRoundtripMessages(array);
        }

        [TestMethod]
        public void VerifyTimestamps()
        {
            var m = GenerateBasicMessage();

            // These are roughly the extremes of the timestamps we support.
            m.SentTimeUtc = new DateTime(year: 2009, month: 1, day: 1, hour: 4, minute: 0, second: 0, kind: DateTimeKind.Utc);
            m.ReceivedTimeUtc = new DateTime(year: 2110, month: 1, day: 1, hour: 4, minute: 0, second: 0, kind: DateTimeKind.Utc);

            VerifyRoundtripMessages(new[] { m });
        }

        private static void VerifyRoundtripMessages(IEnumerable<ChatMessage> input)
        {
            string accountId;
            string counterpartyId;

            if (input.Any())
            {
                var first = input.First();
                accountId = first.IsSentByMe ? first.Sender : first.Recipient;
                counterpartyId = first.IsSentByMe ? first.Recipient : first.Sender;

                // Need to normalize the accounts, since Serialize only supports a single
                // accountId-counterpartyId pair.
                foreach (var message in input)
                {
                    if (message.IsSentByMe)
                    {
                        message.Sender = accountId;
                        message.Recipient = counterpartyId;
                    }
                    else
                    {
                        message.Sender = counterpartyId;
                        message.Recipient = accountId;
                    }
                }
            }
            else
            {
                accountId = "1";
                counterpartyId = "2";
            }

            using (var stream = Serializer.Serialize(input))
            {
                var output = Deserializer.Deserialize(accountId, counterpartyId, stream);

                Assert.AreEqual(input.Count(), output.Count(), "Number of messages changed during serialization");
                Apply(input, output, MessageUtil.VerifyMessagesEqual);
            }
        }

        private static void Apply<TFirst, TSecond>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Action<TFirst, TSecond> action)
        {
            var a = first.GetEnumerator();
            var b = second.GetEnumerator();

            while (a.MoveNext() && b.MoveNext())
                action(a.Current, b.Current);
        }

        private ChatMessage GenerateBasicMessage()
        {
            return new ChatMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                IsSentByMe = true,
                Sender = "1234",
                Recipient = "5678",
                IsArchived = false,
                IsStarred = false,
                IsViewed = true,
                Payload = null,
                PayloadType = ChatMessageType.Unknown,
                DeliveryStatus = ChatMessageDeliveryStatus.ServerTcpAck,
                ReceivedTimeUtc = DateTime.UtcNow,
                SentTimeUtc = DateTime.UtcNow,
            };
        }

        private Random _rand;
        private ChatMessage GenerateRandomMessage()
        {
            var message = new ChatMessage
            {
                MessageId = GenerateMessageId(),
                IsSentByMe = GenerateBoolean(),
                Sender = GenerateAccountId(),
                Recipient = GenerateAccountId(),
                IsArchived = GenerateBoolean(),
                IsStarred = GenerateBoolean(),
                IsViewed = GenerateBoolean(),
                DeliveryStatus = GenerateEnum<ChatMessageDeliveryStatus>(),
                PayloadType = GenerateEnum<ChatMessageType>(),
                ReceivedTimeUtc = GenerateDate(),
                SentTimeUtc = GenerateDate(),
            };

            switch (message.PayloadType)
            {
                case ChatMessageType.Unknown:
                case ChatMessageType.Deleted:
                case ChatMessageType.Block:
                case ChatMessageType.Unblock:
                    break;
                case ChatMessageType.Text:
                case ChatMessageType.Picture:
                    message.Payload = GenerateArbitraryString();
                    break;
                case ChatMessageType.Location:
                    message.Payload = new Location2D(GenerateCoordinate(), GenerateCoordinate());
                    break;
                default:
                    throw new InvalidOperationException($"Can't generate a payload for type {message.PayloadType}");
            }

            return message;
        }

        private string GenerateMessageId()
        {
            if (_rand.Next(10) == 0)
            {
                return GenerateArbitraryString() + Guid.NewGuid();
            }
            else
            {
                return Guid.NewGuid().ToString();
            }
        }

        private string GenerateArbitraryString()
        {
            var len = _rand.Next(50);
            len = len * len;

            var sb = new StringBuilder(len * 3);
            for (var i = 0; i < len; i++)
            {
                sb.Append((char)_rand.Next(1, 0xd800));
                sb.Append(char.ConvertFromUtf32(_rand.Next(0x10000, 0x2FFFF)));
            }

            return sb.ToString();
        }

        private double GenerateCoordinate()
        {
            return _rand.NextDouble() * 180 - 90;
        }

        private bool GenerateBoolean()
        {
            return _rand.Next(2) == 0;
        }

        private string GenerateAccountId()
        {
            return _rand.Next(int.MaxValue).ToString();
        }

        private DateTime? GenerateDate()
        {
            if (_rand.Next(10) == 0)
                return default(DateTime?);

            return DateTime.UtcNow + TimeSpan.FromDays(_rand.NextDouble() * 1000);
        }

        private T GenerateEnum<T>()
        {
            var all = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            return all[_rand.Next(all.Count())];
        }
    }
}
