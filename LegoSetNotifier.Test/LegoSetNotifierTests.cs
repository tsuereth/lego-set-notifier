using LegoSetNotifier.RebrickableData;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LegoSetNotifier.Test
{
    [TestClass]
    public class LegoSetNotifierTests
    {
        [TestMethod]
        public async Task TestNoDataNoErrorsAsync()
        {
            var logger = new TestLogger();

            var testLastUpdatedTime = DateTimeOffset.MinValue;
            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, PreviouslySeenLegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>()));
            mockClient.GetSetsUpdatedTime().Returns(DateTimeOffset.UtcNow);

            var mockNotifier = Substitute.For<INotifier>();

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            await mockData.Received().UpdateSetsAsync(Arg.Is<Dictionary<string, PreviouslySeenLegoSet>>(l => l.Count == 0));

            await legoSetNotifier.SendNewSetNotificationsAsync();

            await mockNotifier.DidNotReceive().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Any<Exception?>());
            await mockNotifier.DidNotReceive().SendNewSetsNotificationAsync(Arg.Any<IEnumerable<LegoSet>>());

            await mockData.DidNotReceive().MarkSetsAsNotifiedAsync(Arg.Any<DateTimeOffset>(), Arg.Any<HashSet<string>>());

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(0, logger.GetLogs(LogLevel.Error).Count());
        }

        [TestMethod]
        public async Task TestNewSetNotificationAsync()
        {
            var testLegoSet = new LegoSet()
            {
                ExtendedSetNumber = "12345-1",
            };

            var logger = new TestLogger();

            var testLastUpdatedTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, PreviouslySeenLegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));
            mockClient.GetSetsUpdatedTime().Returns(DateTimeOffset.UtcNow);

            var mockNotifier = Substitute.For<INotifier>();
            mockNotifier.SendNewSetsNotificationAsync(Arg.Any<IEnumerable<LegoSet>>()).Returns(
                x => Task.FromResult<HashSet<string>>(x.Arg<IEnumerable<LegoSet>>().Select(a => a.ExtendedSetNumber).ToHashSet()));

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            await mockData.Received().UpdateSetsAsync(Arg.Is<Dictionary<string, PreviouslySeenLegoSet>>(l => l.Count == 1 && l.ContainsKey(testLegoSet.ExtendedSetNumber)));

            await legoSetNotifier.SendNewSetNotificationsAsync();

            await mockNotifier.DidNotReceive().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Any<Exception?>());
            await mockNotifier.Received().SendNewSetsNotificationAsync(Arg.Is<IEnumerable<LegoSet>>(l => l.Count() == 1 &&
                l.ElementAt(0).ExtendedSetNumber.Equals(testLegoSet.ExtendedSetNumber, StringComparison.Ordinal)));

            await mockData.Received().MarkSetsAsNotifiedAsync(Arg.Any<DateTimeOffset>(), Arg.Is<HashSet<string>>(s => s.Count == 1 && s.Contains(testLegoSet.ExtendedSetNumber)));

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(0, logger.GetLogs(LogLevel.Error).Count());
        }

        [TestMethod]
        public async Task TestNewNonPurchaseableSetNoNotificationAsync()
        {
            var testLegoSet = new LegoSet()
            {
                // Indicate the set is non-purchaseable with a set number > 5 digits.
                ExtendedSetNumber = "1234567890-1",
            };

            Assert.IsFalse(testLegoSet.IsPurchaseableSet());

            var logger = new TestLogger();

            var testLastUpdatedTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, PreviouslySeenLegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));
            mockClient.GetSetsUpdatedTime().Returns(DateTimeOffset.UtcNow);

            var mockNotifier = Substitute.For<INotifier>();

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            await mockData.Received().UpdateSetsAsync(Arg.Is<Dictionary<string, PreviouslySeenLegoSet>>(l => l.Count == 1 && l.ContainsKey(testLegoSet.ExtendedSetNumber)));

            await legoSetNotifier.SendNewSetNotificationsAsync();

            await mockNotifier.DidNotReceive().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Any<Exception?>());
            await mockNotifier.DidNotReceive().SendNewSetsNotificationAsync(Arg.Any<IEnumerable<LegoSet>>());

            await mockData.DidNotReceive().MarkSetsAsNotifiedAsync(Arg.Any<DateTimeOffset>(), Arg.Any<HashSet<string>>());

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(0, logger.GetLogs(LogLevel.Error).Count());
        }

        [TestMethod]
        public async Task TestExceptionUpdatingDataAsync()
        {
            var testLegoSet = new LegoSet()
            {
                ExtendedSetNumber = "12345-1",
            };

            var logger = new TestLogger();

            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, PreviouslySeenLegoSet>()));
            mockData.UpdateSetsAsync(Arg.Any<Dictionary<string, PreviouslySeenLegoSet>>()).ThrowsAsync(new InvalidOperationException("TestException"));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));
            mockClient.GetSetsUpdatedTime().Returns(DateTimeOffset.UtcNow);

            var mockNotifier = Substitute.For<INotifier>();

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);

            // This should throw an exception, which the real program should handle by logging ... or notifying!
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            {
                await legoSetNotifier.DetectNewSetsAsync();
            });
        }

        [TestMethod]
        public async Task TestExceptionSendingNotificationAsync()
        {
            var testLegoSet = new LegoSet()
            {
                ExtendedSetNumber = "12345-1",
            };

            var logger = new TestLogger();

            var testLastUpdatedTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, PreviouslySeenLegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));
            mockClient.GetSetsUpdatedTime().Returns(DateTimeOffset.UtcNow);

            var mockNotifier = Substitute.For<INotifier>();
            mockNotifier.SendNewSetsNotificationAsync(Arg.Any<IEnumerable<LegoSet>>()).ThrowsAsync(new InvalidOperationException("TestException"));

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            await mockData.Received().UpdateSetsAsync(Arg.Is<Dictionary<string, PreviouslySeenLegoSet>>(l => l.Count == 1 && l.ContainsKey(testLegoSet.ExtendedSetNumber)));

            await legoSetNotifier.SendNewSetNotificationsAsync();

            Assert.IsInstanceOfType<InvalidOperationException>(logger.GetLogs(LogLevel.Error)[0].Exception);
            await mockNotifier.Received().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Is<Exception?>(ex => ex != null && ex.Message.Equals("TestException", StringComparison.Ordinal)));

            await mockData.DidNotReceive().MarkSetsAsNotifiedAsync(Arg.Any<DateTimeOffset>(), Arg.Any<HashSet<string>>());

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(1, logger.GetLogs(LogLevel.Error).Count());
        }

        [TestMethod]
        public async Task TestExceptionSendingNotificationAfterSomeUpdatesAsync()
        {
            var testLastUpdatedTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
            var testPreviouslySeenLegoSets = new List<PreviouslySeenLegoSet>()
            {
                new PreviouslySeenLegoSet(new LegoSet() { ExtendedSetNumber = "12345-1" }, testLastUpdatedTime) { NotifiedAtTime = testLastUpdatedTime },
                new PreviouslySeenLegoSet(new LegoSet() { ExtendedSetNumber = "23456-1" }, testLastUpdatedTime) { NotifiedAtTime = testLastUpdatedTime },
            };
            var testNewlySeenLegoSets = new List<LegoSet>()
            {
                new LegoSet() { ExtendedSetNumber = testPreviouslySeenLegoSets[0].ExtendedSetNumber },
                new LegoSet() { ExtendedSetNumber = testPreviouslySeenLegoSets[1].ExtendedSetNumber },
                new LegoSet() { ExtendedSetNumber = "34567-1" },
            };

            var logger = new TestLogger();

            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetSetsAsync().Returns(Task.FromResult(testPreviouslySeenLegoSets.ToDictionary(s => s.ExtendedSetNumber, s => s)));

            var newSetsSeenTime = DateTimeOffset.UtcNow;
            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsAsync().Returns(Task.FromResult(testNewlySeenLegoSets));
            mockClient.GetSetsUpdatedTime().Returns(newSetsSeenTime);

            // Simulate that the not-before-seen set's notification fails.
            var testNotificationException = new HttpRequestException("TestNotificationException");
            var mockNotifier = Substitute.For<INotifier>();
            mockNotifier.SendNewSetsNotificationAsync(
                Arg.Is<IEnumerable<LegoSet>>(l => l.Count() == 1 &&
                    l.ElementAt(0).ExtendedSetNumber.Equals(testNewlySeenLegoSets[2].ExtendedSetNumber, StringComparison.Ordinal)))
                .ThrowsAsync(testNotificationException);

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            await mockData.Received().UpdateSetsAsync(Arg.Is<Dictionary<string, PreviouslySeenLegoSet>>(
                l => l.Count == 3 &&
                l.ContainsKey(testPreviouslySeenLegoSets[0].ExtendedSetNumber) &&
                l.ContainsKey(testPreviouslySeenLegoSets[1].ExtendedSetNumber) &&
                l.ContainsKey(testNewlySeenLegoSets[2].ExtendedSetNumber)));

            await legoSetNotifier.SendNewSetNotificationsAsync();

            await mockNotifier.Received().SendNewSetsNotificationAsync(Arg.Is<IEnumerable<LegoSet>>(l => l.Count() == 1 &&
                l.ElementAt(0).ExtendedSetNumber.Equals(testNewlySeenLegoSets[2].ExtendedSetNumber, StringComparison.Ordinal)));
            await mockNotifier.Received().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Is<Exception>(testNotificationException));

            await mockData.DidNotReceive().MarkSetsAsNotifiedAsync(Arg.Any<DateTimeOffset>(), Arg.Any<HashSet<string>>());

            Assert.AreEqual(1, logger.GetLogs(LogLevel.Error).Count());
            Assert.IsInstanceOfType<HttpRequestException>(logger.GetLogs(LogLevel.Error)[0].Exception);
        }
    }
}
