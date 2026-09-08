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

Development toward `1.0.0` is active. The current development package is
`0.2.0-alpha.3`, in the Terminal 1.0 input-semantic-parity tranche. T201-T205
establish the complete stable Terminal 1.0 key/modifier/phase mapping and
negotiated modern-keyboard acceptance; T206 updates the live rich-input showcase
and consumer documentation for that expanded contract. The next gate is T207 —
public API, documentation, and package regret review before stable `0.2.0`.

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
the existing DCurses public API or ownership model. The `0.2.0` development
line retains those stable dependency versions.

The retired DCurses backend, native mode, lifecycle-source, input-decoder, and
pre-Terminal session implementations remain removed. DCurses does not add a
mouse parser, paste reader, protocol escape emitter, keyboard decoder, or second
input loop.

The first release line was driven by the requirements of `top`, `slabtop`, and
`watch`. The 1.0 development train broadens that foundation into a general
managed TUI contract.

See `Icod.DCurses-1.0.0-Development-Roadmap.md` for the authoritative release
train from `0.2.0` through `1.0.0`, and
`Icod.DCurses-0.2.0-Development-Roadmap.md` for the active Terminal 1.0 input
parity tranche. `docs/Dependency-Baseline-0.1.1.md` records the stable
Terminal/TermInfo dependency and ownership baseline.

`Icod.DCurses-Development-Roadmap.md` retains the original project roadmap and
0.1 development history. Historical integration checkpoints remain under
`docs/`, including `docs/Icod-Terminal-T10-Integration.md`,
`docs/Icod-Terminal-T19-Rich-Input-Acceptance.md`,
`docs/T13B-Public-API-and-Consumer-Contract.md`, and
`docs/T13C-0.1.0-Stable-Release-Closure.md`.

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

The implementation targets:

- .NET 8
- .NET 9
- .NET 10
- C# 13
- Windows
- Linux
- macOS

The current runtime dependency set is:

- `Icod.Terminal` 1.0.0
- `Icod.TermInfo` 1.10.0

## Installation

The current stable package is:

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

## Modern keyboard input (`0.2` development)

Applications which want key-event phases can request them through the curses
protocol lease without using Terminal protocol types directly:

```csharp
var keyboard = await session.AcquireInputProtocolsAsync(
    new CursesInputProtocolOptions {
        KeyboardReportingMode = CursesKeyboardReportingMode.EventTypes
    }
);

if ( keyboard.IsAvailable ) {
    await using CursesInputProtocolLease lease = keyboard.GetRequiredValue();
    CursesEvent current = await session.ReadEventAsync();

    if ( current.Input is {
        Kind: CursesInputEventKind.Key
    } input ) {
        CursesKey key = input.Key;
        CursesKeyEventPhase? phase = input.KeyPhase;
        CursesKeyModifiers modifiers = input.Modifiers;
    }
}
```

Keyboard reporting is optional. A terminal which cannot provide the requested
mode returns a controlled unavailable result; applications can continue using
the ordinary curses input stream.

## Build

From the repository root:

```sh
build.cmd
```

or:

```sh
./build.sh
```

Both wrappers delegate to `packaging/Invoke-Build.ps1` and use the `Debug`
configuration for local development. With no section argument they perform
restore, build, test, pack, and package validation. They also accept
`clean`, `restore`, `build`, `test`, `pack`, or `validate`; prerequisite phases
are run automatically where needed.

Pull-request validation promotes the build to `Staging`. Pushes to `main` and
release tags use `Release`. Package validation exercises the packed artifact and
a fresh package-only consumer rather than relying only on the repository project
references.

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
