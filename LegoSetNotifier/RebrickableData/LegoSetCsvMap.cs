using CsvHelper;

namespace LegoSetNotifier.RebrickableData
{
    [CsvRecordMap(typeof(LegoSet))]
    public partial class LegoSetCsvMap
    {
        [CsvColumn("set_num")]
        public string ExtendedSetNumber { get; set; } = string.Empty;

        [CsvColumn("name")]
        public string Name { get; set; } = string.Empty;

        [CsvColumn("year")]
        public int ReleaseYear { get; set; } = 0;

        [CsvColumn("theme_id")]
        public int ThemeId { get; set; } = 0;

        [CsvColumn("num_parts")]
        public int NumberOfParts { get; set; } = 0;

        [CsvColumn("img_url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
