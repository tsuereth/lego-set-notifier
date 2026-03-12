namespace LegoSetNotifier
{
    public interface IPreviouslySeenData
    {
        public string GetDataSourceName();

        public Task<Dictionary<string, PreviouslySeenLegoSet>> GetSetsAsync();

        public Task UpdateSetsAsync(Dictionary<string, PreviouslySeenLegoSet> legoSets);

        public Task MarkSetsAsNotifiedAsync(DateTimeOffset notifiedAtTime, ISet<string> legoSetNumbers);
    }
}
