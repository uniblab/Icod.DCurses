# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.1.1/icod_tui_toolchain.jpg)

`Icod.DCurses` is a managed, cross-platform curses-like terminal UI library for
.NET.

The library sits above `Icod.TermInfo` and `Icod.Terminal`. `Icod.TermInfo`
remains the immutable terminal-capability authority; `Icod.Terminal` owns the
live terminal session, host mode, dimensions, lifecycle, input decoding, and
reversible presentation and input-protocol leases. `Icod.DCurses` owns
curses-shaped events, virtual screens and windows, terminal cells and styles,
rendition policy, and refresh/damage synchronization.

## Status

`Icod.DCurses 0.1.1` is the current stable maintenance release of the managed
DCurses 0.1 contract.

`0.1.0-Alpha-15` established the automated Icod.Terminal T19 rich-input
acceptance boundary against `Icod.Terminal 0.2.0-alpha.6`, and
`0.1.0-Alpha-16` added the live rich-input acceptance showcase. Alpha-17 through
Alpha-19 completed the focused `watch`, `slabtop`, and `top` application-shaped
acceptance set. Alpha-20 established the three-host package-only and
tag-controlled release gate. Alpha-21 completed the 0.1 public-API/documentation
regret pass and required styled/updating quick-start sample. Alpha-22 then
validated published `Icod.Terminal 0.3.0-alpha.8` and `Icod.TermInfo 1.3.0`
without requiring DCurses to regain private terminal mechanics.

The original stable `0.1.0` dependency freeze used `Icod.Terminal 0.3.0` and
`Icod.TermInfo 1.4.1`. `0.1.1` advances that accepted contract to
`Icod.Terminal 1.0.0` and `Icod.TermInfo 1.10.0` without intentionally changing
the existing DCurses public API or ownership model.

The retired DCurses backend, native mode, lifecycle-source, input-decoder, and
pre-Terminal session implementations remain removed. DCurses does not add a
mouse parser, paste reader, protocol escape emitter, or second input loop.

The first release line is driven by the requirements of `top`, `slabtop`, and
`watch`.

See `Icod.DCurses-Development-Roadmap.md` for the broader development contract
through `1.0.0`, `docs/Icod-Terminal-T10-Integration.md` for the substrate reset,
`docs/Icod-Terminal-T19-Rich-Input-Acceptance.md` for rich-input acceptance,
`docs/T13B-Public-API-and-Consumer-Contract.md` for the 0.1 regret review,
`docs/T13C-0.1.0-Stable-Release-Closure.md` for the stable release closure, and
`docs/Public-API-Baseline-0.1.md` for the release-line API baseline.

## Architecture

```text
top / slabtop / watch / other TUIs
                 |
            Icod.DCurses
     windows / cells / refresh
       rendition / curses events
                 |
            Icod.Terminal
 session / input / lifecycle / dimensions
 presentation / input-protocol leases
                 |
            Icod.TermInfo
      terminal capability model
                 |
            terminal / tty
```

`Icod.DCurses` does not hard-code one terminal family. Terminal-specific output
continues to be selected through `Icod.TermInfo`, while live session ownership
and reversible terminal state are centralized in `Icod.Terminal`.

## Target

The initial implementation targets:

- .NET 8
- .NET 9
- .NET 10
- C# 13
- Windows
- Linux
- macOS

The `0.1.1` runtime dependency set is:

- `Icod.Terminal` 1.0.0
- `Icod.TermInfo` 1.10.0

## Installation

```text
dotnet add package Icod.DCurses --version 0.1.1
```

## Quick start

```csharp
using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

screen.Clear();
screen.Move(
    0,
    0
);
screen.Write(
    "Hello from Icod.DCurses",
    new CursesStyle(
        CursesColor.Default,
        CursesColor.Default,
        CursesTextAttributes.Bold
    )
);
await session.RefreshAsync();

CursesEvent terminalEvent = await session.ReadEventAsync();
```

The session owns the presentation state it enters and restores that state when
disposed. Applications should consume terminal input and lifecycle activity
through `CursesSession` rather than adding a parallel terminal reader.

## Build

From the repository root:

```sh
build.cmd
```

or:

```sh
./build.sh
```

Both scripts perform clean, restore, build, test, pack, and validate operations
by default and also accept one of those phase names individually.

The repository includes a root `NuGet.Config` which clears inherited package
sources and restores public dependencies from NuGet.org. This keeps local and
hosted builds on the same dependency artifacts instead of allowing a user- or
machine-level feed with the same package IDs and versions to silently change the
compile graph. The local build wrappers also force dependency reevaluation
during restore after package-version changes.

## Authors

Timothy J. Bruce.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See
`LICENSE`.

The NuGet package declares license acceptance as required. Package clients which
honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the
license terms before installation.
