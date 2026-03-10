using LegoSetNotifier.AppriseApi;
using LegoSetNotifier.RebrickableData;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LegoSetNotifier
{
    public class AppriseNotifier : INotifier
    {
        // This implementation ASSUMES notifications will be sent to a Gmail inbox.
        // Gmail limits a recipient to receiving at most 60 messages per minute.
        // Source: https://support.google.com/a/answer/1366776
        // (This limitation allows sub-minute bursts, but will eventually throttle/block new messages.)
        // This notifier uses a cooldown time between notifications to avoid breaching that limit.
        public TimeSpan NotificationCooldownTime { get; set; } = DefaultNotificationCooldownTime;

        // Since Gmail's receive-rate limit is sender-agnostic (!), the default cooldown attempts to
        // constrain this client to a MUCH looser threshold than the theoretical maximum rate.
        public static readonly TimeSpan DefaultNotificationCooldownTime = TimeSpan.FromSeconds(3);

        private static DateTimeOffset lastRequestTime = DateTimeOffset.MinValue;

        private IAppriseApiClient apiClient;
        private string notifyKey;

        public AppriseNotifier(IAppriseApiClient apiClient, string notifyKey)
        {
            this.apiClient = apiClient;
            this.notifyKey = notifyKey;
        }

        public uint GetMaxNotificationBodyChars()
        {
            return AppriseApiClient.MaxBodyChars;
        }

        public uint GetMaxNotificationAttachments()
        {
            return AppriseApiClient.MaxAttachments;
        }

        public async Task<bool> SendErrorNotificationAsync(string message, Exception? ex)
        {
            var notification = new AppriseApiNotifyContent()
            {
                Type = "failure",
                Title = message,
                Body = $"{message}\n\n{ex}",
            };

            return await this.SendThrottledNotificationAsync(notification);
        }

        public async Task<bool> SendLegoSetBatchNotificationAsync(LegoSetBatchNotification notification)
        {
            var notificationContent = notification.GetContent();
            try
            {
                return await this.SendThrottledNotificationAsync(notificationContent);
            }
            catch (AppriseApiException ex) when (ex.ErrorMessage.Equals("Bad Attachment", StringComparison.Ordinal))
            {
                // A very special case: if the notification failed due to "Bad Attachment" then retry without attachments.
                // But FIRST, send an error notification to indicate this failure.
                var setNumbersString = string.Join(", ", notification.GetLegoSetNumbers());
                var attachmentErrorNotification = new AppriseApiNotifyContent()
                {
                    Type = "failure",
                    Title = $"Lego Set Batch Notification error: Bad Attachment(s)",
                    Body = $"Error {ex.ErrorMessage} from Apprise API, will retry with attachments stripped.\n\n",
                };
                foreach (var attachment in notificationContent.Attachments)
                {
                    attachmentErrorNotification.Body += $"\n- {attachment}";
                }
                attachmentErrorNotification.Body += $"Affected set numbers: {setNumbersString}\n\n{ex}\n\n";
                await this.SendThrottledNotificationAsync(attachmentErrorNotification);

                // Now, retry the original notification without attachments.
                notificationContent.Attachments.Clear();
                return await this.SendThrottledNotificationAsync(notificationContent);
            }
        }

        private async Task<bool> SendThrottledNotificationAsync(AppriseApiNotifyContent notification)
        {
            var timeSinceLastRequest = DateTimeOffset.UtcNow - lastRequestTime;
            if (timeSinceLastRequest < NotificationCooldownTime)
            {
                var delayTime = NotificationCooldownTime - timeSinceLastRequest;
                await Task.Delay(delayTime);
            }

            var succeeded = false;
            try
            {
                await this.apiClient.NotifyAsync(this.notifyKey, notification);
                succeeded = true;
            }
            finally
            {
                lastRequestTime = DateTimeOffset.UtcNow;
            }

            return succeeded;
        }
    }
}
