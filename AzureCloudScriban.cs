using CloudLiquid.Azure.Exceptions;
using CloudLiquid.Core;
using CloudLiquid.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Scriban;
using Scriban.Runtime;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CloudLiquid.Azure
{
    public class AzureFunctionScriban
    {
        #region Private Members

        private readonly ILogger<AzureFunctionScriban> logger;
        private readonly Cache<string, string> cache;
        private readonly ScribanProcessor scribanProcessor;
        private readonly AzureStorageManager azureStorageManager;

        #endregion

        #region Constructors

        public AzureFunctionScriban(ILogger<AzureFunctionScriban> logger)
        {
            this.logger = logger;
            this.cache = new Cache<string, string>();
            this.azureStorageManager = new AzureStorageManager(logger);
            this.scribanProcessor = new ScribanProcessor(logger, azureStorageManager.BlobContainerClient);
            this.scribanProcessor.InitializeFunctions();
        }

        #endregion

        #region Public Methods

        [Function("CloudScriban")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            string inContentType;
            string outContentType;
            string liquidBlobContents;
            string contentBody;
            StreamReader reader;
            Task<string> contentStream;
            Task? sync = Task.CompletedTask;

            logger.LogInformation("CloudLiquid HTTP trigger function processed a request.");

            inContentType = req?.Headers?.ContentType.FirstOrDefault() ?? "application/json";
            outContentType = req?.Headers?.Accept.FirstOrDefault() ?? "application/json";

            reader = new(req?.Body ?? new MemoryStream());
            contentStream = reader.ReadToEndAsync();

            var contentReader = ContentFactory.ContentFactory.GetContentReader(inContentType);
            var contentWriter = ContentFactory.ContentFactory.GetContentWriter(outContentType);
            try
            {
                liquidBlobContents = azureStorageManager.GetLiquidBlobContents(false, req?.Headers.From.FirstOrDefault() ?? String.Empty, cache,ref sync);
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
                await sync;
                throw new ReadRequestException(ex.Message, Constants.CloudLiquidFunctionName);
            }

            ScriptObject inputHash = new ScriptObject();;
            try
            {
                var parse = ScriptObject.From(JsonSerializer.Deserialize<JsonElement>(contentBody, new JsonSerializerOptions{}));
                inputHash.Add("content",parse);
            }
            catch (Exception ex)
            {
                await sync;
                throw new ParsingException(ex.Message, Constants.CloudLiquidFunctionName);
            }
            
            string output;
            try
            {
                RunResult result = scribanProcessor.Run(inputHash, liquidBlobContents);

                if (!result.Success)
                {
                    await sync;
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
                await sync;
                throw new RunTemplateException($"Error while running template: {ex.Message}", Constants.CloudLiquidFunctionName, "RunTemplate");
            }
            
            try
            {
                var content = contentWriter.CreateResponse(output);
                await sync;
                return new ContentResult()
                {
                    Content = output,
                    ContentType = outContentType,
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                await sync;
                throw new CreateResponseException(ex.Message, "CloudLiquid");
            }
        }

        #endregion
    }
}