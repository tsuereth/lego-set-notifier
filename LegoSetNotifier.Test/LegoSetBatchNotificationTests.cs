using LegoSetNotifier.RebrickableData;
using NSubstitute;

namespace LegoSetNotifier.Test
{
    [TestClass]
    public class LegoSetBatchNotificationTests
    {
        [TestMethod]
        public void TestBuildNotificationsOneBatch()
        {
            var mockNotifier = Substitute.For<INotifier>();
            mockNotifier.GetMaxNotificationBodyChars().Returns(32768u);
            mockNotifier.GetMaxNotificationAttachments().Returns(32768u);

            var testLegoSet = new LegoSet()
            {
                Name = "Test Lego Set",
                ExtendedSetNumber = "12345-1",
                ImageUrl = "whatever",
            };

            var batches = LegoSetBatchNotification.BuildNotifications(mockNotifier, new List<LegoSet>() { testLegoSet });

            Assert.HasCount(1, batches);

            var batch1 = batches.ElementAt(0);
            Assert.HasCount(1, batch1.GetLegoSetNumbers());
            Assert.Contains(testLegoSet.ExtendedSetNumber, batch1.GetLegoSetNumbers());
            Assert.Contains(testLegoSet.Name, batch1.GetContent().Body);
            Assert.Contains(testLegoSet.ImageUrl, batch1.GetContent().Attachments);
        }

        [TestMethod]
        public async Task TestBuildNotificationsMultipleBatches()
        {
            var mockNotifier = Substitute.For<INotifier>();
            mockNotifier.GetMaxNotificationBodyChars().Returns(32768u);
            mockNotifier.GetMaxNotificationAttachments().Returns(1u);

            var testLegoSet1 = new LegoSet()
            {
                Name = "Test Lego Set 1",
                ExtendedSetNumber = "00001-1",
                ImageUrl = "attachment 1",
            };
            var testLegoSet2 = new LegoSet()
            {
                Name = "Test Lego Set 2",
                ExtendedSetNumber = "00002-1",
                ImageUrl = "attachment 2",
            };

            var batches = LegoSetBatchNotification.BuildNotifications(mockNotifier, new List<LegoSet>() { testLegoSet1, testLegoSet2 });

            Assert.HasCount(2, batches);

            var batch1 = batches.ElementAt(0);
            Assert.HasCount(1, batch1.GetLegoSetNumbers());
            Assert.Contains(testLegoSet1.ExtendedSetNumber, batch1.GetLegoSetNumbers());
            Assert.Contains(testLegoSet1.Name, batch1.GetContent().Body);
            Assert.Contains(testLegoSet1.ImageUrl, batch1.GetContent().Attachments);

            var batch2 = batches.ElementAt(1);
            Assert.HasCount(1, batch2.GetLegoSetNumbers());
            Assert.Contains(testLegoSet2.ExtendedSetNumber, batch2.GetLegoSetNumbers());
            Assert.Contains(testLegoSet2.Name, batch2.GetContent().Body);
            Assert.Contains(testLegoSet2.ImageUrl, batch2.GetContent().Attachments);
        }

        [TestMethod]
        public async Task TestBuildNotificationsContentTooBigAsync()
        {
            var mockNotifier = Substitute.For<INotifier>();
            mockNotifier.GetMaxNotificationBodyChars().Returns(2u);
            mockNotifier.GetMaxNotificationAttachments().Returns(32768u);

            var largeStringLength = mockNotifier.GetMaxNotificationBodyChars() * 2;
            var testLegoSet = new LegoSet()
            {
                Name = StringGenerator.Generate(largeStringLength),
                ExtendedSetNumber = "12345-1",
            };

            Assert.ThrowsExactly<InvalidDataException>(() =>
            {
                LegoSetBatchNotification.BuildNotifications(mockNotifier, new List<LegoSet>() { testLegoSet });
            });
        }
    }
}
