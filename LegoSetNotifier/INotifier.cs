namespace LegoSetNotifier
{
    public interface INotifier
    {
        public Task<bool> SendErrorNotificationAsync(string message, Exception? ex);

        public Task<HashSet<string>> SendNewSetsNotificationAsync(IEnumerable<RebrickableData.LegoSet> legoSets);
    }
}
