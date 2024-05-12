namespace LegoSetNotifier.RebrickableData
{
    public interface IRebrickableDataClient
    {
        public Task<DateTimeOffset> GetSetsUpdatedTimeAsync();

        public Task<List<LegoSet>> GetSetsAsync();

        public void FlushCache();
    }
}
