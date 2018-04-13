using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using System.Diagnostics;

namespace MusicStore
{
    /// <summary>
    /// Configures and returns queue service for application
    /// </summary>
    public class CloudQueueService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CloudQueue getCloudQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse
                 (ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue sampleQueue = queueClient.GetQueueReference("samplemaker");
            sampleQueue.CreateIfNotExists();

            Trace.TraceInformation("Queue initialized");

            return sampleQueue;
        }
    }
}