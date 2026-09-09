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

`Icod.DCurses 1.0.0` is in stable-release closure on PR #23.

The current candidate is `1.0.0-rc.1` with `AssemblyVersion 1.0.0.0`. It promotes the exact public and semantic contract frozen by merged `0.9.0`; it does not introduce another feature family or a deferred breaking-cleanup pass.

The merged `0.9.0` source baseline passed the complete `Release` matrix on Windows, Linux, and macOS x64/ARM64 plus package/fresh-consumer validation before 1.0 work began.

The proposed stable public contract contains:

```text
43 exported types
309 canonical declared contract lines
sha256 274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
```

The 1.0 machine baseline is intentionally identical to the 0.9 freeze. CI regenerates the compiled fingerprint for `net8.0`, `net9.0`, and `net10.0`, and a separate compatibility test requires the 1.0 baseline to match the historical 0.9 baseline exactly.

The direct dependency baseline remains:

- `Icod.Terminal` 1.4.0
- `Icod.TermInfo` 1.10.0

## Installation

The latest published GitHub release remains `0.8.0`. The merged `0.9.0` source and the `1.0.0-rc.1` PR candidate are not presented here as published stable packages until their respective tag/publication workflows complete.

```text
dotnet add package Icod.DCurses --version 0.8.0
```

After `v1.0.0` is merged, tagged, published, and verified, this installation example will move to the stable 1.0 package.

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

## Targets

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

A `CursesSession` restores the presentation and Terminal-owned state it acquires when disposed. Applications should consume terminal input and lifecycle activity through the curses/Terminal ownership model rather than adding a parallel byte reader.

## Stable 1.0 compatibility contract

The 1.0 release candidate carries forward the exact contract frozen in 0.9.

The accepted compatibility rules include:

- coordinates are zero-based and row/column ordered;
- subwindow origins are relative to their immediate parent;
- rows/columns mean height/width;
- column text intervals are half-open and never split a two-column text element;
- Unicode width data is Unicode 17.0.0;
- East Asian Ambiguous characters are narrow by default and wide only through `UnicodeCursesTextWidthProvider.WideAmbiguousInstance`;
- semantic line cells remain distinct from ordinary Unicode box-drawing text;
- logical screen/window/pad/viewport mutation is single-writer unless explicitly documented otherwise;
- one Terminal-owned event consumer may wait concurrently with serialized refresh/output activity;
- caller cancellation remains cancellation, while disposal-unblocked waits surface `ObjectDisposedException`;
- repeated disposal shares one restoration operation;
- uncertain/partial output invalidates retained physical knowledge so a later refresh can repaint safely;
- independently meaningful primary/restoration failures remain observable;
- Terminal remains the authoritative owner of host-state restoration.

The only lower-layer type definitions intentionally visible in the public DCurses contract are:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

These preserve the authoritative session, endpoint, live-size result, and immutable terminal-description vocabulary. No additional Terminal/TermInfo type may enter a public DCurses signature without an explicit compatibility decision.

See:

- `docs/Public-API-Fingerprint-1.0.json`
- `docs/Public-API-Baseline-1.0.md`
- `docs/1.0-Stable-Compatibility-and-Migration-Guide.md`

## Concurrency and production hardening

The stable library uses a deliberately narrow concurrency model rather than pervasive per-cell locking:

- logical screens, windows, pads, and viewports are single-writer;
- one Terminal-owned event wait may coexist with refresh/output work;
- terminal-mutating curses operations are serialized internally;
- caller cancellation of one wait does not discard Terminal decoder state or fragmented input;
- disposal unblocks pending DCurses event/lifecycle waits while preserving authoritative restoration;
- resize/suspend/resume and rich-input/full-screen ownership survive repeated stress cycles;
- output uncertainty invalidates retained physical state and a later refresh returns through a safe repaint path.

No public scheduler, lock/token abstraction, hardening helper, or diagnostics/statistics surface is part of the stable API.

## Refresh and output optimization

The retained logical/physical screen model remains authoritative. DCurses may choose a cheaper terminal operation only when the active TermInfo description advertises the required capability, retained state proves the same final result, and the emitted cost is a strict win. Otherwise it uses the ordinary renderer.

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

Representative deterministic maintainer fixtures include:

```text
T701 established-default -> bold: 19 bytes / 4 writes
T707 established-default -> bold: 13 bytes / 3 writes
editor two-column insertion:       2 optimized vs 34 fallback bytes
pager one-line deletion:            4 optimized vs 166 fallback bytes
160 x 60 full repaint:           9661 bytes / 121 writes / 1 flush
1000 one-cell updates:           2000 bytes / 2000 writes / 1000 flushes
```

These are comparison fixtures, not universal performance claims for every terminal.

## Presentation and semantic drawing

Logical styles remain terminal-independent. The physical renderer resolves them against advertised TermInfo capabilities and degrades unsupported presentation without mutating logical `CursesStyle` values.

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

`CursesPresentationCapabilities` exposes curses-level presentation information without requiring ordinary applications to inspect raw terminfo strings.

## Pads and large surfaces

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

## Window editing and composition

Windows are shared logical views. Editing, copying, overlay, drawing, and damage operations preserve the Unicode/wide-cell contract.

```csharp
CursesScreen logical = new( 80, 24 );
CursesWindow editor = logical.CreateWindow( 2, 4, 18, 60 );

editor.Move( 1, 2 );
editor.Write( "A界B" );
editor.InsertCells( 2 );
editor.DrawHorizontalLine( 16, 1, 58 );
editor.TouchRegion( 0, 0, 18, 60 );
```

`CopyRectangleTo` copies source blanks; `OverlayRectangleTo` treats ordinary source blanks as transparent.

## Unicode column helpers

```csharp
int columns = CursesText.MeasureColumns( "A界B" );
string prefix = CursesText.TruncateToColumns( "A界B", 3 );
string slice = CursesText.SliceByColumns( "A界B", 1, 2 );
```

The helpers normalize malformed UTF-16, reject terminal controls, operate on complete Unicode text elements, and never return half of a two-column element.

## Modern keyboard and rich input

Applications can request richer keyboard reporting through curses-owned protocol options while Terminal remains the decoder and lease owner:

```csharp
var keyboard = await session.AcquireInputProtocolsAsync(
    new CursesInputProtocolOptions {
        KeyboardReportingMode = CursesKeyboardReportingMode.EventTypes
    }
);
```

The curses event facade also carries stable focus, paste, mouse, lifecycle, and end-of-input semantics.

## Validation and packaging

Local wrappers use Debug configuration:

```sh
build.cmd
```

or:

```sh
./build.sh
```

Pull requests use Staging with warnings-as-errors. Pushes to `main` and release tags use Release. Runtime validation covers Windows/Linux/macOS x64 and ARM64; the library/test matrix covers `net8.0`, `net9.0`, and `net10.0`.

Package validation verifies the generated `.nupkg`/`.snupkg`, package and assembly identity, exact dependency groups, README/license/icon/repository metadata, XML documentation, portable symbols, and a fresh package-only consumer rather than relying only on project references.

The release workflow derives displayed `Icod.Terminal` and `Icod.TermInfo` dependency versions directly from the project `PackageReference` values so GitHub Release notes cannot silently drift from package metadata.

## Release documentation

The active release-closure roadmap is:

- `Icod.DCurses-1.0.0-Development-Roadmap.md`

The 1.0 closure records include:

- `docs/T1001-1.0.0-Release-Closure-Foundation.md`
- `docs/T1002-1.0-Public-Contract-Carry-Forward.md`
- `docs/T1003-1.0-Documentation-Package-and-Release-Audit.md`
- `docs/Public-API-Fingerprint-1.0.json`
- `docs/Public-API-Baseline-1.0.md`
- `docs/1.0-Stable-Compatibility-and-Migration-Guide.md`

The frozen 0.9 contract and earlier release roadmaps remain historical compatibility records and are not rewritten merely to reflect later dependency or package versions.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients which honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.
