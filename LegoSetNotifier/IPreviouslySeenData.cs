namespace LegoSetNotifier
{
    public interface IPreviouslySeenData
    {
        public string GetDataSourceName();

        public Task<DateTimeOffset> GetUpdatedTimeAsync();

        public Task<Dictionary<string, RebrickableData.LegoSet>> GetSetsAsync();

        public Task UpdateSetsAsync(DateTimeOffset updatedTime, Dictionary<string, RebrickableData.LegoSet> legoSets);
    }
}
