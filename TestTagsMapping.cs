using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Repositories;

/// <summary>
/// Simple test to verify the Tags mapping fix
/// </summary>
public class TestTagsMapping
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<TestTagsMapping>()
            .Build();
            
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<IConnectionFactory, ConnectionFactory>();
        services.AddScoped<TagRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<TestTagsMapping>>();
        
        try
        {
            logger.LogInformation("Testing Tags mapping fix...");
            
            var tagRepository = serviceProvider.GetRequiredService<TagRepository>();
            var tags = await tagRepository.GetTags(14);
            
            logger.LogInformation("SUCCESS! Retrieved {TagCount} tags for userId = 14", tags.Count);
            
            // Show first few tags
            foreach (var tag in tags.Take(5))
            {
                logger.LogInformation("Tag: ID={Id}, Name={Name}, UserId={UserId}, ParentId={ParentId}, Depth={Depth}",
                    tag.Id, tag.Name, tag.UserId, tag.ParentId, tag.Depth);
            }
            
            if (tags.Count > 5)
            {
                logger.LogInformation("... and {MoreCount} more tags", tags.Count - 5);
            }
            
            logger.LogInformation("Tags mapping fix test completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tags mapping fix test failed");
        }
    }
}