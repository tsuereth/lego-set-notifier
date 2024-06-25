using System.Text;
using System.Text.Json;

namespace LegoSetNotifier.AppriseApi
{
    public class AppriseApiClient : IAppriseApiClient, IDisposable
    {
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
