using Elmah.Io.Functions.Isolated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults((context, app) =>
    {
        app.AddHeartbeat(options =>
        {
            options.ApiKey = "API_KEY";
            options.LogId = new Guid("LOG_ID");
            options.HeartbeatId = "HEARTBEAT_ID";
        });

        // To fetch config from local.settings.json, environment variables, or similar, use the following code:
        //app.Services.Configure<ElmahIoFunctionOptions>(context.Configuration.GetSection("ElmahIo"));
        //app.AddHeartbeat();
    })
    .Build();

host.Run();
