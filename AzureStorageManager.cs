using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace CloudLiquid.Azure
{
    public class AzureStorageManager(ILogger logger)
    {
        #region Private Members

        private readonly BlobContainerClient blobContainerClient = GetBlobContainerClient();
        private readonly ILogger logger = logger;

        #endregion

        #region Constructors

        #endregion

        #region Public Properties

        public BlobContainerClient BlobContainerClient
        {
            get
            {
                return this.blobContainerClient;
            }
        }

        #endregion

        #region Public Methods

        public string GetLiquidBlobContents(bool filesystem, string filename, Cache<string,string> cache)
        {
            filename += ".liquid";

            logger.LogInformation($"File Read: {filename}\nStorageAccount: {blobContainerClient.AccountName}");

            if (!filesystem)
            {
                var value = cache.Get(filename);

                if (value != null)
                {
                    return value;
                }

                var getBlobClientResponse = blobContainerClient.GetBlobClient(filename).Download()?.Value?.Content;

                var output = getBlobClientResponse is null ? String.Empty : new StreamReader(getBlobClientResponse).ReadToEnd();

                if (!string.IsNullOrEmpty(output))
                {
                    cache.Store(filename, output, TimeSpan.FromHours(1));
                }
                return output;
            }
            else
            {
                return Environment.GetEnvironmentVariable(filename) ?? String.Empty;
            }
        }

        #endregion

        #region Private Methods

        private static BlobContainerClient GetBlobContainerClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("StorageAccountConnectionString");
            var containerName = Environment.GetEnvironmentVariable("ContainerName");

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(containerName))
            {
                throw new Exception("Environment variables for StorageAccountConnectionString or ContainerName are not set.");
            }

            var storageAccount = new BlobServiceClient(Environment.GetEnvironmentVariable("StorageAccountConnectionString"));

            return storageAccount.GetBlobContainerClient(Environment.GetEnvironmentVariable("ContainerName"));
        }

        #endregion
    }
}