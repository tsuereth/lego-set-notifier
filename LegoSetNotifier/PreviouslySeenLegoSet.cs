using LegoSetNotifier.RebrickableData;
using System.Text.Json.Serialization;

namespace LegoSetNotifier
{
    public class PreviouslySeenLegoSet : RebrickableData.LegoSet
    {
        [JsonPropertyName("SeenAtTime")]
        public DateTimeOffset? SeenAtTime { get; set; } = null;

        [JsonPropertyName("NotifiedAtTime")]
        public DateTimeOffset? NotifiedAtTime { get; set; } = null;

        public PreviouslySeenLegoSet() { }

        public PreviouslySeenLegoSet(RebrickableData.LegoSet legoSet, DateTimeOffset inSeenAtTime) : base(legoSet)
        {
            this.SeenAtTime = inSeenAtTime;
        }
    }
}
