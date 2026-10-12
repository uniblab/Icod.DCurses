# Icod.DCurses 2.5.0 — Color-Surface Hardening

**Date:** 2026-10-12 (America/New_York)  
**Status:** Approved feature direction; planning contract, not an implementation or release claim  
**Baseline:** Published Icod.DCurses 2.4.0 on published Icod.Terminal 1.29.0  
**Consumers:** Icod.Widgets theming and the dedit application

## Intent and success

Icod.Widgets must be able to paint every control state and owned surface with an independent foreground and background color. Icod.DCurses already represents that intent: every `CursesCell` carries a `CursesStyle`, and every style carries a foreground color, background color, and rendition attributes. Version 2.5 does not replace that model. It certifies and hardens the complete color path so higher layers can depend on it without discovering gaps while implementing themes.

The release succeeds when default, indexed, and direct-RGB foreground/background combinations survive all applicable retained operations; background-only changes produce damage and terminal output; wide-cell footprints remain coherent; opaque panel surfaces retain colored blanks; resize, invalidation, and lifecycle repaint do not retain stale colors; and a public-only sample demonstrates several simultaneous foreground/background combinations. Capability loss must remain deterministic and visible through `CursesPresentationCapabilities`.

This is a DCurses color-surface contract, not a widget theme system. No application palette, widget state, inheritance rule, syntax highlighter, or terminal protocol encoder enters this repository.

## Layer ownership

| Layer | Responsibility |
|---|---|
| Application / dedit | Select an application palette, persist user choices, and assign document meaning |
| Icod.Widgets | Resolve host, subtree, widget, state, and widget-part styles; repaint every owned surface |
| Icod.DCurses 2.5 | Retain complete cell styles through editing/composition; detect style damage; expose presentation capabilities; coordinate refresh |
| Icod.Terminal 1.29 | Normalize semantic screen requests against the selected profile, plan rendition transitions, and encode serialized output |
| Icod.TermInfo | Supply terminal capability evidence transitively through Terminal |

The production graph remains `DCurses -> Terminal -> TermInfo`. Widgets continues to depend directly only on DCurses. DCurses does not emit private escape sequences or inspect raw TermInfo strings.

## Existing contract to preserve

`CursesColor` supports terminal-default, non-negative indexed, and 24-bit RGB requests. `CursesStyle` combines independent foreground/background requests with supported attributes. `CursesCell.Blank(style)` and `CursesCell.Continuation(style)` preserve a complete style. `CursesWindow` exposes styled writes, background cells, fills, editing, composition, and retained damage; pads, viewports, panels, and refresh build on the same cell representation.

`CursesPresentationCapabilities` reports indexed-color count, direct RGB, usable foreground/background selectors, default-color restoration, supported attributes, and color-restricted attributes. The existing presentation resolver preserves supported requests and degrades unsupported colors to terminal default rather than guessing a nearest color. Version 2.5 preserves that policy. A higher layer may choose an appropriate palette from the reported capabilities; DCurses will not introduce implicit quantization.

Preserve the 2.4 public API unless a permanent failing public-consumer witness demonstrates an irreducible gap. Preserve `AssemblyVersion 2.0.0.0`, C# 13, net8.0/net9.0/net10.0, and the direct stable Icod.Terminal 1.29.0 dependency.

## Logical mutation invariants

The exact `CursesStyle` assigned to a cell must survive:

- ordinary, rune, text-element, tab, and prepared-cell writes;
- `FillRectangle`, `Clear`, `Erase`, line/window clears, scrolling, cell/line insertion and deletion;
- rectangular copy and overlay where the source coordinate is defined as opaque;
- pad storage, viewport projection, window/subwindow mapping, and text-layout presentation;
- panel composition, z-order changes, movement, resize, and visibility changes;
- screen resize for preserved coordinates; and
- physical-screen invalidation followed by repaint.

Operations that synthesize vacated or repaired cells use the owning window's complete `BackgroundCell` style. When a width-two element is created, moved, clipped, overwritten, or repaired, its leader and continuation must carry the same style, and no stale colored half may remain.

Cell equality and retained damage include the complete style. Changing only foreground, only background, or only attributes must make the coordinate dirty and eligible for refresh. Reassigning an equal cell must retain the established no-op behavior.

## Composition and transparency

An opaque panel covers lower content with every cell, including a styled blank. This is the required mechanism for widgets that own a background, such as menus, drop-downs, dialogs, tab surfaces, editors, and status regions.

The published `BlankCellsTransparent` behavior remains literal and compatible: an ordinary blank without retained raster state is transparent even when it carries a style or semantic metadata. Version 2.5 will document and test this explicitly rather than silently changing old overlays. Widgets must select `Opaque` when colored blank cells are visually meaningful.

If the public-only color sample or later Widgets package witness proves that callers need a third composition policy—such as transparency only for default-style blanks—the exact failing scenario, compatibility analysis, enum value, metadata/raster behavior, and wide-cell rules must be reviewed in a design amendment before implementation. No speculative transparency mode is promised by this design.

## Physical presentation and fallback

Refresh must recognize foreground-only and background-only changes, preserve independent foreground/background requests through the DCurses-to-Terminal mapper, and commit Terminal-owned rendition plans in deterministic order. Default-color restoration must not be claimed where the terminal profile cannot restore it safely. Unsupported RGB, unsafe indexed values, unavailable foreground/background selectors, and color/attribute restrictions retain their existing controlled-degradation behavior.

`Reverse` remains a semantic attribute. DCurses passes supported reverse intent to Terminal; it does not rewrite a style by swapping its stored foreground and background. Higher layers that need stable selected/focused colors should request explicit color pairs rather than depend on how a host renders reverse video.

Invalidation discards physical certainty, not the logical requested image. A repaint after invalidation, suspend/resume, or terminal resize must re-establish the complete desired style. Screen growth creates default logical cells until the application repaints them; DCurses does not infer a theme or stretch a prior background into new coordinates.

## Public-only acceptance sample

A focused color-surface sample will use only public Icod.DCurses APIs and one application-owned event loop. It will display:

- terminal-default foreground/background;
- multiple indexed foreground/background pairs when safely supported;
- multiple RGB foreground/background pairs when safely supported;
- explicit foreground-only and background-only changes;
- attribute combinations, including a clearly labeled reverse example;
- an opaque colored panel containing styled blank padding and a border-like region;
- a width-two grapheme on a colored background; and
- the reported presentation capabilities and the selected fallback palette.

The sample adapts its requested palette from `CursesPresentationCapabilities`; it does not identify terminals, inspect TermInfo, emit escape sequences, or claim unsupported color fidelity. Resize triggers complete application repaint. Its README gives a runnable command and explains that visible results depend on terminal capability and configuration.

## Testing and evidence

Automated tests will cover each logical mutation family, both color axes independently, equal-cell no-op behavior, wide-footprint repair, opaque and blank-transparent panel behavior, pads/viewports, resize preservation, invalidation, physical-state differencing, mapping, rendition planning, default restoration, restricted attributes, and controlled degradation. Prefer compact parameterized matrices over duplicating every existing test.

The release gate includes warnings-as-errors Staging/Release builds and all tests on net8.0/net9.0/net10.0, the existing Windows/Linux/macOS x64/ARM64 CI matrix, public API fingerprint comparison against 2.4, package validation, and fresh NuGet-only consumers. Live-terminal observations record exact OS, terminal/version, architecture, runtime, and commit. Missing live platforms remain explicitly unverified; CI is not presented as visual color acceptance.

No performance regression is expected from tests and fixes. Measure an unchanged multicolor frame, a foreground-only update, and a background-only update to confirm that work remains proportional to damaged cells and that equal-frame refresh does not emit unnecessary rendition transitions.

## Version and release policy

Implementation begins with `Version` and `PackageVersion` `2.5.0-alpha.1`; `AssemblyVersion` remains `2.0.0.0`. The planning PR does not change package identity. RC and stable promotion occur only after the API/package/documentation regret gate and exact-head qualification. The maintainer alone merges, tags, and publishes.

The root README remains a durable package README and explains the color model, capability checks, fallback, and runnable sample without prerelease qualification prose. CHANGELOG records only implemented behavior. Historical 2.4 qualification records and API artifacts remain unchanged; the 2.4 roadmap may record its completed merge, tag, and publication status.

## Explicit non-goals

- Widget themes, palettes, inheritance, state roles, and widget-part styles.
- Dedit preferences, syntax highlighting, document spans, or file formats.
- Automatic nearest-color conversion, dithering, contrast correction, or accessibility policy.
- New raw terminal protocol, direct TermInfo access, or terminal-name heuristics.
- Alpha blending or partially transparent terminal cells.
- Changing `BlankCellsTransparent` incompatibly.
- A second event loop, automatic repaint scheduler, or retained widget hierarchy.

## Decisions carried into implementation planning

1. Treat the existing cell/style model as the public foundation; add no replacement color abstraction.
2. Preserve controlled degradation to terminal defaults; higher layers choose capability-appropriate palettes.
3. Use opaque panels for visible colored blanks and freeze the existing blank-transparent behavior with tests.
4. Add public API only after an exact package-consumer witness and design amendment.
5. Qualify the complete foreground/background path before beginning Widgets theme inheritance work.
