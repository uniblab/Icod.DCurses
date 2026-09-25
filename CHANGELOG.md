# Changelog

All notable `Icod.DCurses` release-line changes are summarized here. Detailed tranche evidence, API fingerprints, roadmaps, and release qualification records remain under `docs/` and the versioned roadmap files.

## 2.2.0 (development) — Interaction and Application Conveniences

- Added detached, read-only discovery of effective single-key and command-sequence bindings in focused-region, active-scope, then router-global precedence.
- Added bounded two-through-eight-gesture command sequences with explicit pending, completed, mismatch, fallback, and application-driven cancellation results.
- Kept ordinary single-key routing unchanged; sequence state is invalidated by relevant focus, scope, binding, region, resize, and disposal transitions.
- Updated the public-only editor and roguelike samples to generate shortcut help from effective binding discovery and exercise contextual sequences without transferring command execution or event-loop ownership to DCurses.
- Added package-only consumer coverage and adversarial, lifecycle, precedence, capacity, allocation, dependency, API-fingerprint, XML-documentation, and cross-platform Staging gates.
- Preserved the published 2.1 source surface additively, `AssemblyVersion` `2.0.0.0`, .NET 8/9/10 targets, and the sole direct `Icod.Terminal 1.18.0` production dependency. The development package identity is `2.2.0-alpha.1`.

## 2.1.0 — Core Presentation and Text

- Added Unicode text-element layout, spans and bounded visual lines with source/visual caret, hit-test, vertical-movement and selection geometry.
- Added retained window layout projection and prepared bulk cell writes, plus application-owned large-content viewport and stateless fixed/weighted track geometry.
- Added bounded, opt-in refresh diagnostics with explicit outcome publication and disabled-path allocation qualification.
- Added public-only roguelike and editor acceptance samples with synthetic large content, visible-slice rendering, documented controls and headless tests.
- Preserved `AssemblyVersion` `2.0.0.0`, .NET 8/9/10 targets, and the sole direct `Icod.Terminal 1.18.0` production dependency. The stable-source candidate passed the PR Staging matrix; the `main` push validates Release configuration before publication.

## 2.0.0 — Terminal Integration

- Replaced the public `CursesSession.Terminal` description with `CursesSession.Profile : Icod.Terminal.TerminalProfile`; live size results and lifecycle size events now use `Icod.Terminal.TerminalDimensions`.
- Moved refresh, cursor/rendition/edit planning, output serialization, and mixed-media transactions through Terminal-owned semantic APIs while retaining DCurses presentation and interaction behavior.
- Removed the production direct `Icod.TermInfo` package and assembly dependency. The sole direct terminal package dependency is `Icod.Terminal 1.18.0`; TermInfo can still be restored transitively.
- Advanced `AssemblyVersion` to `2.0.0.0`. Applications upgrading from 1.6 must rebuild against the new signatures; consult [the 2.0 migration guide](docs/2.0-Migration-Guide.md).

## 1.6.0 — Retained Mixed-Media Presentation

Status: published stable release.

### Added

- Session-owned `CursesRasterResource` and `CursesRasterPlaceholder` facades over the published `Icod.Terminal 1.15.0` persistent-raster / Unicode-placeholder ownership model.
- Opaque `CursesRasterCell` tokens for retaining raster placeholder cells in DCurses logical presentation without exposing Terminal-private protocol ids.
- `CursesRasterOwnershipState`, `CursesRasterOwnershipStatus`, and `CursesRasterOwnershipLossReason` for side-effect-free DCurses lifecycle observation.
- `CursesSession.CreateRasterResourceAsync(...)` using backend-neutral `TerminalRasterImage` input.
- Retained raster get/set operations on `CursesVirtualScreen` and `CursesWindow`, plus current-cursor `CursesWindow.WriteRasterCell(...)`.
- Lazy row-sparse retained-raster storage separate from ordinary cells and semantic metadata.
- Mixed-media propagation through editing, scrolling, overlapping copy, overlay, subwindows, pads, viewports, clipping, and screen resize.
- Panel composition support for retained raster cells, including deterministic z-order, movement, hide/show/dispose, resize, clipping, and the rule that blank+raster coordinates remain visually present under blank-cell transparency.
- Terminal 1.15 Unicode-placeholder refresh integration through a typed internal output seam; DCurses does not construct backend graphics command bytes.
- Physical retained-raster tracking, sparse redraw, synchronized-output acceptance, and conservative rendition reassertion after raster placeholder emission.
- Lifecycle hardening for stale/released/disposed raster ownership and physical-state invalidation after ownership loss or explicit facade disposal.
- `Icod.DCurses.MixedMedia.Sample` and a package-only raster smoke consumer.
- Adversarial capacity, sparse-allocation, churn, cancellation, output-failure, and deterministic caller-retry coverage.

### Changed

- Retained logical/physical cell state now carries an independent raster axis in addition to ordinary cell and semantic metadata state.
- Raster-bearing coordinates disable refresh optimizations that have not been proven raster-safe.
- Internal raster-cell equality now uses a typed identity comparison to avoid boxing-heavy `ValueType.Equals(object)` allocation during sparse replacement churn.
- Package description/release notes and README now describe the complete 1.6 capability set.

### Compatibility

- Additive over the published `1.5.0` contract.
- `AssemblyVersion` remains `1.0.0.0`.
- Targets remain `net8.0`, `net9.0`, and `net10.0`.
- The published 1.6 package directly depends on `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0`. Version 2.0 changes that boundary as described above.
- No widget framework, hidden application event loop, generic raster scene graph, raw Kitty/Sixel command API, hidden source-image cache/re-upload, or automatic graphics-backend fallback is added.

## 1.5.0 — Advanced Interaction Control

- Added bounded immutable-parent interaction scopes and descendant-only LIFO activation leases.
- Added explicit singular pointer capture.
- Added deterministic spatial focus movement.
- Added clock-free pointer gesture normalization including drag lifecycle gestures.
- Added scope-level command bindings with region → scope chain → global precedence.
- Hardened interaction lifecycle, ownership, capacity, and allocation behavior.

## 1.4.0 — Interaction Routing

- Added interaction regions, hit testing, deterministic logical focus, semantic key gestures, command identities, and pointer-shape preferences.
- Kept command execution, automatic focus policy, and application event-loop ownership above DCurses.

## 1.3.0 — Geometry and Layout

- Added immutable rectangles/insets, clipping, docking, fixed/proportional layout helpers, explicit window bounds, panel resize, and application-owned resize recomputation.

## 1.2.0 — Retained Panels and Layers

- Added independent retained panel surfaces, deterministic z-order, visibility/movement, transparency, composition, clipping, damage-bounded recomposition, and disposal.

## 1.1.0 — Semantic Metadata and Hyperlinks

- Added sparse retained semantic cell metadata and hyperlink integration through Terminal-owned semantic output.

## 1.0.0 — Stable Core Contract

- Established the stable 1.x compatibility floor for sessions, retained screens/windows, Unicode-aware cells/text, events, refresh, lifecycle, and package behavior.
