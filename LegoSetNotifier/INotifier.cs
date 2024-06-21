namespace LegoSetNotifier
{
    public interface INotifier
    {
        public Task SendErrorNotificationAsync(string message, Exception? ex);

        public Task SendNewSetNotificationAsync(RebrickableData.LegoSet legoSet);
    }
}
