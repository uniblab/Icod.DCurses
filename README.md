# Icod.DCurses

`Icod.DCurses` is a managed, cross-platform curses-like terminal UI library for
.NET.

The library sits above `Icod.TermInfo` and `Icod.Terminal`. `Icod.TermInfo`
remains the immutable terminal-capability authority; `Icod.Terminal` owns the
live terminal session, host mode, dimensions, lifecycle, input decoding, and
reversible presentation and input-protocol leases. `Icod.DCurses` owns
curses-shaped events, virtual screens and windows, terminal cells and styles, rendition policy, and
refresh/damage synchronization.

## Status

The project is under active development toward version `0.1.0`.

`0.1.0-Alpha-15` established the automated Icod.Terminal T19 rich-input
acceptance boundary and builds and tests successfully against
`Icod.Terminal 0.2.0-alpha.6`. `0.1.0-Alpha-16` provides the live rich-input
acceptance showcase. Alpha-17 through Alpha-19 complete the focused T12
`watch`, `slabtop`, and `top` application-shaped acceptance set. Alpha-20
established the three-host package-only and tag-controlled release gate.
`0.1.0-Alpha-21` completes the 0.1 public-API/documentation regret pass and the
required styled/updating quick-start sample.

The stable `0.1.0` tag is deliberately held until `Icod.Terminal 0.2.0` is
available as a stable package; DCurses will not publish a stable package whose
required Terminal dependency is still a prerelease.

The retired DCurses backend, native mode, lifecycle-source, input-decoder, and
pre-Terminal session implementations remain removed. DCurses does not add a
mouse parser, paste reader, protocol escape emitter, or second input loop.

The first release continues to be driven by the requirements of `top`,
`slabtop`, and `watch`.

See `Icod.DCurses-Development-Roadmap.md` for the broader development contract
through `1.0.0`, `docs/Icod-Terminal-T10-Integration.md` for the substrate reset,
`docs/Icod-Terminal-T19-Rich-Input-Acceptance.md` for rich-input acceptance,
`docs/T13B-Public-API-and-Consumer-Contract.md` for the 0.1 regret review, and
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

- .NET 10
- C# 13
- Windows
- Linux
- macOS

The active runtime dependencies are:

- `Icod.TermInfo` 1.0.0
- `Icod.Terminal` 0.2.0-alpha.6

## Installation

During the Alpha-21 validation tranche:

```text
dotnet add package Icod.DCurses --version 0.1.0-Alpha-21
```

The stable installation command will use version `0.1.0` after the final
Terminal dependency and T13C release-closure gates pass.

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

```text
build.cmd
```

or:

```text
./build.sh
```

Both scripts perform clean, restore, build, test, and pack operations by default
and also accept one of those phase names individually.

## License

`Icod.DCurses` is licensed under the GNU Lesser General Public License,
version 3 or later.
