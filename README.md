# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET. It provides retained logical screens, windows, pads, panels, Unicode-aware text, semantic metadata, retained mixed-media presentation, geometry/layout, damage-aware refresh, and deterministic interaction routing without taking ownership of the application's event loop or application policy.

## Status

Current release: **`Icod.DCurses 2.0.0`**.

Version **2.1.0 is in development** on [PR #33](https://github.com/uniblab/Icod.DCurses/pull/33). It adds immutable Unicode text layout, source-position and selection geometry, retained layout projection, prepared bulk cell writes, large-content viewport coordinates, stateless track layout, and bounded opt-in refresh diagnostics. The [roguelike and editor samples](https://github.com/uniblab/Icod.DCurses/blob/2.1.0-roadmap/samples/README.md) demonstrate the public APIs with application-owned state. The 2.1 development [API baseline](https://github.com/uniblab/Icod.DCurses/blob/2.1.0-roadmap/docs/Public-API-Baseline-2.1.md) and [roadmap](https://github.com/uniblab/Icod.DCurses/blob/2.1.0-roadmap/Icod.DCurses-2.1.0-Development-Roadmap.md) describe the pending release gates. The current install command below remains the published 2.0 release until a maintainer publishes 2.1.

Version `2.0.0` completes the direct dependency cutover: production DCurses depends only on `Icod.Terminal 1.18.0`, which may restore `Icod.TermInfo` transitively. Version 2.0 changes the public profile and dimensions types and the assembly identity. See the [2.0 migration guide](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/docs/2.0-Migration-Guide.md) and [2.0 roadmap](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/Icod.DCurses-2.0.0-Development-Roadmap.md).

Version 1.6 adds retained mixed-media presentation over the published `Icod.Terminal 1.15.0` persistent-raster / Unicode-placeholder ownership model. Raster placeholder cells participate in ordinary DCurses windows, pads, viewports, panels, clipping, scrolling, composition, damage, and refresh while Terminal remains the sole owner of live raster protocol identity, acknowledgement, encoding, and lifecycle certainty.

The frozen 1.6 public contract is:

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

Version 1.6 is the final published 1.x line. It is additive over the published 1.5 contract and keeps `AssemblyVersion` at `1.0.0.0`.

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

- `Icod.TermInfo` owns immutable terminal capability data and reusable inspection/planning facilities.
- `Icod.Terminal` owns the live terminal conversation: modes, dimensions, lifecycle, input, semantic protocols, persistent raster ownership, reversible terminal state, and serialized output.
- `Icod.DCurses` owns retained terminal-cell presentation and deterministic interaction mechanisms.
- Applications own their event loop, command execution, source-image durability, widget/application semantics, navigation, and high-level layout policy.

The direct 2.0 runtime dependency is:

```text
Icod.Terminal 1.18.0
```

`Icod.TermInfo` is not a direct dependency of DCurses 2.0; NuGet may restore it transitively through Terminal. This major version requires a consumer rebuild; follow the [2.0 migration guide](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/docs/2.0-Migration-Guide.md). The previous 1.6 package retains its historical direct dependencies on `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0`.

## Install

```text
dotnet add package Icod.DCurses --version 2.0.0
```

The package targets:

```text
net8.0
net9.0
net10.0
```

## Quick Start

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

## Retained Raster Presentation

Applications supply a backend-neutral `TerminalRasterImage`, create the live resource/placeholder through `CursesSession`, and retain opaque placeholder cells in ordinary DCurses surfaces:

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

if ( resourceResult.IsAvailable ) {
	await using CursesRasterResource resource = resourceResult.GetRequiredValue();
	TerminalControlResult<CursesRasterPlaceholder> placeholderResult =
		await resource.CreatePlaceholderAsync( 1, 1 );

	if ( placeholderResult.IsAvailable ) {
		await using CursesRasterPlaceholder placeholder = placeholderResult.GetRequiredValue();
		screen.Move( 2, 4 );
		screen.WriteRasterCell( placeholder.GetCell( 0, 0 ) );
		await session.RefreshAsync();
	}
}
```

The public raster ownership shape is:

```text
CursesSession
    -> CursesRasterResource
        -> CursesRasterPlaceholder
            -> CursesRasterCell
```

`CursesRasterOwnershipState` reports `Current`, `Stale`, `Released`, or `Disposed`. DCurses never exposes Terminal-private raster ids, constructs Kitty/Sixel command bytes, retains a hidden source-image cache for replay, or silently switches graphics backends.

## Feature Inventory

- **Logical screens and windows** — retained curses-style cell surfaces, cursor movement, editing, scrolling, styles, and explicit refresh.
- **Unicode-aware text** — terminal text-element measurement, slicing, width-two coherence, and configurable ambiguous-width policy.
- **Pads and viewports** — off-screen retained surfaces with independently clipped projections onto the visible screen.
- **Semantic metadata** — retained metadata such as hyperlinks, emitted through Terminal-owned semantic operations.
- **Retained mixed media** — lazy row-sparse raster state participating in editing, scrolling, copy/overlay, pads/viewports, panels, clipping, resize, and damage refresh.
- **Retained panels** — deterministic z-order, movement, visibility, resizing, clipping, transparency, composition, and disposal. Blank+raster coordinates remain visually present under blank-cell transparency.
- **Geometry and layout** — immutable rectangles/insets plus stateless split, dock, and clip helpers.
- **Lifecycle and refresh** — physical-state invalidation after terminal uncertainty, sparse/full redraw, synchronized output, and stale raster rejection before output.
- **Interaction routing** — bounded regions/scopes, logical and spatial focus, explicit pointer capture, clock-free gestures, semantic commands, and pointer-shape preferences.

## Design Boundaries

- No second terminal capability database or raw-input reader.
- No private OSC/CSI/DCS/APC graphics framing for Terminal-owned facilities.
- No public Terminal-private persistent-raster ids.
- No hidden raster source cache/re-upload or automatic Sixel fallback.
- No widget/control framework, callback dispatcher, retained capture/bubble event tree, automatic focus-on-click, PTY/process hosting, terminal emulation, or application framework.
- Application policy remains above DCurses.

## Samples and Documentation

`Icod.DCurses.MixedMedia.Sample` demonstrates all three retained presentation axes together—ordinary text, `CursesHyperlink` semantic metadata, and raster placeholder cells—inside a pannable pad, with panel overlays, clipping, interaction geometry, serialized refresh, and graceful continuation when raster ownership is unavailable.

Recommended documentation entry points:

- [`CHANGELOG.md`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/CHANGELOG.md)
- [`docs/2.0-Migration-Guide.md`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/docs/2.0-Migration-Guide.md) for upgrading 1.6 applications
- [`samples/README.md`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/samples/README.md) for runnable consumer examples
- [`docs/Public-API-Fingerprint-2.0.json`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/docs/Public-API-Fingerprint-2.0.json)
- [`docs/Public-API-Baseline-2.0.md`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/docs/Public-API-Baseline-2.0.md)
- [`docs/1.0-Stable-Compatibility-and-Migration-Guide.md`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/docs/1.0-Stable-Compatibility-and-Migration-Guide.md)
- [`Icod.DCurses-Development-Roadmap.md`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/Icod.DCurses-Development-Roadmap.md)
- [`Icod.DCurses-2.0.0-Development-Roadmap.md`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/Icod.DCurses-2.0.0-Development-Roadmap.md)
- [`Icod.DCurses-1.6.0-Development-Roadmap.md`](https://github.com/uniblab/Icod.DCurses/blob/v2.0.0/Icod.DCurses-1.6.0-Development-Roadmap.md)

## Compatibility and Versioning

Version 2.0 changes the public profile/dimensions signatures and advances `AssemblyVersion` to `2.0.0.0`; applications upgrading from 1.6 must rebuild. Stable `1.0.0` remains the historical compatibility floor for the 1.x line.

The frozen 2.0 public contract is:

```text
75 exported types
559 canonical declared contract lines
sha256 1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65
```

## Authors

Inspired by original work from Bill Joy, author of the original `termcap`; Mary Ann (born Mark) Horton, author of `terminfo`; Pavel Curtis, author of `pcurses`; and Zeyd Ben-Halim, Eric S. Raymond, and Thomas Dickey, whose work developed and maintained `libtinfo` and `ncurses`.

Managed .NET implementation by Timothy J. Bruce <uniblab@hotmail.com>.

## Copyright

Copyright (c) 2026 Timothy J. Bruce

## License

`Icod.DCurses` is licensed under the GNU Lesser General Public License, version 3 or later. Sample applications are licensed under the GNU General Public License, version 3 or later, as stated in their source headers.

See `LICENSE` and the per-project/source declarations for the applicable terms.
