# Icod.DCurses 0.7.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.7.0`  
**Stable source baseline:** `0.6.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Release theme:** Refresh and output optimization  
**Status:** Active

---

## 1. Release Objective

`Icod.DCurses 0.7.0` SHALL improve physical terminal synchronization efficiency without weakening the logical-screen, cell, rendition, lifecycle, or failure-recovery contracts established by `0.1` through `0.6`.

The retained logical/physical screen model remains authoritative. Optimizations MAY select cheaper terminal operations when the selected TermInfo description makes them safe and when deterministic cost comparison indicates a benefit. Every optimization SHALL have a correctness-preserving fallback to the ordinary retained-screen rendering path.

The release SHALL measure improvement rather than infer it from fewer source-code operations. Relevant measurements include:

- emitted terminal bytes;
- terminal write calls where deterministically observable;
- refresh elapsed time in benchmark workloads;
- managed allocation volume in benchmark workloads;
- changed logical/physical cells;
- selected terminal operation classes.

Correctness remains more important than globally minimizing the escape stream.

---

## 2. Architectural Decisions

### 2.1 `RefreshAsync()` remains the batching boundary

The primary managed synchronization boundary remains:

```csharp
await session.RefreshAsync();
```

`0.7.0` SHALL NOT introduce a native-style `noutrefresh` / `doupdate` split unless concrete downstream evidence demonstrates a semantic need that cannot be expressed through the current retained logical-screen model.

### 2.2 Terminal and TermInfo retain protocol ownership

DCurses SHALL NOT embed private synchronized-output, cursor-motion, erase, insert/delete, or scroll escape strings when a TermInfo or Terminal semantic API owns the operation.

In particular:

- DEC private mode 2026 framing belongs to `Icod.Terminal` through `TerminalSession.AcquireSynchronizedOutputAsync(...)`;
- capability presence, expansion, and parameterization remain `Icod.TermInfo` concerns;
- DCurses owns selection between safe candidate operations for the current refresh diff.

### 2.3 Cost comparison is deterministic

The initial cost model SHALL compare concrete expanded output lengths whenever candidates are known. It SHALL NOT guess latency from terminal names, operating systems, emulator brands, or undocumented heuristics.

Tie-breaking SHALL favor the simpler existing operation unless a later benchmark demonstrates a repeatable reason to prefer another candidate.

### 2.4 Optimization never changes logical intent

No optimization may mutate application-visible logical cells, styles, semantic line-glyph identity, window/pad state, or requested cursor position merely because a cheaper terminal sequence exists.

Physical-screen knowledge SHALL be updated only after the corresponding terminal operation has been emitted successfully according to the existing refresh transaction semantics.

### 2.5 Measurement is internal first

T701 SHALL establish internal deterministic refresh measurements and test helpers before any public diagnostics API is considered.

A public refresh-statistics type SHALL NOT be added unless T708 demonstrates a real consumer need that cannot be met through tests, benchmarks, logging outside the library, or maintainer tooling.

### 2.6 Synchronized output is composition, not capability inference

`Icod.Terminal` defines synchronized output as optimistic DEC mode 2026 framing: unsupported terminals may ignore the begin/end frames, and neither Terminal nor DCurses claims support merely from terminal identity.

DCurses SHALL compose with the Terminal lease rather than infer synchronized-output support from `TERM`, TermInfo names, OS, or emulator lineage.

T702 SHALL decide whether DCurses automatically frames every refresh, exposes a curses-shaped opt-in policy, or retains explicit caller composition. That decision SHALL be based on emitted-byte cost, nesting behavior, lifecycle safety, and downstream usability.

---

## 3. Development Sequence

```text
T701  0.7 package/version and refresh-cost instrumentation foundation
  -> T702  Terminal synchronized-output refresh framing and policy
  -> T703  absolute versus relative cursor-motion selection
  -> T704  erase-to-end / erase-screen / whole-screen clear selection
  -> T705  insert/delete-character optimization
  -> T706  insert/delete-line and scroll-region optimization
  -> T707  rendition-transition and refresh-state minimization
  -> T708  benchmark, package, public-API, and optimization-regret gate
  -> T709  stable 0.7.0 closure
```

Meaningful contract checkpoints SHOULD advance the prerelease version while retaining `AssemblyVersion 0.7.0.0`.

---

# 4. T701 — Package, Cost Model, and Measurement Foundation

T701 SHALL:

- set `<Version>` and `<PackageVersion>` to `0.7.0-alpha.1`;
- set `<AssemblyVersion>` to `0.7.0.0`;
- retain `Icod.Terminal 1.0.0` and `Icod.TermInfo 1.10.0`;
- publish this roadmap;
- update the authoritative 1.0 roadmap to mark `0.6.0` complete and `0.7.0` active;
- add deterministic refresh-measurement infrastructure for tests/benchmarks;
- establish baseline scenarios before terminal-operation optimizations change behavior;
- keep the measurement surface internal unless later regret review proves a public need.

Required baseline scenarios include:

- no-op refresh after a clean physical match;
- one changed ASCII cell;
- one changed two-column cell;
- one changed styled cell;
- contiguous changed span;
- sparse changes across one row;
- sparse changes across multiple rows;
- full-screen repaint;
- large-screen repaint;
- repeated high-frequency small updates.

The deterministic measurement layer SHOULD count encoded output bytes at the DCurses terminal-output seam rather than estimating from character count.

**Gate T701:** Staging build/tests/package validation are green, baseline byte counts are machine-testable, and no public API is added merely for instrumentation.

---

# 5. T702 — Synchronized Refresh Framing

T702 SHALL integrate or deliberately expose composition with `TerminalSession.AcquireSynchronizedOutputAsync(...)` around complete DCurses refresh transactions.

Required review:

- begin frame precedes all physical refresh payload;
- end frame follows payload and final cursor placement;
- final synchronized-output lease release flushes according to Terminal ownership semantics;
- nested external Terminal synchronized-output leases remain valid;
- refresh cancellation and output exceptions do not leak DCurses-owned logical framing state;
- suspend/resume/disposal ordering remains correct;
- unsupported terminals continue receiving ordinary refresh payload even if they ignore mode 2026;
- no terminal-name capability inference is added;
- byte overhead is measured for small and large refreshes.

If automatic framing is not always desirable, any public opt-in policy SHALL be curses-shaped and minimal. The stable `RefreshAsync()` call remains the transaction boundary.

**Gate T702:** synchronized framing is correctly ordered and lifecycle-safe, and the accepted policy is justified by measured cost rather than assumption.

---

# 6. T703 — Cursor-Motion Selection

T703 SHALL compare safe cursor-motion candidates before emitting a move.

Candidate TermInfo operations include, where available and safely expandable:

- absolute cursor address;
- row-address + column-address composition;
- horizontal absolute positioning;
- carriage return where semantically exact;
- single/multiple relative left/right/up/down motion;
- next/previous line movement where exact.

Required behavior:

- candidates are constructed only from advertised capabilities;
- expanded byte length is the primary initial cost metric;
- equal-cost ties favor the established absolute path;
- cursor knowledge is updated only after successful emission;
- line-wrap ambiguity cannot make a nominally shorter motion unsafe;
- the final requested cursor position is unchanged.

**Gate T703:** deterministic tests prove that shorter safe cursor motion is selected and that the old absolute path remains the fallback.

---

# 7. T704 — Erase Operation Selection

T704 SHALL expand the existing clear-to-end-of-line optimization into a cost-aware erase selector.

Candidate operations include:

- writing literal blanks;
- clear-to-end-of-line;
- clear-to-end-of-screen;
- whole-screen clear/erase where logical preconditions permit it.

The selector SHALL consider:

- style/default-background requirements;
- whether erased cells are logically blank and default-styled;
- physical-screen state consequences;
- cursor-position consequences;
- expanded capability length versus literal output length;
- terminals lacking one or more erase capabilities.

Whole-screen erase SHALL NOT be used when it would erase logical content that must immediately be rewritten at greater or unknown cost without a measured win.

**Gate T704:** erase selection reduces bytes in representative blanking workloads without weakening cell/style correctness.

---

# 8. T705 — Insert/Delete Character Optimization

T705 SHALL investigate terminal character insertion/deletion as a physical optimization for row-local shifts already represented correctly in the logical screen.

Required preconditions include:

- a deterministic row-local diff that can be represented as insertion/deletion plus a smaller tail repair;
- advertised and safely expandable insert/delete-character capabilities;
- preservation of wide-cell leader/continuation footprints;
- no use across ambiguous wide-glyph boundaries;
- style/rendition state remains correct for inserted cells;
- emitted-byte cost is lower than rewriting the changed row span.

The logical editing APIs remain unchanged. This tranche optimizes only physical refresh of their resulting state.

**Gate T705:** editor-like insert/delete workloads show deterministic byte savings with exact final physical-screen equivalence.

---

# 9. T706 — Insert/Delete Line and Scroll-Region Optimization

T706 SHALL investigate line-oriented physical operations for vertical shifts.

Candidate operations include:

- insert line;
- delete line;
- forward/reverse scroll;
- temporary scrolling-region selection where safe and beneficial.

Required behavior:

- only complete rows/regions are transformed physically;
- the retained physical-screen model is updated by the same logical transformation;
- nested windows do not imply terminal scroll-region ownership—the optimization operates on final screen state only;
- wide-cell row footprints remain valid;
- cursor and rendition side effects are normalized;
- scrolling-region state is restored before the refresh transaction ends;
- cost must beat direct rewrite for the selected diff.

**Implementation status:** complete in `0.7.0-alpha.6`; final exact-head matrix/package validation pending.

The accepted implementation uses an internal exact-transform resolver over retained full-screen state. It compares advertised `il`/`il1`, `dl`/`dl1`, `ind`/`indn`, and `ri`/`rin` forms, allows temporary `csr` only for a provable full-width interior region, includes region setup/restoration and cursor movement in its strict byte-cost gate, preserves complete wide-cell and semantic-line rows, and restores the full scrolling region even after operation failure or cancellation. Simultaneous operation and restoration failures are both retained. Partial-width window edits remain on the ordinary renderer.

Detailed record: `docs/T706-Physical-Line-Shift-and-Scroll-Region-Optimization.md`.

**Gate T706:** terminal-style pager/editor scrolling workloads reduce bytes while producing the same final physical screen as the fallback renderer.

---

# 10. T707 — Rendition and Refresh-State Minimization

T707 SHALL reduce redundant presentation transitions without weakening the reset-first correctness model frozen in 0.6.

Investigation includes:

- avoiding reset/reapply when consecutive logical styles resolve to the same physical style;
- retaining known physical rendition across spans where safe;
- reducing redundant default-color restoration;
- preserving ACS enter/exit grouping;
- coordinating cursor movement with current rendition where capabilities have side effects;
- invalidating cached physical style knowledge after any operation whose side effects are not modeled safely.

T707 MAY add internal operation-cost abstractions shared by cursor, erase, and rendition selection if they reduce duplication. Such abstractions remain internal by default.

**Gate T707:** style-heavy workloads emit fewer bytes/reset sequences with identical logical and physical results.

---

# 11. T708 — Benchmark, API, Package, and Optimization-Regret Gate

Before stable `0.7.0`:

- compare representative 0.7 workloads against recorded T701 baselines;
- record emitted-byte deltas for small updates, full repaint, editor shifts, pager scrolling, and style-heavy screens;
- run high-frequency and large-screen benchmark workloads;
- inspect allocation volume and elapsed time for regressions;
- verify every optimization has a deterministic fallback test;
- verify failure/invalidation behavior returns to a safe retained-screen state;
- review any new public synchronized-output option or diagnostics type and remove it if not justified;
- machine-guard accepted public API additions, if any;
- update README/showcase/package-only consumers;
- verify warning level 4 / warnings-as-errors remains green in Staging and Release;
- verify no new Terminal/TermInfo public leakage;
- ensure optimization tests do not depend on wall-clock timing for correctness.

A measured optimization MAY be rejected if its complexity or failure-state burden outweighs its demonstrated benefit.

**Gate T708:** the accepted optimization set is measurable, documented, failure-safe, package-consumable, and has no unjustified public surface.

---

# 12. T709 — Stable 0.7.0 Closure

T709 is release closure only.

Required work:

- promote a green release candidate to `0.7.0`;
- retain `AssemblyVersion 0.7.0.0`;
- freeze a 0.7 optimization/contract record;
- run one definitive stable-source PR matrix/package gate;
- merge only a green stable source head;
- require the exact merged `main` commit to pass the full Release x64/ARM64 matrix;
- create `v0.7.0` only after that exact main commit is green;
- verify NuGet.org, GitHub Packages, symbols, checksums, and GitHub Release assets.

---

## 13. Explicit 0.7 Non-Goals

`0.7.0` does not include:

- changing logical window/pad/cell semantics;
- a new raw terminal output protocol layer;
- terminal-name or emulator-brand heuristics;
- a replacement TermInfo cost database;
- pervasive thread safety or concurrency-model freeze (`0.8.0`);
- arbitrary output buffering outside the Terminal synchronized-output contract;
- terminal emulation;
- widget/layout systems;
- source-level ncurses API parity;
- globally optimal escape-stream search.

The release objective is a measurably more efficient implementation of the already-frozen logical presentation contract.