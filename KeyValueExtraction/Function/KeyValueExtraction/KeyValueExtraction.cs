using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Web;
using System.Net.Http.Headers;
using System.Linq;
using CognitiveFunction.Model;
using System.Collections.Generic;
using CognitiveFunction.Services;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;
using KeyValueExtraction.Model;

namespace CognitiveFunction
{

    public static class CognitiveFunction
    {
        #region classes used to serialize the response
        private class WebApiResponseError
        {
            public string message { get; set; }
        }

        private class WebApiResponseWarning
        {
            public string message { get; set; }
        }

        private class WebApiResponseRecord
        {
            public string dataSource { get; set; }
            public Dictionary<string, object> data { get; set; }
            public List<WebApiResponseError> errors { get; set; }
            public List<WebApiResponseWarning> warnings { get; set; }
        }

        private class WebApiEnricherResponse
        {
            public List<WebApiResponseRecord> values { get; set; }
        }
        #endregion

     
        // This is the main function Runs at the beginning.
        [FunctionName("CognitiveFunction")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log, ExecutionContext executionContext)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string imageUrl = data?.url;

            // Check if imageUrl is not empty
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return new BadRequestObjectResult("Please pass an image URL in the request body");
            }
            else
            {
                // Get SAS Access to private containers
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("ConnectionString"));
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Environment.GetEnvironmentVariable("ContainerName"));

                // Get ImageUrl
                Uri uri = new Uri(imageUrl);

                // This code part is part is adjust for forms, find file path & name after container name
                string[] parts = uri.LocalPath.Split(Environment.GetEnvironmentVariable("ContainerName") + "/");
                string filename = parts[parts.Length-1];
                imageUrl = GetBlobSasUri(container, filename, null);

                // Retrieve Classification of the images
                string formTemplate = await MakeCustomVisionRequestByUrl(imageUrl);

                // Key-Value Extraction via OCR Output
                var outputResult = await MakeOCRRequestByUrl(imageUrl, formTemplate, executionContext);

                // Put together response as JSON output
                WebApiResponseRecord responseRecord = new WebApiResponseRecord();
                responseRecord.data = new Dictionary<string, object>();
                responseRecord.dataSource = imageUrl;
                responseRecord.data.Add("KeyValues", outputResult);
                WebApiEnricherResponse response = new WebApiEnricherResponse();
                response.values = new List<WebApiResponseRecord>();
                response.values.Add(responseRecord);

                return (ActionResult)new OkObjectResult(response);
            }
        }


        // This method generates SAS access token to view image
        private static string GetBlobSasUri(CloudBlobContainer container, string blobName, string policyName = null)
        {
            string sasBlobToken;

            // Get a reference to a blob within the container.
            // Note that the blob may not exist yet, but a SAS can still be created for it.
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            if (policyName == null)
            {
                // Create a new access policy and define its constraints.
                // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad hoc SAS, and
                // to construct a shared access policy that is saved to the container's shared access policies.
                SharedAccessBlobPolicy adHocSAS = new SharedAccessBlobPolicy()
                {
                    // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request.
                    // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create
                };

                // Generate the shared access signature on the blob, setting the constraints directly on the signature.
                sasBlobToken = blob.GetSharedAccessSignature(adHocSAS);

                Console.WriteLine("SAS for blob (ad hoc): {0}", sasBlobToken);
                Console.WriteLine();
            }
            else
            {
                // Generate the shared access signature on the blob. In this case, all of the constraints for the
                // shared access signature are specified on the container's stored access policy.
                sasBlobToken = blob.GetSharedAccessSignature(null, policyName);

                Console.WriteLine("SAS for blob (stored access policy): {0}", sasBlobToken);
                Console.WriteLine();
            }

            // Return the URI string for the container, including the SAS token.
            return blob.Uri + sasBlobToken;
        }

        // OCR Request by URL & Extract Key-Value Pairs
        public static async Task<List<string>> MakeOCRRequestByUrl(string imageUrl, string formType, ExecutionContext executionContext)
        {
            Console.Write("C# HTTP trigger function processed: MakeOCRRequestByUrl");

            string urlBase = Environment.GetEnvironmentVariable("CognitiveServicesUrlBase");
            string key = Environment.GetEnvironmentVariable("CognitiveServicesKey");

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            // Request parameters
            queryString["mode"] = "Printed";
            Uri uri = new Uri(urlBase + "recognizeText?" + queryString);

            HttpResponseMessage response;

            var requstbody = "{\"url\":\"" + $"{imageUrl}" + "\"}";

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(requstbody);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }

            string operationLocation = null;

            // The response contains the URI to retrieve the result of the process.
            if (response.IsSuccessStatusCode)
            {
                operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();
            }

            string contentString;
            int i = 0;
            do
            {
                System.Threading.Thread.Sleep(1000);
                response = await client.GetAsync(operationLocation);
                contentString = await response.Content.ReadAsStringAsync();
                ++i;
            }
            while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);


            string json = response.Content.ReadAsStringAsync().Result;
            json = json.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' });
            RecognizeText ocrOutput = JsonConvert.DeserializeObject<RecognizeText>(json);

            if (ocrOutput != null && ocrOutput.RecognitionResult != null && ocrOutput.RecognitionResult.Lines != null)
            {

                List<string> resultText = new List<string>();

                resultText = (from Line sline in ocrOutput.RecognitionResult.Lines
                              select (string)sline.Text).ToList<string>();

                // Extract Key-Value Pairs
                resultText = OCRHelper.ExtractKeyValuePairs(ocrOutput.RecognitionResult.Lines, formType, executionContext);

                return resultText;
            }
            else
            {
                return null;
            }
        }

        // Form Classification using Custom Vision AI
        public static async Task<string> MakeCustomVisionRequestByUrl(string imageUrl)
        {
            var client = new HttpClient();

            // Request headers
            string urlBaseCusvomVision = Environment.GetEnvironmentVariable("CustomVisionUrlBase");
            string keyCustomVision = Environment.GetEnvironmentVariable("CustomVisionPredictionKey");

            //Prediction API endpoint will be here
            client.DefaultRequestHeaders.Add("Prediction-key", keyCustomVision);
            Uri uri = new Uri(urlBaseCusvomVision);
            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"Url\": \"" + imageUrl + "\"}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);

                //JSON Deserialization
                string jsoncust = response.Content.ReadAsStringAsync().Result;
                CustomVision custobjectVision = JsonConvert.DeserializeObject<CustomVision>(jsoncust);

                //Bring the highest scoring tag
                var result = (from p in custobjectVision.Predictions
                              orderby p.Probability descending
                              select p.TagName).FirstOrDefault<string>();

                return result;
            }
        }
    }
}