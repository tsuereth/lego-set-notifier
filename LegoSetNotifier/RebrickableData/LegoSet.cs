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

        public bool IsPurchaseableSet()
        {
            // As of writing, there isn't a discrete data point separating purchaseable LEGO sets
            // from non-purchasable bonus items or transmedia merch (like backpacks).
            // But, all purchaseable sets do have a five-digit number!
            return this.GetShortSetNumber().Length == 5;
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
