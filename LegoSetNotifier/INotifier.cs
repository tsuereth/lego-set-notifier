namespace LegoSetNotifier
{
    public interface INotifier
    {
        public Task SendNewSetNotificationAsync(RebrickableData.LegoSet legoSet);
    }
}
