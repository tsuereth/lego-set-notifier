using LegoSetNotifier.RebrickableData;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegoSetNotifier
{
    public class PreviouslySeenDataJsonFile : IPreviouslySeenData
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            IncludeFields = true,
        };

        private const uint ioExceptionRetryCount = 3;
        private static readonly TimeSpan ioExceptionRetryDelay = TimeSpan.FromMilliseconds(100);

        [JsonPropertyName("sets")]
        public Dictionary<string, PreviouslySeenLegoSet>? Sets { get; set; } = null;

        [JsonIgnore]
        private ILogger? logger = null;

        [JsonIgnore]
        private string filePath = string.Empty;

        public static async Task<PreviouslySeenDataJsonFile> FromFilePathAsync(
            ILogger logger,
            string filePath)
        {
            try
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    var data = await JsonSerializer.DeserializeAsync<PreviouslySeenDataJsonFile>(fileStream);
                    if (data == null)
                    {
                        throw new InvalidDataException($"Unexpected null result from JSON deserializing {filePath}");
                    }

                    data.logger = logger;
                    data.filePath = filePath;
                    return data;
                }
            }
            catch (FileNotFoundException)
            {
                // This is fine, just start fresh with empty data.
                var data = new PreviouslySeenDataJsonFile();
                data.logger = logger;
                data.filePath = filePath;

                return data;
            }
        }

        public string GetDataSourceName()
        {
            return this.filePath;
        }

        public Task<Dictionary<string, PreviouslySeenLegoSet>> GetSetsAsync()
        {
            if (this.Sets == null)
            {
                return Task.FromResult(new Dictionary<string, PreviouslySeenLegoSet>());
            }

            return Task.FromResult(this.Sets);
        }

        public async Task UpdateSetsAsync(Dictionary<string, PreviouslySeenLegoSet> legoSets)
        {
            for (var attempt = 0u; attempt < ioExceptionRetryCount; ++attempt)
            {
                try
                {
                    // Ensure the filepath is writable before updating the current data.
                    using (var fileStream = File.Open(this.filePath, FileMode.Create, FileAccess.Write))
                    {
                        this.Sets = legoSets;

                        await JsonSerializer.SerializeAsync(fileStream, this);
                        break;
                    }
                }
                catch (IOException ex)
                {
                    if ((attempt + 1) == ioExceptionRetryCount)
                    {
                        this.logger?.LogError(ex, "Writing updated sets encountered an IO exception, aborting after {AttemptCount} attempts", attempt + 1);
                        throw;
                    }
                    else
                    {
                        this.logger?.LogError(ex, "Writing updated sets encountered an IO exception, attempt {AttemptCount} will retry up to {AttemptMax}", attempt + 1, ioExceptionRetryCount);
                        await Task.Delay(ioExceptionRetryDelay);
                    }
                }
            }
        }

        public async Task MarkSetsAsNotifiedAsync(DateTimeOffset notifiedAtTime, ISet<string> legoSetNumbers)
        {
            if (this.Sets == null)
            {
                throw new InvalidDataException("Internal sets data was unexpectedly null");
            }

            foreach (var legoSetNumber in legoSetNumbers)
            {
                this.Sets[legoSetNumber].NotifiedAtTime = notifiedAtTime;
            }

            for (var attempt = 0u; attempt < ioExceptionRetryCount; ++attempt)
            {
                try
                {
                    using (var fileStream = File.Open(this.filePath, FileMode.Create, FileAccess.Write))
                    {
                        await JsonSerializer.SerializeAsync(fileStream, this);
                        break;
                    }
                }
                catch (IOException ex)
                {
                    if ((attempt + 1) == ioExceptionRetryCount)
                    {
                        this.logger?.LogError(ex, "Marking notified sets encountered an IO exception, aborting after {AttemptCount} attempts", attempt + 1);
                        throw;
                    }
                    else
                    {
                        this.logger?.LogError(ex, "Marking notified sets encountered an IO exception, attempt {AttemptCount} will retry up to {AttemptMax}", attempt + 1, ioExceptionRetryCount);
                        await Task.Delay(ioExceptionRetryDelay);
                    }
                }
            }
        }
    }
}
