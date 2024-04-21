using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using CloudLiquid;
using CloudLiquid.ContentFactory;
using System.Net;
using System.Security.Policy;
using Grpc.Core;

namespace Azure.Liquid
{
    public class Liquid
    {
        private readonly ILogger<Liquid> _logger;
        private Cache<string, string> _cache;

        public Liquid(ILogger<Liquid> logger)
        {
            _logger = logger;
            _cache = new Cache<string, string>();
            CloudLiquid.Liquid.log = logger;
            CloudLiquid.Liquid.FileSystem = Storage.Client;
        }

        [Function("CloudLiquid")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string inContentType, outContentType, inputBlob,contentBody;
            StreamReader reader;
            Task<string> contentStream;
            _logger.LogInformation("CloudLiquid HTTP trigger function processed a request.");
            inContentType = req?.Headers?.ContentType.FirstOrDefault() ?? "application/json";
            outContentType = req?.Headers?.Accept.FirstOrDefault() ?? "application/json";
            reader = new(req?.Body ?? new MemoryStream());
            contentStream = reader.ReadToEndAsync();
            var contentReader = ContentFactory.GetContentReader(inContentType);
            var contentWriter = ContentFactory.GetContentWriter(outContentType);
            try
            {
                inputBlob = Storage.Azure(_logger, false, req?.Headers.From.FirstOrDefault() ?? String.Empty, _cache);
            }
            catch (Exception ex)
            {
                var error = new CloudError("Azure Function failed to download data from Blob Storage", "CloudLiquid", "Reading_Liquid_from_Blob", HttpStatusCode.InternalServerError, ex.Message
                ).FormatMessage(outContentType);
                _logger.LogError(error);
                return new ContentResult()
                {
                    Content = error,
                    ContentType = outContentType,
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
            _logger.LogInformation("Incoming Request Content Type:" + inContentType);
            _logger.LogInformation("Outcoming Response Content Type:" + outContentType);

            if (inputBlob == String.Empty || inputBlob == null)
            {
                var error = new CloudError("Azure Function failed to download data from Blob Storage", "CloudLiquid", "Reading_Liquid_from_Blob", HttpStatusCode.BadRequest, null).FormatMessage(outContentType);
                _logger.LogError(error);
                return new ContentResult()
                {
                    Content = error,
                    ContentType = outContentType,
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }
            try{
             contentBody = await contentStream;
            }
            catch (Exception ex)
            {
                var error = new CloudError("Azure Function failed read Request", "CloudLiquid", "Reading_Request", HttpStatusCode.BadRequest, ex.Message).FormatMessage(outContentType);
                _logger.LogError(error, ex);
                return new ContentResult()
                {
                    Content = error,
                    ContentType = outContentType,
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }
            DotLiquid.Hash inputHash;
            _logger.LogInformation("Content:"+contentBody);
            try
            {
                inputHash = contentReader.ParseString(contentBody);
            }
            catch (Exception ex)
            {
                var error = new CloudError("Azure Function failed to parse Input", "CloudLiquid", "Parsing_Input", HttpStatusCode.BadRequest, ex.Message).FormatMessage(outContentType);
                _logger.LogError(error, ex);
                return new ContentResult()
                {
                    Content = error,
                    ContentType = outContentType,
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }
            //Template.RegisterFilter(typeof(CustomFilters));
            string output = String.Empty;
            _logger.LogInformation(contentBody);
            try
            {
                output = CloudLiquid.Liquid.Run(inputHash, inputBlob);
            }
            catch (Exception ex)
            {
                var error = new CloudError(CloudLiquid.Liquid.message, "CloudLiquid", CloudLiquid.Liquid.action, HttpStatusCode.InternalServerError, ex.Message).FormatMessage(outContentType);
                _logger.LogError(error);
                return new ContentResult()
                {
                    Content = error,
                    ContentType = outContentType,
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
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
                _logger.LogError(ex.Message, ex);
                return new ContentResult()
                {
                    Content = new CloudError("Azure Function Unable to Transform String to " + outContentType, "CloudLiquid", "Writing_Response", HttpStatusCode.InternalServerError, ex.Message).FormatMessage(outContentType),
                    ContentType = outContentType,
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }
    }
}



