using McpServerKbContentFetcher.Tools;
using McpServerKbContentFetcher.Services;
using McpServerKbContentFetcher.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace UnitTests;

/// <summary>
/// Lightweight anti-drift test ensuring only the approved tool surface exists
/// (get_kb_info, get_kb_content) and no legacy or accidental additions.
/// </summary>
public class ToolSurfaceAntiDriftTests
{
    [Fact]
    public void ToolSurface_Should_Match_Minimal_Approved_Set()
    {
        // Arrange: mirror Program.cs relevant registrations with mocked dependencies
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>()).Build();
        services.Configure<ServerOptions>(config);
        
        // Add logging
        services.AddLogging();
        
        // Use mock cache to avoid file system dependencies in unit tests
        var mockCache = Substitute.For<IKnowledgeBaseContentCache>();
        services.AddSingleton(mockCache);
        
        services.AddSingleton<IKnowledgeBaseService, FileKnowledgeBaseService>();
        services.AddSingleton<GetKbInfoTool>();
        services.AddSingleton<GetKbContentTool>();
        using var provider = services.BuildServiceProvider();

        // Act
        var kbInfoTool = provider.GetService<GetKbInfoTool>();
        var kbContentTool = provider.GetService<GetKbContentTool>();
        var toolTypeNames = typeof(GetKbInfoTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == typeof(GetKbInfoTool).Namespace && t.Name.EndsWith("Tool"))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToArray();

        // Assert
        Assert.NotNull(kbInfoTool);
        Assert.NotNull(kbContentTool);
        Assert.Equal(new[]{ nameof(GetKbContentTool), nameof(GetKbInfoTool) }.OrderBy(n=>n), toolTypeNames);
        Assert.DoesNotContain(toolTypeNames, n => n.Contains("Search", StringComparison.OrdinalIgnoreCase));
    }
}
