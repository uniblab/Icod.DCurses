# Icod.DCurses 1.6.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release:** `1.6.0`  
**Theme:** Retained Mixed-Media Presentation  
**Published baseline:** `Icod.DCurses 1.5.0`  
**Assembly version:** `1.0.0.0`  
**Starting runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Status:** T1601-T1609 complete; T1610 pre-RC release-regret gate active; T1611 pending

## Current implementation status

The roadmap below preserves the approved design requirements and tranche goals in the future-tense form in which they were reviewed. The current release state is later: T1601 through T1609 have been implemented and accepted, T1610 is freezing the final public/package/documentation contract, and T1611 will promote the unchanged accepted implementation to RC and stable-source identities.

The frozen pre-RC public candidate is:

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

Direct dependencies remain `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0` for 1.6. The broader question of whether DCurses should remove its direct TermInfo dependency in favor of a strict `DCurses -> Terminal -> TermInfo` path is deferred to the 1.7 design track.

Tranche records under `docs/` are the acceptance authorities for implemented behavior; this roadmap remains the scope/design authority.

---

## 1. Release objective

`Icod.DCurses 1.6.0` introduces retained mixed-media presentation by integrating the published `Icod.Terminal 1.15.0` persistent-raster and Unicode-placeholder contracts with the existing DCurses logical-screen, window, pad, panel, clipping, scrolling, composition, damage, and refresh model.

The release is intentionally narrower than a generic raster scene graph. Its primary goal is:

> Allow terminal-resident raster content to participate naturally in DCurses retained text-grid rendering while keeping protocol identity and encoding owned by `Icod.Terminal` and keeping application/widget policy above DCurses.

The release must preserve the established layer boundary:

```text
application / future widget layer
    owns application semantics, event loop, command execution,
    high-level layout policy, image meaning, and source-image lifetime

Icod.DCurses
    owns retained logical presentation, windows/pads/panels,
    cell coordinates, clipping, scrolling, composition,
    damage, refresh ordering, and mixed-media logical placement

Icod.Terminal
    owns live terminal state, capability verification,
    persistent raster resource/placement/placeholder ownership,
    protocol-private identity, acknowledgement correlation,
    placeholder encoding, serialized output, and lifecycle certainty

Icod.TermInfo
    owns immutable capability evidence and advisory planning;
    it does not execute graphics or choose hidden application policy
```

---

## 2. Why 1.6 is the right time

The 1.1-1.5 DCurses sequence established the exact higher-level mechanisms required for mixed media:

```text
1.1  semantic retained content
1.2  retained overlapping panels and deterministic composition
1.3  immutable geometry, clipping, layout primitives, and resize policy
1.4  interaction regions, hit testing, focus, gestures, and commands
1.5  scopes, capture, spatial focus, pointer gestures, and scoped commands
```

At the same time, `Icod.Terminal 1.11` through `1.15` established a backend-neutral persistent-raster ownership stack:

```text
1.11  opaque persistent resources and placements
1.12  bounded source-pixel cropping and signed z-order
1.13  immutable-parent relative placement ownership
1.14  side-effect-free lifecycle observation
1.15  opaque Unicode raster placeholders and self-contained placeholder cells
```

Terminal 1.15 deliberately leaves screen coordinates, clipping, scrolling, damage, layout, and refresh ordering to higher-level renderers. Those are already DCurses responsibilities. Version 1.6 therefore fills a real architectural seam rather than inventing a parallel abstraction.

`Icod.TermInfo 1.14` additionally supplies advisory raster-backend evidence and selection planning. That planning remains caller policy and is not promoted into hidden DCurses backend ranking.

---

## 3. Governing design principles

### 3.1 Terminal owns protocol identity

DCurses must not expose or reproduce:

- Kitty image ids;
- Kitty placement ids;
- virtual-placement ids;
- session generation ids;
- Unicode placeholder protocol code points;
- row/column combining-mark tables;
- SGR identity packing;
- raw APC/DCS/OSC graphics framing;
- backend-specific command dictionaries.

The 1.6 public contract speaks only in DCurses presentation concepts and opaque ownership handles.

### 3.2 DCurses owns logical placement and refresh

DCurses remains authoritative for:

- which logical cell contains mixed-media content;
- window-relative and screen-relative coordinates;
- clipping;
- pads/viewports;
- panel composition;
- scrolling and editing semantics;
- damage propagation;
- sparse redraw;
- refresh order;
- lifecycle invalidation of physical knowledge.

### 3.3 No hidden replay

DCurses must not silently retain arbitrary source images solely so stale Terminal resources can be recreated after suspend/resume, session invalidation, resource loss, or transport uncertainty.

The application owns durable source-image data unless a future separately designed API explicitly says otherwise.

A stale/released/disposed raster ownership object never becomes current again.

### 3.4 No automatic backend switch

Version 1.6 does not silently replace Unicode-placeholder rendering with Sixel, ephemeral graphics, or another backend after a failure or unsupported condition.

A backend change may require a different ownership and repaint model. Such policy must remain explicit and separately designed.

### 3.5 Mixed media is not semantic metadata

The existing `CursesCellMetadata` plane carries terminal-independent semantic meaning such as hyperlinks.

Raster placeholder content is different:

- it is session-bound;
- it is generation-sensitive;
- it has live ownership/lifecycle state;
- it can become stale or released independently of textual semantics.

Therefore 1.6 must not overload hyperlink/semantic metadata storage with live raster ownership.

### 3.6 Do not enlarge every `CursesCell`

The established sparse-side-plane precedent remains preferred. Ordinary applications that never use mixed media should not pay a permanent per-cell reference or token cost across every screen, window, and large pad.

T1601/T1602 must prove the representation choice before public API freeze.

---

## 4. Approved representation direction

The leading architecture is a separate row-sparse retained-media plane alongside the existing dense cell plane and sparse semantic-metadata plane:

```text
Curses retained surface
    |
    +-- dense visual/text cell plane
    |
    +-- sparse semantic metadata plane
    |
    +-- sparse retained-media plane
            |
            +-- DCurses-owned retained media reference
                    |
                    +-- opaque Terminal raster placeholder/cell state internally
```

This is a direction, not an excuse to freeze poor names prematurely. T1601 must compare the representation against alternatives and freeze the exact public/internal contract only after memory, copy/edit, lifecycle, and API-regret analysis.

The representation must support:

- O(1) coordinate lookup after row allocation;
- lazy allocation only when mixed media exists;
- row release when the last media reference is removed;
- cross-window/pad/panel copy semantics without protocol-identity remapping by callers;
- coherent clearing when text editing destroys or replaces a media-bearing cell;
- deterministic behavior for width-two text repair next to media cells;
- no stale Terminal identity emission after lifecycle loss.

---

## 5. Ownership model

Version 1.6 should introduce a DCurses-owned facade over Terminal persistent raster ownership rather than exposing protocol-level objects directly throughout the logical rendering model.

Exact public names are frozen in T1601, but the semantic ownership model is:

```text
CursesSession
    |
    +-- raster resource facade
            |
            +-- virtual placeholder facade
                    |
                    +-- retained logical media-cell references
```

### 5.1 Resource facade

The resource facade represents terminal-resident raster data owned through the session.

It must:

- delegate resource creation/cleanup to Terminal;
- expose only the lifecycle information DCurses consumers genuinely need;
- remain tied to exactly one `CursesSession`;
- not expose raw protocol ids;
- not retain an arbitrary hidden copy of source pixels after successful creation unless the caller explicitly owns such data elsewhere.

T1601 must decide whether the creation API accepts Terminal's backend-neutral `TerminalRasterImage` directly or introduces a narrow DCurses image-input abstraction. The decision must minimize duplicate image semantics and public dependency leakage.

### 5.2 Placeholder facade

The placeholder facade represents one Terminal virtual raster placement usable as self-contained text-grid placeholder cells.

It must preserve Terminal's published bounds and lifetime semantics rather than inventing a second incompatible identity system.

### 5.3 Retained media cell/reference

A retained logical media reference is not independently responsible for terminal resource cleanup. It identifies which placeholder cell should be emitted when a given DCurses coordinate is refreshed.

Logical copies may duplicate references to the same live placeholder/cell token where semantically valid. Ownership roots remain explicit resource/placeholder facades.

---

## 6. Logical-surface semantics

T1603-T1605 must define mixed-media behavior across the complete existing DCurses editing/composition model.

### 6.1 Write and replacement

Writing ordinary text into a media-bearing coordinate clears or replaces the retained media reference at that coordinate unless an explicit mixed-media write API says otherwise.

Writing mixed-media content must not silently mutate unrelated semantic metadata.

Text style and semantic metadata remain separate axes.

### 6.2 Clear, erase, insert, delete, and scroll

Every structural editing operation that moves or destroys logical cell coordinates must move or destroy retained media references with the same logical operation.

Required coverage includes at least:

- clear window / clear line / clear-to-end variants;
- insert/delete character cells;
- insert/delete lines;
- scrolling;
- rectangle copy/overlay;
- subwindows;
- pad edits and viewport projection;
- panel movement/resizing/composition;
- screen resize and clipping repair.

### 6.3 Copy semantics

Copying logical content within the same session may retain references to the same live placeholder cells where the ownership contract permits it.

T1603 must explicitly reject or define cross-session copying. No operation may transplant opaque Terminal ownership across sessions by accident.

### 6.4 Panel composition

Panel z-order and transparency remain DCurses concepts. A media-bearing panel coordinate participates in the same deterministic composition ordering as its logical cell coordinate.

The 1.6 design must explicitly define whether blank-cell transparency may reveal lower-layer media and whether a media-bearing coordinate itself counts as visually present even when its ordinary text cell is blank. The chosen rule must be deterministic and covered by composition tests before API freeze.

---

## 7. Physical refresh semantics

The refresh engine must remain damage-driven and must not become a second graphics protocol engine.

For a damaged composed coordinate, the refresh decision conceptually becomes:

```text
composed logical coordinate
    -> ordinary visual/text cell state
    -> semantic metadata state
    -> retained-media state
    -> compare against trusted physical knowledge
    -> emit required cursor/rendition/semantic/media operations
```

Terminal 1.15 placeholder cells are self-contained specifically so sparse redraw, clipping, scrolling, overlap, and arbitrary cell ordering remain correct. DCurses should exploit that property instead of maintaining left-neighbor protocol shorthand state.

### 7.1 Physical knowledge

The physical cache must distinguish enough state to know whether a previously emitted media placeholder cell is still trusted at a coordinate.

If Terminal ownership becomes stale/released or session state is invalidated, corresponding physical media knowledge must be invalidated before further refresh.

### 7.2 Rendition interaction

Terminal placeholder emission temporarily uses private foreground/underline-color channels and then resets those identity channels. DCurses refresh must therefore reassert ordinary desired cell rendition as needed after a media emission without assuming that Terminal preserved DCurses' cached rendition state.

T1606 must freeze the exact refresh/rendition interaction with byte-level and semantic tests.

### 7.3 Synchronized output

When `UseSynchronizedOutput` is enabled, mixed text/semantic/media refresh remains one DCurses refresh transaction under the existing Terminal synchronized-output ownership model where supported.

No second output gate or graphics-specific writer may bypass the canonical session serialization path.

---

## 8. Lifecycle semantics

Version 1.6 must integrate Terminal 1.14/1.15 ownership observation with DCurses retained state without inventing terminal-side existence certainty.

Required states include the semantic equivalents of:

```text
Current
Stale
Released
Disposed
```

and their published Terminal loss/release reasons where exposing them is justified.

### 8.1 Session invalidation / suspend-resume

When Terminal invalidates persistent raster certainty:

- logical DCurses media references may remain retained as logical intent;
- their Terminal-backed ownership becomes unusable/stale;
- refresh must not emit stale protocol identity;
- DCurses physical media knowledge is invalidated;
- no automatic resource recreation occurs.

The application may explicitly create new raster ownership and replace logical references.

### 8.2 Resource or placeholder disposal

Explicit disposal/release must prevent future refresh from emitting that ownership.

The retained logical plane must either observe unusable ownership at refresh time or receive deterministic invalidation notification; T1607 must choose the simpler bounded model and prove it under disposal races and lifecycle churn.

### 8.3 Output uncertainty

Committed output failure continues to invalidate physical knowledge according to existing DCurses rules. It does not authorize blind graphics replay or backend switching.

---

## 9. Capability and TermInfo integration

### 9.1 Terminal capability authority

`Icod.Terminal` remains authoritative for live capability verification and execution.

The initial 1.6 feature depends on the semantic availability required for Terminal 1.15 Unicode raster placeholders. Unsupported/unavailable outcomes must fail predictably without mutating logical or Terminal ownership partially.

### 9.2 TermInfo 1.14

`Icod.TermInfo.Inspection` may be used by an optional sample/integration path to demonstrate advisory lifecycle/placement/backend planning.

Production DCurses must not introduce hidden backend ranking based on `RasterBackendPlanner`.

In particular:

- backend preference remains application policy;
- Sixel is not a transparent persistent-placeholder fallback;
- Unicode-placeholder semantics are not falsely modeled as if TermInfo 1.14 already planned them;
- no terminal-brand heuristic becomes support truth.

The optional planning demonstration was not made part of 1.6 release closure. The broader direct-TermInfo dependency/planning-layer question is deferred to the 1.7 design track.

### 9.3 Dependency policy

The 1.6 line starts from published:

```text
Icod.Terminal 1.15.0
Icod.TermInfo  1.14.0
```

Dependency bumps during development require explicit compatibility qualification. No unreleased sibling dependency should become a hidden prerequisite without a separately approved reason.

---

## 10. Public API policy

Version 1.6 remains additive over the published 1.5 contract.

T1601 freezes the candidate public surface before feature implementation proceeds materially.

Public API goals:

- DCurses-shaped resource/placeholder ownership rather than raw graphics protocol vocabulary;
- minimal new exported types;
- no changes to existing enum numeric identities;
- no breaking constructor or nullability changes;
- no public session-generation or protocol ids;
- no generic raw graphics command surface;
- no widget hierarchy or callback/event-tree API;
- no mandatory allocation/storage penalty for applications that do not use mixed media.

The frozen 1.5 baseline is:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

Every public 1.6 addition must be explained relative to that baseline and recorded in the 1.6 API fingerprint/baseline authority.

---

## 11. Performance, allocation, and boundedness

Mixed-media support must preserve DCurses' established bounded/deterministic philosophy.

Required constraints:

- no per-cell media field added to every `CursesCell` without a measured and approved reversal of the sparse-plane direction;
- no unbounded scene graph;
- no unbounded background queues;
- no hidden image cache proportional to source raster data;
- no background worker or animation scheduler;
- no duplicate terminal reader;
- no graphics-specific output stream bypassing session serialization;
- capacity failures remain controlled and mutation-atomic;
- hot refresh paths receive allocation-regression measurements on all supported TFMs/architectures where the current suite already measures them.

Terminal's published bounds remain upper ownership bounds; DCurses may adopt lower local bounds only if T1601 documents a concrete reason.

---

## 12. Security and trust boundary

Raster acknowledgements and lifecycle observations remain external terminal input handled by Terminal. DCurses must not reinterpret correlation as authenticity.

The 1.6 layer must not:

- parse raw Kitty/APC replies;
- accept caller-supplied protocol-private identities;
- emit stale identities after generation loss;
- treat timeout/silence as proof of unsupported capability;
- trust terminal brand strings as capability evidence;
- replay committed raster transactions blindly after transport failure.

Source image bytes remain application-sensitive data. DCurses does not log or retain them merely for diagnostics or replay.

---

## 13. Development tranche sequence

The proposed 1.6 sequence is:

```text
T1601  architecture, representation, public-API candidate, and dependency regret gate
T1602  bounded session-owned raster resource and placeholder facade
T1603  sparse retained-media plane and core window/screen logical operations
T1604  editing, scrolling, copy/overlay, subwindow, pad, and viewport propagation
T1605  panel composition, transparency, clipping, z-order, and resize coherence
T1606  Terminal 1.15 placeholder refresh integration and physical-state/rendition tracking
T1607  lifecycle observation, suspend/resume, disposal, stale/released ownership hardening
T1608  mixed-media application sample and optional TermInfo 1.14 planning demonstration
T1609  adversarial replay, capacity, performance, allocation, and failure-atomicity hardening
T1610  public API, package, documentation, compatibility, dependency, and licensing regret gate
T1611  release candidate and stable-source 1.6.0 closure
```

The numbering is intentionally contiguous and leaves the public contract frozen early while reserving late tranches for hardening rather than feature expansion.

---

## 14. Tranche details and exit criteria

### T1601 — Architecture / representation / public API / dependency regret gate

Goals:

- inventory every Terminal 1.15 raster type/capability needed by DCurses;
- compare sparse-media-plane alternatives;
- measure the memory cost of inline/token/reference alternatives using the established large-pad reference;
- freeze cross-session copy behavior;
- freeze panel transparency semantics for media-bearing coordinates;
- freeze candidate DCurses facade types/methods and exact nullability;
- audit public Terminal/TermInfo type leakage;
- freeze capacity and lifecycle terminology;
- create the 1.6 public API candidate fingerprint.

Required RED→GREEN evidence includes representation/memory tests, dependency-boundary tests, API shape tests, and unchanged 1.5 compatibility witnesses.

No broad implementation proceeds before this gate is green.

### T1602 — Session-owned raster resource and placeholder facade

Goals:

- create bounded DCurses ownership wrappers over Terminal resource/placeholder ownership;
- create through the canonical `CursesSession`/Terminal activity serialization path;
- expose lifecycle state only at the approved abstraction level;
- ensure deterministic async disposal and no stale-id emission;
- prove wrong-session, disposed-session, cancellation, unavailable capability, and capacity failures are mutation-atomic.

No logical media placement enters this tranche beyond ownership primitives needed by later tranches.

### T1603 — Sparse retained-media plane and core logical operations

Goals:

- implement the selected row-sparse media plane;
- add approved write/clear/inspection operations;
- keep visual cells and semantic metadata independent;
- prove ordinary no-media surfaces allocate no media rows;
- preserve standalone `CursesCell` semantics;
- define same-session copying and reject unsupported cross-session ownership transfer.

Tests must cover row allocation/release, coordinate validation, replacement, clear, and independent semantic metadata retention.

### T1604 — Editing, scrolling, copy/overlay, pads and viewports

Goals:

- move media references with insert/delete/scroll operations;
- propagate through rectangle copy/overlay with deterministic clipping;
- integrate subwindow coordinate transforms;
- integrate pad storage and viewport projection;
- harden interactions with width-two text footprints and Unicode editing.

Every existing structural logical operation touching retained content must be audited explicitly rather than relying on accidental behavior.

### T1605 — Panels, composition, transparency, clipping and resize

Goals:

- compose media-bearing panel coordinates using deterministic panel z-order;
- enforce the T1601 transparency rule;
- clip media through panel/screen bounds;
- preserve media state through supported panel resize where coordinates survive;
- discard clipped/destroyed media references deterministically;
- propagate damage correctly when upper/lower media layers appear, disappear, move, hide, show, or are disposed.

### T1606 — Physical refresh integration

Goals:

- render self-contained Terminal 1.15 placeholder cells through the canonical refresh transaction;
- extend physical knowledge/diff state with the minimum media identity required for correct redraw;
- avoid protocol shorthand assumptions;
- repair DCurses rendition knowledge after placeholder identity-channel resets;
- integrate with synchronized output;
- prove sparse redraw, clipping, overlap, arbitrary dirty order, and full repaint behavior.

Byte-level tests belong at the Terminal adapter seam; DCurses tests should primarily assert semantic ordering and refresh behavior rather than duplicate Kitty wire encoding.

### T1607 — Lifecycle and failure hardening

Goals:

- react correctly to `Current`, `Stale`, `Released`, and `Disposed` ownership;
- handle session invalidation and managed suspend/resume without hidden recreation;
- prevent stale/released placeholder emission;
- invalidate physical media knowledge monotonically;
- harden disposal during refresh and refresh after disposal;
- retain logical intent where safe without pretending terminal ownership still exists;
- prove transport failure does not trigger replay or backend switch.

### T1608 — Application acceptance and planning sample

Goals:

- add a realistic mixed text + hyperlink + panel + raster-placeholder sample;
- demonstrate scrolling/clipping and sparse refresh;
- demonstrate application-owned source image lifetime and explicit replacement after lifecycle loss;
- optionally demonstrate TermInfo 1.14 advisory planning without making Inspection a new hidden production policy owner;
- extend package-only consumer acceptance across `net8.0`, `net9.0`, and `net10.0`.

### T1609 — Adversarial/performance/allocation hardening

Goals:

- deterministic replay under mixed text/media damage streams;
- maximum-capacity resource/placeholder/media-plane churn;
- repeated create/dispose/clear/resize/scroll cycles;
- allocation ceilings for no-media and media refresh paths;
- large-pad sparse-media memory behavior;
- cancellation at pre-commit boundaries;
- failure atomicity under ownership creation and logical mutation;
- no hidden background work, image caching, or reader creation.

### T1610 — Public API/package/docs/dependency regret gate

Goals:

- freeze the final 1.6 public API fingerprint;
- verify 1.5 binary/source compatibility expectations;
- inspect `.nupkg`/`.snupkg` contents and dependency groups;
- execute isolated NuGet-only consumers on all target frameworks;
- audit XML docs, README, CHANGELOG, samples, roadmap, license metadata, and release notes;
- confirm no protocol-private raster identity leaks into the public surface;
- confirm no accidental TermInfo Inspection production dependency was introduced unless explicitly approved.

No new feature enters after this gate.

### T1611 — RC and stable-source closure

Goals:

- promote the accepted implementation/API unchanged to `1.6.0-rc.1`;
- run the complete package + Windows/Linux/macOS x64/ARM64 Staging matrix;
- promote unchanged accepted source to stable `1.6.0` identity;
- rerun exact-head qualification;
- record final dependency graph, API fingerprint, package evidence, and publication sequence.

Merge, post-merge Release qualification, tag creation, and package publication remain explicit separate maintainer actions.

---

## 15. Required test families

At minimum, the 1.6 suite must contain explicit coverage for:

1. unchanged 1.5 behavior when mixed media is unused;
2. sparse-plane lazy allocation and complete release;
3. resource/placeholder facade ownership and disposal;
4. same-session and cross-session ownership rules;
5. write/replace/clear semantics;
6. insert/delete/scroll line and cell operations;
7. rectangle copy and overlay;
8. subwindow transforms;
9. pad storage and multiple viewports;
10. panel z-order, transparency, movement, hide/show, resize, and disposal;
11. screen resize and clipping;
12. semantic metadata coexistence, especially hyperlinks;
13. Unicode width-two footprint interactions;
14. sparse damage redraw and arbitrary dirty ordering;
15. synchronized-output refresh;
16. rendition restoration/reassertion after placeholder emission;
17. stale/released/disposed ownership;
18. suspend/resume and session invalidation;
19. transport/output uncertainty;
20. cancellation and failure atomicity;
21. capacity exhaustion;
22. deterministic replay;
23. no-media allocation regression;
24. mixed-media allocation/performance regression;
25. package-only consumer execution on every target framework;
26. public dependency boundary and protocol-identity leakage tests.

---

## 16. Explicit 1.6 non-goals

Version 1.6 does **not** add:

- a widget/control framework;
- buttons, text boxes, menus, trees, lists, dialogs, or application navigation abstractions;
- callback dispatch or automatic command execution;
- a retained event capture/bubble hierarchy;
- automatic focus-on-click;
- timed double/triple-click policy;
- drag/drop payload/acceptance semantics;
- a retained flex/grid/constraint layout tree;
- automatic layout ownership;
- animation or frame scheduling;
- a generic raster scene graph;
- arbitrary physical raster placement ownership as a second DCurses scene system;
- automatic Sixel fallback for persistent/placeholder content;
- hidden raster backend ranking;
- hidden source-image caching/re-upload;
- image file decoding/transcoding;
- raw Kitty/Sixel command APIs;
- terminal emulation;
- PTY/process hosting;
- accessibility-tree ownership.

A future `Icod.DCurses.Widgets` package remains a strong candidate once the retained mixed-media substrate is stable. A later physical-raster scene extension may also be considered if real application needs remain after Unicode-placeholder integration.

---

## 17. Documentation authorities to create during development

The 1.6 branch should ultimately contain:

- `Icod.DCurses-1.6.0-Development-Roadmap.md` — this release roadmap;
- `docs/superpowers/specs/2026-09-15-icod-dcurses-1.6-retained-mixed-media-presentation-design.md` — approved architecture/design authority;
- an implementation plan under `docs/superpowers/plans/` after the design is reviewed;
- T1601-T1611 tranche records;
- `docs/Public-API-Fingerprint-1.6.json` (or the repository's accepted equivalent after T1601);
- updated root/package `README.md` capability inventory before stable closure;
- updated `CHANGELOG.md` release history before stable closure.

Historical 1.0-1.5 authorities remain unchanged as evidence of the contracts that were true when those releases shipped.

---

## 18. Success condition

`Icod.DCurses 1.6.0` is successful when an application can retain terminal-resident raster-placeholder content inside ordinary DCurses windows, pads, viewports, and panels; edit, scroll, clip, compose, and damage-redraw it deterministically; observe lifecycle loss without stale identity emission or hidden replay; and continue to rely on Terminal as the sole protocol/ownership authority.

The release should make mixed media feel like a natural part of DCurses retained presentation without turning DCurses into either a graphics protocol library or an application/widget framework.
