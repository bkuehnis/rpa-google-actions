using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ZhawRpaChat.Controllers
{


    [Route("webhook")]
	public class WebhookController : ControllerBase
    {

        
		private readonly ILogger<WebhookController> _logger;

		public WebhookController(ILogger<WebhookController> logger)
		{
			_logger = logger;
		}

		private static readonly JsonParser jsonParser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

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
            var DEBUG = false;
            if(DEBUG)
                requestJson = "{\"handler\":{\"name\":\"yeswebhook\"},\"intent\":{\"name\":\"YES\",\"params\":{},\"query\":\"Yes\"},\"scene\":{\"name\":\"Start\",\"slotFillingStatus\":\"UNSPECIFIED\",\"slots\":{}},\"session\":{\"id\":\"ABwppHEzghcQHc-SpGx_I61zj6DqQVq-ROu_vP8W3PI3ez7H_niulAmHyjl7YRMiDCYfVKJdmOruXfo\",\"params\":{},\"typeOverrides\":[],\"languageCode\":\"\"},\"user\":{\"locale\":\"en-US\",\"params\":{},\"accountLinkingStatus\":\"ACCOUNT_LINKING_STATUS_UNSPECIFIED\",\"verificationStatus\":\"VERIFIED\",\"packageEntitlements\":[],\"lastSeenTime\":\"2020-10-02T15:40:35Z\"},\"home\":{\"params\":{}},\"device\":{\"capabilities\":[\"SPEECH\",\"RICH_RESPONSE\",\"LONG_FORM_AUDIO\"]}}";

            // Parse the body of the request using the Protobuf JSON parser,
            // *not* Json.NET.

            _logger.LogInformation("____________________");

            _logger.LogInformation("reuqest:");

            _logger.LogInformation(requestJson);
            
            // Populate the response
            WebhookResponse response = new WebhookResponse
            {
                FulfillmentText = "He Ho",
                
            };

            // Ask Protobuf to format the JSON to return.
            // Again, we don't want to use Json.NET - it doesn't know how to handle Struct
            // values etc.
            dynamic data = JObject.Parse(requestJson);

            string sessionId = data.session.id;
            _logger.LogInformation(sessionId);

            //see how to build a webhook response -> https://developers.google.com/assistant/conversational/webhooks?tool=sdk#example-response
            var responseString = "{\"session\":{\"id\":\""+sessionId+"\",\"params\":{}},\"prompt\":{\"override\":false,\"firstSimple\":{\"speech\":\"Hello World.\",\"text\":\"\"}},\"scene\":{\"name\":\"SceneName\",\"slots\":{},\"next\":{\"name\":\"actions.scene.END_CONVERSATION\"}}}";
            var testData = "{\"result_code\":200, \"person\":{\"name\":\"John\", \"lastName\": \"Doe\"}}";

            string responseJson = response.ToString();
            return Content(responseString, "application/json");
        }      
    }
}
