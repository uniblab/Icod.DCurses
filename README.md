# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET.

It sits above `Icod.Terminal` and `Icod.TermInfo`:

- `Icod.TermInfo` owns immutable terminal capability descriptions and expansion;
- `Icod.Terminal` owns the live terminal session, host mode, dimensions, lifecycle, input decoding, semantic terminal protocols, and output serialization;
- `Icod.DCurses` owns curses-shaped events, logical screens/windows, pads/viewports, retained panels/layers, cells/styles/metadata, composition, and retained refresh policy.

## Status

The current development source is `Icod.DCurses 1.2.0-alpha.1` in PR #26. Version 1.2 adds independent retained panels/layers while preserving the ordinary `CursesWindow` shared-view contract established by 1.0 and extended additively by 1.1.

Current source identity:

```text
Version         1.2.0-alpha.1
PackageVersion  1.2.0-alpha.1
AssemblyVersion 1.0.0.0
Icod.Terminal   1.8.1
Icod.TermInfo   1.10.0
```

Accepted 1.1 compatibility floor:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

Current 1.2 candidate contract:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

The two new exported types are `CursesPanel` and `CursesPanelTransparency`.

## Installation

Install the current package selected by your normal NuGet policy:

```text
dotnet add package Icod.DCurses
```

The `1.2.0-alpha.1` identity described here is the source/development identity of PR #26. Merging, tagging, GitHub Release creation, and NuGet publication are separate explicit release actions.

## Architecture

```text
applications / future widgets / compatibility facades
                         |
                    Icod.DCurses
 windows / pads / panels / cells / semantic metadata
     logical composition / retained refresh / events
                         |
                    Icod.Terminal
   live session / input / lifecycle / semantic protocols
          capability routing / serialized output
                         |
                    Icod.TermInfo
             immutable capability authority
                         |
                  terminal / tty
```

`Icod.DCurses` does not maintain a second terminal capability database, install a competing raw-input loop, own terminal modes independently of `Icod.Terminal`, emit private OSC/CSI/DCS/APC framing for Terminal-owned protocols, emulate a terminal, or create/manage PTYs.

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

## 1.2 retained panels and layers

Ordinary `CursesWindow` instances remain shared logical views. `CursesPanel` is intentionally different: it owns an independent retained surface and participates in a deterministic screen-owned z-order stack.

```csharp
using CursesPanel dialog = session.Screen.CreatePanel(
    row: 3,
    column: 6,
    rows: 8,
    columns: 36
);

dialog.ContentWindow.Write( "Retained dialog content" );
dialog.MoveToTop();
await session.RefreshAsync();
```

Panels are opaque by default. A panel can instead make ordinary blank cells transparent:

```csharp
using CursesPanel overlay = session.Screen.CreatePanel(
    row: 2,
    column: 4,
    rows: 3,
    columns: 20
);
overlay.Transparency = CursesPanelTransparency.BlankCellsTransparent;
overlay.ContentWindow.Write( "overlay" );
```

The 1.2 panel contract includes:

- independent retained content edited through ordinary `CursesWindow` APIs;
- `Show()` / `Hide()` with remembered z-order;
- `MoveTo(...)`, `MoveToTop()`, `MoveToBottom()`, `MoveAbove(...)`, and `MoveBelow(...)`;
- deterministic bottom-to-top logical composition;
- screen-edge clipping without mutating panel content;
- opaque and blank-transparent composition;
- Unicode width-two footprint repair and semantic-metadata coherence;
- damage-bounded incremental recomposition;
- live `CursesSession.RefreshAsync()` integration across resize and suspend/resume;
- deterministic one-way `Dispose()` removal for transient panels.

Disposal removes the panel from its owning screen so repeatedly-created transient popups/dialogs are not retained for the screen lifetime. A disposed panel cannot be reattached or manipulated.

Panel size remains fixed in 1.2. General layout and resize primitives belong to the planned 1.3 release rather than being prematurely frozen into the panel contract.

## 1.1 semantic metadata and hyperlinks

Version 1.1 added semantic meaning attached to retained content, beginning with hyperlinks, while keeping visual rendition in `CursesStyle`.

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

Metadata is retained independently of visible glyph/style equality, follows content through supported editing/composition operations, remains coherent across two-column leader/continuation footprints, and is emitted physically through Terminal-owned semantic hyperlink operations. DCurses does not construct OSC 8 directly.

See the T1102–T1109 records under `docs/` for the detailed 1.1 representation, lifecycle, performance, API-regret, and stable-closure evidence.

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

## Unicode and semantic drawing

The built-in width provider is pinned to Unicode 17.0.0. East Asian Ambiguous characters are narrow by default and can be made wide explicitly with `UnicodeCursesTextWidthProvider.WideAmbiguousInstance`.

`CursesText.MeasureColumns`, `TruncateToColumns`, and `SliceByColumns` operate on complete terminal text elements and never return half of a two-column element. Semantic line cells remain distinct from ordinary Unicode box-drawing text.

## Concurrency and lifecycle

The library deliberately uses a narrow ownership model rather than pervasive per-cell locking:

- logical screens, windows, pads, viewports, and panels are single-writer unless documented otherwise;
- one Terminal-owned event wait may coexist with serialized refresh/output work;
- caller cancellation does not discard Terminal decoder state;
- disposal unblocks pending DCurses waits while preserving authoritative restoration;
- output uncertainty invalidates retained physical knowledge so a later refresh can repaint safely;
- suspend/resume invalidates physical knowledge but retains logical panel content.

## Validation and packaging

Local wrappers use Debug configuration. Pull requests use Staging with warnings-as-errors. Pushes to `main` and release tags use Release.

Runtime validation covers Windows/Linux/macOS x64 and ARM64; the library/test matrix covers `net8.0`, `net9.0`, and `net10.0`.

Package validation verifies the generated `.nupkg`/`.snupkg`, package and assembly identity, dependency groups derived from project declarations, README/license/icon/repository metadata, XML documentation, portable symbols, and a fresh NuGet-only consumer. Package validation does not impose hard-coded sibling dependency versions; restore/build/test establish compatibility.

## Release documentation

Current post-1.0 authorities:

- `Icod.DCurses-Development-Roadmap.md`
- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- `Icod.DCurses-1.2.0-Development-Roadmap.md`
- `docs/T1208-Panel-Application-Performance-and-Allocation-Acceptance.md`
- `docs/T1209-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/Public-API-Fingerprint-1.2.json`
- `docs/Public-API-Baseline-1.2.md`

Historical 1.0 and 1.1 closure records remain compatibility authorities and are not rewritten merely to reflect later development state.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients which honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.
