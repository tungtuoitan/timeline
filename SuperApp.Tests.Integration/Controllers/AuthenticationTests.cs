using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SuperApp.Tests.Integration.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SuperApp.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for authentication endpoints
/// </summary>
public class AuthenticationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthenticationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_MissingCredentials_ReturnsBadRequest()
    {
        // Arrange
        var request = new 
        {
            Email = "",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authen/login", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new 
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authen/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthEndpoint_WithMaliciousInput_ReturnsProperValidation()
    {
        // Arrange - Test SQL injection attempt
        var request = new 
        {
            Email = "'; DROP TABLE Users; --",
            Password = "password"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authen/login", request);

        // Assert - Should return validation error, not crash
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new 
        {
            Email = "invalid-email",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            Name = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authen/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PasswordMismatch_ReturnsBadRequest()
    {
        // Arrange
        var request = new 
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "DifferentPassword123!",
            Name = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authen/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_LongPassword_ReturnsValidationError()
    {
        // Arrange - Test extremely long password
        var longPassword = new string('a', 1000);
        var request = new 
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = longPassword,
            ConfirmPassword = longPassword,
            Name = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authen/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}