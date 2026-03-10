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
        // Apprise email notifications have a body size limit of 32k characters.
        // Source: https://appriseit.com/services/email/
        public const uint MaxBodyChars = 32768;

        // NOTE: The APPRISE_MAX_ATTACHMENTS setting is configurable by an Apprise API service.
        // This implementation assumes the target service uses a default initial setting value.
        public const uint MaxAttachments = 6;

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

        public async Task<HashSet<string>> SendNewSetsNotificationAsync(IEnumerable<RebrickableData.LegoSet> legoSets)
        {
            var successfullyNotifiedSetNumbers = new HashSet<string>();

            // Build notifications out of sets in batches, each batch within the maximum size of a notification body.
            var notificationSetNumbers = new HashSet<string>();
            var notificationBodyBuilder = new StringBuilder();
            var notificationAttachments = new List<string>();
            foreach (var legoSet in legoSets)
            {
                var legoSetBodyContent = $"\n- {legoSet.ExtendedSetNumber} {legoSet.Name} -- [Rebrickable]({legoSet.GetRebrickableUrl()}), [LEGO Shop]({legoSet.GetLegoShopUrl()})";
                if (legoSetBodyContent.Length >= MaxBodyChars)
                {
                    await this.SendErrorNotificationAsync($"Notification for set {legoSet.ExtendedSetNumber} cannot be sent, body size {legoSetBodyContent.Length} exceeds maximum {MaxBodyChars}", null);

                    // Assume that this is as good of a notification as this set is ever gonna get.
                    successfullyNotifiedSetNumbers.Add(legoSet.ExtendedSetNumber);
                    continue;
                }
                else if (
                    ((notificationBodyBuilder.Length + legoSetBodyContent.Length) >= MaxBodyChars) ||
                    (!string.IsNullOrEmpty(legoSet.ImageUrl) && (notificationAttachments.Count == MaxAttachments)))
                {
                    // This can't be added to the current batch; flush that batch, and start building a new one.
                    var batchSuccess = await this.SendBatchedSetsNotificationAsync(notificationSetNumbers, notificationBodyBuilder, notificationAttachments);
                    if (batchSuccess)
                    {
                        successfullyNotifiedSetNumbers.UnionWith(notificationSetNumbers);
                    }

                    // Reset the currently-building notification state.
                    notificationSetNumbers.Clear();
                    notificationBodyBuilder.Clear();
                    notificationAttachments.Clear();
                }

                notificationSetNumbers.Add(legoSet.ExtendedSetNumber);
                notificationBodyBuilder.Append(legoSetBodyContent);
                if (!string.IsNullOrEmpty(legoSet.ImageUrl))
                {
                    notificationAttachments.Add(legoSet.ImageUrl);
                }
            }

            if (notificationSetNumbers.Count > 0)
            {
                var batchSuccess = await this.SendBatchedSetsNotificationAsync(notificationSetNumbers, notificationBodyBuilder, notificationAttachments);
                if (batchSuccess)
                {
                    successfullyNotifiedSetNumbers.UnionWith(notificationSetNumbers);
                }
            }

            return successfullyNotifiedSetNumbers;
        }

        private async Task<bool> SendBatchedSetsNotificationAsync(HashSet<string> setNumbers, StringBuilder bodyBuilder, List<string> attachments)
        {
            var notification = new AppriseApiNotifyContent()
            {
                Title = $"{setNumbers.Count} new LEGO sets",
                Format = "markdown",
                Body = bodyBuilder.ToString(),
                Attachments = attachments,
            };

            try
            {
                return await this.SendThrottledNotificationAsync(notification);
            }
            catch (AppriseApiException ex) when (ex.ErrorMessage.Equals("Bad Attachment", StringComparison.Ordinal))
            {
                // A very special case: if the notification failed due to "Bad Attachment" then retry without attachments.
                notification.Body += "\n\nA bad-attachment error was encountered for one or more attachments:";
                foreach (var attachment in notification.Attachments)
                {
                    notification.Body += $"\n- {attachment}";
                }

                notification.Attachments.Clear();
                return await this.SendThrottledNotificationAsync(notification);
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
