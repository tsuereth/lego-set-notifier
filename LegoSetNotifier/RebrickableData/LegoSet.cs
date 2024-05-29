namespace LegoSetNotifier.RebrickableData
{
    public class LegoSet
    {
        public string ExtendedSetNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int ReleaseYear { get; set; } = 0;

        public int ThemeId { get; set; } = 0;

        public int NumberOfParts { get; set; } = 0;

        public string ImageUrl { get; set; } = string.Empty;

        public string GetShortSetNumber()
        {
            return this.ExtendedSetNumber.Split('-')[0];
        }

        public Uri GetLegoShopUrl()
        {
            var shortSetNumber = this.GetShortSetNumber();
            var urlString = $"https://lego.com/en-us/product/{shortSetNumber}";

            return new Uri(urlString);
        }

        public Uri GetRebrickableUrl()
        {
            var urlString = $"https://rebrickable.com/sets/{this.ExtendedSetNumber}/";

            return new Uri(urlString);
        }
    }
}
