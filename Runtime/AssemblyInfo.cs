using System.Runtime.CompilerServices;

// The test assembly asserts on internals that have no business being public API — the
// domain-reload reset hooks in particular, which Unity invokes by attribute.
[assembly: InternalsVisibleTo("MyGameDevTools.SceneLoading.Tests")]
