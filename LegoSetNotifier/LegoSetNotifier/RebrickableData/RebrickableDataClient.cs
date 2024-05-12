using System.Data.SqlTypes;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using AngleSharp;
using AngleSharp.Dom;
using CsvHelper;

namespace LegoSetNotifier.RebrickableData
{
    public class RebrickableDataClient : IRebrickableDataClient, IDisposable
    {
        private const string DownloadsPageUrl = "https://rebrickable.com/downloads/";

        private IConfiguration htmlConfig;
        private IBrowsingContext htmlContext;
        private HttpClient httpClient;

        private IDocument? downloadsPage = null;
        private byte[]? setsCsvBytes = null;
        private bool disposedValue;

        public RebrickableDataClient()
        {
            this.htmlConfig = Configuration.Default.WithDefaultLoader();
            this.htmlContext = BrowsingContext.New(htmlConfig);
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

        private async Task<IDocument> GetDownloadsPageAsync()
        {
            if (this.downloadsPage == null)
            {
                this.downloadsPage = await this.htmlContext.OpenAsync(DownloadsPageUrl);
            }

            return this.downloadsPage;
        }

        private Uri GetSetsDownloadUrl(IDocument downloadsPage)
        {
            var element = downloadsPage.All.Where(e =>
                e.LocalName.Equals("a", StringComparison.Ordinal) &&
                e.InnerHtml.Equals("sets.csv.gz", StringComparison.Ordinal)).FirstOrDefault();
            if (element == null)
            {
                throw new InvalidDataException($"Couldn't find sets download anchor element at {DownloadsPageUrl}");
            }

            var href = element.GetAttribute("href");
            if (href == null)
            {
                throw new InvalidDataException($"Missing href attribute in sets download anchor element at {DownloadsPageUrl}");
            }

            var url = new Uri(href);
            return url;
        }

        private async Task<byte[]> GetSetsCsvBytesAsync()
        {
            if (this.setsCsvBytes == null)
            {
                var page = await this.GetDownloadsPageAsync();
                var url = this.GetSetsDownloadUrl(page);

                var httpResponse = await this.httpClient.GetAsync(url);
                httpResponse.EnsureSuccessStatusCode();

                using (var responseStream = await httpResponse.Content.ReadAsStreamAsync())
                using (var gunzipStream = new GZipStream(responseStream, CompressionMode.Decompress))
                using (var writeBytesStream = new MemoryStream())
                {
                    gunzipStream.CopyTo(writeBytesStream);
                    this.setsCsvBytes = writeBytesStream.ToArray();
                }
            }

            return this.setsCsvBytes;
        }

        public async Task<DateTimeOffset> GetSetsUpdatedTimeAsync()
        {
            var page = await this.GetDownloadsPageAsync();
            var url = this.GetSetsDownloadUrl(page);

            // The URL is expected to have a timestamp in the querystring, like:
            // https://cdn.rebrickable.com/media/downloads/sets.csv.gz?1715448890.1912346
            var querystring = url.Query;
            if (querystring.Length < 1 || querystring[0] != '?')
            {
                throw new InvalidDataException($"Unexpected empty or malformed querystring in sets download anchor element href: {url}");
            }

            var timestampString = querystring.Substring(1);
            var timestampSeconds = float.Parse(timestampString, CultureInfo.InvariantCulture);
            var timestampMs = Convert.ToInt64(timestampSeconds * 1000.0);

            return DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
        }

        public async Task<List<LegoSet>> GetSetsAsync()
        {
            var csvBytes = await this.GetSetsCsvBytesAsync();

            using (var bytesStream = new MemoryStream(csvBytes))
            using (var streamReader = new StreamReader(bytesStream))
            using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
            {
                csvReader.Context.RegisterClassMap<LegoSetCsvMap>();
                var records = csvReader.GetRecords<LegoSet>();
                return records.ToList();
            }
        }

        public void FlushCache()
        {
            this.downloadsPage = null;
            this.setsCsvBytes = null;
        }
    }
}
