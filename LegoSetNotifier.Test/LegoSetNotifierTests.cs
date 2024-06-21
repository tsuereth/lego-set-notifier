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

            var mockData = Substitute.For<IPreviouslySeenData>();
            mockData.GetDataSourceName().Returns("MockData");
            mockData.GetUpdatedTimeAsync().Returns(Task.FromResult(DateTimeOffset.MinValue));
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
        }

        [TestMethod]
        public async Task TestFirstTimeNoNotificationAsync()
        {
            var testLegoSet = new LegoSet()
            {
                ExtendedSetNumber = "12345-1",
            };

            var logger = new TestLogger();

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
        }

        [TestMethod]
        public async Task TestNewSetNotificationAsync()
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
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
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
            await legoSetNotifier.DetectNewSetsAsync();
        }

        [TestMethod]
        public async Task TestExceptionSendingNotificationAsync()
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
        }
    }
}
