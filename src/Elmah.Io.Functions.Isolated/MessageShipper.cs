using Elmah.Io.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Elmah.Io.Functions.Isolated
{
    internal class MessageShipper
    {
        internal static string _assemblyVersion = typeof(MessageShipper).Assembly.GetName().Version.ToString();
        internal static string _functionsAssemblyVersion = typeof(FunctionContext).Assembly.GetName().Version.ToString();

        internal static IElmahioAPI elmahIoClient;

        public static async Task Ship(Exception exception, FunctionContext functionContext, ElmahIoFunctionOptions options)
        {
            var request = functionContext.GetHttpRequestData();
            var response = functionContext.GetHttpResponseData();

            var baseException = exception?.GetBaseException();
            var createMessage = new CreateMessage
            {
                DateTime = DateTime.UtcNow,
                Detail = Detail(exception),
                Type = baseException?.GetType().FullName,
                Title = baseException.Message,
                Data = Data(exception, functionContext),
                Cookies = Cookies(request),
                ServerVariables = ServerVariables(request),
                StatusCode = StatusCode(response),
                Url = Url(request),
                QueryString = QueryString(request),
                Method = request?.Method,
                Severity = Severity.Error.ToString(),
                Source = Source(baseException),
                Application = options.Application,
                Hostname = Hostname(),
            };

            if (options.OnFilter != null && options.OnFilter(createMessage))
            {
                return;
            }

            if (elmahIoClient == null)
            {
                elmahIoClient = ElmahioAPI.Create(options.ApiKey, new ElmahIoOptions
                {
                    Timeout = options.Timeout,
                    UserAgent = UserAgent(),
                });

                elmahIoClient.Messages.OnMessage += (sender, args) =>
                {
                    options.OnMessage?.Invoke(args.Message);
                };
                elmahIoClient.Messages.OnMessageFail += (sender, args) =>
                {
                    options.OnError?.Invoke(args.Message, args.Error);
                };
            }

            try
            {
                await elmahIoClient.Messages.CreateAndNotifyAsync(options.LogId, createMessage);
            }
            catch (Exception e)
            {
                options.OnError?.Invoke(createMessage, e);
                // If there's a Exception while generating the error page, re-throw the original exception.
            }
        }

        private static string Hostname()
        {
            var machineName = Environment.MachineName;
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            machineName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            machineName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

            return null;
        }

        private static string Url(HttpRequestData request)
        {
            return request?.Url?.AbsolutePath;
        }

        /// <summary>
        /// Combine properties from exception Data dictionary and Azure Functions filter context properties
        /// </summary>
        private static IList<Item> Data(Exception exception, FunctionContext functionContext)
        {
            var data = new List<Item>();
            var exceptionData = exception.ToDataList();
            if (exceptionData?.Count > 0)
            {
                data.AddRange(exceptionData);
            }

            foreach (var property in functionContext.Items.Where(p => p.Key != null))
            {
                data.Add(new Item { Key = property.Key.ToString(), Value = property.Value?.ToString() });
            }

            data.Add(new Item { Key = nameof(functionContext.InvocationId), Value = functionContext.InvocationId });
            data.Add(new Item { Key = nameof(functionContext.FunctionId), Value = functionContext.FunctionId });
            data.Add(new Item { Key = "FunctionName", Value = functionContext.FunctionDefinition.Name });

            return data;
        }

        private static string Detail(Exception exception)
        {
            return exception?.ToString();
        }

        private static List<Item> Cookies(HttpRequestData request)
        {
            if (request == null) return null;
            try
            {
                return request
                    .Cookies?
                    .Select(c => new Item(c.Name, c.Value))
                    .ToList();
            }
            catch
            {
                // The functions runtime sometimes throw exceptions while fetching cookies like IndexOutOfBoundsException.
                return null;
            }
        }

        private static List<Item> ServerVariables(HttpRequestData request)
        {
            if (request == null) return null;
            return request
                .Headers?
                .Select(h => new Item(h.Key, h.Value != null && h.Value.Any() ? string.Join(",", h.Value) : string.Empty))
                .ToList();
        }

        private static List<Item> QueryString(HttpRequestData request)
        {
            if (request == null) return null;
            if (request.Url == null) return null;
            if (string.IsNullOrWhiteSpace(request.Url.Query)) return null;

            var query = request.Url.Query.TrimStart('?');
            return query
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    var splitted = s.Split(new[] { '=' });
                    var item = new Item();
                    if (splitted.Length > 0) item.Key = splitted[0];
                    if (splitted.Length > 1) item.Value = splitted[1];
                    return item;
                })
                .ToList();
        }

        private static int? StatusCode(HttpResponseData response)
        {
            if (response == null) return null;
            return (int)response.StatusCode;
        }

        private static string Source(Exception baseException)
        {
            return baseException?.Source;
        }

        private static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Functions.Isolated", _assemblyVersion)).ToString())
                .Append(" ")
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Microsoft.Azure.Functions.Worker", _functionsAssemblyVersion)).ToString())
                .ToString();
        }
    }
}
