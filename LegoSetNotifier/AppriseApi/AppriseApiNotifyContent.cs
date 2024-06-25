using System.Text.Json.Serialization;

namespace LegoSetNotifier.AppriseApi
{
    public class AppriseApiNotifyContent
    {
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "info";

        [JsonPropertyName("tag")]
        public string Tag { get; set; } = "all";

        [JsonPropertyName("format")]
        public string Format { get; set; } = "text";

        [JsonPropertyName("attach")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Attach { get; set; } = null;
    }
}
