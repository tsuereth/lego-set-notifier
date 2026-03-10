using System.Globalization;
using System.IO.Compression;
using CsvHelper;

namespace LegoSetNotifier.RebrickableData
{
    public class RebrickableDataClient : IRebrickableDataClient, IDisposable
    {
        // Source: https://rebrickable.com/downloads/
        private const string SetsCsvGzipUrl = "https://cdn.rebrickable.com/media/downloads/sets.csv.gz";

        private HttpClient httpClient;

        private byte[]? setsCsvBytes = null;
        private DateTimeOffset? setsCsvUpdatedTime = null;
        private bool disposedValue;

        public RebrickableDataClient()
        {
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

        private async Task<byte[]> GetSetsCsvBytesAsync()
        {
            if (this.setsCsvBytes == null)
            {
                var httpResponse = await this.httpClient.GetAsync(SetsCsvGzipUrl);
                httpResponse.EnsureSuccessStatusCode();

                using (var responseStream = await httpResponse.Content.ReadAsStreamAsync())
                using (var gunzipStream = new GZipStream(responseStream, CompressionMode.Decompress))
                using (var writeBytesStream = new MemoryStream())
                {
                    gunzipStream.CopyTo(writeBytesStream);
                    this.setsCsvBytes = writeBytesStream.ToArray();
                }

                this.setsCsvUpdatedTime = DateTimeOffset.UtcNow;
            }

            return this.setsCsvBytes;
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

        public DateTimeOffset? GetSetsUpdatedTime()
        {
            return this.setsCsvUpdatedTime;
        }

        public void FlushCache()
        {
            this.setsCsvBytes = null;
            this.setsCsvUpdatedTime = null;
        }
    }
}
