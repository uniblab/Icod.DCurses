# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET.

It sits above `Icod.Terminal` and `Icod.TermInfo`:

- `Icod.TermInfo` is the immutable terminal-capability authority;
- `Icod.Terminal` owns the live terminal session, host mode, dimensions, lifecycle, input decoding, semantic terminal protocols, presentation leases, and input-protocol leases;
- `Icod.DCurses` owns curses-shaped events, logical screens and windows, pads and viewports, terminal cells and styles, semantic drawing/content, and retained refresh/damage policy.

## Status

`Icod.DCurses 1.0.0` is the current published stable release.

`Icod.DCurses 1.1.0-alpha.8` is the active post-1.0 pre-RC checkpoint on PR #25.

T1101 froze the stable 1.0 compatibility floor and compatible 1.x assembly-version policy. T1102 selected a lazily allocated row-sparse metadata reference plane. T1103 introduced the semantic-content API. T1104 added retained physical hyperlink rendering through Terminal's typed OSC 8 ownership. T1105 propagated semantic metadata through editing/composition/pads/resize. T1106 hardened cancellation, output failure, synchronized-output cleanup, lifecycle replay, and uncertain hyperlink cleanup. T1107 completed application/performance/allocation acceptance. T1108 has now performed the pre-RC public API, package, documentation, and regret review, including a source-compatibility correction to the semantic convenience name and a forward-compatible metadata nullability decision.

Current development identity:

```text
Version         1.1.0-alpha.8
PackageVersion  1.1.0-alpha.8
AssemblyVersion 1.0.0.0
Icod.Terminal   1.6.0
Icod.TermInfo   1.10.0
```

Stable 1.0 compatibility floor:

```text
43 exported types
309 canonical declared contract lines
sha256 274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
```

Accepted pre-RC 1.1 contract:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

The two intentional new exported types are `CursesHyperlink` and `CursesCellMetadata`. No additional Terminal/TermInfo public type is introduced by 1.1.

## Installation

The current published stable release is `1.0.0`:

```text
dotnet add package Icod.DCurses --version 1.0.0
```

Development packages are not presented here as the stable installation target.

## Architecture

```text
top / slabtop / watch / editors / pagers / other TUIs
                         |
                    Icod.DCurses
       windows / pads / cells / refresh
   rendition / semantic content / events
                         |
                    Icod.Terminal
      session / input / lifecycle / dimensions
       presentation / semantic protocols
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

The post-1.0 release train carries forward the contract frozen in 0.9 and published as 1.0.0.

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

The only lower-layer type definitions intentionally visible in the stable DCurses contract are:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

No additional Terminal/TermInfo type may enter a public DCurses signature without an explicit compatibility decision.

See:

- `docs/Public-API-Fingerprint-1.0.json`
- `docs/Public-API-Baseline-1.0.md`
- `docs/1.0-Stable-Compatibility-and-Migration-Guide.md`

## 1.1 semantic metadata and hyperlinks

The 1.1 release introduces semantic meaning attached to retained content, beginning with hyperlinks.

```text
visual rendition     -> CursesStyle
semantic meaning     -> CursesCellMetadata / CursesHyperlink
wire protocol/state  -> Icod.Terminal
```

### Sparse representation

T1102 measured the portable incremental cost of adding a metadata/token slot rather than assuming one private `CursesCell` size across architectures:

```text
cell + metadata-reference wrapper overhead   +8 bytes per cell
cell + int-token wrapper overhead             +8 bytes per cell
```

At the established 2,048 × 256 pad scale, one unconditional extra eight-byte slot would add exactly 4 MiB even when metadata is unused.

The selected row-sparse metadata plane allocates no per-row metadata storage until needed, releases empty rows, provides O(1) coordinate lookup, composes with row snapshots, and leaves `CursesCell` unchanged.

T1107 binds that representation to the reference scale: the 2,048 top-level row references contribute 16 KiB of deterministic reference payload, and ten populated 256-column rows contribute another 20 KiB, for 36 KiB of reference payload. This deliberately excludes runtime-specific array/object headers from the portable contract.

### Logical public contract

The accepted pre-RC semantic values are immutable DCurses-owned records:

```csharp
CursesCellMetadata metadata = new(
    new CursesHyperlink(
        "https://example.test/docs",
        "docs"
    )
);

screen.WriteWithMetadata(
    "documentation",
    metadata
);
```

For an explicit style plus metadata, use `Write(string, CursesStyle, CursesCellMetadata)`. Metadata can also be inspected or changed at window/virtual-screen coordinates through `GetMetadata(...)` and `SetMetadata(...)`.

The `WriteWithMetadata(...)` name is deliberate. Stable 1.0 already exposes `Write(string, CursesStyle)`; adding `Write(string, CursesCellMetadata)` would make previously valid `Write("text", default)` calls ambiguous when recompiled against 1.1.

`CursesCellMetadata.Hyperlink` is nullable in the accepted contract to leave room for future additive semantic kinds, while the current 1.1 constructor still requires a non-null `CursesHyperlink`.

Two-column text elements carry one coherent metadata value across the leader/continuation footprint. Ordinary unannotated replacement clears overwritten semantic metadata even if the glyph/style value is otherwise unchanged. Metadata-only changes participate in logical damage tracking.

### Retained physical hyperlink rendering

T1104 compares retained physical metadata independently of glyph/style equality. A hyperlink change therefore repaints even when the visible cell is unchanged, while an unchanged linked second refresh emits no repeated hyperlink payload.

Adjacent cells with equal style and metadata are coalesced into one semantic text run. Each linked payload is sent through Terminal's typed hyperlink operation; DCurses does not construct OSC 8 frames.

A real synchronized `TerminalSession`/`CursesSession` integration test verifies canonical Terminal OSC 8 begin/text/end output inside the synchronized-output bracket without deadlock.

### Structural semantic propagation

T1105 moves semantic metadata together with surviving logical content through:

- insert/delete cells;
- insert/delete lines;
- upward/downward scrolling;
- destructive rectangle copy;
- transparent overlay;
- pad presentation and independent pad viewports;
- preserved screen resize;
- wide-cell normalization and clipping repair.

The internal transient `CursesLogicalCellState` pairs `CursesCell` with optional `CursesCellMetadata` during these transformations without altering the public cell layout.

`CopyRectangleTo` transfers semantic state, including an annotated source blank. `OverlayRectangleTo` retains the stable transparency rule: a source blank replaces neither destination content nor destination metadata. Multiple pad viewports acknowledge semantic-only changes independently.

### Failure, cancellation, and recovery

T1106 distinguishes two Terminal-owned cleanup models.

A failed synchronized-output final release is retryable because DCurses still owns the `TerminalSynchronizedOutputLease`. DCurses retains that failed lease, invalidates physical state, and retries the same cleanup before starting another synchronized refresh or resetting rendition during lifecycle/disposal. Repeated cleanup failure blocks the new refresh body.

Terminal's bounded hyperlink operation is different. A non-cancellation failure may leave an internal synthetic hyperlink lease owned by Terminal, but that lease is not returned to DCurses. DCurses therefore cannot safely determine whether OSC 8 begin, application text, or close was the uncertain stage. After such a failure, the Terminal-backed curses output **fails closed for further application text until the owning `CursesSession` is disposed**. Terminal control cleanup and flush remain available, and Terminal session disposal remains the authoritative final hyperlink cleanup path.

Caller cancellation reported before hyperlink transmission does not poison later semantic output. If cancellation arrives after one bounded semantic run has completed but before the full refresh finishes, the refresh engine invalidates retained physical state and a later fresh-token refresh repaints the complete logical image.

Terminal's meaningful hyperlink text + cleanup dual failure remains visible as an aggregate through `CursesSession.RefreshAsync()`. The existing refresh + synchronized-output restoration dual-failure rule likewise remains intact.

Suspend/resume invalidates retained physical semantic knowledge, so visible linked content is repainted after lifecycle re-entry.

### Application, performance, and optimization acceptance

T1107 exercises the semantic implementation in editor-, pager-, and large-pad-shaped workloads rather than only isolated unit examples.

Editor coverage includes linked wide Unicode, style changes that retain hyperlink identity, insertion before and inside linked spans, and repeated semantic-only retargeting. Pager/help coverage includes many links, settled no-op refresh, viewport movement, line insertion/deletion, and scrolling while semantic identity follows the surviving line. The exact 2,048 × 256 reference pad exercises sparse and dense linked rows plus independent viewports.

Equivalent links are coalesced: a dense 256-cell linked row produces one bounded semantic hyperlink write. Intentionally distinct adjacent links remain distinct: 32 different links produce 32 semantic transactions.

Real Terminal-backed tests cover synchronized output both enabled and disabled, one rich-input event wait concurrent with semantic refresh, live resize, suspend/resume, and deterministic input-protocol cleanup.

Terminal-native erase, character-shift, line-shift, and scrolling shortcuts remain conservatively disabled while desired or retained physical semantic metadata exists. The non-semantic cost wins are established, but terminfo does not provide a portable guarantee that those physical transformations preserve emulator-side OSC 8 cell associations. Direct semantic rewriting remains the 1.1 correctness policy.

### Pre-RC regret gate

T1108 freezes the accepted 1.1 contract at 45 exported types / 337 canonical lines / SHA-256 `21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039` across all three target frameworks. It also extends the NuGet-only smoke consumer through the semantic API, adds a hyperlink to the minimal sample, removes the superseded duplicate T1103 draft, and records the machine/human 1.1 API baselines.

See:

- `docs/Public-API-Fingerprint-1.1.json`
- `docs/Public-API-Baseline-1.1.md`
- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`
- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`
- `docs/T1105-Editing-Composition-and-Pad-Semantic-Propagation.md`
- `docs/T1106-Semantic-Output-Lifecycle-Failure-and-Recovery-Hardening.md`
- `docs/T1107-Application-Performance-Allocation-and-Optimization-Acceptance.md`
- `docs/T1108-Public-API-Package-Documentation-and-Regret-Gate.md`

## Concurrency and production hardening

The stable library uses a deliberately narrow concurrency model rather than pervasive per-cell locking:

- logical screens, windows, pads, and viewports are single-writer;
- one Terminal-owned event wait may coexist with refresh/output work;
- terminal-mutating curses operations are serialized internally;
- caller cancellation does not discard Terminal decoder state;
- disposal unblocks pending DCurses event/lifecycle waits while preserving authoritative restoration;
- output uncertainty invalidates retained physical state;
- semantic cleanup uncertainty is never hidden by optimistic continued application output.

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

Internal refresh optimization can select safe cursor motion, erase operations, character/line insertion and deletion, full-width scrolling, temporary scroll regions, and differential rendition transitions for non-semantic content. Semantic state keeps structural terminal shortcuts disabled unless a future portable semantic-equivalence contract proves that the physical operation preserves hyperlink associations.

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

Multiple viewports may observe one pad independently. Pads and viewports do not own terminal sessions or physical refresh state. Semantic-only changes are observed and presented independently by multiple viewports.

## Window editing and composition

Windows are shared logical views. Editing, copying, overlay, drawing, and damage operations preserve the Unicode/wide-cell contract and, where content moves, its semantic metadata.

```csharp
CursesScreen logical = new( 80, 24 );
CursesWindow editor = logical.CreateWindow( 2, 4, 18, 60 );

editor.Move( 1, 2 );
editor.Write( "A界B" );
editor.InsertCells( 2 );
editor.DrawHorizontalLine( 16, 1, 58 );
editor.TouchRegion( 0, 0, 18, 60 );
```

`CopyRectangleTo` copies source blanks and their semantic state; `OverlayRectangleTo` treats source blanks as fully transparent and therefore preserves destination semantic metadata at those coordinates.

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

Package validation verifies the generated `.nupkg`/`.snupkg`, package and assembly identity, exact dependency groups, README/license/icon/repository metadata, XML documentation, portable symbols, and a fresh package-only consumer rather than relying only on project references. The package-only consumer now exercises the 1.1 semantic values and metadata operations as well as the stable screen/window/input/Unicode/pad surface.

The release workflow derives displayed `Icod.Terminal` and `Icod.TermInfo` dependency versions directly from the project `PackageReference` values so GitHub Release notes cannot silently drift from package metadata.

## Release documentation

Current post-1.0 authorities:

- `Icod.DCurses-Development-Roadmap.md`
- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- `Icod.DCurses-1.1.0-Development-Roadmap.md`
- `docs/T1101-1.1.0-Contract-Reference-and-Version-Policy-Freeze.md`
- `docs/T1102-Semantic-Metadata-Representation-and-Memory-Gate.md`
- `docs/T1103-Hyperlink-Value-and-Public-Logical-Metadata-Contract.md`
- `docs/T1104-Retained-Physical-Hyperlink-Renderer.md`
- `docs/T1105-Editing-Composition-and-Pad-Semantic-Propagation.md`
- `docs/T1106-Semantic-Output-Lifecycle-Failure-and-Recovery-Hardening.md`
- `docs/T1107-Application-Performance-Allocation-and-Optimization-Acceptance.md`
- `docs/T1108-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/Public-API-Fingerprint-1.1.json`
- `docs/Public-API-Baseline-1.1.md`

The published 1.0 closure records remain stable compatibility authorities and are not rewritten merely to reflect later development versions.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients which honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.
