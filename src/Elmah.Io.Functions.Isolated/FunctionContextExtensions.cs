using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Elmah.Io.Functions.Isolated
{
    // Replace with a better solution once the HttpContext gets available in middleware (if ever).
    internal static class FunctionContextExtensions
    {
        // Thank you https://github.com/Azure/azure-functions-dotnet-worker/issues/414#issuecomment-828810635
        public static HttpRequestData GetHttpRequestData(this FunctionContext functionContext)
        {
            try
            {
                KeyValuePair<Type, object> keyValuePair = functionContext.Features.SingleOrDefault(f => f.Key.Name == "IFunctionBindingsFeature");
                if (keyValuePair.Equals(default(KeyValuePair<Type, object>))) return null;
                object functionBindingsFeature = keyValuePair.Value;
                if (functionBindingsFeature == null) return null;
                Type type = functionBindingsFeature.GetType();
                var inputData = type.GetProperties().Single(p => p.Name == "InputData").GetValue(functionBindingsFeature) as IReadOnlyDictionary<string, object>;
                return inputData?.Values.SingleOrDefault(o => o is HttpRequestData) as HttpRequestData;
            }
            catch
            {
                return null;
            }
        }

        // Thank you https://github.com/Azure/azure-functions-dotnet-worker/issues/414#issuecomment-872818004
        public static HttpResponseData GetHttpResponseData(this FunctionContext functionContext)
        {
            try
            {
                var request = functionContext.GetHttpRequestData();
                if (request == null) return null;
                var response = HttpResponseData.CreateResponse(request);
                var keyValuePair = functionContext.Features.FirstOrDefault(f => f.Key.Name == "IFunctionBindingsFeature");
                if (keyValuePair.Equals(default(KeyValuePair<Type, object>))) return null;
                object functionBindingsFeature = keyValuePair.Value;
                if (functionBindingsFeature == null) return null;
                PropertyInfo pinfo = functionBindingsFeature.GetType().GetProperty("InvocationResult");
                pinfo.SetValue(functionBindingsFeature, response);
                return response;
            }
            catch
            {
                return null;
            }
        }
    }
}
