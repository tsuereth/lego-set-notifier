using LegoSetNotifier.AppriseApi;
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

            var printHelp = false;
            var dataFilePath = "previouslySeen.json";
            var appriseApiBaseUrl = string.Empty;
            var appriseApiConfigKey = string.Empty;

            var options = new OptionSet()
            {
                { "help", "Print help text", _ => printHelp = true },
                { "f|data-file=", $"Data file path, default: {dataFilePath}", o => dataFilePath = o },
                { "a|apprise-api-baseurl=", $"Apprise API base URL, default: {appriseApiBaseUrl}", o => appriseApiBaseUrl = o },
                { "k|apprise-api-configkey=", $"Apprise API config key, default: {appriseApiConfigKey}", o => appriseApiConfigKey = o },
            };
            options.Parse(args);

            if (printHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            // Options validation: either both Apprise API [base URL + config key] must be specified, or neither.
            if (!string.IsNullOrEmpty(appriseApiBaseUrl) && string.IsNullOrEmpty(appriseApiConfigKey))
            {
                logger.LogError("Invalid options, cannot only provide Apprise API base URL (must also provide config key)");
                options.WriteOptionDescriptions(Console.Out);
                return -1;
            }
            else if (!string.IsNullOrEmpty(appriseApiConfigKey) && string.IsNullOrEmpty(appriseApiBaseUrl))
            {
                logger.LogError("Invalid options, cannot only provide Apprise API config key (must also provide base URL)");
                options.WriteOptionDescriptions(Console.Out);
                return -1;
            }

            AppriseApiClient? appriseClient = null;
            INotifier? notifier = null;
            try
            {
                var seenData = await PreviouslySeenDataJsonFile.FromFilePathAsync(dataFilePath);

                if (!string.IsNullOrEmpty(appriseApiBaseUrl))
                {
                    appriseClient = new AppriseApiClient(appriseApiBaseUrl);
                    notifier = new AppriseNotifier(appriseClient, appriseApiConfigKey);
                }

                using (var dataClient = new RebrickableDataClient())
                {
                    var legoSetNotifier = new LegoSetNotifier(logger, seenData, dataClient, notifier);
                    await legoSetNotifier.DetectNewSetsAsync();
                }
            }
            catch (Exception ex)
            {
                const string exceptionErrorMessage = "Exception running LegoSetNotifier";
                logger.LogError(ex, exceptionErrorMessage);

                if (notifier != null)
                {
                    await notifier.SendErrorNotificationAsync(exceptionErrorMessage, ex);
                }
            }
            finally
            {
                if (appriseClient != null)
                {
                    appriseClient.Dispose();
                }
            }

            return 0;
        }
    }
}
