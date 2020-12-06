using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace ZhawRpaChat.Controllers
{
    public class UiPathClient
    {
        private static readonly string URL_AUTH = "https://account.uipath.com/oauth/token";
        
        public UiPathClient()
        {
        }

        public async Task<string> StartJobAsync(string breed)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-UIPATH-TenantName", ApplicationSettings.TENANT_NAME);

            UiPathAuthResponse uiPathAuthResponse = uiPathOauth(client);

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + uiPathAuthResponse.access_token);

            var responseJobStarted = startUiPathJob(client, breed);

            return await getDogBreedUriFromUiPathJob(client, responseJobStarted);

        }

        private HttpResponseMessage startUiPathJob(HttpClient client, string breed)
        {
            //get release key
            var jobStartData = new Root();
            jobStartData.startInfo = new StartInfo();
            jobStartData.startInfo.ReleaseKey = getReleaseKey(client);
            jobStartData.startInfo.Strategy = "JobsCount";
            jobStartData.startInfo.JobsCount = 1;
            jobStartData.startInfo.Source = "Manual";
            jobStartData.startInfo.InputArguments = (breed == "random") ? "{}" : "{'in_DogBreed':'" + breed.ToLower() + "'}";

            var jsonJobStartData = JsonConvert.SerializeObject(jobStartData);
            var stringJsonJobStartData = new StringContent(jsonJobStartData, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Add("X-UIPATH-OrganizationUnitId", ApplicationSettings.FOLDER_ID.ToString());

            var responseJobStarted = client.PostAsync(ApplicationSettings.URL_START_JOB, stringJsonJobStartData).Result;
            if (!responseJobStarted.IsSuccessStatusCode)
            {
                var content = responseJobStarted.Content.ReadAsStringAsync().Result;
                Console.WriteLine(content);
                throw new Exception("Job start request was not successful: " + responseJobStarted.ReasonPhrase);
            }
            return responseJobStarted;
        }

        private static async Task<string> getDogBreedUriFromUiPathJob(HttpClient client, HttpResponseMessage responseJobStarted)
        {
            string responseJons = responseJobStarted.Content.ReadAsStringAsync().Result;
            dynamic jsonData = JObject.Parse(responseJons);
            string id = jsonData["value"][0].Id;
            string url = string.Format(ApplicationSettings.URL_JOB_BY_ID, id);
            string state;
            int maxTries = 0;
            string htt = string.Empty;
            do
            {
                await Task.Delay(100);
                var responseJobStarted2 = client.GetAsync(url).Result;
                if (!responseJobStarted2.IsSuccessStatusCode)
                {
                    var content = responseJobStarted2.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(content);
                    return "Job start request was not successful: " + responseJobStarted.ReasonPhrase;
                }

                string responseJons2 = responseJobStarted2.Content.ReadAsStringAsync().Result;
                dynamic jsonDataRe = JObject.Parse(responseJons2);
                state = jsonDataRe.State;
                if (state == "Successful")
                {
                    htt = jsonDataRe.OutputArguments;
                    htt = htt.Replace("{\"out_DogUrl\":\"", "");
                    htt = htt.Replace("\"}", "");
                }
                maxTries++;

            } while (state != "Successful" || maxTries > 20);
            return htt;
        }

        private string getReleaseKey(HttpClient client)
        {
            var responseRelease = client.GetAsync(ApplicationSettings.URL_RELEASES).Result;
            if (!responseRelease.IsSuccessStatusCode)
                throw new Exception("Release Get Request to find the key was not successful: " + responseRelease.ReasonPhrase);
            string uiPathReleaseKeyJsonContent = responseRelease.Content.ReadAsStringAsync().Result;
            JObject obj = JObject.Parse(uiPathReleaseKeyJsonContent);
            return obj["value"].First["Key"].ToString();
        }

        private UiPathAuthResponse uiPathOauth(HttpClient client)
        {
            var uiPathData = new UiPathAuthRequest();

            //Oauth
            var json = JsonConvert.SerializeObject(uiPathData);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var response = client.PostAsync(URL_AUTH, data).Result;
            string oauthJsonContentResult = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("OAuth not successful: " + response.ReasonPhrase);
            }
            return JsonConvert.DeserializeObject<UiPathAuthResponse>(oauthJsonContentResult);
        }
    }

    class UiPathAuthRequest
    {
        //header X-UIPATH-TenantName

        //body
        public string grant_type = "refresh_token";
        public string client_id = "8DEv1AMNXczW3y4U15LL3jYf62jK93n5";
        public string refresh_token = "JrTNUWkYyr4XpF0oPzopEJRrTeumzKiFtX1pasnr5hypH";

    }

    class UiPathAuthResponse
    {
        public string access_token { get; set; }
        public string scope { get; set; }
        public string expires_in { get; set; }
        public string token_type { get; set; }
    }

    class InputArgument
    {
        public string in_DogBreed { get; set; }
    }

    class StartInfo
    {
        public string ReleaseKey { get; set; }
        public string Strategy { get; set; }
        public int JobsCount { get; set; }
        public string Source { get; set; }
        public string InputArguments { get; set; }
    }

    class Root
    {
        public StartInfo startInfo { get; set; }
    }
}