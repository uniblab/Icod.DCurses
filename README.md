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

`Icod.DCurses 0.2.0` is the current published stable release.

Development toward `1.0.0` continues on the `0.3.0` Unicode and terminal-cell
line. The current development package is `0.3.0-alpha.6`.

T301 and T302 established the normalized Unicode text-element pipeline used by
`CursesWindow.Write(string)`: malformed UTF-16 is replaced before segmentation,
and width decisions operate on complete normalized text elements.

T303 pins the terminal-width data contract to Unicode 17.0.0, replaces the old
permanent hand-maintained East Asian Width predicate with checked-in generated
data, and exposes explicit narrow/wide East Asian Ambiguous policy. Narrow
remains the default; wide-Ambiguous behavior is opt-in and does not depend on
locale or environment guessing.

T304 adds public terminal-column helpers through `CursesText.MeasureColumns`,
`TruncateToColumns`, and `SliceByColumns`, using the same normalization,
segmentation, and width-provider contract as windows.

T305 hardens screen/window two-column leader/continuation footprints across
preserved resize and subwindow boundaries while retaining `CursesVirtualScreen`
as an exact low-level cell store when used independently. T306 adds the broader
Unicode conformance corpus, package-only consumer coverage, and live Unicode
diagnostics showcase. Windows, Linux, macOS, and package validation are green
for the completed T301-T306 foundation.

T307 is now the active release gate: public API, documentation, and package
regret review before release-candidate promotion. The remaining emoji-property
classification used by VS16/ZWJ recognition is being audited as part of this
gate and will not be silently deferred past `0.3.0`.

`0.2.0` completed stable `Icod.Terminal 1.0.0` semantic-input parity: the curses
facade carries the complete stable key vocabulary, modifier state,
press/repeat/release phases, modern character metadata, and optional keyboard
reporting while keeping raw terminal decoding and protocol lifecycle in
`Icod.Terminal`.

The dependency baseline remains:

- `Icod.Terminal` 1.0.0
- `Icod.TermInfo` 1.10.0

The retired DCurses backend, native mode, lifecycle-source, input-decoder, and
pre-Terminal session implementations remain removed. DCurses does not add a
mouse parser, paste reader, protocol escape emitter, keyboard decoder, or second
input loop.

The first release line was driven by the requirements of `top`, `slabtop`, and
`watch`. The 1.0 development train broadens that foundation into a general
managed TUI contract.

See `Icod.DCurses-1.0.0-Development-Roadmap.md` for the authoritative release
train through `1.0.0`, and `Icod.DCurses-0.3.0-Development-Roadmap.md` for the
active Unicode/terminal-cell tranche. The completed 0.3 checkpoints are recorded
in:

- `docs/T302-Normalized-Grapheme-Text-Pipeline.md`
- `docs/T303-Unicode-Width-Data-and-Ambiguous-Policy.md`
- `docs/T304-Column-Oriented-Text-Helpers.md`
- `docs/T305-Wide-Cell-Footprint-Invariants.md`
- `docs/T306-Unicode-Conformance-and-Consumer-Acceptance.md`

`Icod.DCurses-0.2.0-Development-Roadmap.md` and
`docs/Public-API-Baseline-0.2.md` record the stable semantic-input release.
`docs/Dependency-Baseline-0.1.1.md` retains the Terminal/TermInfo ownership
baseline inherited by later releases.

`Icod.DCurses-Development-Roadmap.md` retains the original project roadmap and
0.1 development history. Historical integration checkpoints remain under
`docs/`.

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

## Installation

The current published stable package is:

```text
dotnet add package Icod.DCurses --version 0.2.0
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

## Unicode column helpers (`0.3`)

Applications can measure or trim printable text using the same terminal-column
contract as window writes:

```csharp
int columns = CursesText.MeasureColumns( "A界B" );
string prefix = CursesText.TruncateToColumns( "A界B", 3 );
string slice = CursesText.SliceByColumns( "A界B", 1, 2 );
```

The default provider uses Unicode 17.0.0 data and treats East Asian Ambiguous
characters as narrow. Applications that deliberately require wide-Ambiguous
semantics can pass `UnicodeCursesTextWidthProvider.WideAmbiguousInstance` to a
`CursesScreen` or to the `CursesText` helpers.

Column helpers normalize malformed UTF-16 before text-element segmentation,
reject terminal controls, and never return half of a two-column text element.

## Modern keyboard input (`0.2`)

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

The Unicode width-data generator is a maintainer tool outside the solution. See
`tools/unicode-width-generator/README.md`; normal builds do not fetch Unicode
data from the network.

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
