using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel.DataAnnotations;

namespace MusicStore.Models
{

    // class for sample data type, used for serialisation
 
    public class Sample
    {
        /// <summary>
        /// Sample ID
        /// </summary>
        [Key]
        public string SampleID { get; set; }

        /// <summary>
        /// Title of the sample
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Artist Name
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// the blob sample url
        /// </summary>
        public string SampleMp3Url { get; set; }


    }
}