# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.4.0/icod_tui_toolchain.jpg)

`Icod.DCurses` is a managed, cross-platform curses-like terminal UI library for
.NET.

The library sits above `Icod.TermInfo` and `Icod.Terminal`. `Icod.TermInfo`
remains the immutable terminal-capability authority; `Icod.Terminal` owns the
live terminal session, host mode, dimensions, lifecycle, input decoding, and
reversible presentation and input-protocol leases. `Icod.DCurses` owns
curses-shaped events, virtual screens and windows, pads and viewports, terminal
cells and styles, rendition policy, and refresh/damage synchronization.

## Status

`Icod.DCurses 0.4.0` is the current published stable release.

Development toward `1.0.0` continues on the `0.5.0` pads and large-surfaces
line. The current development package is `0.5.0-alpha.4`.

T501-T503 established the pad backing surface and direct rectangular
presentation. A `CursesPad` owns a large off-screen logical surface and exposes a
normal `CursesWindow` through `ContentWindow`, so the stable Unicode, editing,
composition, drawing, and damage contracts are reused rather than reimplemented.

T504 added independently pannable `CursesPadViewport` instances with `SetSource`,
`PanBy`, and `Present`. One pad may be shown through multiple viewports without a
global pad scroll position.

T505 deliberately reuses `ContentWindow.CreateSubwindow(...)` as the shared
derived-pad-view contract instead of adding a redundant `CursesSubpad` type.
T506 adds independent per-viewport `HasVisiblePadChanges` observation. There is
no public global pad-clean/acknowledge operation, so one viewport cannot hide
changes from another.

T507 is complete and green on Windows, Linux, macOS, and package-only validation.
It covers destination resize/reposition, all pad corners, wide-cell boundaries,
very tall and wide pads, repeated panning, editor-like mutations, and fresh
package consumption.

T508 is the active public API/documentation/package regret gate. The 0.5 public
surface is feature-frozen. Per-cell viewport revision tracking is internal and
enabled only for pad backing surfaces; ordinary logical screens retain their
pre-0.5 memory profile.

Earlier stable releases established:

- `0.4.0`: Unicode-safe window geometry, editing, composition, drawing, and
  damage-range operations;
- `0.3.0`: Unicode 17 terminal-cell semantics and column-safe text helpers;
- `0.2.0`: complete stable `Icod.Terminal 1.0.0` semantic input parity.

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
train through `1.0.0`, and `Icod.DCurses-0.5.0-Development-Roadmap.md` for the
active pad/large-surface tranche. Current 0.5 decisions and gates are recorded
in:

- `docs/T504-Stateful-Pad-Viewport-and-Panning.md`
- `docs/T505-T506-Derived-Pad-Views-and-Damage.md`
- `docs/T507-Resize-Clipping-and-Large-Surface-Acceptance.md`
- `docs/T508-Public-API-Documentation-and-Package-Regret-Gate.md`
- `docs/Public-API-Baseline-0.5.md`

The completed 0.4 window-editing release is recorded in
`Icod.DCurses-0.4.0-Development-Roadmap.md`, its T402-T409 documents, and
`docs/Public-API-Baseline-0.4.md`.

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
top / slabtop / watch / editors / pagers / other TUIs
                         |
                    Icod.DCurses
       windows / pads / cells / refresh
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
dotnet add package Icod.DCurses --version 0.4.0
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

## Pads and large surfaces (`0.5`)

Pads are in-memory off-screen logical surfaces. They reuse ordinary
`CursesWindow` editing semantics and can be larger than the destination terminal
screen:

```csharp
CursesPad pad = new( 200, 5_000 );
CursesWindow content = pad.ContentWindow;

content.Move( 100, 20 );
content.Write( "A界B — large logical document" );

CursesWindow editor = session.StandardScreen;
CursesPadViewport viewport = pad.CreateViewport(
    editor,
    padRow: 95,
    padColumn: 10,
    rows: 20,
    columns: 70,
    destinationRow: 1,
    destinationColumn: 2
);

viewport.Present();
await session.RefreshAsync();

viewport.PanBy( 10, 0 );
if ( viewport.HasVisiblePadChanges ) {
    viewport.Present();
    await session.RefreshAsync();
}
```

`HasVisiblePadChanges` reports whether the currently visible **pad source** moved
or changed since that viewport last presented. It does not claim ownership of
the destination. `Present()` remains authoritative and will restore the viewport
if another logical window overwrote its destination region.

Multiple viewports can observe one pad independently. Presenting one does not
acknowledge changes for another. Shared pad-local views use
`pad.ContentWindow.CreateSubwindow(...)`; no separate subpad hierarchy is
required.

Pads do not own terminal sessions or physical refresh state, and they do not
resize merely because the terminal resizes. Viewport geometry is revalidated
against the current destination on every presentation.

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
