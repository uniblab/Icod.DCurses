# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET.

It sits above `Icod.Terminal` and `Icod.TermInfo`:

- `Icod.TermInfo` is the immutable terminal-capability authority;
- `Icod.Terminal` owns the live terminal session, host mode, dimensions, lifecycle, input decoding, presentation leases, and input-protocol leases;
- `Icod.DCurses` owns curses-shaped events, logical screens and windows, pads and viewports, terminal cells and styles, semantic drawing, and retained refresh/damage policy.

## Status

`Icod.DCurses 0.8.0` is the current published stable package.

The `0.9.0` line is the final pre-1.0 contract freeze. Its runtime/public behavior has been promoted unchanged from the green `0.9.0-rc.1` candidate to stable `0.9.0` source, retaining assembly version `0.9.0.0`. The remaining branch gate is exact-head Staging validation before merge; post-merge Release validation, tagging, and publication are separate later actions.

The frozen 0.9 public contract contains:

```text
43 exported types
309 canonical declared contract lines
sha256 274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
```

CI regenerates that fingerprint from the compiled assembly for `net8.0`, `net9.0`, and `net10.0`. It includes declared public signatures, enum values, generic constraints, parameter/ref/default metadata, accessor visibility, fields/constants, and compiled nullability. Dedicated compatibility tests additionally freeze geometry, Unicode/cell, lifetime, cancellation, ownership, and failure behavior.

No public breaking cleanup was accepted during the 0.9 regret review. Existing 0.8 source consumers therefore have no planned source migration to the frozen 0.9 contract.

The dependency baseline is:

- `Icod.Terminal` 1.4.0
- `Icod.TermInfo` 1.10.0

## Installation

Until `0.9.0` is merged, tagged, and published, the current published stable package remains:

```text
dotnet add package Icod.DCurses --version 0.8.0
```

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

`Icod.DCurses` does not hard-code one terminal family, maintain a second capability database, install a second raw input loop, own terminal modes independently of `Icod.Terminal`, emulate a terminal, or create/manage PTYs.

## Target

- .NET 8
- .NET 9
- .NET 10
- C# 13
- Windows x64/ARM64
- Linux x64/ARM64
- macOS x64/ARM64

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

A `CursesSession` restores the presentation and Terminal-owned state it acquires when it is disposed. Applications should consume terminal input and lifecycle activity through the curses/Terminal ownership model rather than adding a parallel byte reader.

## Contract freeze and compatibility (`0.9`)

The 0.9 line freezes the public contract intended for `1.0.0` rather than introducing a new feature family.

The accepted compatibility rules include:

- coordinates are zero-based and row/column ordered;
- subwindow origins are relative to their immediate parent;
- rows/columns mean height/width;
- column text intervals are half-open and never split a two-column text element;
- Unicode width data is Unicode 17.0.0;
- East Asian Ambiguous characters are narrow by default and wide only through `UnicodeCursesTextWidthProvider.WideAmbiguousInstance`;
- semantic line cells remain distinct from ordinary Unicode box-drawing text;
- logical screen/window/pad/viewport mutation is single-writer unless a member explicitly states otherwise;
- one Terminal-owned event consumer may wait concurrently with serialized refresh/output activity;
- caller cancellation remains cancellation, while disposal-unblocked waits surface `ObjectDisposedException`;
- uncertain/partial output invalidates retained physical knowledge so a later refresh can repaint safely;
- repeated disposal shares one restoration operation;
- independently meaningful primary/restoration failures are preserved rather than silently discarded.

The final intentional lower-layer public type set is exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

These types preserve the authoritative lower-layer session, endpoint, live-size result, and immutable terminal-description vocabulary. No additional Terminal/TermInfo type may enter a public DCurses signature without an explicit compatibility review.

See `docs/0.9-Contract-Freeze-and-1.0-Migration-Guide.md` and `docs/Public-API-Baseline-0.9.md` for the complete frozen contract.

## Production hardening (`0.8`)

The 0.8 release established the concurrency and failure-resilience model retained by 0.9:

- logical surfaces are deliberately single-writer rather than per-cell locked;
- input waits may coexist with refresh/output without adding another decoder;
- terminal-mutating work is internally serialized;
- disposal unblocks pending DCurses input/lifecycle waits while preserving Terminal decoder state;
- caller cancellation does not discard fragmented UTF-8 or escape input owned by Terminal;
- resize/suspend/resume and rich-input/full-screen ownership are stress-tested;
- uncertain output invalidates retained physical state for safe recovery;
- large pads/screens, sparse refresh, high-frequency refresh, and no-op refresh are exercised under bounded deterministic stress.

The 0.8 release added no public API.

## Refresh and output optimization (`0.7`)

The retained logical/physical screen model remains authoritative. DCurses may select a cheaper physical terminal operation only when the active TermInfo description advertises the required capability, retained state proves the same final result, and the concrete emitted cost is a strict win. Otherwise the ordinary renderer is used.

Synchronized presentation is opt-in:

```csharp
await using CursesSession session = await CursesSession.OpenAsync(
    new CursesSessionOptions {
        UseSynchronizedOutput = true
    }
);
```

`UseSynchronizedOutput` defaults to `false`. DCurses delegates synchronized-output ownership to `Icod.Terminal`; it does not infer support from a terminal name or construct private mode sequences itself.

Internal refresh optimization can select safe cursor motion, erase operations, character/line insertion and deletion, full-width scrolling, temporary scroll regions, and differential rendition transitions. Correctness and recoverability take precedence over minimizing every possible escape stream.

Representative maintainer fixtures include:

```text
T701 established-default -> bold: 19 bytes / 4 writes
T707 established-default -> bold: 13 bytes / 3 writes
editor two-column insertion:       2 optimized vs 34 fallback bytes
pager one-line deletion:            4 optimized vs 166 fallback bytes
160 x 60 full repaint:           9661 bytes / 121 writes / 1 flush
1000 one-cell updates:           2000 bytes / 2000 writes / 1000 flushes
```

These are deterministic comparison fixtures, not universal performance claims for every terminal.

## Rendition and semantic drawing (`0.6`)

Logical styles are terminal-independent. The physical renderer resolves them against advertised capabilities and degrades unsupported presentation without mutating logical `CursesStyle` values.

```csharp
CursesStyle heading = new(
    CursesColor.Indexed( 14 ),
    CursesColor.Default,
    CursesTextAttributes.Bold
        | CursesTextAttributes.Italic
        | CursesTextAttributes.Underline
);

screen.Write( "Presentation-aware heading", heading );
screen.DrawHorizontalLine( 12, 4, 30 );
```

`CursesPresentationCapabilities` exposes the curses-level presentation vocabulary needed by applications without requiring ordinary consumers to inspect raw terminfo strings.

## Pads and large surfaces (`0.5`)

`CursesPad` is an off-screen logical surface and reuses ordinary `CursesWindow` editing semantics.

```csharp
CursesPad pad = new( 200, 5_000 );
CursesWindow content = pad.ContentWindow;
content.Move( 100, 20 );
content.Write( "A界B — large logical document" );

CursesPadViewport viewport = pad.CreateViewport(
    session.StandardScreen,
    padRow: 95,
    padColumn: 10,
    rows: 20,
    columns: 70,
    destinationRow: 1,
    destinationColumn: 2
);

viewport.Present();
await session.RefreshAsync();
```

Multiple viewports may observe one pad independently. Pads and viewports do not own terminal sessions or physical refresh state.

## Window editing and composition (`0.4`)

Windows are shared logical views. Editing, copying, overlay, drawing, and damage operations preserve the Unicode/wide-cell rules established by the text model.

```csharp
CursesScreen logical = new( 80, 24 );
CursesWindow editor = logical.CreateWindow( 2, 4, 18, 60 );

editor.Move( 1, 2 );
editor.Write( "A界B" );
editor.InsertCells( 2 );
editor.DrawHorizontalLine( 16, 1, 58 );
editor.TouchRegion( 0, 0, 18, 60 );
```

`CopyRectangleTo` is destructive copy including blanks; `OverlayRectangleTo` treats ordinary source blanks as transparent.

## Unicode column helpers (`0.3`)

```csharp
int columns = CursesText.MeasureColumns( "A界B" );
string prefix = CursesText.TruncateToColumns( "A界B", 3 );
string slice = CursesText.SliceByColumns( "A界B", 1, 2 );
```

The helpers normalize malformed UTF-16, reject terminal controls, operate on complete Unicode text elements, and never return half of a two-column element.

## Modern keyboard and rich input (`0.2+`)

Applications can request richer keyboard reporting through curses-owned protocol options while Terminal remains the decoder/lease owner:

```csharp
var keyboard = await session.AcquireInputProtocolsAsync(
    new CursesInputProtocolOptions {
        KeyboardReportingMode = CursesKeyboardReportingMode.EventTypes
    }
);
```

The curses event facade also carries the stable focus, paste, mouse, lifecycle, and end-of-input semantics established in the pre-1.0 train.

## Validation and packaging

Local wrappers use Debug configuration:

```sh
build.cmd
```

or:

```sh
./build.sh
```

Pull requests use Staging with warnings-as-errors. `main` and release tags use Release. The PR runtime matrix covers Windows/Linux/macOS x64 and ARM64; the library/test matrix covers `net8.0`, `net9.0`, and `net10.0`.

Package validation verifies the generated `.nupkg`/`.snupkg`, exact dependency groups, README/license/icon/repository metadata, XML documentation, portable symbols, and a fresh package-only consumer rather than relying only on repository project references.

The release workflow derives the displayed `Icod.Terminal` and `Icod.TermInfo` dependency versions directly from the project `PackageReference` values so GitHub Release notes cannot silently drift from package metadata.

## Release-train documentation

The authoritative remaining release train is `Icod.DCurses-1.0.0-Development-Roadmap.md`.

The 0.9 contract freeze is recorded in:

- `Icod.DCurses-0.9.0-Development-Roadmap.md`
- `docs/Public-API-Fingerprint-0.9.json`
- `docs/Public-API-Baseline-0.9.md`
- `docs/T901-Public-Surface-Inventory-and-Regret-Review.md`
- `docs/T904-Lifetime-Ownership-Exception-and-Cancellation-Freeze.md`
- `docs/T905-Nullable-Documentation-and-Dependency-Regret-Review.md`
- `docs/0.9-Contract-Freeze-and-1.0-Migration-Guide.md`
- `docs/T907-Representative-Application-Acceptance-Gate.md`
- `docs/T908-Contract-Regret-Package-Architecture-and-RC-Gate.md`
- `docs/T909-0.9.0-Stable-Release-Closure-and-Documentation-Audit.md`

Historical release roadmaps and tranche documents remain historical records and are not rewritten merely to reflect later dependency or package versions.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients which honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.
