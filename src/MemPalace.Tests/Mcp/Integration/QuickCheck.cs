// Quick verification that our integration test compiles
using System.Net;
using Xunit;

namespace MemPalace.Tests.Mcp.Integration;

public class QuickCheck
{
    [Fact]
    public void CanCompile() => Assert.True(true);
}
