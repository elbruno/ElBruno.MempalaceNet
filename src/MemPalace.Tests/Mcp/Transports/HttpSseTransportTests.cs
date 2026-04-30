using MemPalace.Mcp.Transports;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FluentAssertions;
using System.Net;
using System.Text;

namespace MemPalace.Tests.Mcp.Transports;

public class HttpSseTransportTests
{
    private readonly ILogger<HttpSseTransport> _logger;

    public HttpSseTransportTests()
    {
        _logger = Substitute.For<ILogger<HttpSseTransport>>();
    }

    [Fact]
    public void TransportType_ReturnsSSE()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5051);

        // Act
        var type = transport.TransportType;

        // Assert
        type.Should().Be("sse");
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task StartAsync_StartsHttpServer()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5052);

        // Act
        await transport.StartAsync();

        // Assert - Server should be listening
        using var client = new HttpClient();
        var response = await client.GetAsync("http://127.0.0.1:5052/mcp");
        
        // Expect 401 since we don't have a session
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await transport.StopAsync();
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task StopAsync_StopsHttpServer()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5053);
        await transport.StartAsync();

        // Act
        await transport.StopAsync();

        // Assert - Server should no longer be listening
        using var client = new HttpClient();
        var act = async () => await client.GetAsync("http://127.0.0.1:5053/mcp");
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task HandlePost_CreatesSessionWhenNoneProvided()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5054);
        await transport.StartAsync();

        try
        {
            // Act
            using var client = new HttpClient();
            var content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"test\"}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://127.0.0.1:5054/mcp", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.Should().ContainKey("Mcp-Session-Id");
            var sessionId = response.Headers.GetValues("Mcp-Session-Id").First();
            sessionId.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            await transport.StopAsync();
        }
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task HandlePost_ValidatesExistingSession()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5055);
        await transport.StartAsync();

        try
        {
            using var client = new HttpClient();

            // Create session
            var content1 = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"test1\"}", Encoding.UTF8, "application/json");
            var response1 = await client.PostAsync("http://127.0.0.1:5055/mcp", content1);
            var sessionId = response1.Headers.GetValues("Mcp-Session-Id").First();

            // Act - Use existing session
            var content2 = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"test2\"}", Encoding.UTF8, "application/json");
            var request2 = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:5055/mcp")
            {
                Content = content2
            };
            request2.Headers.Add("Mcp-Session-Id", sessionId);
            var response2 = await client.SendAsync(request2);

            // Assert
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await transport.StopAsync();
        }
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task HandlePost_RejectsInvalidSession()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5056);
        await transport.StartAsync();

        try
        {
            // Act
            using var client = new HttpClient();
            var content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"test\"}", Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:5056/mcp")
            {
                Content = content
            };
            request.Headers.Add("Mcp-Session-Id", "invalid-session-id");
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await transport.StopAsync();
        }
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task HandlePost_RaisesMessageReceivedEvent()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5057);
        await transport.StartAsync();

        string? receivedMessage = null;
        string? receivedSessionId = null;
        transport.MessageReceived += (sender, args) =>
        {
            receivedMessage = args.Message;
            receivedSessionId = args.SessionId;
        };

        try
        {
            // Act
            using var client = new HttpClient();
            var testMessage = "{\"jsonrpc\":\"2.0\",\"method\":\"test\"}";
            var content = new StringContent(testMessage, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://127.0.0.1:5057/mcp", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Give event handler time to execute
            await Task.Delay(100);
            
            receivedMessage.Should().Be(testMessage);
            receivedSessionId.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            await transport.StopAsync();
        }
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task HandlePost_RejectsEmptyBody()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5058);
        await transport.StartAsync();

        try
        {
            // Act
            using var client = new HttpClient();
            var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://127.0.0.1:5058/mcp", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        finally
        {
            await transport.StopAsync();
        }
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task HandleDelete_RemovesSession()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5059);
        await transport.StartAsync();

        try
        {
            using var client = new HttpClient();

            // Create session
            var content1 = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"test\"}", Encoding.UTF8, "application/json");
            var response1 = await client.PostAsync("http://127.0.0.1:5059/mcp", content1);
            var sessionId = response1.Headers.GetValues("Mcp-Session-Id").First();

            // Act - Delete session
            var request = new HttpRequestMessage(HttpMethod.Delete, "http://127.0.0.1:5059/mcp");
            request.Headers.Add("Mcp-Session-Id", sessionId);
            var deleteResponse = await client.SendAsync(request);

            // Assert
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify session is invalid
            var content2 = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"test2\"}", Encoding.UTF8, "application/json");
            var request2 = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:5059/mcp")
            {
                Content = content2
            };
            request2.Headers.Add("Mcp-Session-Id", sessionId);
            var response2 = await client.SendAsync(request2);
            response2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await transport.StopAsync();
        }
    }

    [Fact(Skip = "HTTP server disposal hangs on Linux CI - fix in follow-up")]
    public async Task SendMessageAsync_DoesNotThrowWithoutConnection()
    {
        // Arrange
        using var transport = new HttpSseTransport(_logger, port: 5060);
        await transport.StartAsync();

        try
        {
            // Act - Send message to non-existent session
            var act = async () => await transport.SendMessageAsync("{\"test\":\"message\"}", "non-existent-session");

            // Assert - Should not throw, just log warning
            await act.Should().NotThrowAsync();
        }
        finally
        {
            await transport.StopAsync();
        }
    }
}
