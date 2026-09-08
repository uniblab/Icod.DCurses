# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.3.0/icod_tui_toolchain.jpg)

`Icod.DCurses` is a managed, cross-platform curses-like terminal UI library for
.NET.

The library sits above `Icod.TermInfo` and `Icod.Terminal`. `Icod.TermInfo`
remains the immutable terminal-capability authority; `Icod.Terminal` owns the
live terminal session, host mode, dimensions, lifecycle, input decoding, and
reversible presentation and input-protocol leases. `Icod.DCurses` owns
curses-shaped events, virtual screens and windows, terminal cells and styles,
rendition policy, and refresh/damage synchronization.

## Status

`Icod.DCurses 0.3.0` is the current published stable release.

Development toward `1.0.0` continues on the `0.4.0` window editing and
composition line. The current release candidate is `0.4.0-rc.1`.

T401-T403 established the 0.4 package foundation, explicit non-standard window
repositioning, window-local cell inspection, rectangular fill, and
clear-to-beginning-of-line.

T404 adds Unicode-safe cell and line insertion/deletion using
snapshot-transform-commit semantics. T405 adds deterministic destructive copy
and blank-transparent overlay, including same-window overlap. T406 adds
geometric horizontal/vertical lines and explicit border construction while
leaving capability-aware line-glyph selection to the planned 0.6 presentation
release.

T407 completes the feature-side release with safe range-oriented damage marking
and querying. A public untouch operation is deliberately omitted because
arbitrarily clearing logical dirty state could suppress refresh work still
required by retained physical-screen knowledge.

T408 is complete: the 0.4 public API, documentation, dependency boundary, and
fresh package-consumer surface passed the regret gate on Windows, Linux, macOS,
and canonical package validation. The public contract is feature-frozen.
T409 is the active release-closure tranche; `0.4.0-rc.1` exists only to validate
that frozen contract before stable-source promotion.

`0.3.0` established the Unicode terminal-cell contract: malformed UTF-16 is
normalized before segmentation; width decisions operate on complete text
elements; Unicode 17.0.0 East Asian Width and Emoji-property data are generated
and checked in; East Asian Ambiguous policy is explicit; and public
column-measurement/truncation/slicing helpers share the same rules as window
writes.

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
train through `1.0.0`, and `Icod.DCurses-0.4.0-Development-Roadmap.md` for the
active window editing/composition tranche. The 0.4 checkpoints and release gates
are recorded in:

- `docs/T402-T403-Window-Geometry-and-Region-Editing.md`
- `docs/T405-Window-Composition.md`
- `docs/T406-Geometric-Line-and-Border-Drawing.md`
- `docs/T407-Damage-Ranges-and-Editing-Acceptance.md`
- `docs/T408-Public-API-Documentation-and-Package-Regret-Gate.md`
- `docs/T409-0.4.0-Stable-Release-Closure.md`
- `docs/Public-API-Baseline-0.4.md`

The completed 0.3 Unicode release is recorded in
`Icod.DCurses-0.3.0-Development-Roadmap.md`, its T302-T308 documents, and
`docs/Public-API-Baseline-0.3.md`.

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
dotnet add package Icod.DCurses --version 0.3.0
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

## Window editing and composition (`0.4`)

The 0.4 line adds window-local editing and composition while retaining the
shared-view model:

```csharp
CursesScreen logical = new( 80, 24 );
CursesWindow editor = logical.CreateWindow( 2, 4, 18, 60 );

editor.Reposition( 3, 5 );
editor.FillRectangle( 0, 0, 18, 60, CursesCell.Blank() );
editor.Move( 1, 2 );
editor.Write( "A界B" );
editor.InsertCells( 2 );

CursesWindow status = logical.CreateWindow( 21, 5, 1, 60 );
editor.CopyRectangleTo( status, 1, 0, 1, 20, 0, 0 );

editor.DrawHorizontalLine( 16, 1, 58, new CursesCell( "-" ) );
editor.TouchRegion( 0, 0, 18, 60 );
bool needsRefresh = editor.IsRegionTouched( 0, 0, 18, 60 );
```

`CopyRectangleTo` includes source blanks; `OverlayRectangleTo` treats ordinary
source blanks as transparent. Overlapping copies snapshot the source before any
destination write. Editing and drawing preserve the cursor and retain the 0.3
wide-cell invariants.

Drawing methods accept caller-supplied one-column cells in 0.4. Capability-aware
Unicode/alternate-character-set line-glyph selection is intentionally deferred
to the 0.6 presentation tranche.

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
