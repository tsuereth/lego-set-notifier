using LegoSetNotifier.AppriseApi;
using LegoSetNotifier.RebrickableData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Net;

namespace LegoSetNotifier.Test
{
    [TestClass]
    public class AppriseNotifierTests
    {
        [TestMethod]
        public async Task TestNewSetsNotificationNoExceptionAsync()
        {
            var testLegoSet = new LegoSet()
            {
                Name = "Test Lego Set",
                ExtendedSetNumber = "12345-1",
            };

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var notifiedSetNumbers = await notifier.SendNewSetsNotificationAsync(new List<LegoSet>() { testLegoSet });
            Assert.HasCount(1, notifiedSetNumbers);
            Assert.Contains(testLegoSet.ExtendedSetNumber, notifiedSetNumbers);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Body.Contains(testLegoSet.Name)));
        }

        [TestMethod]
        public async Task TestNewSetsNotificationBadAttachmentRetryAsync()
        {
            var testLegoSet = new LegoSet()
            {
                Name = "Test Lego Set",
                ExtendedSetNumber = "12345-1",
                ImageUrl = "ruhroh",
            };

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Is<AppriseApiNotifyContent>(r => r.Attachments.Count > 0)).
                Returns(x => { throw new AppriseApiException("{\"error\": \"Bad Attachment\"}", new HttpRequestException()); });
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Is<AppriseApiNotifyContent>(r => r.Attachments.Count == 0)).
                Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var notifiedSetNumbers = await notifier.SendNewSetsNotificationAsync(new List<LegoSet>() { testLegoSet });
            Assert.HasCount(1, notifiedSetNumbers);
            Assert.Contains(testLegoSet.ExtendedSetNumber, notifiedSetNumbers);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Attachments.Count == 0));
        }

        [TestMethod]
        public async Task TestNewSetsNotificationMultipleSetsAsync()
        {
            var testLegoSet1 = new LegoSet()
            {
                Name = "Test Lego Set 1",
                ExtendedSetNumber = "00001-1",
            };
            var testLegoSet2 = new LegoSet()
            {
                Name = "Test Lego Set 2",
                ExtendedSetNumber = "00002-1",
            };

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var notifiedSetNumbers = await notifier.SendNewSetsNotificationAsync(new List<LegoSet>() { testLegoSet1, testLegoSet2 });
            Assert.HasCount(2, notifiedSetNumbers);
            Assert.Contains(testLegoSet1.ExtendedSetNumber, notifiedSetNumbers);
            Assert.Contains(testLegoSet2.ExtendedSetNumber, notifiedSetNumbers);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Body.Contains(testLegoSet1.Name) && r.Body.Contains(testLegoSet2.Name)));
        }

        [TestMethod]
        public async Task TestNewSetsNotificationMoreThanOneBatchAsync()
        {
            // To induce unusually-large notification bodies, inflate the sets' Name strings.
            var largeStringLength = (AppriseNotifier.MaxBodyChars / 3) + 1;
            var testLegoSet1 = new LegoSet()
            {
                Name = StringGenerator.Generate(largeStringLength),
                ExtendedSetNumber = "00001-1",
            };
            var testLegoSet2 = new LegoSet()
            {
                Name = StringGenerator.Generate(largeStringLength),
                ExtendedSetNumber = "00002-1",
            };
            var testLegoSet3 = new LegoSet()
            {
                Name = StringGenerator.Generate(largeStringLength),
                ExtendedSetNumber = "00003-1",
            };
            var testLegoSet4 = new LegoSet()
            {
                Name = StringGenerator.Generate(largeStringLength),
                ExtendedSetNumber = "00004-1",
            };

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var notifiedSetNumbers = await notifier.SendNewSetsNotificationAsync(new List<LegoSet>() { testLegoSet1, testLegoSet2, testLegoSet3, testLegoSet4 });
            Assert.HasCount(4, notifiedSetNumbers);
            Assert.Contains(testLegoSet1.ExtendedSetNumber, notifiedSetNumbers);
            Assert.Contains(testLegoSet2.ExtendedSetNumber, notifiedSetNumbers);
            Assert.Contains(testLegoSet3.ExtendedSetNumber, notifiedSetNumbers);
            Assert.Contains(testLegoSet4.ExtendedSetNumber, notifiedSetNumbers);

            // Assumption: the test data's content lengths should result in two batches (two notifications).
            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Body.Contains(testLegoSet1.Name) && r.Body.Contains(testLegoSet2.Name)));
            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Body.Contains(testLegoSet3.Name) && r.Body.Contains(testLegoSet4.Name)));
        }

        [TestMethod]
        public async Task TestNewSetsNotificationContentTooBigAsync()
        {
            var largeStringLength = AppriseNotifier.MaxBodyChars * 2;
            var testLegoSet = new LegoSet()
            {
                Name = StringGenerator.Generate(largeStringLength),
                ExtendedSetNumber = "12345-1",
            };

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var notifiedSetNumbers = await notifier.SendNewSetsNotificationAsync(new List<LegoSet>() { testLegoSet });
            Assert.HasCount(1, notifiedSetNumbers);
            Assert.Contains(testLegoSet.ExtendedSetNumber, notifiedSetNumbers);

            // The oversized data should result in a notification, but without that oversized data.
            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => !r.Body.Contains(testLegoSet.Name)));
        }

        [TestMethod]
        public async Task TestErrorNotificationWithExAsync()
        {
            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var sentError = await notifier.SendErrorNotificationAsync("Test Error Message", new InvalidOperationException());
            Assert.IsTrue(sentError);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Body.Contains("Test Error Message") && r.Body.Contains("InvalidOperationException")));
        }

        [TestMethod]
        public async Task TestErrorNotificationWithoutExAsync()
        {
            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var sentError = await notifier.SendErrorNotificationAsync("Test Error Message", null);
            Assert.IsTrue(sentError);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Body.Contains("Test Error Message")));
        }
    }
}
