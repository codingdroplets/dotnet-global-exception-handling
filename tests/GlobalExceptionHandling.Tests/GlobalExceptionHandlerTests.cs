using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GlobalExceptionHandling.Tests;

/// <summary>
/// Integration tests that verify the GlobalExceptionHandler maps each exception type
/// to the correct HTTP status code and Problem Details response body.
/// </summary>
public class GlobalExceptionHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GlobalExceptionHandlerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_ExistingId_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/products/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_NonExistingId_Returns404WithProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/products/9999");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.ToString());

        var doc = JsonDocument.Parse(body);
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateProduct_EmptyName_Returns400WithValidationErrors()
    {
        // Arrange
        var payload = new { Name = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", payload);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.ToString());

        var doc = JsonDocument.Parse(body);
        Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Validation Failed", doc.RootElement.GetProperty("title").GetString());
        Assert.True(doc.RootElement.TryGetProperty("errors", out _), "Response should contain 'errors' field");
    }

    [Fact]
    public async Task CreateProduct_ValidName_Returns201()
    {
        // Arrange
        var payload = new { Name = "Mouse" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_Returns403WithProblemDetails()
    {
        // Act
        var response = await _client.DeleteAsync("/api/products/1");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.ToString());

        var doc = JsonDocument.Parse(body);
        Assert.Equal(403, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SimulateCrash_Returns500WithProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/products/crash");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.ToString());

        var doc = JsonDocument.Parse(body);
        Assert.Equal(500, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", doc.RootElement.GetProperty("title").GetString());

        // Ensure internal exception message is NOT leaked to the client
        var detail = doc.RootElement.GetProperty("detail").GetString();
        Assert.DoesNotContain("Database connection lost", detail ?? string.Empty);
    }
}
