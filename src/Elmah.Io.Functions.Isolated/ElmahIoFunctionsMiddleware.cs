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

        public ElmahIoFunctionsMiddleware(IOptions<ElmahIoFunctionOptions> options)
        {
            this.options = options.Value;
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
