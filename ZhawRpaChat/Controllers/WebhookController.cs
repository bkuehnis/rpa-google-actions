using System.Threading.Tasks;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ZhawRpaChat.Controllers
{
    [Route("")]
	public class WebhookController : ControllerBase
    {
		private readonly ILogger<WebhookController> _logger;

		public WebhookController(ILogger<WebhookController> logger)
		{
			_logger = logger;
		}

		private static readonly JsonParser jsonParser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

        [HttpGet]
        public ContentResult TestingApplication()
        {
            return Content("Webhook is running.");
        }

        [HttpPost]
        public async Task<ContentResult> DialogAction()
        {
            // Read the request JSON asynchronously, as the Google.Protobuf library
            // doesn't (yet) support asynchronous parsing.
            string requestJson;
            using (TextReader reader = new StreamReader(Request.Body))
            {
                requestJson = await reader.ReadToEndAsync();
            }
            var s = new Google.Cloud.Dialogflow.V2.WebhookResponse();
            
            var DEBUG = false;
            if(DEBUG)
                requestJson = "{\"handler\":{\"name\":\"yeswebhook\"},\"intent\":{\"name\":\"YES\",\"params\":{},\"query\":\"Yes\"},\"scene\":{\"name\":\"Start\",\"slotFillingStatus\":\"UNSPECIFIED\",\"slots\":{}},\"session\":{\"id\":\"ABwppHEzghcQHc-SpGx_I61zj6DqQVq-ROu_vP8W3PI3ez7H_niulAmHyjl7YRMiDCYfVKJdmOruXfo\",\"params\":{},\"typeOverrides\":[],\"languageCode\":\"\"},\"user\":{\"locale\":\"en-US\",\"params\":{},\"accountLinkingStatus\":\"ACCOUNT_LINKING_STATUS_UNSPECIFIED\",\"verificationStatus\":\"VERIFIED\",\"packageEntitlements\":[],\"lastSeenTime\":\"2020-10-02T15:40:35Z\"},\"home\":{\"params\":{}},\"device\":{\"capabilities\":[\"SPEECH\",\"RICH_RESPONSE\",\"LONG_FORM_AUDIO\"]}}";

            // Parse the body of the request using the Protobuf JSON parser,
            // *not* Json.NET.

            _logger.LogInformation("____________________");

            _logger.LogInformation("reuqest:");

            _logger.LogInformation(requestJson);


            if (ApplicationSettings.CALL_UI_PATH) { 
                // UI Path Aufruf
                var uiPathResponse = new UiPathClient().StartJob();
            }

            // Ask Protobuf to format the JSON to return.
            // Again, we don't want to use Json.NET - it doesn't know how to handle Struct
            // values etc.
            if (string.Empty == requestJson)
            {
                return Content("No request Json", "application/json");
            }
            dynamic data = JObject.Parse(requestJson);

            string sessionId = data.session.id;
            _logger.LogInformation(sessionId);

            //see how to build a webhook response -> https://developers.google.com/assistant/conversational/webhooks?tool=sdk#example-response
            var responseString =
                "{" +
                    "\"session\":{" +
                                    "\"id\":\""+sessionId+"\"," +
                                    "\"params\":{}" +
                                "}," +
                    "\"prompt\":{" +
                                    "\"override\":false," +
                                    "\"firstSimple\":{" +
                                                        "\"speech\":\"Hello World Tsüri.\"," +
                                                        "\"text\":\"\"" +
                                                    "}" +
                                "}," +
                    "\"scene\": {" +
                                    "\"name\":\"SceneName\"," +
                                    "\"slots\":{}," +
                                    "\"next\":{\"name\":\"actions.scene.END_CONVERSATION\"}" +
                                "}" +
                "}";

            return Content(responseString, "application/json");
        }      
    }
}

