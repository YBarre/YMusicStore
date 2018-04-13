using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using System.Diagnostics;
using MusicStore.Models;

namespace MusicStore.Migrations
{
    /// <summary>
    /// Migaration class to initialise the database table and populate it with six example entries
    /// </summary>
    public class InitialiseSamples
    {
        /// <summary>
        ///  Method to create table and populate with example data
        /// </summary>
        public static void go()
        {
            const String partitionName = "Samples_Partition_1";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("samples");

            try
            {
                // Checks if table needs to be initialised
                if (!table.Exists())
                {
                    // Create table if it doesn't exist already
                    table.CreateIfNotExists();

                    // Create array of national anthem titles, also will be used as the Artist values
                    string[] titles = { "China", "France", "Italy", "Russia", "U.S.A", "United Kingdom" };

                    // Loop to iterate over titles array to create entities
                    for (int idx = 0; idx < titles.Length; idx++)
                    {
                        SampleEntity sampleEntity = new SampleEntity(partitionName, getNewMaxRowKeyValue());
                        sampleEntity.Title = titles[idx];
                        sampleEntity.Artist = titles[idx];
                        sampleEntity.CreatedDate = DateTime.Now;
                        sampleEntity.Mp3Blob = null;
                        sampleEntity.SampleMp3Blob = null;
                        sampleEntity.SampleMp3Url = null;
                        sampleEntity.SampleDate = DateTime.Now;

                        // Create insert operation for SampleEntity
                        var insertOperation = TableOperation.Insert(sampleEntity);

                        // Execute insert operation
                        table.Execute(insertOperation);
                    }
                }
            }
            catch (StorageException storageEx)
            {
                Debug.WriteLine(storageEx.Message);
                Debug.WriteLine(storageEx.StackTrace);
                Debug.WriteLine(storageEx.RequestInformation.ExtendedErrorInformation.ErrorCode);
                Debug.WriteLine(storageEx.RequestInformation.ExtendedErrorInformation.ErrorMessage);
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }

        // Getting all keys in table and returns next highest possible key value
       
        private static String getNewMaxRowKeyValue()
        {
            const String partitionName = "Samples_Partition_1";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("samples");

            // Create table query to get all entities in table
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            // Initialise max value to zero
            int maxRowKeyValue = 0;

            // Iterate over RowKey fields of table entities and saves maximums value
            foreach (SampleEntity entity in table.ExecuteQuery(query))
            {
                int entityRowKeyValue = Int32.Parse(entity.RowKey);
                if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
            }

            // Increments max value value by one to find next available key value
            maxRowKeyValue++;

            return maxRowKeyValue.ToString();
        }
    }
}