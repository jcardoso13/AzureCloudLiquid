using CloudLiquid.Azure.Exceptions;
using CloudLiquid.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace CloudLiquid.Azure
{
    public class ExceptionMiddleware(ILogger<ExceptionMiddleware> logger) : IFunctionsWorkerMiddleware
    {
        #region Private Members

        private readonly ILogger<ExceptionMiddleware> logger = logger;

        #endregion

        #region Public Methods

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var outContentType = context.GetHttpContext()?.Request.Headers.Accept.FirstOrDefault() ?? "application/json";

                string error = string.Empty;
                string function = string.Empty;
                string action = string.Empty;
                string exceptionMessage = string.Empty;
                HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;

                if (ex is DownloadBlobException downloadBlobException)
                {
                    error = "Azure Function failed to download data from Blob Storage";
                    function = downloadBlobException.Function;
                    action = "Reading_Liquid_from_Blob";
                    httpStatusCode = HttpStatusCode.InternalServerError;
                    exceptionMessage = ex.Message;
                }
                else if (ex is ReadRequestException readRequestException)
                {
                    error = "Azure Function failed read Request";
                    function = readRequestException.Function;
                    action = "Reading_Request";
                    httpStatusCode = HttpStatusCode.BadRequest;
                    exceptionMessage = ex.Message;
                }
                else if (ex is ParsingException)
                {
                    error = "Azure Function failed to parse Input";
                    function = Constants.CloudLiquidFunctionName;
                    action = "Parsing_Input";
                    httpStatusCode = HttpStatusCode.BadRequest;
                    exceptionMessage = ex.Message;
                }
                else if (ex is RunTemplateException runTemplateException)
                {
                    error = "Azure Function failed to run liquid template";
                    function = runTemplateException.Function;
                    action = runTemplateException.ErrorAction;
                    httpStatusCode = HttpStatusCode.InternalServerError;
                    exceptionMessage = runTemplateException.Message;
                }
                else if (ex is CreateResponseException createResponseException)
                {
                    error = $"Azure Function Unable to Transform String to {outContentType}";
                    function = createResponseException.Function;
                    action = "Writing_Response";
                    httpStatusCode = HttpStatusCode.InternalServerError;
                    exceptionMessage = ex.Message;
                }

                string errorContents = new CloudError(error, function, action, exceptionMessage).FormatMessage(outContentType);

                logger.LogError(errorContents, ex);

                var req = await context.GetHttpRequestDataAsync();

                var res = req!.CreateResponse();
                res.StatusCode = httpStatusCode;
                await res.WriteStringAsync(errorContents);
                context.GetInvocationResult().Value = res;
            }
        }

        #endregion
    }
}
