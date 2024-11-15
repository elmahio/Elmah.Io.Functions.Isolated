using Elmah.Io.Functions.Isolated;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// To fetch config from local.settings.json, environment variables, or similar, use the following code:
//var apiKey = builder.Configuration["ApiKey"];
//var logId = builder.Configuration["LogId"];

builder.AddElmahIo(options =>
{
    options.ApiKey = "API_KEY";
    options.LogId = new Guid("LOG_ID");
});

await builder.Build().RunAsync();
