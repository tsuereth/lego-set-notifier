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
        public async Task TestNotificationNoExceptionAsync()
        {
            var testContent = new AppriseApiNotifyContent()
            {
                Body = "Test Notification Content",
            };
            var testNotification = Substitute.For<LegoSetBatchNotification>();
            testNotification.GetContent().Returns(testContent);

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var success = await notifier.SendLegoSetBatchNotificationAsync(testNotification);
            Assert.IsTrue(success);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(testContent));
        }

        [TestMethod]
        public async Task TestNotificationBadAttachmentRetryAsync()
        {
            var testContent = new AppriseApiNotifyContent()
            {
                Body = "Test Notification Content",
                Attachments = new List<string>() { "ruhroh" },
            };
            var testNotification = Substitute.For<LegoSetBatchNotification>();
            testNotification.GetContent().Returns(testContent);

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Is<AppriseApiNotifyContent>(r => r.Attachments.Count > 0)).
                Returns(x => { throw new AppriseApiException("{\"error\": \"Bad Attachment\"}", new HttpRequestException()); });
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Is<AppriseApiNotifyContent>(r => r.Attachments.Count == 0)).
                Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            notifier.NotificationCooldownTime = TimeSpan.Zero;
            var success = await notifier.SendLegoSetBatchNotificationAsync(testNotification);
            Assert.IsTrue(success);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r =>
                r.Type.Equals("failure", StringComparison.Ordinal) &&
                r.Body.Contains("Bad Attachment", StringComparison.Ordinal)));
            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r =>
                r.Body.Equals(testContent.Body, StringComparison.Ordinal) &&
                r.Attachments.Count == 0));
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
