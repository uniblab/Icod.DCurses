# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET.

It sits above `Icod.Terminal` and `Icod.TermInfo`:

- `Icod.TermInfo` owns immutable terminal capability descriptions and expansion;
- `Icod.Terminal` owns the live terminal session, host mode, dimensions, lifecycle, input decoding, semantic terminal protocols, physical pointer protocol/state, and output serialization;
- `Icod.DCurses` owns curses-shaped events, logical screens/windows, pads/viewports, retained panels/layers, cells/styles/metadata, composition, retained refresh policy, terminal-cell layout primitives, interaction regions, logical focus, gesture/command routing, and pointer-shape preferences.

## Status

Current published stable release: `Icod.DCurses 1.3.0`.

`Icod.DCurses 1.4.0` is complete in stable-source form in PR #29 and is undergoing the final T1412 exact-head package/runtime qualification before merge. The implementation and frozen public API passed T1411 and the final `1.4.0-rc.1` matrix; stable-source promotion changes release identity and release-facing documentation only.

Current source identity:

```text
Version         1.4.0
PackageVersion  1.4.0
AssemblyVersion 1.0.0.0
Icod.Terminal   1.13.0
Icod.TermInfo   1.12.0
```

Published 1.3 contract:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

Frozen 1.4 interaction contract:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

Version 1.4 adds the interaction-routing surface over the published 1.3 geometry foundation: bounded interaction regions, panel-aware hit testing, logical focus/traversal/repair, semantic key gestures and command identities, structured routing results, pointer-shape preferences, and a DCurses pointer-shape lease wrapper over Terminal-owned state.

## Installation

Install the current published package selected by your normal NuGet policy:

```text
dotnet add package Icod.DCurses
```

Normal stable package resolution currently selects the published 1.3 line until 1.4 has been merged, release-qualified on `main`, tagged, and published.

## Architecture

```text
applications / future widgets / compatibility facades
                         |
                    Icod.DCurses
 windows / pads / panels / cells / semantic metadata
 immutable geometry / pure layout / retained refresh / events
 interaction regions / logical focus / gesture-command routing
              pointer-shape preferences
                         |
                    Icod.Terminal
   live session / input / lifecycle / semantic protocols
 physical pointer state / capability routing / serialized output
                         |
                    Icod.TermInfo
             immutable capability authority
                         |
                  terminal / tty
```

`Icod.DCurses` does not maintain a second terminal capability database, install a competing raw-input loop, own terminal modes independently of `Icod.Terminal`, emit private OSC/CSI/DCS/APC framing for Terminal-owned protocols, emulate a terminal, or create/manage PTYs.

Version 1.4 also does not add a widget framework, hidden event loop, callback dispatcher, retained layout tree, automatic layout owner, automatic mouse-to-focus policy, or independent pointer-protocol owner. Applications remain responsible for their event loop and command execution. Terminal remains authoritative for physical terminal state and reversible protocol leases.

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

## 1.4 interaction routing

Version 1.4 adds deterministic interaction routing without taking ownership of the application event loop. Applications register bounded logical regions, decide which regions may receive logical focus, bind semantic key gestures to command identities, and route already-normalized `CursesInputEvent` values returned by the ordinary session reader.

A compact application pattern is:

```csharp
using CursesInteractionRouter router = new( session.Screen );
using CursesInteractionRegion body = router.RegisterRegion(
    new CursesInteractionRegionOptions(
        new CursesRectangle( 1, 0, 20, 80 )
    ) {
        IsFocusable = true,
        TraversalOrder = 0,
        PointerShape = CursesPointerShape.Text
    }
);

CursesCommand focusNext = new( "focus.next" );
router.BindGlobalGesture(
    CursesKeyGesture.ForKey( CursesKey.Tab ),
    focusNext
);
_ = router.Focus( body );

CursesEvent current = await session.ReadEventAsync();
if ( CursesEventKind.Input == current.Kind
    && current.Input is not null ) {
    CursesInteractionResult routed = router.Route( current.Input );

    if ( routed.Command is not null
        && "focus.next" == routed.Command.Name ) {
        _ = router.MoveFocus( CursesFocusDirection.Forward );
    }

    // routed.Hit contains region-local mouse coordinates and the
    // region's pointer-shape preference, if one was configured.
}
```

The router deliberately returns structured routing data instead of invoking callbacks. Focus changes are explicit application decisions. Mouse hit testing therefore does **not** automatically change logical focus, and logical focus is independent of terminal/window-manager focus reports.

Panel-associated regions participate in the retained panel stack, so current panel z-order is part of mouse hit-test precedence. Successful mouse hits carry region-local row/column coordinates in addition to the original normalized input. A region's `PointerShape` is only a semantic preference surfaced by hit/routing results; applying that preference requires the application to explicitly acquire and retain a `CursesPointerShapeLease` from the session for as long as the physical preference should remain active:

```csharp
CursesPointerShapeLease pointerLease =
    await session.AcquirePointerShapeAsync( CursesPointerShape.Text );

// Keep pointerLease while the preference applies, then restore prior state.
await pointerLease.DisposeAsync();
```

Region and gesture-binding registries are intentionally bounded and fail before partial mutation when capacity is exhausted. The router owns no background work, event loop, terminal parser, protocol negotiation, or hidden terminal I/O. It routes only the semantic input and geometry state already owned by DCurses/Terminal.

The `Icod.DCurses.Interaction.Sample` project demonstrates focused local-versus-global bindings, forward/backward traversal, retained popup overlap, panel-aware mouse precedence, screen and region-local coordinates, explicit pointer leases, resize/re-layout, and the distinction between terminal focus reports and logical interaction focus.

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

- logical screens, windows, pads, viewports, panels, and interaction routers are single-writer unless documented otherwise;
- one Terminal-owned event wait may coexist with serialized refresh/output work;
- caller cancellation does not discard Terminal decoder state;
- disposal unblocks pending DCurses waits while preserving authoritative restoration;
- output uncertainty invalidates retained physical knowledge so a later refresh can repaint safely;
- suspend/resume invalidates physical knowledge but retains logical panel and interaction registration state;
- lifecycle resize synchronizes the logical screen, while application layout recomputation remains explicit;
- interaction routing never creates a competing input reader or terminal-output path.

## Validation and packaging

Local wrappers use Debug configuration. Pull requests use Staging with warnings-as-errors. Pushes to `main` and release tags use Release.

Runtime validation covers Windows/Linux/macOS x64 and ARM64; the library/test matrix covers `net8.0`, `net9.0`, and `net10.0`.

Package validation verifies `.nupkg`/`.snupkg`, package/assembly identity, dependency groups derived from project declarations, README/license/icon/repository metadata, XML documentation, portable symbols, and a fresh NuGet-only consumer. The package consumer compiles and executes both the 1.3 geometry/layout/panel-resize surface and the 1.4 interaction surface directly from the packed artifact: regions, logical focus/traversal, hit testing, semantic gestures and command bindings, routing API presence, pointer-shape vocabulary, and pointer-lease surface. Package validation does not impose hard-coded sibling dependency versions.

## Release documentation

Current authorities:

- `Icod.DCurses-Development-Roadmap.md`
- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- `Icod.DCurses-1.4.0-Development-Roadmap.md`
- `docs/Public-API-Fingerprint-1.4.json`
- `docs/T1401-Interaction-Contract-and-Public-API-Candidate.md`
- `docs/T1409-Interaction-Acceptance-Sample.md`
- `docs/T1410-Interaction-Performance-Allocation-and-Adversarial-Hardening.md`
- `docs/T1411-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/T1412-RC-and-Stable-1.4.0-Closure.md`

T1411 is complete on head `570715e0764f9791fe197462a953df6eccf6105a`, qualified by workflow #797 / `34773668892` across all seven jobs. The final RC head `7f6bcedf70b9cd5cd15bf2a2a53437e23dac3c2f` passed workflow #802 / `34774226736` across all seven jobs after a test-only timeout hardening correction; production code and the frozen public API were unchanged. T1412 is now qualifying stable-source `1.4.0`. Historical 1.0-1.3 closure records remain compatibility authorities and are not rewritten merely to reflect later development state.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients which honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.
