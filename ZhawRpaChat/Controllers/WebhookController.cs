using System.Threading.Tasks;
using Google.Protobuf;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System;
using System.Collections.Generic;
using WikiDotNet;

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
            string googleActionsRequestJson;
            using (TextReader reader = new StreamReader(Request.Body))
            {
                googleActionsRequestJson = await reader.ReadToEndAsync();
            }

            var DEBUG = false;
            if (DEBUG)
            {
                googleActionsRequestJson = "{\"handler\":{\"name\":\"yeswebhook\"},\"intent\":{\"name\":\"YES\",\"params\":{},\"query\":\"Yes\"},\"scene\":{\"name\":\"Start\",\"slotFillingStatus\":\"UNSPECIFIED\",\"slots\":{}},\"session\":{\"id\":\"ABwppHEzghcQHc-SpGx_I61zj6DqQVq-ROu_vP8W3PI3ez7H_niulAmHyjl7YRMiDCYfVKJdmOruXfo\",\"params\":{},\"typeOverrides\":[],\"languageCode\":\"\"},\"user\":{\"locale\":\"en-US\",\"params\":{},\"accountLinkingStatus\":\"ACCOUNT_LINKING_STATUS_UNSPECIFIED\",\"verificationStatus\":\"VERIFIED\",\"packageEntitlements\":[],\"lastSeenTime\":\"2020-10-02T15:40:35Z\"},\"home\":{\"params\":{}},\"device\":{\"capabilities\":[\"SPEECH\",\"RICH_RESPONSE\",\"LONG_FORM_AUDIO\"]}}";
                googleActionsRequestJson = "{    \"handler\": {      \"name\": \"Beschreibung\"    },    \"intent\": {      \"name\": \"DDogImage\",      \"params\": {        \"rasse1\": {          \"original\": \"boxer\",          \"resolved\": \"boxer\"        }      },      \"query\": \"boxer\"    },    \"scene\": {      \"name\": \"Start\",      \"slotFillingStatus\": \"UNSPECIFIED\",      \"slots\": {},      \"next\": {        \"name\": \"Start\"      }    },    \"session\": {      \"id\": \"ABwppHFW0G0_9PTSjcaHJx__1Vxv_xzKNrUYbFqK2Chp73LLV8vkPRshXDTJB7RCQ5FeJg0toNetvSw\",      \"params\": {},      \"typeOverrides\": [],      \"languageCode\": \"\"    },    \"user\": {      \"locale\": \"en-US\",      \"params\": {},      \"accountLinkingStatus\": \"ACCOUNT_LINKING_STATUS_UNSPECIFIED\",      \"verificationStatus\": \"VERIFIED\",      \"packageEntitlements\": [],      \"gaiamint\": \"\",      \"lastSeenTime\": \"2020-12-05T23:56:18Z\"    },    \"home\": {      \"params\": {}    },    \"device\": {      \"capabilities\": [        \"SPEECH\",        \"RICH_RESPONSE\",        \"LONG_FORM_AUDIO\"      ]    }  }";
            }

            _logger.LogInformation("____________________");
            _logger.LogInformation("reuqest:");
            _logger.LogInformation(googleActionsRequestJson);

            if (string.Empty == googleActionsRequestJson)
            {
                return Content("Request Json empty", "application/json");
            }

            dynamic data = JObject.Parse(googleActionsRequestJson);

            string sessionId = data.session.id;
            _logger.LogInformation(sessionId);


            string handlerName = data["handler"]["name"];
            string breed = data.intent["params"]["rasse1"]["resolved"];

            if (handlerName == "Beschreibung")
            {
                WikiSearchSettings searchSettings = new WikiSearchSettings
                { RequestId = "Request ID", ResultLimit = 5, ResultOffset = 2, Language = "en" };

                WikiSearchResponse response = WikiSearcher.Search(breed, searchSettings);

                Console.WriteLine($"\nResults found ({breed}):\n");
                string text = "No information found";
                foreach (WikiSearchResult result in response.Query.SearchResults)
                {
                    if(result.WordCount > 0 && !result.Title.Contains("Talk"))
                    {
                        text = result.Preview;
                        break;
                    }
                }
                string jsonResponse = getSimpleResponseJson(sessionId, text);
                return Content(jsonResponse, "application/json");
            }
            else
            {
               
                string dogUri = "https://images.dog.ceo/breeds/terrier-wheaten/n02098105_50.jpg";

                if (ApplicationSettings.CALL_UI_PATH)
                {
                    // UI Path Aufruf
                    dogUri = await new UiPathClient().StartJobAsync(breed);
                }
                else
                {                    
                    dogUri = getDogUriFromDogCeo(breed);
                }

                //string responseString = getSimpleResponseJson(sessionId);
                //responseString = getRichBasicCardResponse(sessionId, dogUri);
                var responseString = getRichImageCardResponse(sessionId, dogUri, breed);

                _logger.LogInformation("____________________");
                _logger.LogInformation("response:");
                _logger.LogInformation(responseString);

                return Content(responseString, "application/json");
            }
        }

        private string getDogUriFromDogCeo(string breed)
        {
            using var client = new HttpClient();
            string url = "https://dog.ceo/api/breed/{0}/images/random";
            var result = client.GetAsync(string.Format(url, breed.ToLower())).Result;
            if (!result.IsSuccessStatusCode)
            {
                var content = result.Content.ReadAsStringAsync().Result;
                Console.WriteLine(content);
                return "Job start request was not successful: " + result.ReasonPhrase;
            }

            string jobStartedResult = result.Content.ReadAsStringAsync().Result;
            dynamic jobStartedJson = JObject.Parse(jobStartedResult);            
            return jobStartedJson.message;
        }

        // <summary>
        /// Creates a Rich image card response (see https://developers.google.com/assistant/conversational/prompts-rich#json_3)
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="dogUri"></param>
        /// <returns></returns>
        private string getRichImageCardResponse(string sessionId, string dogUri, string breed)
        {
            return " {" +
                        "\"session\": {" +
                            "\"id\": \"" + sessionId + "\"," +
                            "\"params\": { }" +
                        "}," +
                        "\"prompt\": {" +
                            "\"override\": false," +
                            "\"content\": {" +
                                    "\"image\": {" +
                                        "\"alt\": \"Dog breed " + breed + "\"," +
                                        "\"height\": 0," +
                                        "\"url\": \"" + dogUri + "\"," +
                                        "\"width\": 0" +
                                    "}" +
                            "}," +
                            "\"firstSimple\": {" +
                                "\"speech\": \"Dog breed " + breed + ".\"," +
                                "\"text\": \"Dog breed " + breed + ".\"" +
                            "}" +
                        "}" +
                    "}";
        }

        /// <summary>
        /// Creates a Rich basic card response (see https://developers.google.com/assistant/conversational/prompts-rich#json_3)
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="dogUri"></param>
        /// <returns></returns>
        private string getRichBasicCardResponse(string sessionId, string dogUri)
        {
            return " {" +
                        "\"session\": {" +
                            "\"id\": \"" + sessionId + "\"," +
                            "\"params\": { }" +
                        "}," +
                        "\"prompt\": {" +
                            "\"override\": false," +
                            "\"content\": {" +
                                "\"card\": {" +
                                    "\"title\": \"Card Title\"," +
                                    "\"subtitle\": \"Card Subtitle\"," +
                                    "\"text\": \"Card Content\"," +
                                    "\"image\": {" +
                                    "\"alt\": \"Google Assistant logo\"," +
                                    "\"height\": 0," +
                                    "\"url\": \""+ dogUri + "\"," +
                                    "\"width\": 0" +
                                "}" +
                            "}" +
                        "}," +
                        "\"firstSimple\": {" +
                            "\"speech\": \"This is a card.\"," +
                            "\"text\": \"This is a card.\"" +
                        "}" +
                    "}" +
                "}";
        }

        private static string getSimpleResponseJson(string sessionId, string text)
        {
            return "{" +
                        "\"session\":{" +
                                        "\"id\":\"" + sessionId + "\"," +
                                        "\"params\":{}" +
                                    "}," +
                        "\"prompt\":{" +
                                        "\"override\":false," +
                                        "\"firstSimple\":{" +
                                                            "\"speech\":\""+text+"\"," +
                                                            "\"text\":\"" + text + "\"" +
                                                        "}" +
                                    "}," +
                        "\"scene\": {" +
                                        "\"name\":\"SceneName\"," +
                                        "\"slots\":{}," +
                                        "\"next\":{}" +
                                    "}" +
                    "}";
        }
    }
}

