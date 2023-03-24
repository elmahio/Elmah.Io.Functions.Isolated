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

        /// <summary>
        /// Add the elmah.io heartbeat middleware that will log either a healthy or unhealth heartbeat every time a function is running. This is primarily indented for
        /// timed functions. Functions executed often should manually log heartbeats from a pre-determined schedule to avoid reaching the request limit on the elmah.io API.
        /// Calling this method requires you to configure elmah.io options manually like this:
        /// <code>app.Services.Configure&lt;ElmahIoFunctionOptions&gt;(configuration.GetSection("ElmahIo"));</code>
        /// </summary>
        public static IFunctionsWorkerApplicationBuilder AddHeartbeat(this IFunctionsWorkerApplicationBuilder builder)
        {
            builder.UseMiddleware<ElmahIoHeartbeatMiddleware>();
            return builder;
        }

        /// <summary>
        /// Add the elmah.io heartbeat middleware that will log either a healthy or unhealth heartbeat every time a function is running. This is primarily indented for
        /// timed functions. Functions executed often should manually log heartbeats from a pre-determined schedule to avoid reaching the request limit on the elmah.io API.
        /// </summary>
        public static IFunctionsWorkerApplicationBuilder AddHeartbeat(this IFunctionsWorkerApplicationBuilder builder, Action<ElmahIoFunctionOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.AddHeartbeat();
            return builder;
        }
    }
}
