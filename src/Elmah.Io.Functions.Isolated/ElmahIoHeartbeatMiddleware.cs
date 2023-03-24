using Elmah.Io.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Elmah.Io.Functions.Isolated
{
    internal class ElmahIoHeartbeatMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ElmahIoFunctionOptions options;
        internal IElmahioAPI api;

        public ElmahIoHeartbeatMiddleware(IOptions<ElmahIoFunctionOptions> options)
        {
            this.options = options.Value;
            if (string.IsNullOrWhiteSpace(this.options.ApiKey)) throw new ArgumentNullException(nameof(this.options.ApiKey));
            if (this.options.LogId == Guid.Empty) throw new ArgumentNullException(nameof(this.options.LogId));
            if (string.IsNullOrWhiteSpace(this.options.HeartbeatId)) throw new ArgumentNullException(nameof(this.options.HeartbeatId));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            api ??= ElmahioAPI.Create(options.ApiKey, new ElmahIoOptions
                {
                    Timeout = options.Timeout,
                    UserAgent = MessageShipper.UserAgent(),
                });

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                await next(context);

                await api.Heartbeats.HealthyAsync(options.LogId, options.HeartbeatId, took: stopwatch.ElapsedMilliseconds, cancellationToken: context.CancellationToken);
            }
            catch (Exception e)
            {
                await api.Heartbeats.UnhealthyAsync(options.LogId, options.HeartbeatId, e?.ToString(), took: stopwatch.ElapsedMilliseconds, cancellationToken: context.CancellationToken);
                throw;
            }
        }
    }
}
