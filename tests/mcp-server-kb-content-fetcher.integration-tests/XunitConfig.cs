using Xunit;

// Disable test parallelization for integration tests that spawn the MCP server executable.
// Parallel launches were causing intermittent file locking (apphost.exe copy contention).
[assembly: CollectionBehavior(DisableTestParallelization = true)]