# Tyrell — History

## Core Context
- **Project:** MemPalace.NET — port of https://github.com/MemPalace/mempalace
- **User:** Bruno Capuano
- **Role:** Core Engine Dev
- **Reference Python types:** `PalaceRef(id, local_path, namespace)`, `QueryResult(ids, documents, metadatas, distances, embeddings)`, `BaseCollection`, `BaseBackend`. Errors: `BackendError`, `PalaceNotFoundError`, `DimensionMismatchError`, `EmbedderIdentityMismatchError`, `UnsupportedFilterError`.
- **Default backend (Python):** ChromaDB. **Our default (.NET):** SQLite (Microsoft.Data.Sqlite) + sqlite-vec extension or in-table BLOB vectors. Pluggable interface so Qdrant/Chroma adapters can drop in later.

## Learnings
