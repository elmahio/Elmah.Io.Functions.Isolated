using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Elmah.Io.Functions.Isolated
{
    /// <summary>
    /// Extensions methods for adding logging to elmah.io.
    /// </summary>
    public static class ElmahIoFunctionsExtensions
    {
        /// <summary>
        /// Add elmah.io without any options. Calling this method requires you to configure elmah.io options manually like this:
        /// <code>app.Services.Configure&lt;ElmahIoFunctionOptions&gt;(configuration.GetSection("ElmahIo"));</code>
        /// </summary>
        public static IFunctionsWorkerApplicationBuilder AddElmahIo(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UseMiddleware<ElmahIoFunctionsMiddleware>();
            return builder;
        }

        /// <summary>
        /// Add elmah.io with the specified options.
        /// </summary>
        public static IFunctionsWorkerApplicationBuilder AddElmahIo(this IFunctionsWorkerApplicationBuilder builder, Action<ElmahIoFunctionOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.AddElmahIo();
            return builder;
        }
    }
}
