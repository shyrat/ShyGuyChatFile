using ShyGuy.ChatFile;
using System;

namespace ShyGuy.DumpChats
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: DumpShyGuyChats.exe <filename>");
                return;
            }

            var file = args[0];
            using (var stream = System.IO.File.OpenRead(file))
            {
                string accountId;
                string counterpartyId;
                if (!Deserializer.ParseFilename(file, out accountId, out counterpartyId))
                {
                    Console.WriteLine($"This file doesn't appear to have the correct naming convention. {file}");
                    return;
                }

                var messages = Deserializer.Deserialize(accountId, counterpartyId, stream);

                foreach (var message in messages)
                    PrintMessage(message);
            }
        }

        private static void PrintMessage(ChatMessage message)
        {
            Console.WriteLine(
                $"From: {message.Sender}\n" +
                $"To:   {message.Recipient}\n" +
                $"Sent: {message.SentTimeUtc}");

            switch (message.PayloadType)
            {
                case ChatMessageType.Unknown:
                default:
                    Console.WriteLine("Unknown message");
                    break;
                case ChatMessageType.Deleted:
                    Console.WriteLine("Message was deleted");
                    break;
                case ChatMessageType.Text:
                    Console.WriteLine($"Body: {message.Payload}");
                    break;
                case ChatMessageType.Picture:
                    Console.WriteLine($"Pic:  {message.Payload}");
                    break;
                case ChatMessageType.Location:
                    Console.WriteLine($"Geo:  {message.Payload}");
                    break;
                case ChatMessageType.Block:
                    Console.WriteLine("Blocked.");
                    break;
                case ChatMessageType.Unblock:
                    Console.WriteLine("Unblocked.");
                    break;
            }

            Console.WriteLine();
        }
    }
}
