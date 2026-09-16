# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET. It provides retained logical screens, windows, pads, panels, Unicode-aware text, semantic metadata, retained mixed-media presentation, geometry/layout, damage-aware refresh, and deterministic interaction routing without taking ownership of the application's event loop or application policy.

## Status

Published stable release: `Icod.DCurses 1.5.0`.

Current development line: `Icod.DCurses 1.6.0`; package candidate `1.6.0-alpha.2`.

Version 1.6 adds **retained mixed-media presentation** over the published `Icod.Terminal 1.15.0` persistent-raster / Unicode-placeholder ownership model. Raster placeholder cells participate in ordinary DCurses windows, pads, viewports, panels, clipping, scrolling, composition, damage, and refresh while Terminal remains the sole owner of live raster protocol identity, acknowledgement, encoding, and lifecycle certainty.

The frozen 1.6 candidate public contract is:

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

The retained mixed-media implementation has completed architecture/API freeze, logical propagation, panel composition, physical refresh integration, lifecycle hardening, application/package acceptance, and adversarial/allocation hardening. T1610 is the final public-API/package/documentation/dependency/licensing regret gate before RC promotion.

## Support the Project

`Icod.DCurses` and its ecosystem packages (`Icod.Terminal` and `Icod.TermInfo`) are built and maintained by a solo developer. If these packages save you or your team time, please consider supporting their continued development and maintenance.

[![GitHub Sponsors](https://img.shields.io/badge/GitHub-Sponsor?logo=githubsponsors)](https://github.com/sponsors/uniblab)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support?logo=kofi)](https://ko-fi.com/TimothyBruce)
[![PayPal](https://img.shields.io/badge/PayPal-Support?logo=paypal)](https://paypal.me/uniblab)

## Architecture

`Icod.DCurses` is the retained presentation and interaction layer of the Icod terminal stack:

```text
higher-level terminal applications / future widgets
                         |
                    Icod.DCurses
 retained text + raster presentation
 windows / pads / viewports / panels / cells / metadata
 geometry / layout / composition / clipping / damage / refresh
 interaction regions / scopes / focus / capture / gestures / commands
                         |
                    Icod.Terminal
 live session / input / lifecycle / semantic protocols
 physical terminal state / persistent raster ownership / serialized output
                         |
                    Icod.TermInfo
 immutable terminal capability data and planning
                         |
                  terminal / tty
```

- `Icod.TermInfo` owns immutable terminal capability data, compiled terminfo acquisition, capability expansion, and reusable inspection/planning facilities.
- `Icod.Terminal` owns the live terminal conversation: host modes, dimensions, lifecycle, authoritative input, semantic protocols, physical terminal state, persistent raster ownership, reversible state, and serialized output.
- `Icod.DCurses` owns curses-shaped events, logical screens and windows, pads/viewports, retained panels, text/style/semantic metadata, retained raster coordinates, Unicode-aware drawing, geometry/layout, composition, clipping, scrolling, damage/refresh, and deterministic interaction mechanisms.
- Applications remain responsible for their event loop, command execution, source-image durability, widget/application semantics, layout policy, navigation, and higher-level interaction policy.

The direct 1.6 production dependency graph remains:

```text
Icod.DCurses
├── Icod.Terminal 1.15.0
└── Icod.TermInfo 1.14.0
```

The broader question of whether DCurses should continue to reference TermInfo directly is intentionally deferred to the **1.7 development track**. Version 1.6 does not change the accepted dependency architecture during release closure.

## Quick Start

Install the current 1.6 development package:

```text
dotnet add package Icod.DCurses --version 1.6.0-alpha.2
```

Open a curses session, draw retained text, and refresh through the authoritative session path:

```csharp
using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

screen.Clear();
screen.Move( 0, 0 );
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

For retained raster content, applications supply a backend-neutral `TerminalRasterImage`, create the live resource/placeholder through `CursesSession`, and retain opaque placeholder cells in ordinary DCurses surfaces:

```csharp
using Icod.DCurses;
using Icod.Terminal;

TerminalRasterImage image = TerminalRasterImage.CreateRgb24(
	1,
	1,
	[ 0x20, 0x40, 0x80 ]
);

TerminalControlResult<CursesRasterResource> resourceResult =
	await session.CreateRasterResourceAsync( image );

if ( resourceResult.IsSuccess ) {
	await using CursesRasterResource resource = resourceResult.Value;
	TerminalControlResult<CursesRasterPlaceholder> placeholderResult =
		await resource.CreatePlaceholderAsync( 1, 1 );

	if ( placeholderResult.IsSuccess ) {
		await using CursesRasterPlaceholder placeholder = placeholderResult.Value;
		screen.Move( 2, 4 );
		screen.WriteRasterCell( placeholder.GetCell( 0, 0 ) );
		await session.RefreshAsync();
	}
}
```

DCurses does not expose Terminal-private image/placement/placeholder ids, does not build Kitty/Sixel command bytes, and does not retain a hidden source-image cache for automatic replay.

## Feature Inventory

- **Logical screen and windows** — mutable retained cell surfaces, cursor movement, clearing, insertion/deletion, scrolling, subwindows, styles, and explicit refresh through one session-owned logical screen.
- **Unicode-aware text** — Unicode terminal-width behavior, complete text-element measurement/slicing, width-two footprint coherence, configurable East Asian Ambiguous width, and semantic line cells distinct from ordinary box-drawing text.
- **Pads and viewports** — off-screen logical surfaces with ordinary window editing semantics and independently positioned/clipped viewports onto the visible screen.
- **Semantic cell metadata** — retained terminal-independent metadata such as hyperlinks, emitted through Terminal-owned semantic operations rather than private OSC construction.
- **Retained mixed media** — lazy row-sparse raster state beside the ordinary cell and semantic metadata planes; opaque placeholder cells move through editing, copy/overlay, scrolling, pads/viewports, panels, clipping, resize, and damage refresh without exposing protocol-private identity.
- **Retained panels and layers** — independent retained surfaces, deterministic z-order, visibility, movement, resizing, clipping, opaque or blank-transparent composition, damage-bounded recomposition, and deterministic disposal. Blank+raster coordinates remain visually present under blank-cell transparency.
- **Geometry and layout** — immutable rectangles and insets plus stateless fixed/proportional splitting, docking, clipping, and explicit application of computed bounds to windows and panels.
- **Lifecycle and resize coherence** — logical-screen synchronization with terminal resize/lifecycle events; stale/released/disposed raster ownership is rejected before output while logical retained intent may remain for caller policy.
- **Retained refresh and damage** — logical-to-physical diffing, sparse repaint, synchronized output, conservative rendition repair around raster placeholder cells, physical-state invalidation after output uncertainty, and safe full repaint when prior terminal contents are no longer trustworthy.
- **Curses-shaped events** — normalized input and lifecycle observations layered over Terminal's authoritative reader without installing a second parser or input loop.
- **Interaction regions and scopes** — bounded region routing, panel-aware hit testing, immutable-parent scopes, modal activation, deterministic focus/traversal, semantic gestures, commands, and pointer-shape preferences.
- **Pointer capture and gestures** — explicit singular capture, signed region-local targets outside ordinary hit bounds, spatial focus, and clock-free press/release/move/click/drag/wheel normalization.
- **Semantic command routing** — callback-free command identity lookup with region-local, scope-chain, and router-global precedence; command execution remains application policy.

## Retained Raster Ownership Model

The public DCurses raster facade deliberately separates application intent from Terminal's live protocol identity:

```text
CursesSession
    |
    +-- CursesRasterResource
            |
            +-- CursesRasterPlaceholder
                    |
                    +-- CursesRasterCell
                            retained in windows/pads/panels
```

`CursesRasterOwnershipState` reports `Current`, `Stale`, `Released`, or `Disposed` certainty using DCurses-owned semantic enums. A non-current token is never silently revived, automatically re-uploaded, or switched to another graphics backend.

Applications own durable source-image data. Terminal owns the live terminal resource and virtual-placeholder lifetime. DCurses owns where the resulting placeholder cells participate in the retained text-grid presentation.

## Interaction Control at a Glance

Interaction routing remains mechanism rather than application policy. A compact scoped interaction pattern is:

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

```text
net8.0
net9.0
net10.0
```

The repository uses C# 13. Release qualification covers Windows, Linux, and macOS on x64 and ARM64.

Local wrappers use Debug configuration. Pull requests use Staging with warnings-as-errors. Pushes to `main` and release tags use Release.

## Design Boundaries and Guarantees

- DCurses does not maintain a second terminal capability database.
- DCurses does not install a competing raw-input loop, independently own terminal modes, or bypass Terminal's authoritative event/query path.
- DCurses does not emit private OSC/CSI/DCS/APC graphics framing for Terminal-owned semantic protocols or persistent raster facilities.
- DCurses does not expose Terminal-private persistent-raster ids or retain a hidden arbitrary source-image cache for lifecycle replay.
- DCurses does not automatically fall back from Unicode raster placeholders to Sixel or another backend after failure.
- Logical screens, windows, pads, viewports, panels, and interaction routers are single-writer unless documented otherwise; one Terminal-owned event wait may coexist with serialized refresh/output work.
- The interaction router owns no background work or hidden event loop. It returns structured routing results rather than invoking callbacks or executing application commands.
- Pointer hit testing does not automatically change logical focus. Terminal/window-manager focus and DCurses logical focus are distinct concepts.
- Pointer capture is explicit and singular. Drag/drop payload semantics, timed multi-click policy, timing thresholds, and application movement policy remain above DCurses.
- Layout primitives are pure/stateless. DCurses does not retain or automatically reapply application layout rules after resize.
- Retained physical knowledge is invalidated when output state becomes uncertain; logical state is preserved so a later explicit refresh can repaint safely.
- Panels, interaction registries, gesture bindings, scope depth, and other potentially growing structures are explicitly bounded.
- Widget frameworks, callback dispatch, retained capture/bubble event trees, automatic focus-on-click, PTY/process hosting, terminal emulation, and application frameworks remain outside the library contract.

## Samples and Documentation

The repository contains focused samples for base session/drawing behavior, layout, retained panels, semantic metadata, advanced interaction routing, and retained mixed-media presentation.

`Icod.DCurses.MixedMedia.Sample` demonstrates retained raster placeholders in pads/viewports, panel overlays, clipping, interaction geometry, and ordinary serialized refresh using public DCurses/Terminal APIs only.

Recommended documentation entry points:

- [`CHANGELOG.md`](CHANGELOG.md) — release-by-release feature history;
- [`docs/1.0-Stable-Compatibility-and-Migration-Guide.md`](docs/1.0-Stable-Compatibility-and-Migration-Guide.md) — stable 1.x compatibility floor and migration guidance;
- [`Icod.DCurses-Development-Roadmap.md`](Icod.DCurses-Development-Roadmap.md) — current and longer-range development direction;
- [`Icod.DCurses-1.6.0-Development-Roadmap.md`](Icod.DCurses-1.6.0-Development-Roadmap.md) — retained mixed-media development contract;
- [`docs/Public-API-Fingerprint-1.6.json`](docs/Public-API-Fingerprint-1.6.json) — compiler-derived 1.6 public contract fingerprint;
- [`docs/Public-API-Baseline-1.6.md`](docs/Public-API-Baseline-1.6.md) — human-readable 1.6 public API baseline;
- [`docs/T1609-Retained-Mixed-Media-Adversarial-Performance-and-Allocation-Gate.md`](docs/T1609-Retained-Mixed-Media-Adversarial-Performance-and-Allocation-Gate.md) — final mixed-media hardening evidence;
- [`docs/T1610-Public-API-Package-Documentation-Dependency-and-Licensing-Regret-Gate.md`](docs/T1610-Public-API-Package-Documentation-Dependency-and-Licensing-Regret-Gate.md) — final pre-RC release-regret evidence.

Historical roadmaps, tranche records, public-API baselines/fingerprints, implementation plans, and release-closure documents remain repository engineering evidence and are intentionally not duplicated here.

## Compatibility and Versioning

Stable `1.0.0` remains the compatibility floor. Version 1.6 is additive over the published 1.5 contract and keeps `AssemblyVersion` at `1.0.0.0`.

The package supports `net8.0`, `net9.0`, and `net10.0`. The direct 1.6 runtime dependencies remain `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0`.

The frozen 1.6 candidate fingerprint is:

```text
266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

Compatibility and migration guidance is maintained in [`docs/1.0-Stable-Compatibility-and-Migration-Guide.md`](docs/1.0-Stable-Compatibility-and-Migration-Guide.md). Release chronology belongs in [`CHANGELOG.md`](CHANGELOG.md); engineering qualification details remain in versioned roadmaps, public-API evidence, tranche records, and release audits.

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

`Icod.DCurses` is licensed under the GNU Lesser General Public License, version 3 or later. See `LICENSE`.

The NuGet package declares license acceptance as required. Package clients that honor NuGet's `requireLicenseAcceptance` metadata must obtain acceptance of the license terms before installation.
