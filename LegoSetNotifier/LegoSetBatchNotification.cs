using LegoSetNotifier.AppriseApi;
using System.Text;

namespace LegoSetNotifier
{
    public class LegoSetBatchNotification
    {
        private AppriseApiNotifyContent? Content = null;
        private HashSet<string> LegoSetNumbers = new HashSet<string>();

        public virtual AppriseApiNotifyContent GetContent()
        {
            if (this.Content == null)
            {
                throw new InvalidDataException("Notification content was null");
            }

            return this.Content;
        }

        public virtual ISet<string> GetLegoSetNumbers()
        {
            return this.LegoSetNumbers;
        }

        // Default constructor for testing with NSubstitute; outside of tests, this object will be invalid!
        public LegoSetBatchNotification() { }

        private LegoSetBatchNotification(HashSet<string> legoSetNumbers, StringBuilder bodyBuilder, List<string> attachments)
        {
            this.Content = new AppriseApiNotifyContent()
            {
                Title = $"{legoSetNumbers.Count} new LEGO sets",
                Format = "markdown",
                Body = bodyBuilder.ToString(),
                Attachments = attachments,
            };

            this.LegoSetNumbers = legoSetNumbers;
        }

        public static IEnumerable<LegoSetBatchNotification> BuildNotifications(INotifier notifier, IEnumerable<RebrickableData.LegoSet> legoSets)
        {
            var maxBodyChars = notifier.GetMaxNotificationBodyChars();
            var maxAttachments = notifier.GetMaxNotificationAttachments();
            var batches = new List<LegoSetBatchNotification>();

            // Build notifications out of sets in batches, each batch within the maximum size of a notification body.
            var batchSetNumbers = new HashSet<string>();
            var batchBodyBuilder = new StringBuilder();
            var batchAttachments = new List<string>();
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
                    batches.Add(new LegoSetBatchNotification(batchSetNumbers, batchBodyBuilder, batchAttachments));

                    // Reset the currently-building batch state.
                    batchSetNumbers = new HashSet<string>();
                    batchBodyBuilder = new StringBuilder();
                    batchAttachments = new List<string>();
                }

                batchSetNumbers.Add(legoSet.ExtendedSetNumber);
                batchBodyBuilder.Append(legoSetBodyContent);
                if (!string.IsNullOrEmpty(legoSet.ImageUrl))
                {
                    batchAttachments.Add(legoSet.ImageUrl);
                }
            }

            if (batchSetNumbers.Count > 0)
            {
                batches.Add(new LegoSetBatchNotification(batchSetNumbers, batchBodyBuilder, batchAttachments));
            }

            return batches;
        }
    }
}
