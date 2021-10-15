using System;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Elmah.Io.Functions.Isolated.HttpTrigger
{
    public static class Function1
    {
        [Function("Function1")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("Function1");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            throw new Exception("An error happened");
        }
    }
}
