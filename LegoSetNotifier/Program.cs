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
            var appriseNotifyUrl = string.Empty;

            var options = new OptionSet()
            {
                { "help", "Print help text", _ => printHelp = true },
                { "f|data-file=", $"Data file path, default: {dataFilePath}", o => dataFilePath = o },
                { "n|apprise-notifyurl=", $"Apprise notify url, default: {appriseNotifyUrl}", o => appriseNotifyUrl = o },
            };
            options.Parse(args);

            if (printHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            INotifier? notifier = null;
            try
            {
                var seenData = await PreviouslySeenDataJsonFile.FromFilePathAsync(dataFilePath);

                if (!string.IsNullOrEmpty(appriseNotifyUrl))
                {
                    notifier = new AppriseNotifier(appriseNotifyUrl);
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

            return 0;
        }
    }
}
