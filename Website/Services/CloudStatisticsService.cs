﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace NuGetGallery
{
    public class CloudStatisticsService : IStatisticsService
    {
        private string _connectionString;

        public CloudStatisticsService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string LoadReport(string name)
        {
            string connectionString = _connectionString;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("popularity");
            CloudBlockBlob blob = container.GetBlockBlobReference(name);

            //TODO: async OpenRead

            string content;
            using (TextReader reader = new StreamReader(blob.OpenRead()))
            {
                content = reader.ReadToEnd();
            }

            return content;
        }
    }
}