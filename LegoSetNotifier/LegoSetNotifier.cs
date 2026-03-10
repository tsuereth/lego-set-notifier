using LegoSetNotifier.RebrickableData;
using Microsoft.Extensions.Logging;

namespace LegoSetNotifier
{
    public class LegoSetNotifier
    {
        private readonly ILogger logger;
        private readonly IPreviouslySeenData seenData;
        private readonly IRebrickableDataClient dataClient;
        private readonly INotifier? notifier;

        public LegoSetNotifier(
            ILogger logger,
            IPreviouslySeenData seenData,
            IRebrickableDataClient dataClient,
            INotifier? notifier)
        {
            this.logger = logger;
            this.seenData = seenData;
            this.dataClient = dataClient;
            this.notifier = notifier;
        }

        public async Task DetectNewSetsAsync()
        {
            var liveDataSetsList = await this.dataClient.GetSetsAsync();
            var liveDataSets = liveDataSetsList.ToDictionary(s => s.ExtendedSetNumber, s => s);
            var liveDataSeenAtTime = this.dataClient.GetSetsUpdatedTime();
            if (liveDataSeenAtTime == null)
            {
                throw new InvalidDataException("After retrieving current sets from live data, the updated time was null");
            }

            var seenSets = await this.seenData.GetSetsAsync();

            var newSetsCount = 0;
            foreach (var extendedSetNumber in liveDataSets.Keys)
            {
                if (!seenSets.ContainsKey(extendedSetNumber))
                {
                    ++newSetsCount;
                    var newSet = new PreviouslySeenLegoSet(liveDataSets[extendedSetNumber], liveDataSeenAtTime.Value);

                    this.logger.LogInformation(
                        "New {PurchaseabilityType} found in live data: {ExtendedSetNumber} {SetName}",
                        newSet.IsPurchaseableSet() ? "set" : "non-purchaseable set",
                        newSet.ExtendedSetNumber,
                        newSet.Name);

                    seenSets.Add(extendedSetNumber, newSet);
                }
            }

            this.logger.LogInformation(
                "Found {NumberOfNewSets} new sets in live data as of {LastUpdatedTime}",
                    newSetsCount,
                    liveDataSeenAtTime.Value);

            this.logger.LogDebug(
                "Updating data file at {DataSource}",
                this.seenData.GetDataSourceName());
            await this.seenData.UpdateSetsAsync(seenSets);
        }

        public async Task SendNewSetNotificationsAsync()
        {
            if (this.notifier == null)
            {
                return;
            }

            var seenSets = await this.seenData.GetSetsAsync();
            var notYetNotifiedSets = seenSets.Select(p => p.Value).Where(s => s.IsPurchaseableSet() && s.NotifiedAtTime == null);
            if (!notYetNotifiedSets.Any())
            {
                return;
            }

            var notifications = LegoSetBatchNotification.BuildNotifications(this.notifier, notYetNotifiedSets);
            foreach (var notification in notifications)
            {
                try
                {
                    var notificationSuccess = await this.notifier.SendLegoSetBatchNotificationAsync(notification);
                    if (notificationSuccess)
                    {
                        var notifiedAtTime = DateTimeOffset.UtcNow;
                        var notifiedSetNumbers = notification.GetLegoSetNumbers();
                        await this.seenData.MarkSetsAsNotifiedAsync(notifiedAtTime, notifiedSetNumbers);

                        this.logger.LogInformation(
                            "Sent notification(s) of {NumberOfNotifiedSets} new sets as of {NotifiedAtTime}",
                            notifiedSetNumbers.Count,
                            notifiedAtTime);
                    }
                    else
                    {
                        this.logger.LogError(
                            "Failed to send notification(s) for {NumberOfNotYetNotifiedSets} new sets",
                            notYetNotifiedSets.Count());
                    }
                }
                catch (Exception ex)
                {
                    const string exceptionErrorMessage = "Exception sending notification(s) of new sets";
                    this.logger.LogError(ex, exceptionErrorMessage);
                    await notifier.SendErrorNotificationAsync($"An exception occurred while attempting to send notification(s) of {notYetNotifiedSets.Count()} new sets", ex);
                }
            }
        }
    }
}
