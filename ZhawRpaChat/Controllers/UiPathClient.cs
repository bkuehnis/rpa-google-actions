using System;
using System.Net.Http;
using System.Text;
using System.Configuration;
using Microsoft.Extensions.Configuration.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ZhawRpaChat.Controllers
{
    public class UiPathClient
    {



        private static readonly string URL_AUTH = "https://account.uipath.com/oauth/token";
        //private static readonly string URL_RELEASES = "https://cloud.uipath.com/zhawmgygfxg/zhawDefault/odata/Releases?filter=ProcessKey='demo-rest-api-calls'";
        //private static readonly string URL_START_JOB = "https://cloud.uipath.com/zhawmgygfxg/zhawDefault/odata/Jobs/UiPath.Server.Configuration.OData.StartJobs";


        //private static readonly string TENANT_NAME = "zhawDefault";
        //private static readonly string USER_KEY = "JrTNUWkYyr4XpF0oPzopEJRrTeumzKiFtX1pasnr5hypH";

        //private static readonly int FODLER_ID = 646504;

        public UiPathClient()
        {
        }

        public string StartJob()
        {
            var uiPathData = new UiPathAuthRequest();

            //Oauth
            var json = JsonConvert.SerializeObject(uiPathData);
            Console.WriteLine(json);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-UIPATH-TenantName", ApplicationSettings.TENANT_NAME);
            var response = client.PostAsync(URL_AUTH, data).Result;
            string jsonContent = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
                return "OAuth not successful: " + response.ReasonPhrase;

            UiPathAuthResponse uiPathAuthResponse = JsonConvert.DeserializeObject<UiPathAuthResponse>(jsonContent);

            //get release key
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + uiPathAuthResponse.access_token);
            var responseRelease = client.GetAsync(ApplicationSettings.URL_RELEASES).Result;
            if (!responseRelease.IsSuccessStatusCode)
                return "Release Get Request to find the key was not successful: " + responseRelease.ReasonPhrase;
            string jsonContent2 = responseRelease.Content.ReadAsStringAsync().Result;
            //"{\"@odata.context\":\"https://cloud.uipath.com/zhawmgygfxg/zhawDefault/odata/$metadata#Releases\",\"@odata.count\":1,\"value\":[{\"Key\":\"99520e1f-9a08-40a5-b4ab-009975c11dd6\",\"ProcessKey\":\"demo-rest-api-calls\",\"ProcessVersion\":\"1.0.6\",\"IsLatestVersion\":false,\"IsProcessDeleted\":false,\"Description\":\"Gets a random dog image from dog api and saves it to desktop\",\"Name\":\"demo-rest-api-calls_env_kuhs\",\"EnvironmentId\":131688,\"EnvironmentName\":\"env_kuhs\",\"InputArguments\":null,\"ProcessType\":\"Process\",\"SupportsMultipleEntryPoints\":true,\"RequiresUserInteraction\":true,\"AutoUpdate\":false,\"FeedId\":\"57f1b9d4-b6c3-4d18-be91-7320da97398d\",\"JobPriority\":\"Normal\",\"CreationTime\":\"2020-11-03T10:32:25.26Z\",\"OrganizationUnitId\":646504,\"OrganizationUnitFullyQualifiedName\":\"Default\",\"Id\":193359,\"Arguments\":{\"Input\":\"[{\\\"name\\\":\\\"in_TargetPath\\\",\\\"type\\\":\\\"System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b7…
            JObject obj = JObject.Parse(jsonContent2);
            var releaseKey = obj["value"].First["Key"].ToString();

            var jobStartData = new Root();
            jobStartData.startInfo = new StartInfo();
            jobStartData.startInfo.ReleaseKey = releaseKey;
            jobStartData.startInfo.Strategy = "JobsCount";
            jobStartData.startInfo.JobsCount = 1;
            jobStartData.startInfo.Source = "Manual";

            var jsonJobStartData = JsonConvert.SerializeObject(jobStartData);
            Console.WriteLine(jsonJobStartData);
            var ddata = JObject.Parse("{\"startInfo\":{ \"ReleaseKey\":\"99520e1f-9a08-40a5-b4ab-009975c11dd6\", \"Strategy\":\"JobsCount\", \"JobsCount\":1, \"Source\":\"Manual\"}}");
            string payload = JsonConvert.SerializeObject(new
            {
                startInfo = new
                {
                    ReleaseKey = releaseKey,
                    Strategy = "JobsCount",
                    JobsCount = 1,
                    Source = "Manual"

                }
            });
            var stringJsonJobStartData = new StringContent(jsonJobStartData, Encoding.UTF8, "application/json");


            //using var client2 = new HttpClient();
            //client2.DefaultRequestHeaders.Add("Authorization", "Bearer " + uiPathAuthResponse.access_token);
            //client2.DefaultRequestHeaders.Add("X-UIPATH-TenantName", TENANT_NAME);
            //client2.DefaultRequestHeaders.Add("X-UIPATH-OrganizationUnitId", FODLER_ID.ToString());
            //client2.DefaultRequestHeaders.Add("Content-Type", "application/json");


            client.DefaultRequestHeaders.Add("X-UIPATH-OrganizationUnitId", ApplicationSettings.FOLDER_ID.ToString());

            var responseJobStarted = client.PostAsync(ApplicationSettings.URL_START_JOB, stringJsonJobStartData).Result;
            if (!responseJobStarted.IsSuccessStatusCode)
            {
                var content = responseJobStarted.Content.ReadAsStringAsync().Result;
                Console.WriteLine(content);
                return "Job start request was not successful: " + responseJobStarted.ReasonPhrase;
            }
                

            return "Job request started";

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

    class StartInfo
    {
        public string ReleaseKey { get; set; }
        public string Strategy { get; set; }
        public int JobsCount { get; set; }
        public string Source { get; set; }
    }

    class Root
    {
        public StartInfo startInfo { get; set; }
    }
}