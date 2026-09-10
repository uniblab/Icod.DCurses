# Fresh Package Smoke Consumer

This project is intentionally not part of `Icod.DCurses.sln` and has no project reference to the repository library.

Package validation copies the project into a temporary directory, uses an isolated NuGet package cache, restores the exact current DCurses package version from the local artifact directory, and resolves the project-declared `Icod.Terminal 1.8.1` and `Icod.TermInfo 1.10.0` dependencies through NuGet.org.

Dependency versions are not duplicated as verifier policy. The package metadata is authoritative; restore/build/run establish whether the generated package is consumable with its declared dependency graph.

The ordinary CI execution uses only non-interactive public APIs, so it never requires or mutates the runner's real terminal. The consumer validates representative stable surfaces including:

- virtual screens, windows, editing, damage, pads, Unicode-width helpers, presentation, and semantic metadata;
- modern semantic-input contracts and the approved Terminal/TermInfo public-type boundary;
- the 1.2 `CursesPanel` creation, retained content, visibility, movement, transparency, ordering, `IDisposable`, idempotent disposal, and use-after-dispose contract.

The same package-only source also contains a real `CursesSession.OpenAsync` interactive path selected only when:

```text
ICOD_DCURSES_SMOKE_INTERACTIVE=1
```

The package-only consumer targets `net8.0`, `net9.0`, and `net10.0`; each framework is restored, built, and executed independently by the validation wrappers. This verifies the packed public contract rather than relying only on project-reference compilation.
