using Xunit;

// WebApplicationFactory + Serilog two-stage init (CreateBootstrapLogger) race condition:
// each factory's entry point sets the shared static Log.Logger = new ReloadableLogger(),
// and if factories start concurrently, multiple threads call Freeze() on the same instance.
// Running collections sequentially eliminates the race entirely.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
