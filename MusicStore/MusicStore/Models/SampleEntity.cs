using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage.Table;

namespace MusicStore.Models
{
  
    public class SampleEntity : TableEntity
    {
        // The title of the song
        public string Title { get; set; }

        // Name of artist
        public string Artist { get; set; }

        // date of record creation
        public DateTime CreatedDate { get; set; }

        // MP3 audio blob
        public string Mp3Blob { get; set; }

        // sample MP3 blob
        public string SampleMp3Blob { get; set; }

        // URL of the sample
        public string SampleMp3Url { get; set; }


        // Date sample was created
        public DateTime SampleDate { get; set; }
 

        // overloaded constructor of the class
        public SampleEntity(string partitionKey, string sampleID)
        {
            PartitionKey = partitionKey;
            RowKey = sampleID;
        }

        // Empty constructor
        public SampleEntity() { }
    }
}