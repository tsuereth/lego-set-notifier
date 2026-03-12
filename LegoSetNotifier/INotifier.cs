namespace LegoSetNotifier
{
    public interface INotifier
    {
        public uint GetMaxNotificationBodyChars();

        public uint GetMaxNotificationAttachments();

        public Task<bool> SendErrorNotificationAsync(string message, Exception? ex);

        public Task<bool> SendLegoSetBatchNotificationAsync(LegoSetBatchNotification notification);
    }
}
