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
            mockData.GetUpdatedTimeAsync().Returns(Task.FromResult(testLastUpdatedTime));
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, LegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.UtcNow));
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>()));

            var mockNotifier = Substitute.For<INotifier>();

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(0, logger.GetLogs(LogLevel.Error).Count());
            await mockNotifier.DidNotReceive().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Any<Exception?>());
            await mockNotifier.DidNotReceive().SendNewSetNotificationAsync(Arg.Any<LegoSet>());

            await mockData.Received().UpdateSetsAsync(Arg.Is<DateTimeOffset>(t => t > testLastUpdatedTime), Arg.Is<Dictionary<string, LegoSet>>(l => l.Count == 0));
        }

        [TestMethod]
        public async Task TestFirstTimeNoNotificationAsync()
        {
            var testLegoSet = new LegoSet()
            {
                ExtendedSetNumber = "12345-1",
            };

            var logger = new TestLogger();

            var testLastUpdatedTime = DateTimeOffset.MinValue;
            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.MinValue)); // Indicates "first-time" data, no notifications.
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, LegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.UtcNow));
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));

            var mockNotifier = Substitute.For<INotifier>();

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(0, logger.GetLogs(LogLevel.Error).Count());
            await mockNotifier.DidNotReceive().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Any<Exception?>());
            await mockNotifier.DidNotReceive().SendNewSetNotificationAsync(Arg.Any<LegoSet>());

            await mockData.Received().UpdateSetsAsync(Arg.Is<DateTimeOffset>(t => t > testLastUpdatedTime), Arg.Is<Dictionary<string, LegoSet>>(l => l.Count == 1 &&
                l.ContainsKey(testLegoSet.ExtendedSetNumber)));
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
            mockData.GetUpdatedTimeAsync().Returns(Task.FromResult(testLastUpdatedTime));
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, LegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.UtcNow));
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));

            var mockNotifier = Substitute.For<INotifier>();

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(0, logger.GetLogs(LogLevel.Error).Count());
            await mockNotifier.DidNotReceive().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Any<Exception?>());
            await mockNotifier.Received().SendNewSetNotificationAsync(Arg.Is<LegoSet>(s => testLegoSet.ExtendedSetNumber.Equals(s.ExtendedSetNumber, StringComparison.Ordinal)));

            await mockData.Received().UpdateSetsAsync(Arg.Is<DateTimeOffset>(t => t > testLastUpdatedTime), Arg.Is<Dictionary<string, LegoSet>>(l => l.Count == 1 &&
                l.ContainsKey(testLegoSet.ExtendedSetNumber)));
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
            mockData.GetUpdatedTimeAsync().Returns(Task.FromResult(testLastUpdatedTime));
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, LegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.UtcNow));
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));

            var mockNotifier = Substitute.For<INotifier>();

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(0, logger.GetLogs(LogLevel.Error).Count());
            await mockNotifier.DidNotReceive().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Any<Exception?>());
            await mockNotifier.DidNotReceive().SendNewSetNotificationAsync(Arg.Any<LegoSet>());

            await mockData.Received().UpdateSetsAsync(Arg.Is<DateTimeOffset>(t => t > testLastUpdatedTime), Arg.Is<Dictionary<string, LegoSet>>(l => l.Count == 1 &&
                l.ContainsKey(testLegoSet.ExtendedSetNumber)));
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
            mockData.GetUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.UtcNow - TimeSpan.FromHours(1)));
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, LegoSet>()));
            mockData.UpdateSetsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<Dictionary<string, LegoSet>>()).ThrowsAsync(new InvalidOperationException("TestException"));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.UtcNow));
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));

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
            mockData.GetUpdatedTimeAsync().Returns(Task.FromResult(testLastUpdatedTime));
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, LegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.UtcNow));
            mockClient.GetSetsAsync().Returns(Task.FromResult(new List<LegoSet>() { testLegoSet }));

            var mockNotifier = Substitute.For<INotifier>();
            mockNotifier.SendNewSetNotificationAsync(Arg.Any<LegoSet>()).ThrowsAsync(new InvalidOperationException("TestException"));

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            Assert.AreEqual(0, logger.GetLogs(LogLevel.Warning).Count());
            Assert.AreEqual(1, logger.GetLogs(LogLevel.Error).Count());
            Assert.IsInstanceOfType<InvalidOperationException>(logger.GetLogs(LogLevel.Error)[0].Exception);
            await mockNotifier.Received().SendErrorNotificationAsync(Arg.Any<string>(), Arg.Is<Exception?>(ex => ex != null && ex.Message.Equals("TestException", StringComparison.Ordinal)));

            await mockData.Received().UpdateSetsAsync(Arg.Is<DateTimeOffset>(t => t > testLastUpdatedTime), Arg.Is<Dictionary<string, LegoSet>>(l => l.Count == 1 &&
                l.ContainsKey(testLegoSet.ExtendedSetNumber)));
        }

        [TestMethod]
        public async Task TestExceptionSendingNotificationAfterSomeUpdatesAsync()
        {
            var testLegoSets = new List<LegoSet>()
            {
                new LegoSet() { ExtendedSetNumber = "12345-1" },
                new LegoSet() { ExtendedSetNumber = "23456-1" },
                new LegoSet() { ExtendedSetNumber = "34567-1" },
            };

            var logger = new TestLogger();

            var testLastUpdatedTime = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetUpdatedTimeAsync().Returns(Task.FromResult(testLastUpdatedTime));
            mockData.GetSetsAsync().Returns(Task.FromResult(new Dictionary<string, LegoSet>()));

            var mockClient = Substitute.For<IRebrickableDataClient>();
            mockClient.GetSetsUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.UtcNow));
            mockClient.GetSetsAsync().Returns(Task.FromResult(testLegoSets));

            // Simulate that some set notifications work, but not all of them.
            var mockNotifier = Substitute.For<INotifier>();
            mockNotifier.SendNewSetNotificationAsync(Arg.Is(testLegoSets[2])).ThrowsAsync(new HttpRequestException("TestNotificationException"));

            var legoSetNotifier = new LegoSetNotifier(logger, mockData, mockClient, mockNotifier);
            await legoSetNotifier.DetectNewSetsAsync();

            Assert.AreEqual(1, logger.GetLogs(LogLevel.Warning).Count()); // A warning is logged when sending a notification fails.
            Assert.AreEqual(1, logger.GetLogs(LogLevel.Error).Count());
            Assert.IsInstanceOfType<HttpRequestException>(logger.GetLogs(LogLevel.Error)[0].Exception);
            await mockNotifier.Received().SendNewSetNotificationAsync(Arg.Is<LegoSet>(s => testLegoSets[0].ExtendedSetNumber.Equals(s.ExtendedSetNumber, StringComparison.Ordinal)));
            await mockNotifier.Received().SendNewSetNotificationAsync(Arg.Is<LegoSet>(s => testLegoSets[1].ExtendedSetNumber.Equals(s.ExtendedSetNumber, StringComparison.Ordinal)));

            await mockData.Received().UpdateSetsAsync(Arg.Is(testLastUpdatedTime), Arg.Is<Dictionary<string, LegoSet>>(l => l.Count == 2 &&
                l.ContainsKey(testLegoSets[0].ExtendedSetNumber) &&
                l.ContainsKey(testLegoSets[1].ExtendedSetNumber) &&
                !l.ContainsKey(testLegoSets[2].ExtendedSetNumber)));
        }
    }
}
