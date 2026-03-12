using LegoSetNotifier.AppriseApi;
using System.Text;

namespace LegoSetNotifier
{
    public class LegoSetBatchNotification
    {
        public class NotificationSettings
        {
            public required string Label { get; set; }
            public required bool IncludeLegoSetDetails { get; set; }
            public required bool IncludeAttachments { get; set; }
        }

        public static NotificationSettings FullDetailSettings(string notificationLabel)
        {
            return new NotificationSettings()
            {
                Label = notificationLabel,
                IncludeLegoSetDetails = true,
                IncludeAttachments = true,
            };
        }

        public static NotificationSettings DigestSettings(string notificationLabel)
        {
            return new NotificationSettings()
            {
                Label = notificationLabel,
                IncludeLegoSetDetails = true,
                IncludeAttachments = false,
            };
        }

        public static NotificationSettings SummaryOnlySettings(string notificationLabel)
        {
            return new NotificationSettings()
            {
                Label = notificationLabel,
                IncludeLegoSetDetails = false,
                IncludeAttachments = false,
            };
        }

        class BatchContext
        {
            public required uint FirstLegoSetIndex;
            public required uint LastLegoSetIndex;
            public required uint TotalLegoSetCount;
            public required HashSet<string> LegoSetNumbers;
        }

        private BatchContext? Context = null;
        private AppriseApiNotifyContent? NotificationContent = null;

        public virtual AppriseApiNotifyContent GetNotificationContent()
        {
            if (this.NotificationContent == null)
            {
                throw new InvalidDataException("Notification content was null");
            }

            return this.NotificationContent;
        }

        public virtual ISet<string> GetLegoSetNumbers()
        {
            if (this.Context == null)
            {
                throw new InvalidDataException("Batch context was null");
            }

            return this.Context.LegoSetNumbers;
        }

        // Default constructor for testing with NSubstitute; outside of tests, this object will be invalid!
        public LegoSetBatchNotification() { }

        private LegoSetBatchNotification(NotificationSettings settings, BatchContext context, StringBuilder bodyBuilder, List<string> attachments)
        {
            this.Context = context;

            this.NotificationContent = new AppriseApiNotifyContent()
            {
                Title = $"Lego Set Notifier: {this.Context.LegoSetNumbers.Count} {settings.Label}",
                Format = "markdown",
                Body = bodyBuilder.ToString(),
                Attachments = attachments,
            };

            // If context indicates there are multiple pages/batches, include a pagination description in the title.
            if (this.Context.LegoSetNumbers.Count != this.Context.TotalLegoSetCount)
            {
                this.NotificationContent.Title += $" ({this.Context.FirstLegoSetIndex + 1} - {this.Context.LastLegoSetIndex + 1} of {this.Context.TotalLegoSetCount})";
            }
        }

        public static IEnumerable<LegoSetBatchNotification> BuildNotifications(INotifier notifier, NotificationSettings settings, IEnumerable<RebrickableData.LegoSet> legoSets)
        {
            var totalCount = (uint)legoSets.Count();

            if (!settings.IncludeLegoSetDetails)
            {
                // Shortcut the batching process, and just create a simple summary.
                var legoSetsList = legoSets.ToList();

                var summaryContext = new BatchContext()
                {
                    FirstLegoSetIndex = 0,
                    LastLegoSetIndex = totalCount - 1,
                    TotalLegoSetCount = totalCount,
                    LegoSetNumbers = legoSetsList.Select(s => s.ExtendedSetNumber).ToHashSet(),
                };

                var purchaseableCount = legoSetsList.Where(s => s.IsPurchaseableSet()).Count();
                var nonPurchaseableCount = totalCount - purchaseableCount;
                var summaryBody = $"Summary: {purchaseableCount} purchaseable sets, {nonPurchaseableCount} non-purchaseable sets";
                var summaryBuilder = new StringBuilder(summaryBody);

                return new List<LegoSetBatchNotification>()
                {
                    new LegoSetBatchNotification(settings, summaryContext, summaryBuilder, new List<string>())
                };
            }

            var maxBodyChars = notifier.GetMaxNotificationBodyChars();
            var maxAttachments = notifier.GetMaxNotificationAttachments();
            var batches = new List<LegoSetBatchNotification>();

            // Build notifications out of sets in batches, each batch within the maximum size of a notification body.
            var batchContext = new BatchContext()
            {
                FirstLegoSetIndex = 0,
                LastLegoSetIndex = 0,
                TotalLegoSetCount = totalCount,
                LegoSetNumbers = new HashSet<string>(),
            };
            var batchBodyBuilder = new StringBuilder();
            var batchAttachments = new List<string>();

            var legoSetIndex = 0u;
            foreach (var legoSet in legoSets)
            {
                var legoSetBodyContent = $"- {legoSet.ExtendedSetNumber} {legoSet.Name} ({legoSet.ReleaseYear}) -- [Rebrickable]({legoSet.GetRebrickableUrl()})";
                if (legoSet.IsPurchaseableSet())
                {
                    legoSetBodyContent += $", [LEGO Shop]({legoSet.GetLegoShopUrl()})";
                }
                legoSetBodyContent += "\n";

                if (legoSetBodyContent.Length >= maxBodyChars)
                {
                    // Try a simpler format, as a fallback.
                    legoSetBodyContent = $"- {legoSet.ExtendedSetNumber} (content exceeded max {maxBodyChars}) -- [Rebrickable]({legoSet.GetRebrickableUrl()})\n";

                    // If even this fallback exceeds the maximum size of a batch, then assume the size constraint is unworkable.
                    // NOTE: This will break the caller's batching attempt, beyond repair.
                    if (legoSetBodyContent.Length >= maxBodyChars)
                    {
                        throw new InvalidDataException($"Notification content for set {legoSet.ExtendedSetNumber} exceeds notifier's max body size {maxBodyChars}");
                    }
                }

                if (
                    ((batchBodyBuilder.Length + legoSetBodyContent.Length) >= maxBodyChars) ||
                    (settings.IncludeAttachments && !string.IsNullOrEmpty(legoSet.ImageUrl) && (batchAttachments.Count == maxAttachments)))
                {
                    // This can't be added to the current batch; flush that batch, and start building a new one.
                    batches.Add(new LegoSetBatchNotification(settings, batchContext, batchBodyBuilder, batchAttachments));

                    // Reset the currently-building batch state.
                    batchContext = new BatchContext()
                    {
                        FirstLegoSetIndex = legoSetIndex,
                        LastLegoSetIndex = legoSetIndex,
                        TotalLegoSetCount = totalCount,
                        LegoSetNumbers = new HashSet<string>(),
                    };
                    batchBodyBuilder = new StringBuilder();
                    batchAttachments = new List<string>();
                }

                batchContext.LastLegoSetIndex = legoSetIndex;
                batchContext.LegoSetNumbers.Add(legoSet.ExtendedSetNumber);
                batchBodyBuilder.Append(legoSetBodyContent);
                if (settings.IncludeAttachments && !string.IsNullOrEmpty(legoSet.ImageUrl))
                {
                    batchAttachments.Add(legoSet.ImageUrl);
                }

                ++legoSetIndex;
            }

            if (batchContext.LegoSetNumbers.Count > 0)
            {
                batches.Add(new LegoSetBatchNotification(settings, batchContext, batchBodyBuilder, batchAttachments));
            }

            return batches;
        }
    }
}
