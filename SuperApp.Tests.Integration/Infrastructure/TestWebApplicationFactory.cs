using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SuperApp.Tests.Integration.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<SuperAppAPI.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.Test.json", optional: false)
                  .AddEnvironmentVariables();
        });

        builder.ConfigureServices(services =>
        {
            // Remove any existing logging configuration for tests
            var serviceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ILogger<>));
            
            if (serviceDescriptor != null)
                services.Remove(serviceDescriptor);

            // Add test-specific services here if needed
            // For example, mock repositories or external services
        });

        builder.UseEnvironment("Test");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}