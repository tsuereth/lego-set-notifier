using CsvHelper.Configuration;

namespace LegoSetNotifier.RebrickableData
{
    public class LegoSetCsvMap : ClassMap<LegoSet>
    {
        public LegoSetCsvMap()
        {
            Map(m => m.ExtendedSetNumber).Name("set_num");
            Map(m => m.Name).Name("name");
            Map(m => m.ReleaseYear).Name("year");
            Map(m => m.ThemeId).Name("theme_id");
            Map(m => m.NumberOfParts).Name("num_parts");
            Map(m => m.ImageUrl).Name("img_url");
        }
    }
}
