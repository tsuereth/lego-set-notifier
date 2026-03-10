using System.Globalization;
using System.Text.Json;

namespace LegoSetNotifier
{
    public class LocalFilesNotifier : INotifier
    {
        private JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
        };

        private string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "notifications");

        public LocalFilesNotifier(string notificationFilesDir)
        {
            this.baseDir = notificationFilesDir;

            if (!Directory.Exists(this.baseDir))
            {
                Directory.CreateDirectory(this.baseDir);
            }
        }

        public uint GetMaxNotificationBodyChars()
        {
            // Simulate Apprise API constraints.
            return AppriseApi.AppriseApiClient.MaxBodyChars;
        }

        public uint GetMaxNotificationAttachments()
        {
            // Simulate Apprise API constraints.
            return AppriseApi.AppriseApiClient.MaxAttachments;
        }

        public async Task<bool> SendErrorNotificationAsync(string message, Exception? ex)
        {
            var notification = new AppriseApi.AppriseApiNotifyContent()
            {
                Type = "failure",
                Title = message,
                Body = $"{message}\n\n{ex}",
            };

            return await this.WriteNotificationContentAsFileAsync(notification);
        }

        public async Task<bool> SendLegoSetBatchNotificationAsync(LegoSetBatchNotification notification)
        {
            return await this.WriteNotificationContentAsFileAsync(notification.GetContent());
        }

        private async Task<bool> WriteNotificationContentAsFileAsync(AppriseApi.AppriseApiNotifyContent content)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var filePath = Path.Combine(this.baseDir, nowMs.ToString(CultureInfo.InvariantCulture) + ".json");

            using (var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, content, this.serializerOptions);
            }

            return true;
        }
    }
}
