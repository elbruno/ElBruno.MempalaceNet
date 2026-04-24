using MemPalace.Core.Backends;
using MemPalace.Core.Backends.InMemory;

namespace MemPalace.Tests.Backends;

/// <summary>
/// Conformance tests for the in-memory backend.
/// </summary>
public sealed class InMemoryBackendConformanceTests : BackendConformanceTests
{
    protected override IBackend CreateBackend() => new InMemoryBackend();
}
