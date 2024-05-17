using LegoSetNotifier.RebrickableData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Mono.Options;

namespace LegoSetNotifier
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(c => c.AddSystemdConsole());
            var logger = loggerFactory.CreateLogger<Program>();

            var dataFilePath = "previouslySeen.json";
            var appriseNotifyUrl = string.Empty;

            var options = new OptionSet()
            {
                { "f|data-file=", $"Data file path, default: {dataFilePath}", o => dataFilePath = o },
                { "n|apprise-notifyurl=", $"Apprise notify url, default: {appriseNotifyUrl}", o => appriseNotifyUrl = o },
            };
            options.Parse(args);

            var dataFirstTime = false;
            var seenData = await PreviouslySeenDataJsonFile.FromFilePathAsync(dataFilePath);
            var seenDataUpdated = await seenData.GetUpdatedTimeAsync();
            if (seenDataUpdated == DateTimeOffset.MinValue)
            {
                logger.LogDebug(
                    "Data file at {FilePath} appears empty, skipping notifications for this first-time data check.",
                    dataFilePath);
                dataFirstTime = true;
            }

            INotifier? notifier = null;
            if (!string.IsNullOrEmpty(appriseNotifyUrl))
            {
                notifier = new AppriseNotifier(appriseNotifyUrl);
            }

            using (var liveDataClient = new RebrickableDataClient())
            {
                var liveDataUpdated = await liveDataClient.GetSetsUpdatedTimeAsync();
                if (dataFirstTime || liveDataUpdated > seenDataUpdated)
                {
                    var liveDataSetsList = await liveDataClient.GetSetsAsync();
                    var liveDataSets = liveDataSetsList.ToDictionary(s => s.ExtendedSetNumber, s => s);

                    if (!dataFirstTime)
                    {
                        var seenSets = await seenData.GetSetsAsync();

                        var newSetsCount = 0;
                        foreach (var extendedSetNumber in liveDataSets.Keys)
                        {
                            if (!seenSets.ContainsKey(extendedSetNumber))
                            {
                                ++newSetsCount;
                                var newSet = liveDataSets[extendedSetNumber];

                                if (notifier != null)
                                {
                                    try
                                    {
                                        await notifier.SendNewSetNotificationAsync(newSet);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogError(
                                            "Notification failed for new set {ExtendedSetNumber} {SetName} {LegoShopUrl} -- {Exception}",
                                            newSet.ExtendedSetNumber,
                                            newSet.Name,
                                            newSet.GetLegoShopUrl(),
                                            ex);
                                    }
                                }
                                else
                                {
                                    logger.LogInformation(
                                        "New set found in live data: {ExtendedSetNumber} {SetName} {LegoShopUrl}",
                                        newSet.ExtendedSetNumber,
                                        newSet.Name,
                                        newSet.GetLegoShopUrl());
                                }
                            }
                        }

                        logger.LogInformation(
                            "Found {NumberOfNewSets} new sets in live data as of {LastUpdatedTime}",
                                newSetsCount,
                                liveDataUpdated);
                    }

                    logger.LogDebug(
                        "Updating data file at {FilePath}",
                        dataFilePath);
                    await seenData.UpdateSetsAsync(liveDataUpdated, liveDataSets);
                }
                else
                {
                    logger.LogDebug(
                        "Live data is not newer than seen data, last updated {LastUpdatedTime}",
                        liveDataUpdated);
                }
            }

            return 0;
        }
    }
}
