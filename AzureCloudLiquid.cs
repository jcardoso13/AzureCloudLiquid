using CloudLiquid.Azure.Exceptions;
using CloudLiquid.Core;
using CloudLiquid.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CloudLiquid.Azure
{
    public class AzureFunctions
    {
        #region Private Members

        private readonly ILogger<AzureFunctions> logger;
        private readonly Cache<string, string> cache;
        private readonly LiquidProcessor liquidProcessor;
        private readonly AzureStorageManager azureStorageManager;

        #endregion

        #region Constructors

        public AzureFunctions(ILogger<AzureFunctions> logger)
        {
            this.logger = logger;
            this.cache = new Cache<string, string>();
            this.azureStorageManager = new AzureStorageManager(logger);
            this.liquidProcessor = new LiquidProcessor(logger, azureStorageManager.BlobContainerClient);
            this.liquidProcessor.InitializeTagsAndFilters();
        }

        #endregion

        #region Public Methods

        [Function("CloudLiquid")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string inContentType;
            string outContentType;
            string liquidBlobContents;
            string contentBody;
            StreamReader reader;
            Task<string> contentStream;

            logger.LogInformation("CloudLiquid HTTP trigger function processed a request.");

            inContentType = req?.Headers?.ContentType.FirstOrDefault() ?? "application/json";
            outContentType = req?.Headers?.Accept.FirstOrDefault() ?? "application/json";

            reader = new(req?.Body ?? new MemoryStream());
            contentStream = reader.ReadToEndAsync();

            var contentReader = ContentFactory.ContentFactory.GetContentReader(inContentType);
            var contentWriter = ContentFactory.ContentFactory.GetContentWriter(outContentType);

            try
            {
                liquidBlobContents = azureStorageManager.GetLiquidBlobContents(false, req?.Headers.From.FirstOrDefault() ?? String.Empty, cache);
            }
            catch (Exception ex)
            {
                throw new DownloadBlobException(ex.Message, Constants.CloudLiquidFunctionName);
            }

            logger.LogInformation("Incoming Request Content Type:" + inContentType);
            logger.LogInformation("Outcoming Response Content Type:" + outContentType);

            if (string.IsNullOrEmpty(liquidBlobContents))
            {
                throw new DownloadBlobException("Empty blob content", Constants.CloudLiquidFunctionName);
            }

            try
            {
                contentBody = await contentStream;
            } 
            catch(Exception ex)
            {
                throw new ReadRequestException(ex.Message, Constants.CloudLiquidFunctionName);
            }

            DotLiquid.Hash inputHash;

            logger.LogInformation($"Content: {contentBody}");

            try
            {
                inputHash = contentReader.ParseString(contentBody);
            }
            catch (Exception ex)
            {
                throw new ParsingException(ex.Message, Constants.CloudLiquidFunctionName);
            }
            
            string output;

            logger.LogInformation(contentBody);

            try
            {
                RunResult result = liquidProcessor.Run(inputHash, liquidBlobContents);

                if (!result.Success)
                {
                    throw new RunTemplateException($"Error while running template: {result.ErrorMessage}", Constants.CloudLiquidFunctionName, result.ErrorAction);
                }

                output = result.Output;
            }
            catch(RunTemplateException)
            { 
                throw; 
            }
            catch (Exception ex)
            {
                throw new RunTemplateException($"Error while running template: {ex.Message}", Constants.CloudLiquidFunctionName, "RunTemplate");
            }
            
            try
            {
                var content = contentWriter.CreateResponse(output);
                return new ContentResult()
                {
                    Content = output,
                    ContentType = outContentType,
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                throw new CreateResponseException(ex.Message, "CoudLiquid");
            }
        }

        #endregion
    }
}