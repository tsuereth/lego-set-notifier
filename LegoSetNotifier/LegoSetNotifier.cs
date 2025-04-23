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

                if (!dataFirstTime)
                {
                    var seenSets = await this.seenData.GetSetsAsync();

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
                                continue;
                            }

                            if (this.notifier != null)
                            {
                                try
                                {
                                    await this.notifier.SendNewSetNotificationAsync(newSet);
                                }
                                catch (Exception ex)
                                {
                                    var setNotificationFailure = $"Failed to send notification for new set {newSet.ExtendedSetNumber} {newSet.Name}";
                                    this.logger.LogError(ex, "{ErrorMessage}", setNotificationFailure);

                                    await this.notifier.SendErrorNotificationAsync(setNotificationFailure, ex);
                                }
                            }
                        }
                    }

                    this.logger.LogInformation(
                        "Found {NumberOfNewSets} new sets in live data as of {LastUpdatedTime}",
                            newSetsCount,
                            liveDataUpdated);
                }

                this.logger.LogDebug(
                    "Updating data file at {DataSource}",
                    this.seenData.GetDataSourceName());
                await this.seenData.UpdateSetsAsync(liveDataUpdated, liveDataSets);
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
