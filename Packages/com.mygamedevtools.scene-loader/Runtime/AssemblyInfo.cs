using System.Runtime.CompilerServices;

// The test assembly asserts on internals that have no business being public API:
// domain-reload reset hooks, in particular, which Unity invokes by attribute and which
// no user should ever call.
[assembly: InternalsVisibleTo("MyGameDevTools.SceneLoading.Tests")]
