using LegoSetNotifier.AppriseApi;
using System.Text;

namespace LegoSetNotifier
{
    public class LegoSetBatchNotification
    {
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

        private LegoSetBatchNotification(BatchContext context, StringBuilder bodyBuilder, List<string> attachments)
        {
            this.Context = context;

            this.NotificationContent = new AppriseApiNotifyContent()
            {
                Title = $"Lego Set Notifier: {this.Context.LegoSetNumbers.Count} new LEGO sets",
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

        public static IEnumerable<LegoSetBatchNotification> BuildNotifications(INotifier notifier, IEnumerable<RebrickableData.LegoSet> legoSets)
        {
            var maxBodyChars = notifier.GetMaxNotificationBodyChars();
            var maxAttachments = notifier.GetMaxNotificationAttachments();
            var totalCount = (uint)legoSets.Count();
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
                var legoSetBodyContent = $"- {legoSet.ExtendedSetNumber} {legoSet.Name} ({legoSet.ReleaseYear}) {legoSet.NumberOfParts} pcs -- [Rebrickable]({legoSet.GetRebrickableUrl()})";
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
                    (!string.IsNullOrEmpty(legoSet.ImageUrl) && (batchAttachments.Count == maxAttachments)))
                {
                    // This can't be added to the current batch; flush that batch, and start building a new one.
                    batches.Add(new LegoSetBatchNotification(batchContext, batchBodyBuilder, batchAttachments));

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
                if (!string.IsNullOrEmpty(legoSet.ImageUrl))
                {
                    batchAttachments.Add(legoSet.ImageUrl);
                }

                ++legoSetIndex;
            }

            if (batchContext.LegoSetNumbers.Count > 0)
            {
                batches.Add(new LegoSetBatchNotification(batchContext, batchBodyBuilder, batchAttachments));
            }

            return batches;
        }
    }
}
