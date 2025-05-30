﻿using Elmah.Io.Client;
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
    internal static class MessageShipper
    {
        private static readonly string _assemblyVersion = typeof(MessageShipper).Assembly.GetName().Version.ToString();
        private static readonly string _elmahIoClientAssemblyVersion = typeof(IElmahioAPI).Assembly.GetName().Version.ToString();
        private static readonly string _functionsAssemblyVersion = typeof(FunctionContext).Assembly.GetName().Version.ToString();

#pragma warning disable S2223 // Non-constant static fields should not be visible
        internal static IElmahioAPI elmahIoClient;
#pragma warning restore S2223 // Non-constant static fields should not be visible

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
                Title = baseException?.Message ?? "An error happened",
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

            EnsureClient(options);

            try
            {
                await elmahIoClient.Messages.CreateAndNotifyAsync(options.LogId, createMessage, functionContext.CancellationToken);
            }
            catch (Exception e)
            {
                options.OnError?.Invoke(createMessage, e);
                // If there's a Exception while generating the error page, re-throw the original exception.
            }
        }

        public static void CreateInstallation(ElmahIoFunctionOptions options)
        {
            try
            {
                var logger = new LoggerInfo
                {
                    Type = "Elmah.Io.Functions.Isolated",
                    Assemblies =
                    [
                        new AssemblyInfo
                        {
                            Name = "Elmah.Io.Functions.Isolated",
                            Version = _assemblyVersion,
                        },
                        new AssemblyInfo
                        {
                            Name = "Elmah.Io.Client",
                            Version = _elmahIoClientAssemblyVersion,
                        },
                        new AssemblyInfo
                        {
                            Name = "Microsoft.Azure.Functions.Worker",
                            Version = _functionsAssemblyVersion,
                        }
                    ],
                    ConfigFiles = [],
                    Properties = [],
                    EnvironmentVariables = [],
                };

                EnvironmentVariablesHelper.GetElmahIoAppSettingsEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));
                EnvironmentVariablesHelper.GetAzureFunctionsEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));
                EnvironmentVariablesHelper.GetDotNetEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));
                EnvironmentVariablesHelper.GetAzureEnvironmentVariables().ForEach(v => logger.EnvironmentVariables.Add(v));

                var installation = new CreateInstallation
                {
                    Name = options.Application,
                    Type = "azurefunction",
                    Loggers = [logger]
                };

                EnsureClient(options);

                options.OnInstallation?.Invoke(installation);

                elmahIoClient.Installations.CreateAndNotify(options.LogId, installation);
            }
            catch
            {
                // We don't want to crash the entire application if the installation fails. Carry on.
            }
        }

        private static void EnsureClient(ElmahIoFunctionOptions options)
        {
            if (elmahIoClient == null)
            {
                elmahIoClient = ElmahioAPI.Create(options.ApiKey, new ElmahIoOptions
                {
                    Timeout = options.Timeout,
                    UserAgent = UserAgent(),
                });

                elmahIoClient.Messages.OnMessageFilter += (sender, args) =>
                {
                    var filter = options.OnFilter?.Invoke(args.Message);
                    if (filter.HasValue && filter.Value)
                    {
                        args.Filter = true;
                    }
                };

                elmahIoClient.Messages.OnMessage += (sender, args) =>
                {
                    options.OnMessage?.Invoke(args.Message);
                };
                elmahIoClient.Messages.OnMessageFail += (sender, args) =>
                {
                    options.OnError?.Invoke(args.Message, args.Error);
                };
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
            try
            {
                return request?.Url?.AbsolutePath;
            }
            catch (UriFormatException)
            {
                // GrpcHttpRequestData tries to create URLs from empty strings. In this case there's nothing else to do than to return null.
            }

            return null;
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
            if (request == null) return [];
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
                return [];
            }
        }

        private static List<Item> ServerVariables(HttpRequestData request)
        {
            if (request == null) return [];
            return request
                .Headers?
                .Select(h => new Item(h.Key, h.Value != null && h.Value.Any() ? string.Join(",", h.Value) : string.Empty))
                .ToList();
        }

        private static List<Item> QueryString(HttpRequestData request)
        {
            if (request == null) return [];
            try
            {
                if (request.Url == null) return [];
                if (string.IsNullOrWhiteSpace(request.Url.Query)) return [];
            }
            catch (UriFormatException)
            {
                // GrpcHttpRequestData tries to create URLs from empty strings. In this case there's nothing else to do than to return null.
                return [];
            }

            var query = request.Url.Query.TrimStart('?');
            return query
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    var splitted = s.Split('=');
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

        internal static string UserAgent()
        {
            return new StringBuilder()
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.Functions.Isolated", _assemblyVersion)).ToString())
                .Append(' ')
                .Append(new ProductInfoHeaderValue(new ProductHeaderValue("Microsoft.Azure.Functions.Worker", _functionsAssemblyVersion)).ToString())
                .ToString();
        }
    }
}
