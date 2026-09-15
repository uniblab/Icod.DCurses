# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET. It sits above `Icod.Terminal` and `Icod.TermInfo`, providing logical screens, windows, pads, retained panels, Unicode-aware cells, layout, composition, refresh/damage policy, and deterministic interaction routing without taking ownership of the application's event loop or application policy.

## Status

Current release line: `Icod.DCurses 1.5.0`.

Version `1.5.0` adds advanced deterministic interaction control over the published 1.4 router: bounded interaction scopes, explicit singular pointer capture, deterministic spatial logical focus, clock-free pointer gesture normalization, and scope-level command bindings with region-to-scope-to-global precedence.

The 1.5 release contract passed the complete Staging qualification matrix across package candidate plus Windows, Linux, and macOS x64/ARM64 execution. Package-only consumers compile and execute the additive 1.5 surface on `net8.0`, `net9.0`, and `net10.0`.

The frozen 1.5 public contract is:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

## Support the Project

`Icod.DCurses` and its ecosystem packages (`Icod.Terminal` and `Icod.TermInfo`) are built and maintained by a solo developer. If these packages save you or your team time, please consider supporting their continued development and maintenance.

[![GitHub Sponsors](https://img.shields.io/badge/GitHub-Sponsor?logo=githubsponsors)](https://github.com/sponsors/uniblab)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support?logo=kofi)](https://ko-fi.com/TimothyBruce)
[![PayPal](https://img.shields.io/badge/PayPal-Support?logo=paypal)](https://paypal.me/uniblab)

## Architecture

`Icod.DCurses` is the presentation and interaction layer of the Icod terminal stack:

```text
higher-level terminal applications / future widgets
                         |
                    Icod.DCurses
 windows / pads / panels / cells / semantic metadata
 geometry / layout / composition / retained refresh / damage
 interaction regions / scopes / logical + spatial focus
 capture / pointer gestures / gesture-command routing
                         |
                    Icod.Terminal
 live session / input / lifecycle / semantic protocols
 physical terminal state / raster ownership / serialized output
                         |
                    Icod.TermInfo
 immutable terminal capability data and planning
                         |
                  terminal / tty
```

- `Icod.TermInfo` owns immutable terminal capability data, compiled terminfo acquisition, capability expansion, and reusable inspection/planning facilities.
- `Icod.Terminal` owns the live terminal conversation: host modes, dimensions, lifecycle, authoritative input, semantic protocols, physical pointer state, raster ownership, reversible terminal state, and serialized output.
- `Icod.DCurses` owns curses-shaped events, logical screens and windows, pads and viewports, retained panels and layers, cells/styles/semantic metadata, Unicode-aware drawing, geometry/layout, composition, retained refresh/damage policy, and deterministic interaction routing.
- Applications remain responsible for their event loop, command execution, widget/application semantics, layout policy, and higher-level interaction policy.

The direct production dependency graph is:

```text
Icod.DCurses
├── Icod.Terminal 1.15.0
└── Icod.TermInfo 1.14.0
```

The explicit `Icod.TermInfo` reference is intentional because production DCurses code consumes TermInfo APIs directly; it is not present merely because Terminal also depends on TermInfo.

## Quick Start

Install the package:

```text
dotnet add package Icod.DCurses --version 1.5.0
```

Open a curses session, draw into the logical standard screen, refresh it, and read through the authoritative event path:

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

A `CursesSession` restores the presentation and Terminal-owned state it acquires when disposed. Applications should consume terminal input and lifecycle activity through the DCurses/Terminal ownership model rather than introducing a competing byte reader.

## Feature Inventory

The root README describes the current product by capability rather than by the release in which each feature first appeared.

- **Logical screen and windows** — mutable curses-style cell surfaces, cursor movement, clearing, insertion/deletion, scrolling, subwindows, styles, and explicit refresh through one session-owned logical screen.
- **Unicode-aware text** — Unicode 17.0.0 terminal-width behavior, complete terminal text-element measurement/slicing, width-two footprint coherence, configurable East Asian Ambiguous width, and semantic line cells distinct from ordinary box-drawing text.
- **Pads and viewports** — off-screen logical surfaces with ordinary window editing semantics and independently positioned/clipped viewports onto the visible screen.
- **Semantic cell metadata** — retained metadata independent of visible glyph/style equality, including hyperlinks emitted through Terminal-owned semantic operations rather than private OSC construction.
- **Retained panels and layers** — independent retained surfaces, deterministic z-order, visibility, movement, resizing, clipping, opaque or blank-transparent composition, damage-bounded recomposition, and deterministic disposal.
- **Geometry and layout** — immutable rectangles and insets plus stateless fixed/proportional splitting, docking, clipping, and explicit application of computed bounds to windows and panels.
- **Lifecycle and resize coherence** — logical-screen synchronization with terminal resize/lifecycle events while leaving application layout recomputation explicit; suspend/resume invalidates physical knowledge without discarding logical retained state.
- **Retained refresh and damage** — logical-to-physical diffing, bounded repaint, physical-state invalidation after output uncertainty, and safe full repaint when prior terminal contents are no longer trustworthy.
- **Curses-shaped events** — normalized input and lifecycle observations layered over Terminal's authoritative reader without installing a second parser or input loop.
- **Interaction regions** — bounded logical regions with panel-aware hit testing, region-local coordinates, focus eligibility, traversal order, semantic key gestures, commands, and pointer-shape preferences.
- **Interaction scopes** — bounded immutable-parent scope trees with explicit descendant-only LIFO activation, modal routing boundaries, focus restoration/repair, and scope-owned gesture bindings.
- **Logical focus** — explicit focus, deterministic forward/backward traversal, repair after topology changes, and deterministic spatial movement using clipped geometry and integer-only ranking.
- **Pointer capture and gestures** — explicit singular capture, signed region-local targets outside ordinary hit bounds, and clock-free `Press`, `Release`, `Move`, `Click`, `DragStart`, `DragMove`, `DragEnd`, and wheel gesture normalization.
- **Semantic command routing** — callback-free command identity lookup with region-local, scope-chain, and router-global precedence; the router reports mechanism and leaves command execution to the application.
- **Pointer-shape preferences** — semantic region preferences surfaced through routing results and applied only when the application explicitly acquires a Terminal-backed `CursesPointerShapeLease`.

## Interaction Control at a Glance

Interaction routing is deliberately mechanism rather than application policy. A compact scoped interaction pattern is:

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
using CursesPointerCaptureLease capture =
	router.CapturePointer( popup, CursesMouseButton.Primary );

_ = router.MoveFocus( CursesFocusDirection.Right );
```

With a focused region, semantic key-command lookup is:

```text
region local
-> region scope
-> parent scopes through the active modal boundary
-> router global
```

Commands remain `CursesCommand` identities. Routing never invokes application callbacks or silently changes focus merely because a pointer hit occurred.

The public interaction registries are explicitly bounded:

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

## Platforms and Targets

The package targets:

```text
net8.0
net9.0
net10.0
```

The repository uses C# 13. Release qualification covers Windows, Linux, and macOS on x64 and ARM64.

Local wrappers use Debug configuration. Pull requests use Staging with warnings-as-errors. Pushes to `main` and release tags use Release.

## Design Boundaries and Guarantees

- DCurses does not maintain a second terminal capability database; TermInfo remains the terminal-capability authority.
- DCurses does not install a competing raw-input loop, independently own terminal modes, or bypass Terminal's authoritative event/query path.
- DCurses does not emit private OSC/CSI/DCS/APC framing for Terminal-owned semantic protocols or persistent raster facilities.
- Logical screens, windows, pads, viewports, panels, and interaction routers are single-writer unless documented otherwise; one Terminal-owned event wait may coexist with serialized refresh/output work.
- The interaction router owns no background work or hidden event loop. It returns structured routing results rather than invoking callbacks or executing application commands.
- Pointer hit testing does not automatically change logical focus. Terminal/window-manager focus and DCurses logical focus are distinct concepts.
- Pointer capture is explicit and singular. Drag/drop payload semantics, timed multi-click policy, timing thresholds, and application movement policy remain above DCurses.
- Layout primitives are pure/stateless. DCurses does not retain or automatically reapply application layout rules after resize.
- Retained physical knowledge is invalidated when output state becomes uncertain; logical state is preserved so a later refresh can repaint safely.
- Panels, interaction registries, gesture bindings, scope depth, and other potentially growing structures are explicitly bounded.
- Widget frameworks, callback dispatch, retained capture/bubble event trees, automatic focus-on-click, PTY/process hosting, terminal emulation, and application frameworks remain outside the library contract.

## Samples and Documentation

The repository contains focused samples for base session/drawing behavior, layout, retained panels, semantic metadata, and advanced interaction routing. `Icod.DCurses.Interaction.Sample` demonstrates nested scopes, sequential and spatial focus, scoped/global commands, explicit pointer capture, drag gesture results, application-owned popup movement, pointer-shape preferences, resize handling, and the mechanism/policy split using public DCurses APIs only.

Recommended documentation entry points:

- [`CHANGELOG.md`](CHANGELOG.md) — release-by-release feature history;
- [`docs/1.0-Stable-Compatibility-and-Migration-Guide.md`](docs/1.0-Stable-Compatibility-and-Migration-Guide.md) — stable 1.x compatibility floor and migration guidance;
- [`Icod.DCurses-Development-Roadmap.md`](Icod.DCurses-Development-Roadmap.md) — current and longer-range development direction;
- [`Icod.DCurses-1.5.0-Development-Roadmap.md`](Icod.DCurses-1.5.0-Development-Roadmap.md) — 1.5 advanced-interaction development contract;
- [`docs/Public-API-Fingerprint-1.5.json`](docs/Public-API-Fingerprint-1.5.json) — frozen 1.5 public contract and fingerprint;
- [`docs/T150-1.5.0-Architecture-API-Regret-and-Contract-Freeze.md`](docs/T150-1.5.0-Architecture-API-Regret-and-Contract-Freeze.md) — 1.5 architecture/API freeze;
- [`docs/T158-Advanced-Interaction-Regret-and-Qualification-Gate.md`](docs/T158-Advanced-Interaction-Regret-and-Qualification-Gate.md) — final advanced-interaction qualification gate;
- [`docs/T159-RC-and-Stable-1.5.0-Closure.md`](docs/T159-RC-and-Stable-1.5.0-Closure.md) — RC/stable-source closure evidence.

Release audits, public-API fingerprints/baselines, tranche records, implementation plans, and historical roadmaps remain in the repository as engineering evidence. They are intentionally not repeated in this README.

## Compatibility and Versioning

Stable `1.0.0` remains the compatibility floor. `Icod.DCurses 1.5.0` keeps `AssemblyVersion` at `1.0.0.0` and adds public interaction capabilities without silently repurposing established signatures, lifecycle guarantees, root/unscoped routing behavior, or application-policy boundaries.

The package supports `net8.0`, `net9.0`, and `net10.0`. The current direct runtime dependencies are `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0`.

The final 1.5 public API fingerprint is:

```text
8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

Compatibility and migration guidance is maintained in [`docs/1.0-Stable-Compatibility-and-Migration-Guide.md`](docs/1.0-Stable-Compatibility-and-Migration-Guide.md). Release-by-release chronology belongs in [`CHANGELOG.md`](CHANGELOG.md), versioned roadmaps, public-API evidence, tranche records, and release audits rather than accumulating here.

This README is maintained as the current product, NuGet package, and contributor entry point.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

`Icod.DCurses` is licensed under the GNU Lesser General Public License, version 3 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients that honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.