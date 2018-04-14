using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MusicStoreClient.Models;
using Microsoft.Rest;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Web;
using System.Diagnostics;
using System.Security.Permissions;

namespace MusicStoreClient
{
    class Program
    {
        static MusicStore client = new MusicStore(new AnonymousCredential());
        static string clientUri = client.BaseUri.GetLeftPart(UriPartial.Authority);

        // Inititialise CLI menu UI constructs
        static string welcome = " Y Barre Coursework";
        static string line = " -------------------------------------\n";
   

        static void Main(string[] args)
        {
            RunClient().Wait();
        }

        static async Task RunClient()
        {
            // start the menu selection
            int selection = -1;

            do
            {
                // Write menu options to console
                Console.WriteLine(" \n" + welcome);
                Console.WriteLine(" \n" + line);
                Console.WriteLine(" 1 : List all sample entities");
                Console.WriteLine(" 2 : List a specific sample entity");
                Console.WriteLine(" 3 : Create a new sample entity");
                Console.WriteLine(" 4 : Update and existing sample entity");
                Console.WriteLine(" 5 : Delete a sample entity by ID");
                Console.WriteLine(" 6 : Upload an MP3 from the local file sample");
                Console.WriteLine(" 7 : Download and mp3 sample to local filesystem");
                Console.WriteLine(" 0 : EXIT");

                // get the selection from the user
                Console.WriteLine("\n" + "Please Choose an option: ");

                try { selection = Int32.Parse(Console.ReadLine()); }
                catch (Exception ex)
                { Console.WriteLine("Invalid input, please enter a valid number", ex.Message); }

                switch (selection)
                {
                    // Gets and displays all samples in table - GET
                    case 1:
                        Console.WriteLine(line + " Display all samples: \n");
                        Console.WriteLine("ID - Title - Artist - Sample URL");
                        IList<Sample> samples = await getAllSamples();
                        foreach (Sample sample in samples)
                        {
                            Console.WriteLine("{0} --  {1} --  {2} --  {3}",
                                sample.SampleID, sample.Title, sample.Artist, sample.SampleMp3Url);
                        }
                        break;

                    // Get specific entity from table by id - GET
                    case 2:
                        Console.WriteLine(line + " Enter ID of sample to display:");
                        string id = Console.ReadLine();
                        HttpOperationResponse<Sample> getResponse = await getSample(id);
                        if (getResponse.Response.IsSuccessStatusCode)
                        {
                            Sample sampleById = getResponse.Body;
                            Console.WriteLine(" {0}\n {1}\n {2}\n {3}\n",
                                sampleById.SampleID, sampleById.Title, sampleById.Artist, sampleById.SampleMp3Url);
                        }
                        else { Console.WriteLine(" No matching sample found in table by ID"); }
                        break;

                    // Creates new Sample entity from user input - POST
                    case 3:
                        Console.WriteLine(" Enter the song title: ");
                        string title = Console.ReadLine();

                        Console.WriteLine(" Enter the artist name: ");
                        string artist = Console.ReadLine();

                        Sample newSample = new Sample()
                        {
                            Title = title,
                            Artist = artist,
                            SampleMp3Url = null
                        };
                        await postSample(newSample);
                        break;

                    // Updates an existing entity in the table with new info from user input - PUT
                    case 4:
                        Console.WriteLine(" Enter ID for sample to update: ");
                        string updateId = Console.ReadLine();

                        HttpOperationResponse<Sample> getForUpdateResponse = await getSample(updateId);
                        if (!getForUpdateResponse.Response.IsSuccessStatusCode) { Console.WriteLine(" No matching sample"); }
                        else
                        {
                            Sample sampleForUpdate = getForUpdateResponse.Body;
                            Console.WriteLine(" Retrieved sample: ");
                            Console.WriteLine(" {0}\n {1}\n {2}\n {3}\n",
                                sampleForUpdate.SampleID, sampleForUpdate.Title, sampleForUpdate.Artist, sampleForUpdate.SampleMp3Url);

                            Console.WriteLine(" Enter new song title: ");
                            string newTitle = Console.ReadLine();

                            Console.WriteLine(" Enter new artist name: ");
                            string newArtist = Console.ReadLine();

                            await putSample(new Sample()
                            {
                                SampleID = sampleForUpdate.SampleID,
                                Title = newTitle,
                                Artist = newArtist
                            });
                        }
                        break;

                    // Delete a sample from the table - DELETE
                    case 5:
                        Console.WriteLine(" Enter ID of sample you want to delete");
                        string deleteId = Console.ReadLine();
                        await deleteSample(deleteId);
                        break;

                    // Upload an mp3 to blob storage - PUT
                    case 6:
                        Console.WriteLine(" Enter ID of sample entity you want to upload an mp3 to");
                        string uploadId = Console.ReadLine();
                        HttpOperationResponse<Sample> uploadResponse = await getSample(uploadId);
                        if (!uploadResponse.Response.IsSuccessStatusCode) { Console.WriteLine("No matching entity found by ID input"); }
                        else
                        {
                            Console.WriteLine("Enter full path of file to upload on local filesystem (example format C:/Folder/Filename.ext): ");
                            string fileName = Console.ReadLine();
                            if (File.Exists(fileName)) { await uploadAudio(uploadId, fileName); }
                            else { Console.WriteLine("Invalid file path"); }
                        }

                        break;

                    // Download a sample mp3 from blob storage - GET
                    case 7:
                        Console.WriteLine(" Enter ID of sample entity you want to download the sample from: ");
                        string downloadId = Console.ReadLine();
                        HttpOperationResponse<Sample> downloadResponse = await getSample(downloadId);
                        if (!downloadResponse.Response.IsSuccessStatusCode) { Console.WriteLine("No matching entity found by ID input"); }
                        else
                        {
                            Console.WriteLine(" Enter the folder path you would like the sample downloaded to (example format C:/Folder ");
                            string folderPath = Console.ReadLine();
                            if (Directory.Exists(folderPath)) { await downloadSample(downloadId, folderPath, downloadResponse.Body); }
                            else { Console.WriteLine(" Invalid directory path"); }

                        }
                        break;
                }
            } while (selection != 0);
        }

        /// <summary>
        /// Gets all samples in table - GET
        /// </summary>
        /// <returns></returns>
        static async Task<IList<Sample>> getAllSamples()
        {
            HttpOperationResponse<IList<Sample>> getListResponse = await client.Samples.GetWithHttpMessagesAsync();
            Console.WriteLine(" GET complete; status code: " + getListResponse.Response.StatusCode);

            return getListResponse.Body;
        }

        /// <summary>
        /// Gets sample in table by ID - GET
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        static async Task<HttpOperationResponse<Sample>> getSample(string id)
        {
            HttpOperationResponse<Sample> getSampleResponse = await client.Samples.GetSampleWithHttpMessagesAsync(id);
            Console.WriteLine(" GET complete; status code: " + getSampleResponse.Response.StatusCode);

            return getSampleResponse;
        }

        /// <summary>
        /// Creates new Sample entity from user input - POST
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        static async Task<HttpStatusCode> postSample(Sample sample)
        {
            HttpOperationResponse<Sample> postResponse = await client.Samples.PostSampleWithHttpMessagesAsync(sample);
            Console.WriteLine(" POST complete; status code: {0}; location: {1}", postResponse.Response.StatusCode, postResponse.Response.Headers.Location);

            return postResponse.Response.StatusCode;
        }

        /// <summary>
        /// Updates an existing entity in the table with new info from user input - PUT
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        static async Task<HttpStatusCode> putSample(Sample sample)
        {
            HttpOperationResponse putResponse = await client.Samples.PutSampleWithHttpMessagesAsync(sample.SampleID, sample);
            Console.WriteLine(" PUT complete; status code: " + putResponse.Response.StatusCode);

            return putResponse.Response.StatusCode;
        }

        /// <summary>
        ///  Delete a sample from the table - DELETE
        /// </summary>
        /// <param name="deleteId"></param>
        /// <returns></returns>
        static async Task<HttpStatusCode> deleteSample(string deleteId)
        {
            HttpOperationResponse deleteResponse = await client.Samples.DeleteSampleWithHttpMessagesAsync(deleteId);
            Console.WriteLine(" DELETE complete; status code: " + deleteResponse.Response.StatusCode);

            return deleteResponse.Response.StatusCode;
        }

        /// <summary>
        /// Upload an mp3 to blob storage - PUT
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static async Task<HttpStatusCode> uploadAudio(string id, string fileName)
        {
            // Create HTTP client to send request to API
            HttpClient client = new HttpClient();
            Uri path = new Uri((clientUri + "/api/Data/PutBlob/" + id));


            Console.WriteLine(" Beginning upload of {0} to {1}", fileName, path);
            try
            {

                using (var stream = File.OpenRead(fileName))
                {
                    var response = await client.PutAsync(path, new System.Net.Http.StreamContent(stream));
                    Console.Out.WriteLine("Upload outcome: {0} \n Reason: {1}", response.StatusCode, response.ReasonPhrase);
                }
                Console.WriteLine("Upload complete");

                return HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(" Client - uploadAudio: \n{0} \n{1}", ex.Message, ex.StackTrace);
                return HttpStatusCode.InternalServerError;
            }
        }

        /// <summary>
        /// Downloads a sample to the local filesystem from blob storage - GET
        /// </summary>
        /// <param name="id"></param>
        /// <param name="folderPath"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        static async Task<HttpStatusCode> downloadSample(string id, string folderPath, Sample sample)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                Uri uri = new Uri((clientUri + "/api/Data/GetBlob/" + id));

                Console.WriteLine(" Beginning download of {0} to {1}", sample.Title, folderPath);

                var response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;

                Console.WriteLine(" Downloaded {0} bytes", bytes.Length);

                if (response.IsSuccessStatusCode)
                {
                    FileIOPermission fileAccess = new FileIOPermission(FileIOPermissionAccess.Write, folderPath);
                    fileAccess.Demand();
                    using (FileStream fileStream = new FileStream(folderPath + "/" + sample.Title + ".mp3", FileMode.Create, FileAccess.Write))
                    using (BinaryWriter binaryFileWriter = new BinaryWriter(fileStream))
                    {
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            binaryFileWriter.Write(bytes[i]);
                        }
                        binaryFileWriter.Flush();
                        binaryFileWriter.Close();
                    }
                    return HttpStatusCode.OK;
                }
                else { return HttpStatusCode.OK; }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(" Client - downloadAudio: \n{0} \n{1}", ex.Message, ex.StackTrace);
                return HttpStatusCode.InternalServerError;
            }
        }


    }
}
