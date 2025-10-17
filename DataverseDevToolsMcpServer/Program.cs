using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.PowerPlatform.Dataverse.Client;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

public class Program
{
    public static async Task Main(string[] args)
    {
        string environmentUrl = null;
        string tenantId = null;
        string clientId = null;
        string clientSecret = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--environmentUrl" && (i + 1) < args.Length)
            {
                environmentUrl = args[i + 1];
            }
            else if (args[i] == "--tenantId" && (i + 1) < args.Length)
            {
                tenantId = args[i + 1];
            }
            else if (args[i] == "--clientId" && (i + 1) < args.Length)
            {
                clientId = args[i + 1];
            }
            else if (args[i] == "--clientSecret" && (i + 1) < args.Length)
            {
                clientSecret = args[i + 1];
            }
        }

        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            // Configure all logs to go to stderr
            consoleLogOptions.LogToStandardErrorThreshold = Microsoft.Extensions.Logging.LogLevel.Trace;
        });

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly();

        builder.Services.AddLogging(logging => {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
        });

        builder.Services.AddSingleton(_ =>{
            string connectionString;
            
            // Check if client credentials are provided
            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                // Use client credentials authentication
                connectionString = $"AuthType=ClientSecret;Url={environmentUrl};ClientId={clientId};ClientSecret={clientSecret};TenantId={tenantId}";
            }
            else
            {
                // Fall back to interactive OAuth authentication
                connectionString = $"AuthType=OAuth;Url={environmentUrl};RedirectUri=http://localhost;LoginPrompt=Auto";
            }
            
            var crm = new ServiceClient(connectionString);
            if (!crm.IsReady)
            {
                throw new McpException($"Failed to connect to Dataverse at {environmentUrl}. Error: {crm.LastError}");
            }
            return crm;
        });

        await builder.Build().RunAsync();
    }
}

