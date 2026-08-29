# Fresh Package Smoke Consumer

This project is intentionally not part of `Icod.DCurses.sln` and has no project
reference to the repository library.

T13 validation copies the project into a temporary directory, uses an isolated
NuGet package cache, restores the exact current DCurses version from the local
artifact directory, and resolves `Icod.Terminal 0.3.0` plus
`Icod.TermInfo 1.4.1` through NuGet.org for the stable `0.1.0` release gate.

The ordinary CI execution uses only public virtual-screen/window/style APIs, so
it never requires or mutates the runner's real terminal. The same source also
contains a real `CursesSession.OpenAsync` interactive path, selected only when:

```text
ICOD_DCURSES_SMOKE_INTERACTIVE=1
```

This ensures a fresh package-only consumer compiles the public interactive
session surface while keeping automated validation non-interactive.
