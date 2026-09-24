# Icod.DCurses 2.1.0 Development Roadmap

**Project:** `Icod.DCurses`\
**Release:** `2.1.0`\
**Theme:** Core presentation and text foundations\
**Compatibility baseline:** published `2.0.0`\
**Assembly version:** `2.0.0.0`\
**Direct runtime dependency:** `Icod.Terminal 1.18.0` minimum; no direct `Icod.TermInfo` reference\
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`\
**Configurations:** `Debug`; `Staging`; `Release`\
**Status:** T2111 accepted; T2112 RC and stable-source qualification next\
**Planning snapshot:** 2026-09-23

**T2101 artifacts:** [2.0 baseline](docs/T2101-2.0-Core-Presentation-Baseline.md); [accepted API design](docs/2.1-Core-Presentation-and-Text-API-Design.md); [T2102-T2108 implementation plan](docs/superpowers/plans/2026-09-23-icod-dcurses-2.1-core-presentation-text.md); [foundation gate](docs/T2101-Core-Presentation-and-Text-Foundation-Gate.md)

---

## 1. Executive intent

Version 2.1 builds the reusable core presentation and text facilities needed by two long-term application goals:

- a terminal-native roguelike with a scrollable world view, status areas, messages, overlays and efficient local updates;
- a screen editor in the style of `pico` or DOS `edit.exe`, with Unicode-aware cursor movement, selection, wrapping, horizontal and vertical navigation, prompts and large-document presentation.

The release does not attempt to ship either complete application. It adds the mechanisms both applications need, then qualifies those mechanisms through two small public-only acceptance samples. Application data, editing commands, undo/redo, persistence, game rules, pathfinding and event-loop policy remain above DCurses.

The release covers six related capability families:

1. rich text layout;
2. caret and selection geometry;
3. large-content viewport and virtualization primitives;
4. bulk cell operations;
5. additional stateless layout primitives;
6. opt-in rendering diagnostics and measured optimization.

These families belong in one release because they share a coordinate contract. Text source positions, terminal columns, visible rectangles, cell spans and damage must agree before editor or game-oriented higher layers can build on them safely.

---

## 2. Release contract

Version 2.1 is additive over 2.0. The following constraints are release requirements:

- preserve the public 2.0 API unless a defect makes a change unavoidable and a separate compatibility decision approves it;
- keep `AssemblyVersion` at `2.0.0.0`;
- keep the direct production dependency graph at `Icod.DCurses -> Icod.Terminal`; TermInfo may remain transitive through Terminal;
- keep all terminal I/O, live capability selection, input decoding, lifecycle and serialized output inside Terminal;
- keep text layout, coordinate mapping, viewport calculations and higher layout calculations pure and usable without opening a terminal session;
- keep application content and policy caller-owned;
- keep mutation single-writer unless an API explicitly documents otherwise;
- keep capacities, overflow behavior, failure atomicity and allocation bounds explicit;
- qualify all new public APIs on `net8.0`, `net9.0` and `net10.0` across the established Windows, Linux and macOS matrix.

No new Terminal feature is presently required. If implementation discovers a live-terminal gap, that gap must be fixed and published in Terminal before DCurses consumes it. DCurses must not bypass the boundary with TermInfo calls, terminal-brand tests or private escape sequences.

---

## 3. Application acceptance stories

### 3.1 Roguelike witness

The roguelike sample owns a synthetic world larger than the terminal. It presents only the visible map cells, keeps the player visible while moving, updates nearby cells without rebuilding the whole world, and composes a message/status area plus an overlay. It demonstrates:

- two-dimensional viewport clamping and coordinate translation;
- fixed and weighted layout tracks for map, status and message regions;
- bulk row/cell updates with styles and metadata;
- deterministic damage after local world changes;
- resize recomputation;
- keyboard navigation through the existing event path.

The sample does not add combat, pathfinding, procedural generation, save files, an entity/component system or a game loop owned by DCurses.

### 3.2 Editor witness

The editor sample owns an in-memory multi-line document. It presents a visible slice, moves a caret across Unicode text elements, scrolls vertically and horizontally, optionally wraps lines, displays a selection, edits text and uses a status/prompt area. It demonstrates:

- text-element-safe source-position and terminal-column mapping;
- tabs, wide text, combining sequences and ambiguous-width policy;
- hard line breaks, clipping, wrapping, alignment and ellipsis where applicable;
- caret affinity at wrapped boundaries;
- selection rectangles across visual lines;
- viewport `EnsureVisible` behavior;
- large-document presentation without a pad proportional to the entire document.

The sample does not become a reusable editor engine. File I/O, encodings, undo/redo, search/replace, syntax highlighting, clipboard policy and editor command architecture remain application or future-package work.

---

## 4. Coordinate and text model

The 2.1 features must use an explicit vocabulary:

- **source position** — a UTF-16 offset at a validated text-element boundary in caller-owned .NET text;
- **text element** — the indivisible Unicode unit accepted by the configured `ICursesTextWidthProvider`;
- **logical line** — content between hard line breaks;
- **visual line** — one laid-out row after wrapping and width constraints;
- **column** — a zero-based terminal cell column;
- **cell rectangle** — rows and columns in a retained DCurses surface;
- **content coordinate** — row/column or text position in application-owned content;
- **viewport coordinate** — a visible coordinate after applying the viewport origin.

Public mapping APIs must not silently split a text element, place a caret inside a width-two cell, or treat UTF-16 code units as terminal columns. Public source indices use UTF-16 offsets because they compose directly with .NET strings and ranges, but only offsets at validated text-element boundaries are legal. Conversion is deterministic and validated.

DCurses does not apply Unicode normalization such as NFC/NFD. It preserves the existing malformed-UTF-16 contract by replacing each malformed code unit with U+FFFD before segmentation; the replacement remains one UTF-16 code unit, so source offsets remain stable. CRLF is one hard line break; lone CR and lone LF are each one hard line break. A tab advances to the next positive tab stop anchored at absolute layout column zero, so its width is `tabInterval - (absoluteColumn % tabInterval)`. Other C0/C1 controls are rejected before layout. Leading zero-width elements have no cell to attach to and are omitted from presentation; zero-width elements following a visible element remain attached to that element. Width-two elements and ambiguous-width policy follow the configured width provider and receive explicit tests.

---

## 5. Rich text layout

### 5.1 Input

The layout input combines caller-owned text with ordered presentation spans. A span may supply a `CursesStyle` and optional `CursesCellMetadata`. Span boundaries must be validated at legal source positions. Spans are sorted, non-overlapping and half-open; overlaps are rejected before layout, while uncovered text uses the caller-supplied default style and metadata. Layout therefore never depends on insertion order.

The initial layout options cover:

- available column width and optional row limit;
- no-wrap, text-element wrap and word-aware wrap;
- start, center and end alignment;
- clip or ellipsis overflow;
- starting column and configurable positive tab interval;
- the existing `ICursesTextWidthProvider`.

Hard breaks are excluded from rendered fragments but preserved in source mappings; a trailing hard break produces a trailing empty logical line. Word-aware wrapping prefers the last legal whitespace boundary and otherwise falls back to text-element wrapping so it always makes progress. A text element wider than the available line is omitted as clipped rather than split. If an ellipsis cannot fit as a complete text element, the visual line is empty and marked clipped. Locale-sensitive word breaking and hyphenation are deferred.

### 5.2 Output

Layout produces an immutable result containing visual lines and fragments. The result must preserve enough information to:

- enumerate cells or efficiently write a visual-line range;
- map legal source positions to visual row/column positions;
- map a visual row/column back to the nearest legal source position;
- identify source ranges represented by each visual line;
- preserve styles and semantic metadata;
- distinguish hard breaks from soft wrapping;
- report clipping or ellipsis without claiming hidden source text was rendered.

The layout result is session-independent. It contains no Terminal resource, protocol identity or live screen ownership.

### 5.3 Presentation integration

`CursesWindow` gains a bounded way to present a layout or selected visual-line range at the current cursor or explicit destination. Presentation must use ordinary retained cells and metadata, obey window clipping and wrap policy deliberately, and produce the same damage behavior as equivalent existing cell writes.

Layout presentation does not open a hidden refresh, move the physical cursor directly or own application scrolling.

---

## 6. Caret and selection geometry

The text geometry layer provides pure mapping helpers over a completed layout:

- source position to visual position;
- visual position to the nearest legal source position;
- movement to the previous/next text-element boundary;
- movement by visual row while preserving a preferred column;
- line start/end movement;
- selection normalization and visual rectangles;
- hit testing for width-two and zero-width text elements;
- explicit leading/trailing affinity where one source boundary appears at the end of one wrapped line and the start of the next.

Selection uses half-open source ranges. An empty range is a caret, not a one-cell selection. Selection rectangles must be clipped safely to a requested visible visual-line range.

The library calculates geometry; it does not own a blinking caret, selection colors, clipboard behavior, editing commands or document mutation.

---

## 7. Large-content viewport and virtualization

The viewport layer supplies deterministic geometry for content larger than a window without retaining the whole content in a `CursesPad`. Content extents and origins use non-negative `int` coordinates with checked arithmetic, matching the existing DCurses geometry model; extents beyond `Int32.MaxValue` are rejected rather than wrapped.

It must support:

- a content extent independent of the visible extent;
- vertical and horizontal origins;
- clamping after content or viewport resize;
- translation among content, viewport and destination-screen coordinates;
- `EnsureVisible` for a point or rectangle;
- line, page, home/end and bounded pan calculations;
- empty content and a viewport larger than its content;
- an explicit visible content range with optional caller-selected overscan.

Application code remains responsible for storing content and rendering the requested visible slice. The initial design must not install a callback-driven data source, background fetcher, cache eviction policy or hidden retained copy of the entire document/map.

Memory use must be proportional to the visible slice and explicit overscan, not the total content extent. Checked `int` coordinates allow documents or maps far larger than terminal dimensions without implying support beyond the existing geometry domain.

---

## 8. Bulk cell operations

Bulk APIs reduce repeated bounds checks and per-cell call overhead for already-prepared content. They must complement, not replace, `Write`, `FillRectangle`, `CopyRectangleTo` and `OverlayRectangleTo`.

The design gate must evaluate at least:

- writing a contiguous row span;
- writing a rectangular block with an explicit source stride;
- filling or replacing style/metadata over a bounded region;
- whether a unified public retained-cell value is justified for ordinary cell, metadata and optional raster axes.

Required semantics include:

- validation before mutation when the operation promises failure atomicity;
- explicit clipping versus rejection;
- well-defined overlapping-source behavior;
- width-two lead/trailing-cell coherence;
- metadata preservation/replacement rules;
- raster preservation/replacement rules and session ownership checks;
- damage limited to cells whose retained value changes;
- span-based input where useful without retaining caller memory.

No API should be added solely because it looks faster. T2101/T2102 benchmarks must identify the repeated-call cost and each accepted API must demonstrate a meaningful application-shaped benefit.

---

## 9. Additional stateless layout primitives

`CursesLayout` remains a pure calculator rather than a retained layout tree. Version 2.1 adds track distribution that composes with existing split and dock helpers:

- fixed tracks;
- weighted tracks;
- minimum/maximum constraints;
- gaps;
- start, center, end and space distribution where integer rounding is deterministic;
- row and column variants using the same rules;
- explicit behavior when minima exceed available space.

The result is a caller-applied set of rectangles. DCurses does not retain parent/child layout ownership, measure widgets, invoke callbacks or mutate windows automatically after resize.

The editor sample uses these primitives for document/status/prompt regions. The roguelike sample uses them for map/sidebar/message regions.

---

## 10. Rendering diagnostics and optimization

Version 2.1 adds opt-in diagnostics sufficient to explain retained refresh behavior without exposing Terminal-private plans or bytes. Candidate counters include:

- full versus sparse repaint;
- logical cells examined and changed;
- rows or regions damaged;
- prepared output item and application-payload counts;
- selected semantic operation categories;
- physical-state invalidation and publication outcome;
- raster-placeholder cells included or rejected as stale.

The design must choose a bounded snapshot/polling surface rather than an unbounded event log. Diagnostics are disabled by default, must not alter refresh decisions and must add no material allocation to the disabled path.

Optimization follows evidence. Benchmarks and allocation tests cover rich text layout, mapping, viewport calculations, bulk writes, local roguelike updates, editor typing, vertical scroll and horizontal scroll. Public complexity and capacity limits are documented; wall-clock thresholds are used only where CI variance can be bounded honestly.

---

## 11. Ownership and failure rules

- Layout inputs and results are caller-owned managed values with no terminal lifetime.
- Viewport state contains geometry, not content ownership.
- Bulk operations mutate only their target retained surface and never retain caller spans.
- Layout and viewport calculations either return a complete valid result or fail before publishing a partial result.
- Arithmetic overflow, invalid spans, invalid dimensions, impossible strides and illegal source boundaries fail explicitly.
- Presentation preserves existing session/raster ownership checks.
- Refresh cancellation, output failure, physical uncertainty and retry behavior remain the published 2.0 contract.
- Diagnostics describe observed DCurses work; they do not become a second capability or protocol surface.

---

## 12. Capacity and performance requirements

T2101 freezes portable bounds after measuring representative workloads. The accepted design must preserve these qualitative guarantees:

- text layout work is linear in inspected text plus produced fragments/cells;
- source/visual mapping does not rescan the entire document for every caret move;
- viewport calculations are constant-time and independent of total content volume;
- virtualized presentation memory is bounded by the visible slice and explicit overscan;
- bulk mutation is linear in the addressed cell count;
- track layout is linear in the track count;
- disabled diagnostics introduce no per-refresh heap allocation attributable to diagnostics;
- ordinary applications that do not use 2.1 features retain 2.0 behavior and allocation characteristics within the accepted measurement floor.

Hard limits must fail predictably before integer overflow, excessive allocation or partial retained mutation. Limits must be documented in XML documentation and user guidance.

---

## 13. Compatibility and public API policy

The 2.0 public API fingerprint and baseline remain immutable historical evidence. T2101 captures a new 2.1 development baseline; T2111 freezes the final additive delta.

T2101 freezes the public names and semantics in the [2.1 core presentation and text API design](docs/2.1-Core-Presentation-and-Text-API-Design.md). The accepted type families are:

- immutable rich-text spans and layout options;
- immutable visual-line/fragment and mapping results;
- source-position, visual-position, affinity and selection geometry values;
- viewport geometry/state calculations;
- bulk retained-cell input values or overloads where benchmarks justify them;
- track definitions and layout results;
- bounded refresh-diagnostics options and snapshots.

The regret gate must reject abstractions that belong to an editor model, game engine, widget tree or Terminal. It must also reject APIs whose only purpose is avoiding a few lines of application policy.

---

## 14. Test strategy

Pure facilities receive deterministic unit and invariant coverage for:

- empty, narrow and oversized constraints;
- CR, LF and CRLF policy;
- combining sequences, emoji sequences, width-two text and ambiguous-width modes;
- tab stops at different starting columns;
- every wrap/alignment/overflow combination;
- round-trip source/visual mappings and documented affinity exceptions;
- selection normalization and clipping;
- viewport clamp/translation/ensure-visible behavior near numeric bounds;
- track rounding and unsatisfied-minimum behavior;
- bulk overlap, clipping, metadata, raster, failure atomicity and damage;
- diagnostic enable/disable and publication outcomes.

Application-shaped tests cover editor and roguelike sequences without depending on a real terminal. Existing pseudo-terminal and package-consumer paths prove that the built package still refreshes through Terminal.

Adversarial tests cover malformed inputs, capacity boundaries, allocation pressure, cancellation, resize, stale raster ownership and failed Terminal commits where relevant. Tests use C#, PowerShell 5.1-compatible PowerShell, cmd and sh; no Python is introduced.

---

## 15. Tranche sequence

### T2101 — architecture, baseline and API regret gate — accepted

- Capture the published 2.0 API, package, behavior, performance and dependency baseline.
- Turn the coordinate/text model in this roadmap into reviewed public API candidates.
- Measure current per-cell, pad, text-width and layout-helper behavior with editor/roguelike workloads.
- Freeze capacity, overflow, span precedence, wrap, tab, affinity, viewport and diagnostic semantics.
- Confirm that Terminal 1.18.0 remains sufficient.
- Write the implementation plan for accepted APIs.

**Acceptance:** the baseline, reviewed design, reversible RED witness, T2102-T2108 plan and foundation gate are complete. Exact-head workflow 35911150258 passed all 14 jobs after steady-state allocation fixtures were isolated from .NET 9 multi-TFM and xUnit collection parallelism. No allocation ceiling, production behavior or package identity changed.

### T2102 — development identity and text coordinate foundation — accepted

- Advance `Version` and `PackageVersion` together to `2.1.0-alpha.1` while retaining `AssemblyVersion 2.0.0.0`.
- Add rich-text input spans and validated source-position/text-element boundaries.
- Add deterministic hard-line and tab analysis shared by later layout.
- Preserve existing `CursesText` behavior.

**Acceptance:** `CursesTextPosition`, `CursesTextSpan`, and the shared Unicode text-element scanner are implemented with permanent boundary, malformed-UTF-16, hard-break, tab, zero-width, extended-grapheme, control, and width-policy coverage. `Version` and `PackageVersion` are `2.1.0-alpha.1`; `AssemblyVersion` remains `2.0.0.0`; production still references only `Icod.Terminal 1.18.0`. Exact executable head `efa04a6cfc0e1035c4daf81d6df0be0c685aff69` passed all 14 jobs in workflow 35919436482.

### T2103 — rich text layout — accepted

- Implement visual lines/fragments, wrapping, alignment, clipping and ellipsis.
- Preserve style and semantic metadata through layout.
- Add bounded enumeration/presentation data without per-cell object allocation.

**Acceptance:** immutable visual lines and styled fragments now cover validated options and spans, hard lines, no-wrap/text-element/word wrapping, per-line alignment, absolute tab stops, malformed UTF-16 with stable offsets, zero-width and width-two elements, clipping, width and row-limit ellipsis, and explicit source/span/fragment/cell capacities. The 80x40 editor qualification reads only 40 lines from a caller-owned 10,000-line document, observes linear width-provider calls, and remains below its portable minimum-of-eight allocation ceiling. Exact executable head `ef6ab4f3d428c8b648f1022b50b8b3b07bfba953` passed all 14 package/runtime jobs in workflow 35928677166 across .NET 8, 9, and 10.

### T2104 — caret, hit-testing and selection geometry — accepted

- Implement bidirectional source/visual mapping.
- Implement caret affinity and preferred-column vertical movement.
- Implement normalized selections and clipped visual rectangles.

**Acceptance:** bidirectional source/visual mapping, wrap affinity, hit clamping, saturating preferred-column movement, line edges, legal Unicode navigation, and half-open selection rectangles now cover hard breaks, clipped and ellipsized source, width-two and zero-width elements, and reversed selections. Legal-boundary rank indexes provide O(1) previous/next navigation; line and local-element searches are indexed without rescanning source text. A 65,536-element qualification proves repeated point mapping and local navigation allocate nothing, while selection allocates only exact owned result arrays. Exact executable head `7cb7f87b86675094fcabc2097c7235e82237cece` passed all 14 package/runtime jobs in workflow 35940297901 across .NET 8, 9, and 10.

### T2105 — retained presentation and bulk mutation

- Integrate selected layout ranges with `CursesWindow`.
- Add only benchmark-justified bulk row/block operations.
- Freeze metadata, raster, clipping, overlap, failure-atomicity and damage semantics.

**Acceptance:** equivalence with scalar retained writes, lower measured overhead in accepted workloads, and no physical output outside normal refresh.

**Acceptance:** selected-line projection, clipping, metadata/raster replacement, wide-cell repair, failure atomicity, coherent bulk row/block input, exact damage, destination/stride/source arithmetic rejection, and the 80x24/nine-cell scalar equivalence and allocation gate are covered by permanent tests. Exact executable head `b26b5a12333319f60a6cfbf9f0954629e892088c` passed all 14 package/runtime jobs in workflow 35947857085 on .NET 8, 9, and 10. The [T2105 gate](docs/T2105-Retained-Presentation-and-Bulk-Cells-Gate.md) records the red and green evidence.

### T2106 — viewport and large-content virtualization foundation

- Implement content/viewport/destination coordinate translation.
- Implement clamping, panning, paging and ensure-visible calculations.
- Prove presentation of a large synthetic document/map with memory proportional to visible content.

**Acceptance:** numeric-boundary tests and large-content allocation gates green; no retained content-provider callback or hidden full-content cache.

**Acceptance:** immutable clamped viewport geometry, widened pan/page arithmetic, smallest ensure-visible movement, overscan and two-way translation, empty/zero-extent boundaries, and visible-slice materialization for a ten-million-row document and 2,048x2,048 world are covered by permanent tests. Exact executable head `5d8063bd6e3f0ef08e842de21d33fdbce469dd54` passed all 14 package/runtime jobs in workflow 35950838881. The [T2106 gate](docs/T2106-Viewport-Geometry-Gate.md) records red and green evidence.

### T2107 — track layout primitives

- Add fixed/weighted/minimum/maximum track calculations and gaps.
- Define deterministic integer remainder distribution and unsatisfied-minimum behavior.
- Qualify resize recomputation for editor and roguelike region arrangements.

**Acceptance:** pure geometry tests, allocation gate and application-layout scenarios green.

**Acceptance:** fixed and weighted tracks, minimum reservation, capped proportional redistribution, low-index remainder, six surplus distributions, bounded input validation, numeric extremes, editor/roguelike resize shapes, row/column symmetry, and allocation costs proportional to track count are covered by permanent tests. Exact executable head `8c53b3e30a3780884c26ca609f6a1a255c56cf6d` passed all 14 package/runtime jobs in workflow 35954736073. The [T2107 gate](docs/T2107-Stateless-Track-Layout-Gate.md) records the red and green evidence.

### T2108 — bounded refresh diagnostics and performance qualification

- Add the accepted opt-in diagnostic snapshot surface.
- Cover sparse/full refresh, invalidation, preparation, commitment and publication outcomes.
- Tune only evidence-backed layout, mapping, viewport and retained-mutation hot paths.

**Acceptance:** disabled-path allocation parity, bounded enabled state, truthful failure/cancellation results and workload benchmarks green.

**Acceptance:** the opt-in public snapshot contract, bounded accumulation, truthful success/cancellation/failure publication, physical invalidation and repaint, scalar-only public boundary, warmed disabled/enabled allocation comparison, and visible-world full-frame versus nine-cell update qualification are covered by permanent tests. Exact executable head `878fcdd8630f97556ee2791509d805ed4a35ece7` passed all 14 package/runtime jobs in workflow 36035524413. The [T2108 gate](docs/T2108-Bounded-Refresh-Diagnostics-Gate.md) records the failure and correction evidence. PR #33 remains open for T2109–T2112.

### T2109 — roguelike application acceptance

**Implementation plan:** [T2109 roguelike application](docs/superpowers/plans/2026-09-24-icod-dcurses-t2109-roguelike-application.md).

**Acceptance:** the public-only executable, deterministic coordinate-generated large world, bounded visible frame and message state, clamped movement, track regions and resize geometry, two-cell local updates, retained help overlay, documented controls and headless tests passed all 14 package/runtime jobs at executable head `0297270695ce1ffc6b8bb42a99fbb18d2ac3a219` in workflow 36040652864. The [T2109 gate](docs/T2109-Roguelike-Application-Gate.md) records red and green evidence and the manual terminal-run limitation. PR #33 remains open.

- Add `Icod.DCurses.Roguelike.Sample` using public APIs only.
- Demonstrate a virtualized scrolling map, local updates, track layout, status/messages, resize and an overlay.
- Document controls and architectural ownership.

**Acceptance:** deterministic headless application tests plus manual runnable sample; no game-engine or Terminal-private dependency.

### T2110 — editor application acceptance

**Implementation plan:** [T2110 editor application](docs/superpowers/plans/2026-09-24-icod-dcurses-t2110-editor-application.md).

**Acceptance:** the public-only fixed-record editor sample, sparse ten-million-record document, Unicode editing and geometry, wrap/no-wrap, selection, two-axis viewport, resize and status/prompt regions passed all 14 package/runtime jobs at executable head `035f78603740fbc22a41fbf1325f5c1f58938bc9` in workflow 36043351407. The [T2110 gate](docs/T2110-Editor-Application-Gate.md) records exact-head evidence and manual terminal-run limits. PR #33 remains open.

- Add `Icod.DCurses.Editor.Sample` using public APIs only.
- Demonstrate Unicode caret movement, insertion/deletion, selection, wrap/no-wrap, horizontal/vertical scrolling, status and prompt regions.
- Exercise a large synthetic document without a document-sized pad.

**Acceptance:** deterministic editing/navigation tests plus manual runnable sample; no reusable document engine hidden inside DCurses.

### T2111 — adversarial, package, documentation and API freeze

**Implementation plan:** [T2111 release closure](docs/superpowers/plans/2026-09-24-icod-dcurses-t2111-release-closure.md).

**Acceptance:** adversarial boundary inventory, selection-edit atomicity, development API baseline, root/sample/changelog/package documentation and direct dependency/consumer validation passed all 14 runtime/package jobs at executable head `f0b69470cace0bfe29cd3db311ac156a4a50deac` in workflow 36044565032. The [T2111 gate](docs/T2111-Adversarial-Package-Documentation-and-API-Gate.md) records 1,222 tests per TFM in a representative runtime job and package artifact digests. PR #33 remains open.

- Run capacity, overflow, allocation, cancellation, failure and resize qualification.
- Update root/package README, sample index, changelog, XML documentation and package consumer.
- Freeze and review the 2.1 public API fingerprint and baseline.
- Re-run the direct dependency and license gates.

**Acceptance:** exact-source runtime/package/API/documentation gates green with no unresolved public API regret.

### T2112 — RC and stable-source closure

**Implementation plan:** [T2112 RC and stable-source closure](docs/superpowers/plans/2026-09-24-icod-dcurses-t2112-rc-stable-source.md).

- Promote the unchanged accepted source through RC and stable-source identities.
- Run the full Staging/Release OS and architecture matrix.
- Validate fresh package-only consumers and artifact provenance.
- Record exact-head evidence and maintainer handoff.

**Acceptance:** exact-head release matrix and artifacts green. Merge, post-merge validation, tagging, GitHub Release creation and NuGet publication remain separate maintainer actions.

---

## 16. Explicit non-goals for 2.1

Version 2.1 does not add:

- a widget/control package or retained widget hierarchy;
- callback event dispatch, capture/bubble or automatic focus policy;
- a reusable editor document/buffer, undo/redo, search or file format layer;
- a game engine, entity model, pathfinding or world persistence;
- animation/frame scheduling;
- physical raster placement scenes, sprite ownership or image decoding;
- terminal emulation or PTY/process hosting;
- a background virtual-content loader or cache manager;
- locale-sensitive word segmentation or hyphenation;
- hidden terminal capability access or any direct TermInfo dependency.

These remain candidates for later releases or sibling packages. The intended sequence after 2.1 is interaction/application conveniences followed by higher-level packages such as `Icod.DCurses.Widgets`, guided by evidence from the two acceptance applications.

---

## 17. Immediate next step

Begin T2112 RC and stable-source qualification using the accepted T2101–T2111 surface. Retain the open PR through the release gates.
