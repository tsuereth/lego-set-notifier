using System.Text.Json;
using System.Text.Json.Nodes;

namespace LegoSetNotifier.AppriseApi
{
    public class AppriseApiException : AggregateException
    {
        public string ErrorMessage { get; set; } = string.Empty;

        public AppriseApiException(string responseString, Exception innerException) :
            base(responseString, innerException)
        {
            this.ErrorMessage = GetErrorMsgFromResponseString(responseString);
        }

        static private string GetErrorMsgFromResponseString(string responseString)
        {
            try
            {
                var responseObject = JsonSerializer.Deserialize<JsonObject>(responseString);
                if (responseObject == null)
                {
                    return string.Empty;
                }

                var responseError = responseObject["error"];
                if (responseError == null)
                {
                    return string.Empty;
                }

                return responseError.ToString();
            }
            catch (Exception)
            {
                // We're already generating an exception, so another exception here wouldn't be helpful.
                return string.Empty;
            }
        }
    }
}
