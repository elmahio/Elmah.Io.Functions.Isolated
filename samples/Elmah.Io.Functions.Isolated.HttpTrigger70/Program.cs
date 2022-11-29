using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elmah.Io.Functions.Isolated;

var host = new HostBuilder()
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
