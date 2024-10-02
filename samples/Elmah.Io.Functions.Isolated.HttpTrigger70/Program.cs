using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elmah.Io.Functions.Isolated;
using Microsoft.Extensions.Configuration;

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

    // The following lines are needed if you want to load configuration from the ElmahIo object in local.settings.json.
    // As a default, only values inside the Values object is made available through IConfiguration. By loading the full
    // local.settings.json file when running locally, the GetSection("ElmahIo") method works as intended.
    .ConfigureAppConfiguration(c =>
    {
        c.SetBasePath(Directory.GetCurrentDirectory());
#if DEBUG
        c.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
#endif
        c.AddEnvironmentVariables();
    })
    .Build();

host.Run();
