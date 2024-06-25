namespace LegoSetNotifier.AppriseApi
{
    public interface IAppriseApiClient
    {
        public Task NotifyAsync(string configKey, AppriseApiNotifyContent notifyRequest);
    }
}
