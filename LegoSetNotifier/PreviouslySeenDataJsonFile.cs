using LegoSetNotifier.RebrickableData;
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

        [JsonPropertyName("sets")]
        public Dictionary<string, PreviouslySeenLegoSet>? Sets { get; set; } = null;

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

        public string GetDataSourceName()
        {
            return this.FilePath;
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
            // Ensure the filepath is writable before updating the current data.
            using (var fileStream = File.Open(this.FilePath, FileMode.Create, FileAccess.Write))
            {
                this.Sets = legoSets;

                await JsonSerializer.SerializeAsync(fileStream, this);
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

            using (var fileStream = File.Open(this.FilePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(fileStream, this);
            }
        }
    }
}
