# Custom Backend Template

This is a working template demonstrating how to implement a custom `IBackend` for MemPalace.NET.

## Overview

- **CustomBackend.cs** - Full implementation of `IBackend` and `ICollection`
- **Program.cs** - Example demonstrating basic usage

## Key Features

✓ Complete `IBackend` implementation  
✓ Complete `ICollection` implementation  
✓ Proper error handling (PalaceNotFoundException, EmbedderIdentityMismatchException, etc.)  
✓ Vector similarity search with cosine distance  
✓ CRUD operations (Add, Upsert, Get, Query, Delete)  
✓ Collection management (List, Delete)  
✓ 70 lines of well-commented code  

## How It Works

1. **CustomBackend** manages palaces (in-memory dictionary)
2. **CustomCollection** stores records and performs vector search
3. Cosine similarity distance is computed in-memory (brute-force O(n))

## Running the Example

```bash
cd examples/CustomBackendTemplate
dotnet run
```

Expected output:
```
🏛️  MemPalace.NET - Custom Backend Example

✓ Created collection with custom backend

📝 Adding records...
✓ Added 3 records

🔍 Searching for 'security features'...
Found 2 results:

  [1] Implement authentication system with JWT tokens
      Distance: 0.123

✅ Custom backend example completed!
```

## Adapting for Your Storage System

To use this template for your backend (Postgres, Qdrant, Chroma, etc.):

### 1. Replace In-Memory Storage
Replace the `Dictionary` with your client:
```csharp
// Before (in-memory)
private readonly Dictionary<string, Dictionary<string, CustomCollection>> _palaces = new();

// After (Postgres example)
private readonly PgClient _client;
```

### 2. Translate ICollection Methods
Replace dictionary operations with your API calls:
```csharp
// Before (in-memory)
_records[id] = record;

// After (Postgres example)
await _client.InsertAsync("records", record);
```

### 3. Implement Where Clause Support
The template throws `UnsupportedFilterException` for where clauses. Implement them based on your backend:
```csharp
// Example: Postgres filter translation
if (where is WhereClause.Eq eq)
{
    sql = $"WHERE metadata->>'{eq.Field}' = '{eq.Value}'";
}
```

### 4. Optimize Vector Search
Replace brute-force cosine with your backend's native search:
```csharp
// Before (brute-force)
candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

// After (Qdrant example)
await _qdrantClient.SearchAsync(collectionName, queryVector, topK: nResults);
```

## Validation Checklist

- [ ] All `IBackend` methods implemented
- [ ] All `ICollection` methods implemented
- [ ] Error types match: `PalaceNotFoundException`, `BackendClosedException`, `EmbedderIdentityMismatchException`, `DimensionMismatchException`, `UnsupportedFilterException`
- [ ] Vector search returns results sorted by distance (ascending)
- [ ] Collections can be listed and deleted
- [ ] Embedder identity is validated on collection open
- [ ] Handles disposed state properly
- [ ] Passes `BackendConformanceTests` from MemPalace.Tests

## Links

- **Architecture:** [../../../docs/architecture.md](../../../docs/architecture.md)
- **Backend Storage:** [../../../docs/backends.md](../../../docs/backends.md)
- **Library Guide:** [../../../docs/guides/csharp-library-developers.md](../../../docs/guides/csharp-library-developers.md)
- **InMemoryBackend Reference:** [../../../src/MemPalace.Core/Backends/InMemory/InMemoryBackend.cs](../../../src/MemPalace.Core/Backends/InMemory/InMemoryBackend.cs)
