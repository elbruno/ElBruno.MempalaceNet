using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using MemPalace.Ai;

namespace MemPalace.Tests.Ai;

/// <summary>
/// Tests for IEmbedderHealthCheck implementations (Ollama, OpenAI).
/// Covers success paths, timeout scenarios, network failures, and error responses.
/// </summary>
public sealed class EmbedderHealthCheckTests
{
    #region OllamaHealthCheck Tests

    [Fact]
    public async Task OllamaHealthCheck_WithSuccessfulResponse_ReturnsHealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(HttpStatusCode.OK, "{\"models\":[]}");
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OllamaHealthCheck("http://localhost:11434", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
        status.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task OllamaHealthCheck_WithTimeout_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandlerWithDelay(TimeSpan.FromSeconds(5));
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OllamaHealthCheck("http://localhost:11434", httpClient);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var status = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("timed out");
    }

    [Fact]
    public async Task OllamaHealthCheck_WithNetworkError_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandlerWithException(new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OllamaHealthCheck("http://localhost:11434", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("Network error");
        status.ErrorMessage.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task OllamaHealthCheck_WithServerError_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(HttpStatusCode.InternalServerError, "Server error");
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OllamaHealthCheck("http://localhost:11434", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("HTTP 500");
    }

    [Fact]
    public async Task OllamaHealthCheck_WithServiceUnavailable_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(HttpStatusCode.ServiceUnavailable, "Service unavailable");
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OllamaHealthCheck("http://localhost:11434", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("HTTP 503");
    }

    [Fact]
    public async Task OllamaHealthCheck_WithFastResponse_RecordsAccurateResponseTime()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(HttpStatusCode.OK, "{\"models\":[]}");
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OllamaHealthCheck("http://localhost:11434", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.ResponseTime.Should().BeLessThan(TimeSpan.FromSeconds(1));
        status.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void OllamaHealthCheck_Constructor_WithNullBaseUrl_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OllamaHealthCheck(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("baseUrl");
    }

    [Fact]
    public void OllamaHealthCheck_Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OllamaHealthCheck("http://localhost:11434", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    #endregion

    #region OpenAIHealthCheck Tests

    [Fact]
    public async Task OpenAIHealthCheck_WithSuccessfulResponse_ReturnsHealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(HttpStatusCode.OK, "{\"data\":[{\"id\":\"text-embedding-3-small\"}]}");
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OpenAIHealthCheck("test-api-key", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
        status.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task OpenAIHealthCheck_WithTimeout_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandlerWithDelay(TimeSpan.FromSeconds(5));
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OpenAIHealthCheck("test-api-key", httpClient);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var status = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("timed out");
    }

    [Fact]
    public async Task OpenAIHealthCheck_WithInvalidApiKey_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(HttpStatusCode.Unauthorized, "{\"error\":{\"message\":\"Invalid API key\"}}");
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OpenAIHealthCheck("invalid-key", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("Invalid API key");
    }

    [Fact]
    public async Task OpenAIHealthCheck_WithNetworkError_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandlerWithException(new HttpRequestException("DNS resolution failed"));
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OpenAIHealthCheck("test-api-key", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("Network error");
        status.ErrorMessage.Should().Contain("DNS resolution failed");
    }

    [Fact]
    public async Task OpenAIHealthCheck_WithRateLimitError_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(HttpStatusCode.TooManyRequests, "{\"error\":{\"message\":\"Rate limit exceeded\"}}");
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OpenAIHealthCheck("test-api-key", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("HTTP 429");
    }

    [Fact]
    public async Task OpenAIHealthCheck_WithServerError_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler(HttpStatusCode.InternalServerError, "Internal server error");
        var httpClient = new HttpClient(mockHandler.Object);
        var healthCheck = new OpenAIHealthCheck("test-api-key", httpClient);

        // Act
        var status = await healthCheck.CheckHealthAsync();

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ErrorMessage.Should().Contain("HTTP 500");
    }

    [Fact]
    public void OpenAIHealthCheck_Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OpenAIHealthCheck(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiKey");
    }

    [Fact]
    public void OpenAIHealthCheck_Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OpenAIHealthCheck("test-key", null!, "http://api.openai.com");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void OpenAIHealthCheck_Constructor_WithNullEndpoint_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OpenAIHealthCheck("test-key", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("endpoint");
    }

    #endregion

    #region EmbedderHealthStatus Tests

    [Fact]
    public void EmbedderHealthStatus_Healthy_CreatesHealthyStatus()
    {
        // Arrange
        var responseTime = TimeSpan.FromMilliseconds(50);

        // Act
        var status = EmbedderHealthStatus.Healthy(responseTime);

        // Assert
        status.IsHealthy.Should().BeTrue();
        status.ResponseTime.Should().Be(responseTime);
        status.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void EmbedderHealthStatus_Unhealthy_CreatesUnhealthyStatus()
    {
        // Arrange
        var responseTime = TimeSpan.FromMilliseconds(100);
        var errorMessage = "Service unavailable";

        // Act
        var status = EmbedderHealthStatus.Unhealthy(responseTime, errorMessage);

        // Assert
        status.IsHealthy.Should().BeFalse();
        status.ResponseTime.Should().Be(responseTime);
        status.ErrorMessage.Should().Be(errorMessage);
    }

    #endregion

    #region Helper Methods

    private static Mock<HttpMessageHandler> CreateMockHttpHandler(HttpStatusCode statusCode, string content)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
        return mockHandler;
    }

    private static Mock<HttpMessageHandler> CreateMockHttpHandlerWithDelay(TimeSpan delay)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken ct) =>
            {
                await Task.Delay(delay, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        return mockHandler;
    }

    private static Mock<HttpMessageHandler> CreateMockHttpHandlerWithException(Exception exception)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
        return mockHandler;
    }

    #endregion
}
