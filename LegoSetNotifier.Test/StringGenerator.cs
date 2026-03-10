namespace LegoSetNotifier.Test
{
    public class StringGenerator
    {
        private const string SourceStr =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            "0123456789";

        public static string Generate(uint length)
        {
            var random = new Random();
            var chars = Enumerable.Range(0, (int)length).Select(_ => SourceStr[random.Next(SourceStr.Length)]);
            return new string(chars.ToArray());
        }
    }
}
