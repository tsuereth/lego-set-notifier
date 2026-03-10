namespace LegoSetNotifier.RebrickableData
{
    public interface IRebrickableDataClient
    {
        public Task<List<LegoSet>> GetSetsAsync();

        public DateTimeOffset? GetSetsUpdatedTime();

        public void FlushCache();
    }
}
