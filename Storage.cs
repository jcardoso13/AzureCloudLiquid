






using System;
using System.IO;
using Azure.Storage.Blobs;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Azure.Liquid
{
    public static class Storage
    {
        public static BlobContainerClient Client;
        private static string AccountName;
        public static string Azure(ILogger _logger, bool filesystem, string filename, Cache<string,string> _cache)
        {
            filename += ".liquid";
            _logger.LogInformation("File Read:"+filename+"\nStorageAccount:"+Client.AccountName);
            if (!filesystem)
            {
                var value =_cache.Get(filename);
                if(value != null ) return value;
                var AZResponse = Client.GetBlobClient(filename).Download()?.Value?.Content;
                var output = AZResponse is null ? String.Empty : new StreamReader(AZResponse).ReadToEnd();
                if (output != String.Empty) _cache.Store(filename,output,TimeSpan.FromHours(1));
                return output;
            }
            else
            {
                return Environment.GetEnvironmentVariable(filename) ?? String.Empty;
            }
        }

        static Storage()
        {
                var storageAccount = new BlobServiceClient(Environment.GetEnvironmentVariable("StorageAccountName"));
                AccountName=storageAccount.AccountName.ToString();
                Client = storageAccount.GetBlobContainerClient(Environment.GetEnvironmentVariable("ContainerName"));
        }
    }
}