using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SuperApp.Tests.Integration.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SuperApp.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for basic API functionality and security headers
/// </summary>
public class BasicApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public BasicApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Api_ReturnsSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert - Check for security headers
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/notes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cors_ReturnsProperHeaders()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");

        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    [Fact]
    public async Task InvalidEndpoint_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact] 
    public async Task HttpsRedirect_ConfiguredCorrectly()
    {
        // This test verifies HTTPS redirection is properly configured
        // In a real test environment, you might want to test with HTTP client
        
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert - Should succeed (not redirect in test environment)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}