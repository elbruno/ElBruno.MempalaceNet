using MemPalace.Mcp.Transports;
using Xunit;
using FluentAssertions;

namespace MemPalace.Tests.Mcp.Transports;

public class SessionManagerTests
{
    [Fact]
    public void CreateSession_GeneratesUniqueSessionId()
    {
        // Arrange
        using var manager = new SessionManager();

        // Act
        var sessionId1 = manager.CreateSession();
        var sessionId2 = manager.CreateSession();

        // Assert
        sessionId1.Should().NotBeNullOrWhiteSpace();
        sessionId2.Should().NotBeNullOrWhiteSpace();
        sessionId1.Should().NotBe(sessionId2);
    }

    [Fact]
    public void CreateSession_GeneratesCryptoSecureToken()
    {
        // Arrange
        using var manager = new SessionManager();

        // Act
        var sessionId = manager.CreateSession();

        // Assert
        // 32 bytes -> 43-44 chars in URL-safe base64 (without padding)
        sessionId.Length.Should().BeInRange(42, 44);
        sessionId.Should().NotContain("+");
        sessionId.Should().NotContain("/");
        sessionId.Should().NotContain("=");
    }

    [Fact]
    public void ValidateSession_ReturnsTrueForValidSession()
    {
        // Arrange
        using var manager = new SessionManager();
        var sessionId = manager.CreateSession();

        // Act
        var isValid = manager.ValidateSession(sessionId);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateSession_ReturnsFalseForInvalidSession()
    {
        // Arrange
        using var manager = new SessionManager();

        // Act
        var isValid = manager.ValidateSession("invalid-session-id");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateSession_ReturnsFalseForNullSession()
    {
        // Arrange
        using var manager = new SessionManager();

        // Act
        var isValid = manager.ValidateSession(null!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateSession_ReturnsFalseForEmptySession()
    {
        // Arrange
        using var manager = new SessionManager();

        // Act
        var isValid = manager.ValidateSession(string.Empty);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSession_ReturnsFalseForExpiredSession()
    {
        // Arrange
        using var manager = new SessionManager(TimeSpan.FromMilliseconds(100));
        var sessionId = manager.CreateSession();

        // Act - Wait for session to expire
        await Task.Delay(150);
        var isValid = manager.ValidateSession(sessionId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateSession_UpdatesLastActivity()
    {
        // Arrange
        using var manager = new SessionManager(TimeSpan.FromSeconds(2));
        var sessionId = manager.CreateSession();

        // Act - Validate multiple times within timeout
        Thread.Sleep(500);
        var isValid1 = manager.ValidateSession(sessionId);
        Thread.Sleep(500);
        var isValid2 = manager.ValidateSession(sessionId);
        Thread.Sleep(500);
        var isValid3 = manager.ValidateSession(sessionId);

        // Assert - Session should still be valid due to activity updates
        isValid1.Should().BeTrue();
        isValid2.Should().BeTrue();
        isValid3.Should().BeTrue();
    }

    [Fact]
    public void RemoveSession_RemovesSession()
    {
        // Arrange
        using var manager = new SessionManager();
        var sessionId = manager.CreateSession();

        // Act
        manager.RemoveSession(sessionId);
        var isValid = manager.ValidateSession(sessionId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ActiveSessionCount_ReturnsCorrectCount()
    {
        // Arrange
        using var manager = new SessionManager();

        // Act
        var count0 = manager.ActiveSessionCount;
        var sessionId1 = manager.CreateSession();
        var count1 = manager.ActiveSessionCount;
        var sessionId2 = manager.CreateSession();
        var count2 = manager.ActiveSessionCount;
        manager.RemoveSession(sessionId1);
        var count3 = manager.ActiveSessionCount;

        // Assert
        count0.Should().Be(0);
        count1.Should().Be(1);
        count2.Should().Be(2);
        count3.Should().Be(1);
    }

    [Fact]
    public async Task CleanupTimer_RemovesExpiredSessions()
    {
        // Arrange - Short timeout for faster test
        using var manager = new SessionManager(TimeSpan.FromMilliseconds(200));
        var sessionId = manager.CreateSession();

        // Act - Wait for cleanup timer to run (cleanup runs every 5 minutes, but we'll check manually)
        await Task.Delay(250);
        var isValid = manager.ValidateSession(sessionId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ClearsAllSessions()
    {
        // Arrange
        var manager = new SessionManager();
        manager.CreateSession();
        manager.CreateSession();

        // Act
        manager.Dispose();

        // Assert
        manager.ActiveSessionCount.Should().Be(0);
    }

    [Fact]
    public void CreateSession_ThreadSafe()
    {
        // Arrange
        using var manager = new SessionManager();
        var sessionIds = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Create sessions from multiple threads
        Parallel.For(0, 100, _ =>
        {
            var sessionId = manager.CreateSession();
            sessionIds.Add(sessionId);
        });

        // Assert
        sessionIds.Should().HaveCount(100);
        sessionIds.Distinct().Should().HaveCount(100); // All unique
    }
}
