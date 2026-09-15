# Icod.DCurses 1.6.0 Retained Mixed-Media Presentation Design

**Date:** 2026-09-15  
**Status:** approved architecture authority  
**Baseline:** published `Icod.DCurses 1.5.0`  
**Dependencies:** published `Icod.Terminal 1.15.0`; published `Icod.TermInfo 1.14.0`

## Objective

Version 1.6 lets Terminal-backed raster placeholder content participate in DCurses retained terminal-cell presentation: logical coordinates, windows, pads, viewports, panels, clipping, scrolling, composition, damage, and sparse refresh.

The governing rule is:

> `Icod.Terminal` owns live raster ownership and encoding; `Icod.DCurses` owns retained presentation coordinates, composition, damage, and refresh; applications own durable source-image data and higher-level policy.

The release is not a generic graphics scene graph and does not turn DCurses into a widget framework.

## Selected representation

The approved model is a separate sparse retained-media plane:

```text
Curses logical surface
    +-- dense visual/text cells
    +-- sparse semantic metadata
    +-- sparse retained-media references
```

Mixed-media state is not stored inside every `CursesCell` and is not folded into `CursesCellMetadata`.

Reasons:

- no permanent per-cell cost for applications that never use mixed media;
- `CursesCell` keeps its existing standalone value semantics;
- semantic metadata stays terminal-independent;
- raster ownership remains session-bound and lifecycle-sensitive;
- the established row-sparse metadata design provides a proven storage pattern.

T1601 measures the representation again and freezes exact public spelling, but changing the separate-plane architecture requires a new design review.

## Ownership model

DCurses introduces a small facade over Terminal persistent raster ownership:

```text
CursesSession
    -> DCurses raster resource facade
        -> DCurses raster placeholder facade
            -> retained logical placeholder-cell references
```

Exact public type and method names freeze in T1601.

The facade must never expose protocol-private raster identities, generation ids, raw graphics command payloads, or backend-specific command dictionaries.

Resource and placeholder facades are ownership roots. A retained logical media-cell reference is presentation state only and does not independently own terminal cleanup.

## Image-input boundary

DCurses is not an image decoder or transcoder.

The preferred design is to reuse Terminal's backend-neutral raster-image model for resource creation rather than duplicate pixel formats, dimension rules, alpha semantics, and validation in a second DCurses image hierarchy.

T1601 may introduce a thinner DCurses input value only if dependency-boundary tests show that direct reuse would expose more Terminal surface than is acceptable. It may not create a second image-codec subsystem.

## Logical editing semantics

Mixed-media references move or clear with the same logical operations that move or clear ordinary retained content.

The implementation must cover:

- write and replacement;
- erase and clear operations;
- insert/delete cells and lines;
- scrolling;
- rectangle copy and overlay;
- subwindows;
- pads and viewports;
- panel retained surfaces;
- resize and clipping.

Visual cells, semantic metadata, and retained media are separate logical axes. A text write may replace media at a coordinate without silently deleting unrelated semantic metadata.

Cross-session copying must not silently transplant Terminal ownership. Same-session copies may retain references to the same live placeholder cells when the logical operation permits it.

## Panel composition

Panel z-order remains the composition authority.

A retained media reference makes its panel coordinate visually present even when the ordinary carrier cell is blank. Therefore `BlankCellsTransparent` treats a coordinate as transparent only when it has neither visible ordinary cell content nor retained media content.

Semantic metadata alone does not make a blank coordinate visually opaque.

## Refresh model

The existing DCurses refresh engine remains the single physical presentation coordinator.

For each damaged coordinate it compares:

```text
visual/text state
semantic metadata state
retained-media state
```

against trusted physical knowledge and emits required operations through the canonical Terminal-backed session path.

Terminal 1.15 placeholder cells are self-contained, so DCurses does not depend on left-neighbor encoding state or raster emission order. This preserves correctness under sparse redraw, clipping, scrolling, overlap, and arbitrary damage order.

No graphics-specific writer or second input reader is added.

## Rendition interaction

Terminal placeholder emission uses private rendition channels internally and restores those channels according to its contract.

DCurses must conservatively reassert the next ordinary cell's required rendition when necessary rather than assuming cached foreground/underline-color state remains valid across placeholder emission.

DCurses does not duplicate Terminal's private placeholder encoding.

## Lifecycle model

Terminal remains authoritative for persistent ownership certainty.

When resource or placeholder ownership becomes stale, released, or disposed:

- DCurses must not emit stale identity;
- trusted physical media knowledge is invalidated;
- logical retained references may remain as application-visible intent;
- no hidden re-upload or automatic recreation occurs;
- stale ownership never becomes current again.

The default design uses lazy validation of retained references rather than an unbounded reverse index from each ownership object to every logical coordinate that references it.

Transport/output uncertainty invalidates physical knowledge under the existing DCurses model. It does not trigger blind replay or backend switching.

## Capability and TermInfo boundary

Terminal owns live capability verification and execution.

DCurses does not infer Unicode-placeholder support from terminal brand, `TERM`, generic Kitty naming, or Sixel support.

`Icod.TermInfo.Inspection 1.14` may be used in an optional sample for advisory lifecycle/placement/backend planning, but production DCurses does not turn `RasterBackendPlanner` into hidden backend ranking.

Sixel is not an automatic fallback for the retained-placeholder ownership model.

## Interaction with 1.5

Version 1.6 does not introduce a second interaction system.

Applications and future widgets continue to use 1.4/1.5 interaction regions, scopes, focus, capture, gestures, and commands over the geometry containing media.

Raster content does not automatically own focus, hit testing, callbacks, commands, or pointer capture.

## Boundedness

Version 1.6 adds no unbounded scene graph, background queue, animation scheduler, hidden source-image cache, second terminal reader, or second output stream.

The sparse media plane allocates only for rows containing media. T1601 reuses the established large-pad memory reference when comparing representation cost.

Terminal's live persistent-raster capacities remain the authoritative upper ownership bounds.

## Public API freeze

The architecture is approved now. T1601 freezes exact public type/member names, nullability, result signatures, image-input spelling, lifecycle projection, and dependency allow-list after tests.

T1601 may refine spelling and narrow exposure. It may not change these decisions without a new design review:

- separate sparse media plane;
- media distinct from semantic metadata;
- Terminal owns protocol identity/encoding;
- DCurses owns logical coordinates/composition/damage/refresh;
- no hidden source cache/replay;
- no automatic Sixel fallback/backend ranking;
- no silent cross-session ownership transplant;
- media-bearing panel coordinates are visually present;
- no widget/event/layout framework enters 1.6.

## Deliberate non-goals

Version 1.6 excludes widgets/controls, callback event trees, automatic focus-on-click, timed multi-click policy, drag/drop payload semantics, retained flex/grid/constraint layout, animation, generic raster scene graphs, automatic physical-placement scene ownership, image codecs/transcoding, raw graphics protocol APIs, terminal emulation, PTY/process hosting, and accessibility-tree ownership.

## Success condition

Version 1.6 succeeds when Terminal-backed raster placeholder cells can be retained and manipulated through ordinary DCurses windows, pads, viewports, panels, clipping, scrolling, composition, damage, and sparse refresh while preserving the published 1.5 behavior for applications that do not use mixed media and preserving Terminal as the sole live graphics protocol/ownership authority.
