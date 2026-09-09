# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-like terminal UI library for
.NET.

The library sits above `Icod.TermInfo` and `Icod.Terminal`. `Icod.TermInfo`
remains the immutable terminal-capability authority; `Icod.Terminal` owns the
live terminal session, host mode, dimensions, lifecycle, input decoding, and
reversible presentation and input-protocol leases. `Icod.DCurses` owns
curses-shaped events, virtual screens and windows, pads and viewports, terminal
cells and styles, rendition policy, semantic line drawing, and refresh/damage
synchronization.

## Status

`Icod.DCurses 0.8.0` is the current published stable release.

The `0.9.0` development line is the contract-freeze and release-candidate pass
for the managed API intended to become `1.0.0`. It begins from the published
0.8 production-hardening baseline with package checkpoint `0.9.0-alpha.1` and
assembly version `0.9.0.0`.

The initial 0.9 public-surface inventory contains **43 exported types** and
**309 canonical declared contract lines**. CI regenerates a deterministic
public API fingerprint from each compiled target framework; the initial accepted
SHA-256 is:

```text
274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
```

The fingerprint freezes type/member signatures, enum values, parameter/ref/default
metadata, generic constraints, accessor visibility, and compiled nullability.
Separate compatibility tests pin geometry, Unicode/cell, lifetime, cancellation,
ownership, and failure semantics which reflection cannot express by itself.
No major feature family is planned for 0.9, and no breaking cleanup is intended
to be deferred to `1.0.0`.

The published 0.8 release deliberately added no public API; it strengthened
lifetime, concurrency, cancellation, lifecycle, failure-recovery, repeated
ownership, and large-surface/high-frequency behavior around the 0.7 contract.

The accepted 0.8 hardening contract includes:

- single-writer logical `CursesScreen`, `CursesWindow`, `CursesPad`, and
  `CursesPadViewport` mutation unless a member explicitly documents otherwise;
- one Terminal-owned event consumer waiting concurrently with refresh/output
  activity without adding a second byte reader;
- internal serialization of terminal-mutating refresh, presentation, cursor,
  alert, protocol, suspend, and disposal work;
- disposal-induced cancellation for pending DCurses input/lifecycle waits while
  retaining authoritative Terminal restoration;
- preservation of Terminal decoder state and fragmented input when one caller
  cancels a DCurses wait;
- safe retained-screen invalidation and repaint after uncertain/partial output;
- repeated resize/suspend/resume and rich-input/full-screen ownership cycles;
- bounded large-pad, large-screen, sparse-refresh, high-frequency, and no-op
  refresh stress;
- six-architecture validation across Windows, Linux, and macOS x64/ARM64.

The published 0.7 release added or improved:

- opt-in Terminal-owned synchronized-output framing at the complete refresh
  transaction boundary;
- deterministic cost-aware absolute/relative cursor-motion selection;
- cost-aware literal blank, clear-to-end-of-line, clear-to-end-of-screen, and
  whole-screen erase selection;
- exact physical insert/delete-character optimization for safe row-local shifts;
- exact physical insert/delete-line and forward/reverse scrolling optimization,
  including temporary full-width scrolling regions with failure-safe restoration;
- differential retained-rendition transitions that avoid redundant reset/reapply
  work while preserving reset-first safety when state must be normalized;
- deterministic byte/write release gates, including optimized-vs-fallback editor
  and pager workloads;
- one deliberate public API addition,
  `CursesSessionOptions.UseSynchronizedOutput`, default `false`.

Earlier stable releases established:

- `0.6.0`: complete semantic rendition, presentation-capability observation,
  semantic line drawing, and capability-aware degradation;
- `0.5.0`: large off-screen pads, independent pannable viewports, derived views,
  and per-viewport visible-change observation;
- `0.4.0`: Unicode-safe window geometry, editing, composition, drawing, and
  damage-range operations;
- `0.3.0`: Unicode 17 terminal-cell semantics and column-safe text helpers;
- `0.2.0`: complete stable `Icod.Terminal 1.0.0` semantic input parity.

The dependency baseline remains:

- `Icod.Terminal` 1.4.0
- `Icod.TermInfo` 1.10.0

The retired DCurses backend, native mode, lifecycle-source, input-decoder, and
pre-Terminal session implementations remain removed. DCurses does not add a
mouse parser, paste reader, protocol escape emitter, keyboard decoder, or second
input loop.

The first release line was driven by the requirements of `top`, `slabtop`, and
`watch`. The 1.0 development train broadens that foundation into a general
managed TUI contract.

See `Icod.DCurses-1.0.0-Development-Roadmap.md` for the authoritative release
train through `1.0.0`, and `Icod.DCurses-0.9.0-Development-Roadmap.md` for the
contract-freeze tranche. The 0.9 inventory, compatibility, and migration decisions
are recorded in:

- `docs/Public-API-Fingerprint-0.9.json`
- `docs/T901-Public-Surface-Inventory-and-Regret-Review.md`
- `docs/T904-Lifetime-Ownership-Exception-and-Cancellation-Freeze.md`
- `docs/T905-Nullable-Documentation-and-Dependency-Regret-Review.md`
- `docs/0.9-Contract-Freeze-and-1.0-Migration-Guide.md`
- `docs/T907-Representative-Application-Acceptance-Gate.md`.

The completed 0.8 production-hardening release is recorded in
`Icod.DCurses-0.8.0-Development-Roadmap.md`, its T801-T809 documents, and
`docs/Public-API-Baseline-0.8.md`.

The completed 0.7 refresh/output release is recorded in
`Icod.DCurses-0.7.0-Development-Roadmap.md`, its T701-T709 documents, and
`docs/Public-API-Baseline-0.7.md`.

The completed 0.6 presentation release is recorded in
`Icod.DCurses-0.6.0-Development-Roadmap.md`, its T603-T609 documents, and
`docs/Public-API-Baseline-0.6.md`.

The completed 0.5 pad release is recorded in
`Icod.DCurses-0.5.0-Development-Roadmap.md`, its T504-T509 documents, and
`docs/Public-API-Baseline-0.5.md`.

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
     rendition / drawing / curses events
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
dotnet add package Icod.DCurses --version 0.8.0
```

The `0.9.0` line is under development on PR #22 and is not presented as a stable
published package until its release-candidate, stable-source, merge, Release,
tag, and publication gates complete.

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

## Contract freeze and compatibility (`0.9`)

The 0.9 line turns the pre-1.0 contract into a machine-guarded compatibility
baseline. Existing 0.8 consumers have no planned source migration at the initial
freeze. Any public cleanup identified before 1.0 must occur during 0.9 together
with a fingerprint update and migration note; it will not be deferred to the
`1.0.0` closure.

The accepted compatibility rules include:

- zero-based row/column coordinates and parent-local subwindow origins;
- half-open column intervals which never split a two-column text element;
- Unicode 17.0.0 width data with narrow East Asian Ambiguous characters by
  default and an explicit wide-Ambiguous provider;
- semantic line cells remaining distinct from ordinary Unicode box-drawing text;
- single-writer logical surfaces with one supported event wait concurrent with
  refresh/output work;
- caller cancellation remaining cancellation while disposal-unblocked waits
  surface `ObjectDisposedException`;
- conservative retained-state invalidation after uncertain output and safe retry;
- exactly five intentionally exposed lower-layer public type definitions:
  `TerminalSession`, `TerminalEndpoint`, `TerminalControlResult<T>`,
  `TerminalDescription`, and `TerminalSize`.

See `docs/0.9-Contract-Freeze-and-1.0-Migration-Guide.md` for the compatibility
and migration details.

## Production hardening and concurrency (`0.8`)

The 0.8 line freezes a deliberately narrow concurrency model rather than adding
pervasive locking.

Logical screen/window/pad/view mutation is single-writer. Applications may keep
one event wait pending while another application path performs refresh/output
work, but they should not create competing independent event-reader loops over
one session. Terminal-mutating operations are serialized internally.

Disposal begins a private DCurses lifetime-cancellation path for pending public
input/lifecycle waits. A wait canceled by its caller remains cancellation; a
wait unblocked because the session is being disposed surfaces
`ObjectDisposedException`. This private wait cancellation does not replace or
cancel Terminal's authoritative decoder read, so fragmented UTF-8 or escape
input remains preserved for the next valid wait.

If a refresh or control write fails after uncertain partial progress, retained
physical-screen knowledge is invalidated. A later refresh returns through the
safe renderer rather than trusting partially emitted state. Existing dual-
failure rules continue to preserve both primary-operation and restoration
failures when synchronized-output or temporary terminal-state restoration also
fails.

The release gate exercises this contract under:

- concurrent input wait and refresh;
- caller cancellation and disposal during waits;
- resize storms and repeated suspend/resume;
- disposal while suspended;
- partial output and rendition-restoration failures;
- repeated rich-input/full-screen ownership cycles;
- large pads and full screens;
- 1,000 sparse refreshes and repeated no-op refreshes;
- Windows/Linux/macOS x64 and ARM64.

No public scheduler, lock/token abstraction, diagnostics type, or performance
statistics API is added by 0.8.

## Refresh and output optimization (`0.7`)

The retained logical/physical screen model remains authoritative. DCurses may
select a cheaper physical terminal operation only when the current TermInfo
description advertises the required capabilities, the retained state proves the
operation produces the same final screen, and the concrete emitted-byte cost is
a strict win. Otherwise the ordinary renderer is used.

Applications that prefer synchronized presentation can opt in at the curses
session boundary:

```csharp
await using CursesSession session = await CursesSession.OpenAsync(
    new CursesSessionOptions {
        UseSynchronizedOutput = true
    }
);
```

This asks `Icod.Terminal` to frame each complete `RefreshAsync()` transaction
using its synchronized-output lease. The option defaults to `false`: one
unnested begin/end pair adds 16 protocol bytes, so framing can be a net cost for
small or high-frequency updates. DCurses does not infer support from a terminal
name and does not construct private mode 2026 sequences itself.

The other 0.7 optimizations are internal. Depending on the advertised TermInfo
capabilities and the exact diff, the refresh engine can choose:

- shorter safe absolute/relative cursor movement;
- literal blanks versus `el`, `ed`, or whole-screen clear;
- `ich`/`dch` for exact row-local character shifts;
- `il`/`dl` or forward/reverse scroll for exact full-row shifts;
- temporary `csr` regions for exact full-width interior vertical shifts, always
  restored before the refresh transaction finishes;
- incremental style transitions when a known physical style only gains
  attributes or changes non-default colors.

Safety preconditions are deliberately conservative. Ambiguous wide-cell
character shifts, partial-width scroll ownership, non-default erase regions,
unknown retained state, and equal-cost candidates stay on the ordinary path.
Output failure invalidates retained knowledge so the next refresh returns through
the safe fallback path.

The deterministic release-gate fixtures record:

```text
T701 established-default -> bold: 19 bytes / 4 writes
T707 established-default -> bold: 13 bytes / 3 writes
editor two-column insertion:       2 optimized vs 34 fallback bytes
pager one-line deletion:            4 optimized vs 166 fallback bytes
160 x 60 full repaint:           9661 bytes / 121 writes / 1 flush
1000 one-cell updates:           2000 bytes / 2000 writes / 1000 flushes
```

These are synthetic maintainer fixtures used to make optimization decisions
repeatable; they are not universal performance claims for all terminals.

## Rendition and semantic drawing (`0.6`)

Logical styles remain terminal-independent. The physical refresh layer resolves
them against the selected terminal and degrades unsupported presentation
features in a controlled way:

```csharp
CursesPresentationCapabilities presentation =
    session.PresentationCapabilities;

CursesStyle heading = new(
    CursesColor.Indexed( 14 ),
    CursesColor.Default,
    CursesTextAttributes.Bold
        | CursesTextAttributes.Italic
        | CursesTextAttributes.Underline
);

screen.Write( "Presentation-aware heading", heading );

CursesWindow panel = screen.CreateSubwindow(
    2,
    4,
    8,
    40
);
panel.DrawBorder( heading.WithForeground( CursesColor.Default ) );
```

The complete semantic attribute vocabulary is:

```text
Bold
Dim
Underline
Reverse
Standout
Italic
Blink
Conceal
Strikeout
```

`CursesPresentationCapabilities` reports the safe indexed-color range, direct
RGB support, native attributes, `ncv` color restrictions, alternate-character-
set support, default-color restoration, and cursor-presentation availability.
Applications can make ordinary TUI presentation decisions without inspecting
raw terminfo strings.

Color resolution uses `Icod.TermInfo.TerminalColors`. An indexed request is
emitted only when it falls inside the safely addressable range. Direct RGB is
used only when TermInfo advertises a safe direct-color model. Unsafe or
unsupported color requests degrade to terminal default rather than being wrapped
or passed to an unsupported selector. Logical `CursesStyle` values stored in
cells are not changed by this physical degradation.

Semantic line drawing carries intent in `CursesCell` rather than storing terminal
escape sequences:

```csharp
CursesCell crossing = CursesCell.Line(
    CursesLineGlyph.Crossing,
    heading
);

screen.DrawHorizontalLine( 12, 4, 30 );
screen.DrawVerticalLine( 4, 34, 8 );
```

At refresh time, semantic line cells use the terminal's advertised alternate
character set when the requested glyph is mapped. Otherwise DCurses uses the
canonical one-column Unicode box-drawing glyph, with ASCII as the final safe
fallback. Ordinary text containing characters such as `─` remains ordinary text
and is never reinterpreted as ACS content.

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

The original drawing methods continue to accept exact caller-supplied one-column
cells. The 0.6 semantic overloads are additive and do not change the 0.4 exact-
cell contract.

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
release tags use `Release`. Both the library and test projects use warning level
4 with warnings-as-errors under Staging, so ordinary PR validation exercises the
same compiler/analyzer severity policy that matters to Release without compiling
the solution twice. The 0.9 contract-freeze PR validates Windows x64/ARM64,
Linux x64/ARM64, and macOS x64/ARM64. Package validation exercises the packed
artifact and a fresh package-only consumer rather than relying only on repository
project references.

The Unicode width-data generator is a maintainer tool outside the solution. See
`tools/unicode-width-generator/README.md`; normal builds do not fetch Unicode
data from the network.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See
`LICENSE`.

The NuGet package declares license acceptance as required. Package clients which
honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the
license terms before installation.