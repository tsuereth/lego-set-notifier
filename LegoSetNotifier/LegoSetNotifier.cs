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

        private bool isInitializingSeenData = false;

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
            if (seenSets.Count == 0)
            {
                this.logger.LogInformation("Seen data is empty, will initialize it with from-scratch notification settings");
                this.isInitializingSeenData = true;
            }

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

        public async Task SendNotificationsAsync(int releaseYearForNewSets)
        {
            if (this.notifier == null)
            {
                return;
            }

            var seenSets = await this.seenData.GetSetsAsync();
            var notYetNotifiedSets = seenSets.Select(p => p.Value).Where(s => s.NotifiedAtTime == null);
            if (!notYetNotifiedSets.Any())
            {
                return;
            }

            var oldOrNonPurchaseableSets = notYetNotifiedSets.Where(s => s.ReleaseYear < releaseYearForNewSets || !s.IsPurchaseableSet());
            var newPurchaseableSets = notYetNotifiedSets.Where(s => s.ReleaseYear >= releaseYearForNewSets && s.IsPurchaseableSet());

            if (this.isInitializingSeenData)
            {
                // Older sets, and newer non-purchaseable sets, should be notified in a simple summary.
                // Newer and purchaseable sets will be collected in digest notifications.

                if (oldOrNonPurchaseableSets.Any())
                {
                    await this.BuildAndSendLegoSetNotificationsAsync(LegoSetBatchNotification.SummaryOnlySettings("Old or Non-Purchaseable Sets (Initialization Summary)"), oldOrNonPurchaseableSets);
                }

                if (newPurchaseableSets.Any())
                {
                    await this.BuildAndSendLegoSetNotificationsAsync(LegoSetBatchNotification.DigestSettings($"New Sets (Initialization Digest)"), newPurchaseableSets);
                }
            }
            else
            {
                // Older sets, and newer non-purchaseable sets, should be notified in limited digests.
                // Newer and purchaseable sets will be included in full-detail notifications with image attachments.

                if (oldOrNonPurchaseableSets.Any())
                {
                    await this.BuildAndSendLegoSetNotificationsAsync(LegoSetBatchNotification.DigestSettings("Old or Non-Purchaseable Sets"), oldOrNonPurchaseableSets);
                }

                if (newPurchaseableSets.Any())
                {
                    await this.BuildAndSendLegoSetNotificationsAsync(LegoSetBatchNotification.FullDetailSettings("New Sets"), newPurchaseableSets);
                }
            }
        }

        private async Task BuildAndSendLegoSetNotificationsAsync(LegoSetBatchNotification.NotificationSettings settings, IEnumerable<RebrickableData.LegoSet> legoSets)
        {
            if (this.notifier == null)
            {
                return;
            }

            var batches = LegoSetBatchNotification.BuildNotifications(this.notifier, settings, legoSets);
            foreach (var notification in batches)
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
                            "Sent a {NotificationLabel} notification for {NumberOfSets} sets as of {NotifiedAtTime}",
                            settings.Label,
                            notifiedSetNumbers.Count,
                            notifiedAtTime);
                    }
                    else
                    {
                        this.logger.LogError(
                            "Failed to send a {NotificationLabel} notification for {NumberOfSets} sets",
                            settings.Label,
                            notification.GetLegoSetNumbers().Count);
                    }
                }
                catch (Exception ex)
                {
                    const string exceptionErrorMessage = "Exception sending a notification";
                    this.logger.LogError(ex, exceptionErrorMessage);
                    await notifier.SendErrorNotificationAsync($"An exception occurred while attempting to send a {settings.Label} notification for {notification.GetLegoSetNumbers().Count} sets", ex);
                }
            }
        }
    }
}
