using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using MusicStore.Models;
using Swashbuckle.Swagger.Annotations;

namespace MusicStore.Controllers
{
    /// <summary>
    /// Controller class for servicing HTTP GET, POST, PUT and DELETE requests for table entities
    /// </summary>
    public class SamplesController : ApiController
    {
        const String partitionName = "Samples_Partition_1";

        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;
        private BlobStorageService _blobStorageService = new BlobStorageService();

        /// <summary>
        /// Constructor for default configuration of the controller
        /// </summary>
        public SamplesController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Samples");
        }

        // GET: api/Samples
        /// <summary>
        /// Get a list of all samples
        /// </summary>
        /// <returns>sampleList</returns>
        [ResponseType(typeof(IEnumerable<Sample>))]
        public IHttpActionResult Get()
        {
            try
            {
                // Create query to get all sample entities in the table
                TableQuery<SampleEntity> query = new TableQuery<SampleEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

                // Assign result of query to a list of SampleEntities that can be operated on further
                List<SampleEntity> entityList = new List<SampleEntity>(table.ExecuteQuery(query));

                // Operate on List of sample Entities to create a new IEnumerable of Sample objects to serialise and return
                IEnumerable<Sample> sampleList = from e in entityList
                                                 select new Sample()
                                                 {
                                                     SampleID = e.RowKey,
                                                     Title = e.Title,
                                                     Artist = e.Artist,
                                                     SampleMp3Url = e.SampleMp3Url
                                                 };
                return Ok(sampleList);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                return BadRequest(ex.Message + "\n" + ex.StackTrace);
            }
        }

        // GET: api/Samples/5
        /// <summary>
        /// Returns a specfic sample by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult GetSample(string id)
        {
            try
            {
                // Create get operation to retrieve specific SampleEntity by ID
                TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

                // Execute get operation
                TableResult getOperationResult = table.Execute(getOperation);

                // Check result and return 404 Not Found HTTTP Status if there is no matching entity in table
                if (getOperationResult.Result == null) return NotFound();

                // Create new Sample object from result of get operation and return
                else
                {
                    SampleEntity entity = (SampleEntity)getOperationResult.Result;
                    Sample sample = new Sample()
                    {
                        SampleID = entity.RowKey,
                        Title = entity.Title,
                        Artist = entity.Artist,
                        SampleMp3Url = entity.SampleMp3Url
                    };

                    // Return 200 Status code with sample entity in response body
                    return Ok(sample);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                return BadRequest(ex.Message + "\n" + ex.StackTrace);
            }
        }

        //POST: api/Samples
        /// <summary>
        /// Create a new Sample entity
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        [SwaggerResponse(HttpStatusCode.Created)]
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostSample(Sample sample)
        {
            try
            {
                // Create new SampleEntity from Sample object 
                SampleEntity sampleEntity = new SampleEntity()
                {
                    RowKey = getNewMaxRowKeyValue(),
                    PartitionKey = partitionName,
                    Title = sample.Title,
                    Artist = sample.Artist,
                    SampleMp3Url = sample.SampleMp3Url,
                    CreatedDate = DateTime.Now,
                    Mp3Blob = null,
                    SampleMp3Blob = null,
                    SampleDate = DateTime.Now
                };
                // Create insert operation for SampleEntity
                var insertOperation = TableOperation.Insert(sampleEntity);

                // Execute insert operation
                table.Execute(insertOperation);

                // Return HTTP status 201 Created code with details in response body
                return CreatedAtRoute("DefaultApi", new { id = sampleEntity.RowKey }, sampleEntity);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                return BadRequest(ex.Message + "\n" + ex.StackTrace);
            }
        }

        // PUT: api/Samples/5
        /// <summary>
        /// Update a SampleEntity from PUT operation
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSample(string id, Sample sample)
        {
            try
            {
                // Return 400 error if id provided doesn't match the ID of the Sample provided
                if (id != sample.SampleID) return BadRequest();

                // Retrieve operation to get product entity to modify
                TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

                // Execute operation and get result
                TableResult retrieveResult = table.Execute(retrieveOperation);

                // If not existing sample is found return 404 Not Found error
                if (retrieveResult.Result == null) return NotFound();

                // Assign retrieved entity from operation result to SampleEntity object
                SampleEntity updateEntity = (SampleEntity)retrieveResult.Result;

                // Delete all related blobs
                deleteBlobs(updateEntity);

                //Update all fields of SampleEntity with new data from Sample object
                updateEntity.Title = sample.Title;
                updateEntity.Artist = sample.Artist;
                updateEntity.Mp3Blob = null;
                updateEntity.SampleMp3Blob = null;
                updateEntity.SampleDate = DateTime.Now;
                updateEntity.SampleMp3Url = sample.SampleMp3Url;

                // InsertOrReplace operation to replace the entity in the table with the updated entity
                TableOperation insertOperation = TableOperation.InsertOrReplace(updateEntity);

                // Executes the insert operation
                table.Execute(insertOperation);

                // return HTTP Status code stating request has succeeded
                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (StorageException storageEx)
            {
                Debug.WriteLine(storageEx.Message + "\n" + storageEx.StackTrace);
                Debug.WriteLine(storageEx.RequestInformation.ExtendedErrorInformation.AdditionalDetails + "\n" + storageEx.RequestInformation.ExtendedErrorInformation.ErrorMessage);
                return BadRequest(storageEx.Message + "\n" + storageEx.StackTrace);
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                return BadRequest(ex.Message + "\n" + ex.StackTrace);
            }
        }

        // DELETE: api/Samples/5
        /// <summary>
        /// Deletes SampleEntity from table and all associated blobs
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [ResponseType(typeof(void))]
        public IHttpActionResult deleteSample(string id)
        {
            try
            {
                // Create retrieve operation to get entity to delete
                TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

                // Execute retrieve operation 
                TableResult retrieveOperationResult = table.Execute(retrieveOperation);

                // If entity not found returns 404 HTTP status code
                if (retrieveOperationResult.Result == null) return NotFound();

                // Create SampleEntity object from result of retrieve operation
                SampleEntity sampleEntity = (SampleEntity)retrieveOperationResult.Result;

                // Delete all blobs associated with sampleEntity
                deleteBlobs(sampleEntity);

                // Create delete operation to delete entity from table
                TableOperation deleteOperation = TableOperation.Delete(sampleEntity);

                // Execute delete operation
                table.Execute(deleteOperation);

                // return HTTP Status code stating request has succeeded
                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                return BadRequest(ex.Message + "\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Deletes all blobs associated with an entity
        /// </summary>
        /// <param name="sampleEntity"></param>
        public void deleteBlobs(SampleEntity sampleEntity)
        {
            if (sampleEntity.Mp3Blob != null)
            {
                var mp3Blob = getAudioStorageContainer().GetBlockBlobReference(sampleEntity.Mp3Blob);
                mp3Blob.DeleteIfExists();
            }

            if (sampleEntity.SampleMp3Blob != null)
            {
                var sampleBlob = getAudioStorageContainer().GetBlockBlobReference(sampleEntity.SampleMp3Blob);
                sampleBlob.DeleteIfExists();
            }
        }

        /// <summary>
        /// Gets all keys in table and returns next highest possible key value
        /// </summary>
        /// <returns></returns>
        private String getNewMaxRowKeyValue()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            int maxRowKeyValue = 0;
            foreach (SampleEntity entity in table.ExecuteQuery(query))
            {
                int entityRowKeyValue = Int32.Parse(entity.RowKey);
                if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
            }
            maxRowKeyValue++;
            return maxRowKeyValue.ToString();
        }

        /// <summary>
        ///  Gets the blob container
        /// </summary>
        /// <returns></returns>
        private CloudBlobContainer getAudioStorageContainer()
        {
            return _blobStorageService.getCloudBlobContainer();
        }
    }
}
