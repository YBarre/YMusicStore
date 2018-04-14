using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using MusicStore.Models;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Web;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;

namespace MusicStore.Controllers
{


    /// <summary>
    /// data controler class
    /// </summary>
    public class DataController : ApiController
    {
        const String partitionName = "Samples_Partition_1";

        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;
        private BlobStorageService _blobStorageService = new BlobStorageService();
        private CloudQueueService _queueStorageService = new CloudQueueService();
        private string fullAudioPath;
        private string samplePath;

        /// <summary>
        /// Constructor for default configuration of the controller
        /// </summary>
        public DataController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Samples");
            fullAudioPath = _blobStorageService.FullAudioPath;
            samplePath = _blobStorageService.SamplePath;
        }

        // GET: api/Data/5
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [SwaggerResponse(HttpStatusCode.OK)]
        [ResponseType(typeof(StreamContent))]
        public HttpResponseMessage GetBlob(string id)
        {
            try
            {
                // Create get operation to retrieve specific SampleEntity by ID
                TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

                // Execute get operation
                TableResult getOperationResult = table.Execute(getOperation);

                // Check result and return 404 Not Found HTTTP Status if there is no matching entity in table
                if (getOperationResult.Result == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;

                // Check there is a matching sample mp3 for entity and return 404 Not Found HTTTP Status if it doesnt exist
                if (sampleEntity.SampleMp3Url == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                // Retrieves the blob from the blob container
                var blob = getAudioStorageContainer()
                    .GetDirectoryReference(samplePath)
                    .GetBlockBlobReference(sampleEntity.SampleMp3Blob);

                // Gets the content of the blob as a binary stream
                Stream blobStream = blob.OpenRead();

                // Creates HTTP response message to return with HTTP status cpde
                HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);

                // Inserts stream data as message content
                message.Content = new StreamContent(blobStream);

                // Sets message headers for metadata
                message.Content.Headers.ContentLength = blob.Properties.Length;
                message.Content.Headers.ContentType = new
                    System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg3");
                message.Content.Headers.ContentDisposition = new
                    System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = blob.Name,
                    Size = blob.Properties.Length
                };

                return message;
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + "\n" + ex.StackTrace;
                Debug.WriteLine(errorMessage);
                HttpResponseMessage errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent(errorMessage);
                return errorResponse;
            }
        }
        // PUT: api/Data/5
        /// <summary>
        /// Streams a new blob and puts in in storage
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [SwaggerResponse(HttpStatusCode.OK)]
        [ResponseType(typeof(void))]
        public HttpResponseMessage PutBlob(string id)
        {
            try
            {
                SamplesController sampleController = new SamplesController();

                // Create get operation to retrieve specific SampleEntity by ID
                TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

                // Execute get operation
                TableResult getOperationResult = table.Execute(getOperation);

                // Check result and return 404 Not Found HTTTP Status if there is no matching entity in table
                if (getOperationResult.Result == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                // Create sample entity object from get operation
                SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;

                // Generate sample URL from HTTP request
                var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
                String sampleURL = baseUrl.ToString() + "/api/data/GetBlob/" + sampleEntity.RowKey;

                // Delete all related existing blobs
                sampleController.deleteBlobs(sampleEntity);

                // Get HTTP request
                var request = HttpContext.Current.Request;

                // Generate unique name for blob
                string blobName = string.Format("{0}-{1}{2}", Guid.NewGuid(), sampleEntity.Title, ".mp3"); /* !!!CHANGE THIS SO IT SETS THE CORRECT BLOB NAME!!! */

                // Instantiate blob
                var blob = getAudioStorageContainer().GetBlockBlobReference(fullAudioPath + blobName);

                // Stream binary data from HTTP context
                blob.Properties.ContentType = "audio/mpeg3";
                blob.UploadFromStream(request.InputStream);

                // Set all values of entity accordingly
                sampleEntity.Mp3Blob = blobName;
                sampleEntity.SampleMp3Url = sampleURL;
                sampleEntity.SampleMp3Blob = null;

                // Create and executy update operation to update the table entry
                TableOperation updateOperation = TableOperation.InsertOrReplace(sampleEntity);
                table.Execute(updateOperation);

                CloudQueue sampleQueue = getSampleQueue();
                var queueMessageSample = new SampleEntity(partitionName, id);
                sampleQueue.AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(queueMessageSample)));

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {

                string errorMessage = ex.Message + "\n" + ex.StackTrace;
                Debug.WriteLine(errorMessage);
                HttpResponseMessage errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent(errorMessage);
                return errorResponse;
            }
        }

        /// <summary>
        ///  Gets the blob container
        /// </summary>
        /// <returns></returns>
        private CloudBlobContainer getAudioStorageContainer()
        {
            return _blobStorageService.getCloudBlobContainer();
        }

        /// <summary>
        /// Gets cloud queue
        /// </summary>
        /// <returns></returns>
        private CloudQueue getSampleQueue()
        {
            return _queueStorageService.getCloudQueue();
        }

    }
}
