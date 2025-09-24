#pragma warning disable S125 // Sections of code should not be commented out
using Elmah.Io.Functions.Isolated;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// To fetch config from the Values object in local.settings.json,
// environment variables, or similar, use the following code:
//var apiKey = builder.Configuration["ApiKey"];
//var logId = builder.Configuration["LogId"];

// If the settings are stored elsewhere in local.settings.json, you need to load it
// on localhost and replace it with environment variables when running in Azure.
//#if DEBUG
//builder.Configuration.AddJsonFile("local.settings.json");
//#endif

// If using the ElmahIo section in local.settings.json, use the following code:
//builder.Services.Configure<ElmahIoFunctionOptions>(builder.Configuration.GetSection("ElmahIo"));

builder.AddElmahIo(options =>
{
    options.ApiKey = "API_KEY";
    options.LogId = new Guid("LOG_ID");

    // Optional set application name on all errors
    options.Application = "Isolated Azure Functions application";

    // Optional enrich all errors with one or more properties
    options.OnMessage = m =>
    {
        m.Version = "9.0.0";
    };

    // Enrich installation when notifying elmah.io after launch:
    //options.OnInstallation = installation =>
    //{
    //    installation.Name = "Isolated Azure Functions application";
    //    var logger = installation.Loggers.FirstOrDefault(l => l.Type == "Elmah.Io.Functions.Isolated");
    //    logger?.Properties.Add(new Elmah.Io.Client.Item("Foo", "Bar"));
    //};
}
);

await builder.Build().RunAsync();
#pragma warning restore S125 // Sections of code should not be commented out
