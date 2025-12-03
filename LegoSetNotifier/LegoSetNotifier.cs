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
            var dataFirstTime = false;
            var seenDataUpdated = await this.seenData.GetUpdatedTimeAsync();
            if (seenDataUpdated == DateTimeOffset.MinValue)
            {
                this.logger.LogDebug(
                    "Data file at {DataSource} appears empty, skipping notifications for this first-time data check.",
                    this.seenData.GetDataSourceName());
                dataFirstTime = true;
            }

            var liveDataUpdated = await this.dataClient.GetSetsUpdatedTimeAsync();
            if (dataFirstTime || liveDataUpdated > seenDataUpdated)
            {
                var liveDataSetsList = await this.dataClient.GetSetsAsync();
                var liveDataSets = liveDataSetsList.ToDictionary(s => s.ExtendedSetNumber, s => s);
                var seenSets = new Dictionary<string, LegoSet>();

                var notificationsFailed = false;
                if (!dataFirstTime)
                {
                    seenSets = await this.seenData.GetSetsAsync();

                    var newSetsCount = 0;
                    foreach (var extendedSetNumber in liveDataSets.Keys)
                    {
                        if (!seenSets.ContainsKey(extendedSetNumber))
                        {
                            ++newSetsCount;
                            var newSet = liveDataSets[extendedSetNumber];

                            this.logger.LogInformation(
                                "New {PurchaseabilityType} found in live data: {ExtendedSetNumber} {SetName}",
                                newSet.IsPurchaseableSet() ? "set" : "non-purchaseable set",
                                newSet.ExtendedSetNumber,
                                newSet.Name);

                            if (!newSet.IsPurchaseableSet())
                            {
                                // Don't send notifications for non-purchaseable sets (like merch).
                                seenSets.Add(extendedSetNumber, newSet);
                                continue;
                            }

                            if (this.notifier != null)
                            {
                                try
                                {
                                    await this.notifier.SendNewSetNotificationAsync(newSet);
                                    seenSets.Add(extendedSetNumber, newSet);
                                }
                                catch (Exception ex)
                                {
                                    var setNotificationFailure = $"Failed to send notification for new set {newSet.ExtendedSetNumber} {newSet.Name}";
                                    this.logger.LogError(ex, "{ErrorMessage}", setNotificationFailure);

                                    if (ex is HttpRequestException)
                                    {
                                        // An HTTP exception when sending the notification means that we need to try again later.
                                        notificationsFailed = true;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            await this.notifier.SendErrorNotificationAsync(setNotificationFailure, ex);
                                            seenSets.Add(extendedSetNumber, newSet);
                                        }
                                        catch (Exception moreEx)
                                        {
                                            this.logger.LogError(moreEx, "Failed to send error notification");
                                            notificationsFailed = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    this.logger.LogInformation(
                        "Found {NumberOfNewSets} new sets in live data as of {LastUpdatedTime}",
                            newSetsCount,
                            liveDataUpdated);
                }

                if (!notificationsFailed)
                {
                    this.logger.LogDebug(
                        "Updating data file at {DataSource}",
                        this.seenData.GetDataSourceName());
                    await this.seenData.UpdateSetsAsync(liveDataUpdated, liveDataSets);
                }
                else
                {
                    this.logger.LogWarning(
                        "Some notifications failed, partially updating data file at {DataSource}",
                        this.seenData.GetDataSourceName());
                    // Don't update the updated-timestamp, because the next attempt should see current sets as "new" again.
                    await this.seenData.UpdateSetsAsync(seenDataUpdated, seenSets);
                }
            }
            else
            {
                this.logger.LogDebug(
                    "Live data is not newer than seen data, last updated {LastUpdatedTime}",
                    liveDataUpdated);
            }
        }
    }
}
