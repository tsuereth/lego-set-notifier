using LegoSetNotifier.AppriseApi;
using System.Text.Json;

namespace LegoSetNotifier.Test
{
    [TestClass]
    public class AppriseApiTests
    {
        [TestMethod]
        public void TestAppriseApiNotifyContentSerializeWithMultipleAttachments()
        {
            var testNotifyContent = new AppriseApiNotifyContent()
            {
                Title = "Test Content",
            };
            testNotifyContent.Attachments.Add("attach1");
            testNotifyContent.Attachments.Add("attach2");

            var serialized = JsonSerializer.Serialize(testNotifyContent);
            Assert.Contains("\"attach\":[\"attach1\",\"attach2\"]", serialized);
        }

        [TestMethod]
        public void TestAppriseApiNotifyContentSerializeWithoutAttachments()
        {
            var testNotifyContent = new AppriseApiNotifyContent()
            {
                Title = "Test Content",
            };
            testNotifyContent.Attachments.Clear();

            var serialized = JsonSerializer.Serialize(testNotifyContent);
            Assert.DoesNotContain("attach", serialized);
        }
    }
}
