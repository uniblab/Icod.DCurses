# Icod.DCurses 2.3.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release:** `2.3.0`  
**Theme:** First-class `CursesRasterAtlas` coordination (approved Option 1)  
**Baseline:** published and tagged `v2.2.0`  
**Current source and package version:** `2.3.0-alpha.1`  
**Assembly version:** `2.0.0.0`  
**Direct runtime dependency:** `Icod.Terminal 1.24.0` minimum; no direct `Icod.TermInfo` reference  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Status:** T2301–T2306 accepted; T2307–T2309 exact-head and live acceptance qualification in progress
**Planning snapshot:** 2026-10-03

**Design authority:** [`CursesRasterAtlas` design](docs/superpowers/specs/2026-10-03-icod-dcurses-2.3-curses-raster-atlas-design.md)  
**Implementation plan:** [2.3 atlas implementation plan](docs/superpowers/plans/2026-10-03-icod-dcurses-2.3-curses-raster-atlas.md)

---

## 1. Release decision and goal

The approved choice is Option 1: add a generalized, first-class `CursesRasterAtlas` to DCurses 2.3. Terminal 1.24 now provides the missing mechanics—pixel geometry, planning observations, intrinsic resource geometry, focused operation evidence, acknowledged frame composition, RGB24/RGBA32 partial replacement and frame selection. DCurses can therefore own the presentation policy that belongs above those mechanics.

The release target is a cell-aligned tile renderer suitable for an Ultima-style application while preserving functional breaks:

```text
game/application model
    source art, maps, actors, rules, loop, fallback
                    |
                    v
DCurses 2.3 CursesRasterAtlas
    tile geometry, retained coordinates, damage list,
    front/back sequencing, panels/clipping/refresh ordering
                    |
                    v
Terminal 1.24
    active geometry, opaque resources/frames/placeholders,
    acknowledged mutations, selection and lifecycle evidence
```

This is not a game engine and not a generic raster scene graph.

## 2. Compatibility and architecture requirements

- Preserve every published 2.2 public member and enum value. The 2.3 surface is additive; `AssemblyVersion` remains `2.0.0.0`.
- Keep the production graph `Icod.DCurses -> Icod.Terminal -> Icod.TermInfo`. No raw terminal string, direct TermInfo access or protocol-private identity enters DCurses.
- Reuse the existing `CursesRasterCell`, windows, pads, viewports, panels, clipping, editing, composition, damage and refresh machinery. Do not add a second retained raster plane.
- Keep application assets caller-owned. DCurses must not retain arbitrary image data merely to replay after lifecycle loss.
- Serialize atlas mutations through the existing session terminal-activity gate so they cannot interleave with refresh or lifecycle output.
- Validate and copy a complete presentation request before first output. Never retain caller collections.
- Treat Terminal planning/inspection as advisory. Actual controlled results and acknowledgement remain authoritative.
- Mark ambiguous committed failure as requiring recreation; never blind-retry or pretend a remote transaction is atomic.
- Keep all limits explicit and checked before allocation/output.
- The Ultima-style sample is an acceptance witness, not an API excuse for game-specific names or types.

## 3. Planned public capability

### 3.1 Atlas geometry

Add a bounded cell-pixel geometry query for an atlas grid. Use the direct Terminal cell-pixel query first. Only its timeout may select exact terminal-pixel/character-grid derivation. Reject fractional geometry, overflow and stale assumptions; applications re-query after resize.

### 3.2 Atlas ownership and creation

Create one session-owned facade around one Terminal raster resource, one virtual placeholder grid and two known full-size frames. The initial immutable image must divide exactly into the requested rows and columns. Expose immutable atlas/tile pixel geometry, retained `GetCell`, conservative ownership and recreation-required state—never Terminal ids or frames.

### 3.3 Retained projection

Add a validate-before-mutation `CursesWindow.WriteRasterAtlas` bulk helper for an atlas source rectangle. It projects existing `CursesRasterCell` values into the current retained plane, preserves text/metadata, does not move the cursor, and inherits window/pad/panel/viewport clipping and damage.

### 3.4 Double-buffered presentation

Add bounded tile updates with deterministic row-major ordering. Each nonempty presentation copies current front to back with replacement composition, applies acknowledged tile regions, selects the back only after success, then swaps internal front/back references. Empty work performs no terminal I/O.

Controlled failures return explicit status and completed-update counts without selecting a partial back frame. Ambiguous committed failures poison the atlas for further use and require application-driven recreation from durable source art.

## 4. Tranche plan and gates

| Tranche | Deliverable | Acceptance gate |
|---|---|---|
| **T2300** | Record Option 1; add design, roadmap and implementation plan; advance to `2.3.0-alpha.1`; update the sole direct dependency to Terminal 1.24.0 | Documents agree on ownership, sequencing, failure and non-goals; project identity/reference are exact; no atlas implementation is claimed |
| **T2301** | API-regret review and permanent failing contract tests | Exact names, enum values, defaults, validation, limits, result semantics and XML intent frozen; RED failures are only missing 2.3 API |
| **T2302** | Pixel geometry and planning integration | Direct query, timeout-only exact fallback, resize/re-query, overflow, malformed/cancel/transport behavior and advisory capacity tests pass |
| **T2303** | Atlas creation, immutable geometry, ownership and rollback | Resource/placeholder/two-frame order is correct; all partial failures clean up in reverse; no source image is retained; disposal is idempotent |
| **T2304** | Window/pad/viewport atlas projection | Bulk projection is failure-atomic and cursor-neutral; clipping, panel transparency/z-order, scrolling, copying, resize and damage retain published behavior |
| **T2305** | Front/back presentation coordinator | Front-to-back replace precedes row-major updates; selection is last; swap occurs only after selection; empty/duplicate/oversized/format/dimension cases are exact |
| **T2306** | Refresh serialization and lifecycle/failure hardening | Atlas work cannot interleave with refresh/lifecycle output; definite failures recover through the next front copy; ambiguous failures and generation loss require recreation |
| **T2307** | Adversarial, capacity, allocation and workload measurement | Bounds fail before output; 1/4/16/64/121/256-tile package workloads recorded; no map-sized storage or hidden image cache; later Terminal work is evidence-gated |
| **T2308** | Public-only Ultima-style sample and package-only consumer | Raster and explicit text fallback share one model; movement, overlays, viewport, resize/recreate and clean exit pass automated/manual acceptance |
| **T2309** | Public API, package, dependency, XML and documentation freeze | Additive API fingerprint accepted; sole direct dependency exactly Terminal 1.24.0; README/sample guidance and package artifacts verified on all TFMs |
| **T2310** | RC, live-terminal acceptance and stable-source qualification | Exact RC head green across the PR matrix; live checklist accepted; unchanged stable source green before merge/tag/publication |

Every tranche records an exact commit and evidence document. Green CI is necessary but not sufficient: semantic, package and manual gates still apply.

## 5. Test strategy

### Pure contract tests

- geometry construction, checked multiplication and exact divisibility;
- tile-coordinate to pixel-coordinate mapping;
- update-list copying, duplicate rejection and deterministic row-major order;
- empty presentation and capacity boundaries;
- window source/destination validation and failure atomicity.

### Terminal integration tests

- direct cell query success and timeout-only exact fallback;
- advisory planning snapshot checked before allocation without treating it as a reservation;
- resource, placeholder, second-frame creation and every partial rollback point;
- focused operation evidence per composition/RGB24/RGBA32 operation;
- exact command order: front copy, updates, select, swap;
- controlled unsupported/unavailable/failed results at each step;
- timeout, cancellation, malformed reply, transport loss and resource-missing invalidation.

### Retained-presentation tests

- windows, subwindows, pads and viewports;
- panel transparency and z-order;
- scrolling, copy/overlay, resize and clipping;
- sparse damage and unchanged text/metadata;
- stale/released/disposed atlas cells rejected before output;
- atlas operation and refresh/lifecycle mutual exclusion.

### Package and live evidence

- fresh NuGet-only consumers for `net8.0`, `net9.0` and `net10.0`;
- Windows/Linux/macOS x64/ARM64 Staging matrix;
- representative Kitty-capable live terminal for tile alignment, overlays, resize/recreation and clean exit;
- text fallback on a terminal where the raster path is unavailable.

## 6. Bounds and failure policy

- Atlas rows/columns: 1–256 each.
- Updates per presentation: 0–4096, unique by atlas coordinate.
- Update images: exact tile dimensions and RGB24 or RGBA32.
- All row/column/pixel products use checked or widened arithmetic before narrowing.
- Creation and presentation requests are copied/validated before first mutation or output.
- A controlled failure before selection never presents the partial back frame. The next call resynchronizes it from the known front.
- An exception after possible output commitment sets `RequiresRecreation`; subsequent present calls fail before output.
- Lifecycle loss is not repaired from hidden data. The application recreates from its own source art and replaces retained cells.
- No claim of remote atomicity, gapless display or terminal memory reservation is made.

## 7. Documentation and sample requirements

Before T2309, update the README with the atlas ownership boundary, minimum Terminal version, fallback guidance, failure/recreation rule and a compact public usage example. The sample README must explain which terminals are expected to support the raster path, how to force text fallback, how resize recreation works, and what the sample does not prove.

The Ultima-style sample must remain clearly original and minimal. It demonstrates a top-down tile viewport and status/help overlays; it must not copy copyrighted Ultima art, maps, names or data.

## 8. Non-goals

No tile-map container, camera policy, entity/component system, collision, visibility/fog, pathfinding, AI, combat, inventory, persistence, game loop, asset file decoder, palette conversion, texture scaling, arbitrary transforms, physical sprite scene, sub-cell motion, animation scheduler, widget library, terminal emulator or PTY host enters 2.3.

No hidden fallback ladder, source-image replay, automatic retry, raster batching, Indexed8 partial updates, remote-capacity query or protocol extension is invented in DCurses. A later Terminal 1.25 proposal requires T2307 package evidence.

## 9. Release policy

T2309 is the API/package regret deadline. After it, only fixes required by acceptance evidence enter the release. T2310 promotes one exact source through RC and stable-source gates. Merge, the post-merge Release workflow, tag, GitHub Release and NuGet publication remain separate maintainer actions.

## 10. Immediate next step

T2301 freezes the exact public surface and validation vectors. Capture its expected missing-API RED run, then implement T2302 geometry and planning against those permanent tests.
