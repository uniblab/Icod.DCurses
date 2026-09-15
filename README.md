# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET.

It sits above `Icod.Terminal` and `Icod.TermInfo`:

- `Icod.TermInfo` owns immutable terminal capability descriptions and expansion;
- `Icod.Terminal` owns the live terminal session, host mode, dimensions, lifecycle, input decoding, semantic terminal protocols, physical pointer protocol/state, and output serialization;
- `Icod.DCurses` owns curses-shaped events, logical screens/windows, pads/viewports, retained panels/layers, cells/styles/metadata, composition, retained refresh policy, terminal-cell layout primitives, interaction regions/scopes, logical focus, pointer capture, pointer-gesture/command routing, and pointer-shape preferences.

## Status

Stable-source release: `Icod.DCurses 1.5.0`.

`Icod.DCurses 1.5.0` is complete in stable-source form in PR #30. The unchanged RC head `23113b130d674da315ccbbcd384a60a0e6b47baa` passed workflow #899 / `34993884108` across the complete seven-job Staging matrix. Stable-source head `af30a6c2df842d76b8cb9e9b7855aeb3a16e9ff9` passed workflow #905 / `34994761777` across package candidate plus Windows/Linux/macOS x64/ARM64. Publication preparation subsequently refreshed the final direct dependencies to `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0`; dependency-refresh head `bc583d5ded07c0784bab27fe389f4bdab690394e` passed workflow #915 / `35003069419` across the same seven-job matrix. Merge, tagging, and publication remain separate maintainer actions.

Current source/package identity:

```text
Version         1.5.0
PackageVersion  1.5.0
AssemblyVersion 1.0.0.0
Icod.Terminal   1.15.0
Icod.TermInfo   1.14.0
```

Published 1.4 contract:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

Frozen 1.5 contract:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

Version 1.4 established deterministic interaction routing: bounded interaction regions, panel-aware hit testing, logical focus/traversal/repair, semantic key gestures and command identities, structured routing results, pointer-shape preferences, and a DCurses pointer-shape lease wrapper over Terminal-owned state.

Version 1.5 extends that mechanism with bounded interaction scopes, explicit singular pointer capture, deterministic spatial logical focus, clock-free pointer gesture normalization, and scope-level command bindings. It does not add widgets, callbacks, automatic focus-on-click, drag/drop policy, a retained event tree, or a hidden application event loop.

## Support the Project

`Icod.Terminal` and its ecosystem packages (`Icod.TermInfo` and `Icod.DCurses`) are built and maintained by a solo developer. If these packages save you or your team time, please consider supporting their continued development and maintenance.

[![GitHub Sponsors](https://img.shields.io/badge/GitHub-Sponsor?logo=githubsponsors)](https://github.com/sponsors/uniblab)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support?logo=kofi)](https://ko-fi.com/TimothyBruce)
[![PayPal](https://img.shields.io/badge/PayPal-Support?logo=paypal)](https://paypal.me/uniblab)

## Installation

Install the latest published stable package selected by your normal NuGet policy:

```text
dotnet add package Icod.DCurses
```

To install this release explicitly:

```text
dotnet add package Icod.DCurses --version 1.5.0
```

Source qualification and package publication are separate operations. The NuGet package page and GitHub Releases page are authoritative for distribution availability.

## Architecture

```text
applications / future widgets / compatibility facades
                         |
                    Icod.DCurses
 windows / pads / panels / cells / semantic metadata
 immutable geometry / pure layout / retained refresh / events
 interaction regions / scopes / logical + spatial focus
 capture / pointer gestures / gesture-command routing
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

Versions 1.4 and 1.5 also do not add a widget framework, hidden event loop, callback dispatcher, retained event tree, automatic layout owner, automatic mouse-to-focus policy, drag/drop framework, or independent pointer-protocol owner. Applications remain responsible for their event loop and command execution. Terminal remains authoritative for physical terminal state and reversible protocol leases.

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

## 1.5 advanced interaction control

Version 1.5 extends the 1.4 router with additional bounded mechanisms while preserving the existing root/unscoped behavior for applications that do not opt in.

Explicit scopes establish modal interaction boundaries without introducing widgets:

```csharp
using CursesInteractionRouter router = new( session.Screen );
using CursesInteractionScope popupScope = router.RegisterScope();
using CursesInteractionRegion popup = router.RegisterRegion(
    new CursesInteractionRegionOptions(
        new CursesRectangle( 3, 8, 8, 32 )
    ) {
        Scope = popupScope,
        IsFocusable = true
    }
);
using CursesInteractionScopeLease active = router.ActivateScope( popupScope );
```

While an explicit scope is active, hit testing and logical focus are restricted to its subtree. Nested activation is descendant-only and LIFO. Ending a scope activation restores saved logical focus when it is eligible again; otherwise ordinary deterministic focus repair applies.

Logical focus now supports deterministic spatial movement as well as the published forward/backward traversal:

```csharp
_ = router.MoveFocus( CursesFocusDirection.Right );
_ = router.MoveFocus( CursesFocusDirection.Down );
```

Spatial ranking uses clipped terminal-cell rectangles and integer-only ordering. It does not wrap and does not manufacture initial focus when no region is currently focused.

Pointer capture is explicit and singular:

```csharp
using CursesPointerCaptureLease capture =
    router.CapturePointer( popup, CursesMouseButton.Primary );
```

Captured pointer motion continues to target the captured region outside its current bounds. `CursesInteractionResult.PointerTarget` then exposes signed region-local row/column coordinates plus `IsInside` rather than weakening the ordinary in-bounds `CursesInteractionHit` contract.

Mouse routing also exposes clock-free `CursesPointerGesture` snapshots for `Press`, `Release`, `Move`, `Click`, `DragStart`, `DragMove`, `DragEnd`, and four wheel directions. Click/drag classification is based on normalized cell movement rather than timing; double/triple-click policy, timing thresholds, and drag/drop payload semantics remain application concerns.

Explicit scopes may own semantic key-command bindings. With a focused region, lookup is:

```text
region local
-> region scope
-> parent scopes through the active modal boundary
-> router global
```

Commands remain `CursesCommand` identities. The router does not invoke callbacks or execute application behavior.

The complete interaction registries remain bounded:

```text
MaximumRegions                  4096
MaximumScopes                    256
MaximumScopeDepth                 32
MaximumRegionGestureBindings     256
MaximumScopeGestureBindings      256
MaximumGlobalGestureBindings    1024
MaximumGestureBindings         16384 total
Active pointer captures            1
```

The `Icod.DCurses.Interaction.Sample` demonstrates nested scopes, sequential/spatial focus, scoped/global commands, explicit capture, drag gesture results, application-owned popup movement, pointer-shape preferences, resize handling, and the mechanism/policy split using public DCurses APIs only.

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

Package validation verifies `.nupkg`/`.snupkg`, package/assembly identity, dependency groups derived from project declarations, README/license/icon/repository metadata, XML documentation, portable symbols, and a fresh NuGet-only consumer. The package consumer compiles and executes the complete additive 1.5 interaction surface directly from the packed artifact on net8/net9/net10, including scopes, spatial focus, scoped commands, explicit capture lifetime, and public pointer target/gesture/result contracts. Package validation does not impose hard-coded sibling dependency versions.

## Release documentation

Current authorities:

- `Icod.DCurses-Development-Roadmap.md`
- `Icod.DCurses-1.5.0-Development-Roadmap.md`
- `docs/Public-API-Fingerprint-1.5.json`
- `docs/T150-1.5.0-Architecture-API-Regret-and-Contract-Freeze.md`
- `docs/T151-Bounded-Interaction-Scopes.md`
- `docs/T152-Explicit-Pointer-Capture.md`
- `docs/T153-Deterministic-Spatial-Focus.md`
- `docs/T154-Deterministic-Pointer-Gesture-Normalization.md`
- `docs/T155-Scoped-Command-Bindings-and-Precedence.md`
- `docs/T156-Coherence-and-Adversarial-Hardening.md`
- `docs/T157-Application-Acceptance-and-Package-Consumer.md`
- `docs/T158-Advanced-Interaction-Regret-and-Qualification-Gate.md`
- `docs/T159-RC-and-Stable-1.5.0-Closure.md`

Published 1.4 compatibility/release authorities remain historical evidence and are not rewritten to simulate current 1.5 development state.

T158 final evidence head `218f900aaf689029f8f5de26906f86724d029ebc` passed #893 / `34915390381`. RC head `23113b130d674da315ccbbcd384a60a0e6b47baa` passed #899 / `34993884108`, all seven jobs. Stable-source head `af30a6c2df842d76b8cb9e9b7855aeb3a16e9ff9` passed #905 / `34994761777`, all seven jobs. Final dependency-refresh head `bc583d5ded07c0784bab27fe389f4bdab690394e` passed #915 / `35003069419`, all seven jobs; its generated `.nupkg` directly declares `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0` for net8/net9/net10. Production source and the frozen public API remained unchanged through RC/stable-source promotion and dependency refresh.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

Licensed under the GNU Lesser General Public License v3.0 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients which honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.
