using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShyGuy.ChatFile.Tests
{
    [TestClass]
    public class FilenameTests
    {
        struct FilenameTestCase
        {
            public readonly string Filename;
            public readonly string AccountId;
            public readonly string CounterpartyId;

            public FilenameTestCase(string input, string expectedAccountId, string expectedCounterpartyId)
            {
                Filename = input;
                AccountId = expectedAccountId;
                CounterpartyId = expectedCounterpartyId;
            }
        }

        [TestMethod]
        public void VerifyFilenameParser()
        {
            var testCases = new FilenameTestCase[]
            {
                // Relative & absolute paths
                new FilenameTestCase("g_1~2.shyguy-buddy", "1", "2"),
                new FilenameTestCase(@"c:\user\blah\blah\g_1~2.shyguy-buddy", "1", "2"),
                new FilenameTestCase(@"\g_1~2.shyguy-buddy", "1", "2"),
                new FilenameTestCase(@"\\server\share\g_1~2.shyguy-buddy", "1", "2"),

                // Different kinds of account IDs
                new FilenameTestCase(@"g_111111111111~2222222222222.shyguy-buddy", "111111111111", "2222222222222"),
                new FilenameTestCase(@"g_0001~0.shyguy-buddy", "0001", "0"),

                // Bad prefix
                new FilenameTestCase(@"w_1~2.shyguy-buddy", null, null),

                // Bad suffix
                new FilenameTestCase(@"g_1~2.shyguy-buddy2", null, null),

                // Bad account id
                new FilenameTestCase(@"g_a~2.shyguy-buddy", null, null),
                
                // Bad delimiter
                new FilenameTestCase(@"g_a`2.shyguy-buddy", null, null),
            };

            foreach (var test in testCases)
            {
                string accountId;
                string counterpartyId;

                if (Deserializer.ParseFilename(test.Filename, out accountId, out counterpartyId))
                {
                    Assert.IsNotNull(test.AccountId, $"Expected ParseFilename to fail on input, but it returned true instead. Input: {test.Filename}");
                    Assert.IsNotNull(test.CounterpartyId);
                    Assert.AreEqual(test.AccountId, accountId, "Mismatched AccountId");
                    Assert.AreEqual(test.CounterpartyId, counterpartyId, "Mismatched CounterpartyId");
                }
                else
                {
                    Assert.IsNull(test.AccountId, $"Expected ParseFilename to succeed on input, but it returned false instead. Input: {test.Filename}");
                    Assert.IsNull(test.CounterpartyId);
                    Assert.IsNull(accountId);
                    Assert.IsNull(counterpartyId);
                }
            }
        }

        [TestMethod]
        public void VerifyFilenameGenerator()
        {
            var testCases = new FilenameTestCase[]
            {
                new FilenameTestCase("g_1~2.shyguy-buddy", "1", "2"),
                new FilenameTestCase(@"g_111111111111~2222222222222.shyguy-buddy", "111111111111", "2222222222222"),
                new FilenameTestCase(@"g_0001~0.shyguy-buddy", "0001", "0"),
            };

            foreach (var test in testCases)
            {
                var result = Serializer.CreateFilename(test.AccountId, test.CounterpartyId);
                Assert.AreEqual(test.Filename, result);
            }
        }

        [TestMethod]
        public void VerifyFilenameRoundtrip()
        {
            VerifyFilenameRoundtrip("1", "2");
            VerifyFilenameRoundtrip("00000", "999999999999999999999999999");
            VerifyFilenameRoundtrip("0", "0");
        }

        private static void VerifyFilenameRoundtrip(string accountId, string counterpartyId)
        {
            var filename = Serializer.CreateFilename(accountId, counterpartyId);

            string resultAccountId;
            string resultCounterpartyId;
            Assert.IsTrue(Deserializer.ParseFilename(filename, out resultAccountId, out resultCounterpartyId));

            Assert.AreEqual(accountId, resultAccountId, "AccountId didn't round-trip");
            Assert.AreEqual(counterpartyId, counterpartyId, "CounterpartyId didn't round-trip");
        }
    }
}
