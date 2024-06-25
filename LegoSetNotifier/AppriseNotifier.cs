using LegoSetNotifier.AppriseApi;
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
        private IAppriseApiClient apiClient;
        private string notifyKey;

        public AppriseNotifier(IAppriseApiClient apiClient, string notifyKey)
        {
            this.apiClient = apiClient;
            this.notifyKey = notifyKey;
        }

        public async Task SendErrorNotificationAsync(string message, Exception? ex)
        {
            var notification = new AppriseApiNotifyContent()
            {
                Type = "failure",
                Title = message,
                Body = $"{message}\n\n{ex}",
            };

            await this.apiClient.NotifyAsync(this.notifyKey, notification);
        }

        public async Task SendNewSetNotificationAsync(RebrickableData.LegoSet legoSet)
        {
            var notification = new AppriseApiNotifyContent()
            {
                Title = $"New LEGO set {legoSet.Name}",
                Format = "markdown",
                Body = $"A new LEGO set {legoSet.ExtendedSetNumber} {legoSet.Name} is posted: [Rebrickable]({legoSet.GetRebrickableUrl()}), [LEGO Shop]({legoSet.GetLegoShopUrl()})",
                Attach = legoSet.ImageUrl,
            };

            try
            {
                await this.apiClient.NotifyAsync(this.notifyKey, notification);
            }
            catch (AppriseApiException ex) when (ex.ErrorMessage.Equals("Bad Attachment", StringComparison.Ordinal))
            {
                // A very special case: if the notification failed due to "Bad Attachment" then retry without it.
                notification.Body += $"\n\nA bad-attachment error was encountered for {notification.Attach}";
                notification.Attach = null;

                await this.apiClient.NotifyAsync(this.notifyKey, notification);
            }
        }
    }
}
