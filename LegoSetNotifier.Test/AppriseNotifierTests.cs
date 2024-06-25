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
        public async Task TestNewSetNotificationNoExceptionAsync()
        {
            var testLegoSet = new LegoSet()
            {
                Name = "Test Lego Set",
            };

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            await notifier.SendNewSetNotificationAsync(testLegoSet);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Title.Contains(testLegoSet.Name)));
        }

        [TestMethod]
        public async Task TestNewSetNotificationBadAttachmentRetryAsync()
        {
            var testLegoSet = new LegoSet()
            {
                Name = "Test Lego Set",
                ImageUrl = "ruhroh",
            };

            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Is<AppriseApiNotifyContent>(r => r.Attach != null)).
                Returns(x => { throw new AppriseApiException("{\"error\": \"Bad Attachment\"}", new HttpRequestException()); });
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Is<AppriseApiNotifyContent>(r => r.Attach == null)).
                Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            await notifier.SendNewSetNotificationAsync(testLegoSet);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Attach == null));
        }

        [TestMethod]
        public async Task TestErrorNotificationWithExAsync()
        {
            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            await notifier.SendErrorNotificationAsync("Test Error Message", new InvalidOperationException());

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Body.Contains("Test Error Message") && r.Body.Contains("InvalidOperationException")));
        }

        [TestMethod]
        public async Task TestErrorNotificationWithoutExAsync()
        {
            var mockApiClient = Substitute.For<IAppriseApiClient>();
            mockApiClient.NotifyAsync(Arg.Any<string>(), Arg.Any<AppriseApiNotifyContent>()).Returns(Task.CompletedTask);

            var notifier = new AppriseNotifier(mockApiClient, "testkey");
            await notifier.SendErrorNotificationAsync("Test Error Message", null);

            await mockApiClient.Received().NotifyAsync("testkey", Arg.Is<AppriseApiNotifyContent>(r => r.Body.Contains("Test Error Message")));
        }
    }
}
