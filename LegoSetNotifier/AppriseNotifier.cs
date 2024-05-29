using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LegoSetNotifier
{
    public class AppriseNotifier : INotifier, IDisposable
    {
        private string notifyUrl;
        private HttpClient httpClient;
        private bool disposedValue;

        public AppriseNotifier(string notifyUrl)
        {
            this.notifyUrl = notifyUrl;
            this.httpClient = new HttpClient();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.httpClient.Dispose();
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task SendNewSetNotificationAsync(RebrickableData.LegoSet legoSet)
        {
            var apprisePayloadObject = new JsonObject()
            {
                ["type"] = "info",
                ["format"] = "markdown",
                ["title"] = $"New LEGO set {legoSet.Name}",
                ["body"] = $"A new LEGO set {legoSet.ExtendedSetNumber} {legoSet.Name} is posted: [Rebrickable]({legoSet.GetRebrickableUrl()}), [LEGO Shop]({legoSet.GetLegoShopUrl()})",
                ["tag"] = "all",
                ["attach"] = legoSet.ImageUrl,
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, this.notifyUrl))
            {
                var apprisePayloadString = JsonSerializer.Serialize(apprisePayloadObject);
                request.Content = new StringContent(apprisePayloadString, Encoding.UTF8, "application/json");

                var response = await this.httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
