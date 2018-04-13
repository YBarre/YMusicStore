using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.Diagnostics;

namespace MusicStore
{
    /// <summary>
    /// Handles blob storage configuration and provides relevant container and paths
    /// </summary>
    public class BlobStorageService
    {
        // Initialise blob storage paths
        private String samplePath = "audio/samples/";
        private String fullAudioPath = "audio/full/";

        /// <summary>
        /// Returns configured blob container
        /// </summary>
        /// <returns></returns>
        public CloudBlobContainer getCloudBlobContainer()
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse
                       (ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                CloudBlobContainer blobContainer = blobClient.GetContainerReference("audiostorage");
                if (blobContainer.CreateIfNotExists())
                {
                    // Enable public access on the newly created "audiofiles" container.
                    blobContainer.SetPermissions(
                        new BlobContainerPermissions
                        {
                            PublicAccess = BlobContainerPublicAccessType.Blob
                        });
                }
                return blobContainer;
            }
            catch (NullReferenceException nullRefEx)
            {
                Debug.WriteLine(nullRefEx.Message + "\n" + nullRefEx.InnerException);
                return new CloudBlobContainer(new Uri(""));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n" + ex.StackTrace);
                return new CloudBlobContainer(new Uri(""));
            }

        }

        /// <summary>
        /// Getter for sample path
        /// </summary>
        public string SamplePath
        {
            get
            {
                return samplePath;
            }
        }

        /// <summary>
        /// Getter for full audio path
        /// </summary>
        public string FullAudioPath
        {
            get
            {
                return fullAudioPath;
            }
        }
    }
}