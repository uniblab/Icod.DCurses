# Icod.DCurses 0.1.1 Dependency Baseline

**Project:** `Icod.DCurses`  
**Current stable maintenance release:** `0.1.1`  
**Next stable contract target:** `1.0.0`  
**Direct runtime dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`

## Purpose

This document is the authoritative dependency-integration baseline after the
`0.1.1` maintenance update and before feature development toward `1.0.0`.

Older T10, T19, Alpha-22, and T13 documents intentionally retain the package
versions used by those historical checkpoints. They should not be interpreted as
the current dependency recommendation.

## Active dependency graph

```text
Applications
    top / slabtop / watch / other TUIs
                    |
               Icod.DCurses 0.1.1
                    |
               Icod.Terminal 1.0.0
                    |
               Icod.TermInfo 1.10.0
                    |
             terminal / console / tty
```

`Icod.TermInfo` remains the immutable terminal-capability authority.

`Icod.Terminal` owns live endpoint observation, host terminal modes, dimensions,
lifecycle, input decoding, output transport, and reversible presentation and
input-protocol leases.

`Icod.DCurses` owns curses-shaped events, cells, styles, windows, logical and
physical screen state, damage/refresh, rendition policy, and curses-level
presentation policy.

DCurses must not regain a second raw terminal reader, terminal-family detector,
private CSI/DCS response router, native terminal-mode implementation, or private
terminal capability database.

## Package contract

The `Icod.DCurses` NuGet package targets:

```text
net8.0
net9.0
net10.0
```

Each target-framework dependency group must contain exactly:

```text
Icod.Terminal 1.0.0
Icod.TermInfo 1.10.0
```

The package verifier enforces those exact direct dependencies. The fresh package
smoke consumer restores the packed DCurses artifact through an isolated package
cache and resolves Terminal and TermInfo through NuGet rather than repository
project references.

## Intentional public dependency surface

The accepted 0.1 public API intentionally exposes only these lower-layer types:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

This allow-list is now enforced by `PublicDependencyBoundaryTests`. Any additional
Terminal or TermInfo type appearing in a public DCurses signature requires an
explicit API decision rather than arriving accidentally through an upstream
upgrade.

Whether this exact allow-list remains appropriate for `Icod.DCurses 1.0.0`
should be reviewed during the 1.0 API-regret/freeze phase. Until then, it is the
accepted compatibility boundary.

## Samples and tests

Repository samples and unit tests reference `Icod.DCurses.csproj` rather than
pinning separate Terminal or TermInfo versions. They therefore exercise the same
dependency graph as the library build.

The package-only smoke consumer is intentionally different: it has no repository
project reference and compile-checks both ordinary DCurses APIs and the approved
transitive Terminal/TermInfo types. This protects the installable consumer
contract rather than only the source-tree build graph.

## Historical checkpoints

The following documents remain useful historical records and intentionally retain
their contemporary dependency versions:

- `Icod-Terminal-T10-Integration.md` — original Terminal substrate cutover;
- `Icod-Terminal-T19-Rich-Input-Acceptance.md` — rich-input acceptance;
- `Icod-Terminal-0.3-Downstream-Acceptance.md` — Terminal 0.3 downstream check;
- `T13A-Package-and-Release-Foundation.md` — initial package/release machinery;
- `T13B-Public-API-and-Consumer-Contract.md` — 0.1 API regret review;
- `T13C-0.1.0-Stable-Release-Closure.md` — original 0.1.0 stable dependency freeze.

The active baseline supersedes those documents only for current version and
status information; it does not rewrite their historical conclusions.
