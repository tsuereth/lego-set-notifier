using System.Text;
using System.Text.Json;

namespace LegoSetNotifier.AppriseApi
{
    public class AppriseApiClient : IAppriseApiClient, IDisposable
    {
        // Apprise email notifications have a body size limit of 32k characters.
        // Source: https://appriseit.com/services/email/
        public const uint MaxBodyChars = 32768;

        // NOTE: The APPRISE_MAX_ATTACHMENTS setting is configurable by an Apprise API service.
        // This implementation assumes the target service uses a default initial setting value.
        public const uint MaxAttachments = 6;

        private readonly string baseUrl;

        private HttpClient httpClient;
        private bool disposedValue = false;

        public AppriseApiClient(string baseUrl)
        {
            this.baseUrl = baseUrl;

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
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task NotifyAsync(string configKey, AppriseApiNotifyContent notifyRequest)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, $"{this.baseUrl}/notify/{configKey}"))
            {
                var apprisePayloadString = JsonSerializer.Serialize(notifyRequest);
                request.Content = new StringContent(apprisePayloadString, Encoding.UTF8, "application/json");

                var response = await this.httpClient.SendAsync(request);

                var responseString = await response.Content.ReadAsStringAsync();

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex) when (!string.IsNullOrEmpty(responseString))
                {
                    throw new AppriseApiException(responseString, ex);
                }
            }
        }
    }
}
