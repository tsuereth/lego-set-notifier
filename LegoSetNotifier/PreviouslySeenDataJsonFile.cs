using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegoSetNotifier
{
    public class PreviouslySeenDataJsonFile : IPreviouslySeenData
    {
        [JsonPropertyName("updatedTime")]
        public DateTimeOffset? UpdatedTime { get; set; } = null;

        [JsonPropertyName("sets")]
        public Dictionary<string, RebrickableData.LegoSet>? Sets { get; set; } = null;

        [JsonIgnore]
        private string FilePath = string.Empty;

        public static async Task<PreviouslySeenDataJsonFile> FromFilePathAsync(string filePath)
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

                    data.FilePath = filePath;
                    return data;
                }
            }
            catch (FileNotFoundException)
            {
                // This is fine, just start fresh with empty data.
                var data = new PreviouslySeenDataJsonFile();
                data.FilePath = filePath;

                return data;
            }
        }

        public Task<DateTimeOffset> GetUpdatedTimeAsync()
        {
            if (!this.UpdatedTime.HasValue)
            {
                return Task.FromResult(DateTimeOffset.MinValue);
            }

            return Task.FromResult(this.UpdatedTime.Value);
        }

        public Task<Dictionary<string, RebrickableData.LegoSet>> GetSetsAsync()
        {
            if (this.Sets == null)
            {
                return Task.FromResult(new Dictionary<string, RebrickableData.LegoSet>());
            }

            return Task.FromResult(this.Sets);
        }

        public async Task UpdateSetsAsync(DateTimeOffset updatedTime, Dictionary<string, RebrickableData.LegoSet> legoSets)
        {
            // Ensure the filepath is writable before updating the current data.
            using (var fileStream = File.OpenWrite(this.FilePath))
            {
                this.UpdatedTime = updatedTime;
                this.Sets = legoSets;

                await JsonSerializer.SerializeAsync(fileStream, this);
            }
        }
    }
}
