# Fresh Package Smoke Consumer

This project is intentionally not part of `Icod.DCurses.sln` and has no project reference to the repository library.

Package validation copies the smoke project and its explicitly staged source witnesses into a temporary directory, uses an isolated NuGet package cache, restores the exact current DCurses package version from the local artifact directory, and resolves the package-declared dependency graph. The 2.0 candidate declares `Icod.Terminal 1.18.0` directly; `Icod.TermInfo` remains a transitive dependency through Terminal.

Dependency versions are not duplicated as verifier policy. The package metadata is authoritative; restore/build/run establish whether the generated package is consumable with its declared dependency graph.

The ordinary CI execution uses only non-interactive public APIs, so it never requires or mutates the runner's real terminal. The consumer validates representative stable surfaces including:

- virtual screens, windows, editing, damage, pads, Unicode-width helpers, presentation, and semantic metadata;
- modern semantic-input contracts and the Terminal-only public-type boundary, including `Profile`, both dimensions methods, and nullable lifecycle dimensions;
- retained panels, geometry/layout helpers, and explicit bounds application;
- 1.4/1.5 interaction routing, focus, scopes, gesture bindings, pointer capture, pointer target/gesture results, and pointer-shape leases;
- the 1.6 retained-raster facade and its one intentional lower-layer image input, `TerminalRasterImage`.

`RasterSmoke.cs` is staged beside the established `Program.cs` by both the Unix and Windows package validators. A module initializer executes its package-only checks before the ordinary smoke program. It constructs a backend-neutral `TerminalRasterImage`, validates the DCurses raster ownership vocabulary, and freezes the public signatures for resource creation, placeholder creation, placeholder-cell retrieval, window raster writes, and virtual-screen raster mutation. It contains no project reference, raw Kitty/Sixel protocol identity, or backend-selection branch.

The established `Program.cs` remains the package consumer entry point and continues to carry the prior interaction/package contract. Keeping that filename stable is intentional because release validation and existing acceptance tests treat it as the canonical packed-consumer source.

The same package-only program also contains a real `CursesSession.OpenAsync` interactive path. It reads the Terminal-owned profile and live dimensions and displays them through the retained screen. This path is selected only when:

```text
ICOD_DCURSES_SMOKE_INTERACTIVE=1
```

The package-only consumer targets `net8.0`, `net9.0`, and `net10.0`; each framework is restored, built, and executed independently by the validation wrappers. This verifies the packed public contract rather than relying only on project-reference compilation.
