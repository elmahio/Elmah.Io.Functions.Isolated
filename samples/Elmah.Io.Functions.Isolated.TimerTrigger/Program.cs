using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace Elmah.Io.Functions.Isolated.TimerTrigger
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(Directory.GetCurrentDirectory());
#if DEBUG
                    c.AddJsonFile("local.settings.json");
#endif
                    c.AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults((context, app) =>
                {
                    app.AddElmahIo(options =>
                    {
                        options.ApiKey = "API_KEY";
                        options.LogId = new Guid("LOG_ID");
                    });

                    // To fetch config from local.settings.json, environment variables, or similar, use the following code:
                    //app.Services.Configure<ElmahIoFunctionOptions>(context.Configuration.GetSection("ElmahIo"));
                    //app.AddElmahIo();
                })
                .Build();

            host.Run();
        }
    }
}