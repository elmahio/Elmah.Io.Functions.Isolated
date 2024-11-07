using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Elmah.Io.Functions.Isolated
{
    internal class ElmahIoFunctionsMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ElmahIoFunctionOptions options;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3928:Parameter names used into ArgumentException constructors should match an existing one ", Justification = "The arguments are on options")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "The arguments are on options")]
        public ElmahIoFunctionsMiddleware(IOptions<ElmahIoFunctionOptions> options)
        {
            this.options = options.Value;
            if (string.IsNullOrWhiteSpace(this.options.ApiKey)) throw new ArgumentNullException(nameof(this.options.ApiKey));
            if (this.options.LogId == Guid.Empty) throw new ArgumentNullException(nameof(this.options.LogId));

            MessageShipper.CreateInstallation(this.options);
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                await MessageShipper.Ship(e, context, options);
                throw;
            }
        }
    }
}
