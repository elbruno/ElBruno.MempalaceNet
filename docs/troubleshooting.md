# Troubleshooting Guide

This guide covers common issues and their remediation steps for MemPalace.NET CLI.

---

## Palace Initialization Issues

### Palace Not Found

**Error:** `No palace found at path: ~/my-palace`

**Remediation:**
1. Initialize a palace: `mempalacenet init ~/my-palace`
2. Or specify an existing palace path with `--path` flag
3. Check file permissions on the specified directory

**Cause:** The specified directory doesn't exist or doesn't contain a valid palace database.

---

### Directory Already Exists

**Error:** `Directory already exists and is not empty`

**Remediation:**
1. Use a different path for the new palace
2. Or use the existing palace with `mempalacenet search` or other commands
3. Backup and remove existing directory if you want to reinitialize

**Cause:** The target directory already contains files, which could be overwritten during initialization.

---

## Mining Issues

### Path Does Not Exist

**Error:** `Path '/path/to/docs' is invalid: directory does not exist`

**Remediation:**
1. Verify the path exists: check for typos
2. Use absolute paths to avoid confusion
3. Check file permissions

**Cause:** The specified path doesn't exist or is inaccessible.

---

### Access Denied

**Error:** `Failed to mine path: access denied`

**Remediation:**
1. Check file permissions on the target directory
2. Run with appropriate user privileges
3. Ensure the directory is not locked by another process

**Cause:** Insufficient permissions to read files in the target directory.

---

### No Files Found

**Error:** `Mining failed: no files found to mine`

**Remediation:**
1. Verify the directory contains readable files
2. Check if files are hidden or excluded by `.gitignore`
3. Specify `--mode` flag explicitly (e.g., `--mode files` or `--mode convos`)

**Cause:** The directory is empty or all files are excluded.

---

## Search Issues

### Empty Query

**Error:** `Search failed: query cannot be empty`

**Remediation:**
1. Provide a non-empty search query: `mempalacenet search "your query"`
2. Quote queries with spaces: `mempalacenet search "multi word query"`

**Cause:** Search requires a query string.

---

### Palace Not Initialized

**Error:** `Search failed: palace is not initialized`

**Remediation:**
1. Initialize a palace first: `mempalacenet init ~/my-palace`
2. Or specify the correct palace path with `--path` flag
3. Verify the palace database file exists

**Cause:** The palace database doesn't exist at the specified location.

---

### No Memories Found

**Error:** `No memories found matching query`

**Remediation:**
1. Try a different query
2. Check if the specified wing exists and contains memories
3. Mine content first: `mempalacenet mine /path/to/docs --wing docs`
4. Remove `--wing` filter to search all wings

**Cause:** No memories match the search criteria.

---

## Embedder Issues

### Embedder Configuration Error

**Error:** `Embedder operation failed: configuration is invalid`

**Remediation:**
1. Check embedder configuration in palace settings
2. For local ONNX: ensure model files are present in the expected location
3. For API embedders: verify API keys are set correctly
4. Run `mempalacenet health` to diagnose embedder status

**Cause:** Embedder is not configured correctly or required files/credentials are missing.

---

### ONNX Model Not Found

**Error:** `Embedder error: ONNX model file not found`

**Remediation:**
1. Ensure ONNX model files are downloaded
2. Check the model path in configuration
3. Reinstall the package: `dotnet tool update -g mempalacenet`

**Cause:** Local ONNX embedding model files are missing.

---

### API Rate Limit

**Error:** `Embedder error: API rate limit exceeded`

**Remediation:**
1. Wait before retrying (rate limits typically reset after 1 minute)
2. Consider using local ONNX embeddings instead: reconfigure the palace
3. Reduce batch sizes in mining operations

**Cause:** External API (OpenAI, Azure) has rate limited your requests.

---

## Backend Issues

### Database Locked

**Error:** `Backend error: database is locked`

**Remediation:**
1. Close other applications accessing the palace database
2. Wait a few seconds and retry
3. Check for stale lock files in the palace directory

**Cause:** Another process has locked the SQLite database file.

---

### Database Corruption

**Error:** `Backend error: database file is corrupt`

**Remediation:**
1. Restore from backup if available
2. Try SQLite recovery tools: `sqlite3 palace.db ".recover"`
3. Reinitialize the palace and re-mine content (last resort)

**Cause:** Database file is corrupted (power loss, disk error, etc.).

---

## Configuration Issues

### Invalid Configuration Syntax

**Error:** `Configuration is invalid: JSON syntax error`

**Remediation:**
1. Check palace configuration file for syntax errors
2. Validate JSON with a linter: https://jsonlint.com/
3. Reset to defaults by reinitializing the palace

**Cause:** Configuration file contains invalid JSON.

---

### Missing Required Settings

**Error:** `Configuration error: required setting 'X' is missing`

**Remediation:**
1. Check configuration file for the missing setting
2. Refer to documentation for required settings: `mempalacenet init --help`
3. Reinitialize the palace with correct settings

**Cause:** Required configuration values are not present.

---

## Build & Publish Issues

### NativeAOT Publish Fails

**Error:** `IL3050` and/or `IL2026` errors when publishing with `PublishAot=true`

**Remediation:**
1. Use standard publish without NativeAOT for now.
2. If you control the app project, remove or disable `<PublishAot>true</PublishAot>`.

**Cause:** NativeAOT is not supported yet.

---

## Performance Issues

### Slow Search

**Symptom:** Search takes several seconds or minutes

**Remediation:**
1. Reduce search scope with `--wing` filter
2. Reduce `--top-k` value (default: 10)
3. Consider upgrading to a vector database backend (Qdrant, pgvector) for large datasets
4. Index your vectors if using a vector database backend

**Cause:** Large number of memories (>100K) with brute-force cosine similarity search.

---

### Slow Mining

**Symptom:** Mining takes a long time

**Remediation:**
1. Use `.gitignore` to exclude unnecessary files
2. Reduce file chunk size if processing very large files
3. Check disk I/O performance
4. Use `--verbose` to identify slow files

**Cause:** Large number of files or slow embedder API calls.

---

## Knowledge Graph Issues

### Invalid Entity ID

**Error:** `Entity ID 'X' does not exist`

**Remediation:**
1. Verify the entity exists: `mempalacenet kg query X`
2. Create the entity first: `mempalacenet kg add-entity X --type person`
3. Check for typos in entity IDs

**Cause:** Referenced entity doesn't exist in the knowledge graph.

---

### Temporal Query Error

**Error:** `Temporal query failed: invalid date format`

**Remediation:**
1. Use ISO 8601 date format: `YYYY-MM-DD` or `YYYY-MM-DDTHH:MM:SSZ`
2. Example: `mempalacenet kg query alice --as-of 2024-06-01`

**Cause:** Date parameter is not in a recognized format.

---

## General Troubleshooting

### Enable Verbose Mode

For detailed diagnostic information, add `--verbose` flag to any command:

```bash
mempalacenet search "query" --verbose
mempalacenet mine /path --verbose
```

Verbose mode shows:
- Detailed error stack traces
- File-by-file processing logs
- API call details
- Configuration values

---

### Check Palace Health

Run health check to diagnose configuration issues:

```bash
mempalacenet health
```

This will verify:
- Database connectivity
- Embedder configuration
- Available collections
- Disk space

---

### Report Issues

If you encounter persistent issues:

1. Enable verbose mode and capture full output
2. Check existing issues: https://github.com/elbruno/mempalacenet/issues
3. Create a new issue with:
   - Command that failed
   - Error message (with `--verbose` output)
   - Operating system and .NET version
   - Palace configuration (remove any API keys)

---

## Exit Codes

MemPalace.NET CLI uses standard exit codes:

- `0` - Success
- `1` - General error (validation, configuration, runtime)
- `2` - Command-line parsing error

Use exit codes for scripting:

```bash
if mempalacenet search "query"; then
  echo "Search succeeded"
else
  echo "Search failed with exit code $?"
fi
```

---

## Additional Resources

- **Documentation:** https://github.com/elbruno/mempalacenet/tree/main/docs
- **CLI Reference:** [cli.md](cli.md)
- **Architecture:** [architecture.md](architecture.md)
- **GitHub Issues:** https://github.com/elbruno/mempalacenet/issues
