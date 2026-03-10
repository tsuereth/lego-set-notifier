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

        [JsonIgnore]
        public List<string> Attachments { get; set; } = new List<string>();

        [JsonPropertyName("attach")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? AttachmentsSerialized
        {
            get
            {
                if (this.Attachments == null || this.Attachments.Count == 0)
                {
                    return null;
                }

                return this.Attachments;
            }

            set
            {
                if (value == null)
                {
                    this.Attachments = new List<string>();
                }
                else
                {
                    this.Attachments = value;
                }
            }
        }
    }
}
