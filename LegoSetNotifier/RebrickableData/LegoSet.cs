using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LegoSetNotifier.RebrickableData
{
    public class LegoSet
    {
        private static readonly Regex PurchaseableSetNumberPattern = new Regex(@"^[0-9]{5}$", RegexOptions.Compiled);

        [JsonPropertyName("ExtendedSetNumber")]
        public string ExtendedSetNumber { get; set; } = string.Empty;

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("ReleaseYear")]
        public int ReleaseYear { get; set; } = 0;

        [JsonPropertyName("ThemeId")]
        public int ThemeId { get; set; } = 0;

        [JsonPropertyName("NumberOfParts")]
        public int NumberOfParts { get; set; } = 0;

        [JsonPropertyName("ImageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        public LegoSet() { }

        public LegoSet(LegoSet legoSet)
        {
            this.ExtendedSetNumber = legoSet.ExtendedSetNumber;
            this.Name = legoSet.Name;
            this.ReleaseYear = legoSet.ReleaseYear;
            this.ThemeId = legoSet.ThemeId;
            this.NumberOfParts = legoSet.NumberOfParts;
            this.ImageUrl = legoSet.ImageUrl;
        }

        public string GetShortSetNumber()
        {
            return this.ExtendedSetNumber.Split('-')[0];
        }

        public bool IsPurchaseableSet()
        {
            // As of writing, there isn't a discrete data point separating purchaseable LEGO sets
            // from non-purchasable bonus items or transmedia merch (like backpacks).
            // But, all purchaseable sets do have a five-digit number!
            return PurchaseableSetNumberPattern.IsMatch(this.GetShortSetNumber());
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
