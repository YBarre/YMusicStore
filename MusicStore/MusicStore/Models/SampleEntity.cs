using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.WindowsAzure.Storage.Table;

namespace MusicStore.Models
{
  
    /// <summary>
    /// Sample entitiy class
    /// </summary>
    public class SampleEntity : TableEntity
    {
        /// <summary>
        /// Title of the MP3
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Name of the artist
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// date Blob created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Full MP3 blob
        /// </summary>
        public string Mp3Blob { get; set; }

        /// <summary>
        /// Sample MP3 blob
        /// </summary>
        public string SampleMp3Blob { get; set; }

        /// <summary>
        /// Sample MP3 URL
        /// </summary>
        public string SampleMp3Url { get; set; }


        /// <summary>
        /// Date Sample created
        /// </summary>
        public DateTime SampleDate { get; set; }
 

        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="sampleID"></param>
        public SampleEntity(string partitionKey, string sampleID)
        {
            PartitionKey = partitionKey;
            RowKey = sampleID;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SampleEntity() { }
    }
}