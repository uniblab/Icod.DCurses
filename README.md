# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET.

It sits above `Icod.Terminal` and `Icod.TermInfo`:

- `Icod.TermInfo` owns immutable terminal capability descriptions and expansion;
- `Icod.Terminal` owns the live terminal session, host mode, dimensions, lifecycle, input decoding, semantic terminal protocols, and output serialization;
- `Icod.DCurses` owns curses-shaped events, logical screens/windows, pads/viewports, retained panels/layers, cells/styles/metadata, composition, retained refresh policy, and terminal-cell layout primitives.

## Status

Published stable baseline: `Icod.DCurses 1.2.0`.

Active development in PR #27 is `Icod.DCurses 1.3.0`, focused on immutable terminal-cell geometry, pure deterministic layout allocation, retained panel resizing, and explicit live resize recomputation. T1301-T1309 are qualified; T1310 is the API/package/documentation/licensing regret gate before release-candidate promotion.

Current source identity:

```text
Version         1.3.0-alpha.1
PackageVersion  1.3.0-alpha.1
AssemblyVersion 1.0.0.0
Icod.Terminal   1.9.0
Icod.TermInfo   1.10.0
```

Published 1.2 contract:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Current reviewed 1.3 candidate:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

The four new exported types are `CursesRectangle`, `CursesInsets`, `CursesDockEdge`, and `CursesLayout`.

## Installation

Install the current published package selected by your normal NuGet policy:

```text
dotnet add package Icod.DCurses
```

This README describes the 1.3 source candidate in PR #27. Merge, tag, GitHub Release creation, and NuGet publication are separate explicit release actions; the current `1.3.0-alpha.1` source identity is not a statement that a corresponding package has been published.

## Architecture

```text
applications / future widgets / compatibility facades
                         |
                    Icod.DCurses
 windows / pads / panels / cells / semantic metadata
 immutable geometry / pure layout / retained refresh / events
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

Version 1.3 also does not add a retained layout tree or automatic layout owner. Geometry remains caller-owned data and layout recomputation remains explicit application policy.

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

## 1.3 geometry, layout, and resize

`CursesRectangle` and `CursesInsets` are immutable terminal-cell value types. Empty rectangles are valid geometry results; applying bounds to a window or panel still requires positive dimensions.

`CursesLayout` is a stateless utility for fixed splits, proportional splits, docking, and clipping:

```csharp
CursesRectangle bounds = session.Screen.Bounds;

CursesLayout.Dock(
    bounds,
    CursesDockEdge.Top,
    2,
    out CursesRectangle headerBounds,
    out CursesRectangle remaining
);
CursesLayout.SplitColumnsProportional(
    remaining,
    1,
    3,
    out CursesRectangle sidebarBounds,
    out CursesRectangle bodyBounds
);
```

Geometry is applied explicitly to ordinary windows and retained panels:

```csharp
header.SetBounds( headerBounds );
sidebar.SetBounds( sidebarBounds );
body.SetBounds( bodyBounds );
dialog.SetBounds(
    bodyBounds.Inset(
        new CursesInsets( 2, 4, 2, 4 )
    )
);
```

`CursesPanel.Resize` and `SetBounds` preserve surviving upper-left retained content and semantic metadata, clamp the retained cursor after shrink, repair width-two footprints at resize boundaries, preserve visibility/z-order/transparency, and validate the final screen-relative rectangle before mutation.

For live resize, DCurses deliberately does not retain layout rules. The application recomputes from the synchronized logical screen:

```csharp
CursesRectangle current = session.Screen.Bounds;
// derive rectangles from current
// apply them with SetBounds / Resize
await session.RefreshAsync();
```

The `Icod.DCurses.Layout.Sample` project demonstrates this explicit lifecycle-driven recomputation model with ordinary windows plus a retained panel.

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

The published 1.2 panel contract includes independent retained content, show/hide with remembered z-order, movement and relative ordering, clipping, opaque/blank-transparent composition, Unicode width-two and semantic-metadata coherence, damage-bounded recomposition, live session refresh/lifecycle integration, and deterministic one-way `Dispose()` removal.

Disposal removes a transient panel from its owning screen so repeatedly-created popups/dialogs are not retained for the screen lifetime. A disposed panel cannot be reattached or manipulated.

Version 1.3 extends this published model with retained resizing; it does not replace panel ownership or composition semantics.

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

## Pads, Unicode, and semantic drawing

`CursesPad` is an off-screen logical surface that reuses ordinary `CursesWindow` editing semantics. Multiple viewports may observe one pad independently.

The built-in width provider is pinned to Unicode 17.0.0. East Asian Ambiguous characters are narrow by default and can be made wide explicitly with `UnicodeCursesTextWidthProvider.WideAmbiguousInstance`.

`CursesText.MeasureColumns`, `TruncateToColumns`, and `SliceByColumns` operate on complete terminal text elements and never return half of a two-column element. Semantic line cells remain distinct from ordinary Unicode box-drawing text.

## Concurrency and lifecycle

The library deliberately uses a narrow ownership model rather than pervasive per-cell locking:

- logical screens, windows, pads, viewports, and panels are single-writer unless documented otherwise;
- one Terminal-owned event wait may coexist with serialized refresh/output work;
- caller cancellation does not discard Terminal decoder state;
- disposal unblocks pending DCurses waits while preserving authoritative restoration;
- output uncertainty invalidates retained physical knowledge so a later refresh can repaint safely;
- suspend/resume invalidates physical knowledge but retains logical panel content;
- lifecycle resize synchronizes the logical screen, while application layout recomputation remains explicit.

## Validation and packaging

Local wrappers use Debug configuration. Pull requests use Staging with warnings-as-errors. Pushes to `main` and release tags use Release.

Runtime validation covers Windows/Linux/macOS x64 and ARM64; the library/test matrix covers `net8.0`, `net9.0`, and `net10.0`.

Package validation verifies `.nupkg`/`.snupkg`, package/assembly identity, dependency groups derived from project declarations, README/license/icon/repository metadata, XML documentation, portable symbols, and a fresh NuGet-only consumer. T1310 extends that consumer to compile and execute the 1.3 rectangle/inset/layout/bounds/panel-resize surface directly from the packed artifact. Package validation does not impose hard-coded sibling dependency versions.

## Release documentation

Current authorities:

- `Icod.DCurses-Development-Roadmap.md`
- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- `Icod.DCurses-1.3.0-Development-Roadmap.md`
- `docs/Public-API-Fingerprint-1.3.json`
- `docs/Public-API-Baseline-1.3.md`
- `docs/T1309-Layout-Application-Performance-and-Allocation-Acceptance.md`
- `docs/T1310-Public-API-Package-Documentation-and-Regret-Gate.md`

Historical 1.0-1.2 closure records remain compatibility authorities and are not rewritten merely to reflect later development state.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients which honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.
