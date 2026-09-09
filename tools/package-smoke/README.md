# Fresh Package Smoke Consumer

This project is intentionally not part of `Icod.DCurses.sln` and has no project
reference to the repository library.

Package validation copies the project into a temporary directory, uses an
isolated NuGet package cache, restores the exact current DCurses version from
the local artifact directory, and resolves `Icod.Terminal 1.4.0` plus
`Icod.TermInfo 1.10.0` through NuGet.org.

The ordinary CI execution uses only non-interactive public APIs, so it never
requires or mutates the runner's real terminal. In addition to the virtual-screen,
window, and style surface, the consumer validates the `0.2` semantic-input
contract from the packed assembly:

- legacy `CursesKey` and modifier numeric compatibility;
- `CursesKeyboardReportingMode` and keyboard protocol options;
- `CursesInputEvent.KeyPhase`;
- shifted and base-layout character identities;
- associated key text;
- representative new keypad/media/unrecognized keys and modifier flags;
- `CursesInputProtocolLease.KeyboardReportingMode`;
- the approved transitive Terminal/TermInfo public-type boundary.

The same source also contains a real `CursesSession.OpenAsync` interactive path,
selected only when:

```text
ICOD_DCURSES_SMOKE_INTERACTIVE=1
```

The package-only consumer targets `net8.0`, `net9.0`, and `net10.0`; each
framework is executed independently by the validation wrappers. This ensures a
fresh consumer compiles the public session and semantic-input surface across the
full supported framework set while keeping automated validation non-interactive.
