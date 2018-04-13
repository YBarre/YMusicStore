using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using NAudio.Wave;
using NLayer.NAudioSupport;
using Microsoft.WindowsAzure.Storage.Table;
using MusicStore;
using MusicStore.Models;
using System.Diagnostics;

namespace MusicStore_Webjob
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void GenerateSample(
        [QueueTrigger("samplemaker")] SampleEntity entityInQueue,
        [Table("samples", "{PartitionKey}", "{RowKey}")] SampleEntity entityInTable,
        [Table("samples")] CloudTable tableBinding,

        TextWriter logger)

        {
            try
            {
                BlobStorageService _blobStorageService = new BlobStorageService();
                string fullAudioPath = _blobStorageService.FullAudioPath;
                string samplePath = _blobStorageService.SamplePath;
                CloudBlobContainer blobContainer = _blobStorageService.getCloudBlobContainer();

                // Get full audio as input blob from blob container
                var inputBlob = blobContainer.GetDirectoryReference(fullAudioPath)
                    .GetBlobReference(entityInTable.Mp3Blob);

                // Generates unique name for sample blob and updates entity
                string sampleName = string.Format("{0}-{1}{2}", Guid.NewGuid(), entityInTable.Title, ".mp3");  /* !!!! CHANGE SO ITS GETS THE RIGHT NAME !!!! */
                entityInTable.SampleMp3Blob = sampleName;

                // Get reference to block blob to write sample to as output blob
                var outputBlob = blobContainer.GetBlockBlobReference(samplePath + sampleName);

                // Opens stream between input and output blobs passing through Capture Sample method
                using (Stream input = inputBlob.OpenRead())
                using (Stream output = outputBlob.OpenWrite())
                {
                    CaptureSampleFromAudio(input, output, 20);
                    outputBlob.Properties.ContentType = "audio/mpeg3";
                }

                // Update sample date
                entityInTable.SampleDate = DateTime.Now;

                // Creates and executes an update operation to update the entity in the table
                TableOperation updateOperation = TableOperation.InsertOrReplace(entityInTable);
                tableBinding.Execute(updateOperation);
            }
            catch (Exception ex)
            {

                Debug.WriteLine("Webjob GenerateSample: \n {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        public static void CaptureSampleFromAudio(Stream input, Stream output, int duration)
        {
            try
            {
                using (var reader = new Mp3FileReader(input, wave => new NLayer.NAudioSupport.Mp3FrameDecompressor(wave)))
                {
                    Mp3Frame frame;
                    frame = reader.ReadNextFrame();
                    int frameTimeLength = (int)(frame.SampleCount / (double)frame.SampleRate * 1000.0);
                    int framesRequired = (int)(duration / (double)frameTimeLength * 1000.0);

                    int frameNumber = 0;
                    while ((frame = reader.ReadNextFrame()) != null)
                    {
                        frameNumber++;

                        if (frameNumber <= framesRequired)
                        {
                            output.Write(frame.RawData, 0, frame.RawData.Length);
                        }
                        else break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Webjob CaptureSample: \n {0}\n{1}", ex.Message, ex.StackTrace);
            }

        }
    }
}
