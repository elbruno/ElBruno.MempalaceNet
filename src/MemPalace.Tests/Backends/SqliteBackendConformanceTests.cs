using MemPalace.Backends.Sqlite;
using MemPalace.Core.Backends;

namespace MemPalace.Tests.Backends;

/// <summary>
/// Conformance tests for SqliteBackend using a temporary directory for each test.
/// </summary>
public sealed class SqliteBackendConformanceTests : BackendConformanceTests, IDisposable
{
    private readonly string _tempDir;

    public SqliteBackendConformanceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"mempalace-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    protected override IBackend CreateBackend()
    {
        return new SqliteBackend(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
