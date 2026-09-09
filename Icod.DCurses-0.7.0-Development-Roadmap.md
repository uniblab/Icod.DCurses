# Icod.DCurses 0.7.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.7.0`  
**Stable source baseline:** `0.6.0`  
**Dependency baseline:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Release theme:** Refresh and output optimization  
**Status:** Stable `0.7.0` source promoted; T709 merge gate active

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

T701 through T708 are complete. T709 has promoted the green release candidate to stable `0.7.0` source while retaining `AssemblyVersion 0.7.0.0`; the stable-source merge gate remains active.

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

**Gate T701:** complete and green.

---

# 5. T702 — Synchronized Refresh Framing

T702 SHALL integrate or deliberately expose composition with `TerminalSession.AcquireSynchronizedOutputAsync(...)` around complete DCurses refresh transactions.

The accepted policy exposes `CursesSessionOptions.UseSynchronizedOutput`, default `false`, and delegates physical synchronized-output framing to `Icod.Terminal`. One unnested begin/end pair costs 16 protocol bytes, so automatic framing is not forced on every refresh.

**Gate T702:** complete and green.

---

# 6. T703 — Cursor-Motion Selection

T703 compares safe advertised cursor-motion candidates by concrete expanded byte cost, with equal-cost ties and unsafe/unknown cursor state retaining the established absolute path.

**Gate T703:** complete and green at the alpha.3 checkpoint.

---

# 7. T704 — Erase Operation Selection

T704 selects among literal blanks, clear-to-end-of-line, clear-to-end-of-screen, and whole-screen clear only when the complete affected logical region is safely default-styled blank and the selected capability is a strict byte win.

**Gate T704:** complete and green at the alpha.4 checkpoint.

---

# 8. T705 — Insert/Delete Character Optimization

T705 recognizes exact row-local physical insert/delete transformations, rejects wide/semantic-line ambiguity in shifted ranges, requires default inserted/vacated blanks and known default rendition, and retains ordinary rendering as fallback.

**Gate T705:** complete and green at the alpha.5 checkpoint.

---

# 9. T706 — Insert/Delete Line and Scroll-Region Optimization

T706 recognizes exact whole-row transformations and selects advertised `il`/`il1`, `dl`/`dl1`, `ind`/`indn`, and `ri`/`rin` operations when they are a strict total byte win. Temporary `csr` is restricted to provable full-width interior regions and is restored even after operation failure or cancellation; simultaneous operation/restoration failures are both preserved.

Complete wide-cell and semantic-line rows may move because the transformation is row-granular. Partial-width window edits never claim terminal scroll-region ownership.

Detailed record: `docs/T706-Physical-Line-Shift-and-Scroll-Region-Optimization.md`.

**Gate T706:** complete; exact alpha.6 head `6e6b6f0f5f7d6af378ec5468c1411827551b5090` passed Windows, Linux, macOS, and package/fresh-consumer validation.

---

# 10. T707 — Rendition and Refresh-State Minimization

T707 reduces redundant presentation transitions without weakening reset-first correctness.

The accepted transition model retains reset-first behavior for unknown physical rendition, attribute removal, and transitions that return a color channel to terminal default. Known monotonic transitions are differential: additive attributes emit only newly required selectors, direct non-default color changes emit only changed color selectors, and logical styles that resolve to the same physical style remain no-ops. Output failure continues to invalidate retained physical/rendition/cursor knowledge.

The deterministic T701 bold-cell workload improves from `19 bytes / 4 writes / 1 flush` to `13 bytes / 3 writes / 1 flush` after an established default baseline, a 31.6% byte reduction and one fewer terminal write.

Detailed record: `docs/T707-Rendition-and-Refresh-State-Minimization.md`.

**Gate T707:** complete and accepted into the green T708/rc.1 candidate.

---

# 11. T708 — Benchmark, API, Package, and Optimization-Regret Gate

T708 is complete. The accepted optimization set is measurable, documented, failure-safe, package-consumable, and contains no unjustified public surface.

Deterministic release-gate fixtures include:

```text
T701 established-default -> bold: 19 bytes / 4 writes
T707 established-default -> bold: 13 bytes / 3 writes
editor two-column insertion:       2 optimized vs 34 fallback bytes
pager one-line deletion:            4 optimized vs 166 fallback bytes
160 x 60 full repaint:           9661 bytes / 121 writes / 1 flush
1000 one-cell updates:           2000 bytes / 2000 writes / 1000 flushes
```

Correctness gates use deterministic bytes, writes, final-screen equivalence, and failure recovery rather than wall-clock timing. Elapsed time and allocation remain observational benchmark concerns.

The public regret review accepts exactly one 0.7 addition: `CursesSessionOptions.UseSynchronizedOutput`, default `false`. All cost models, candidate resolvers, operation plans, retained physical state, and measurement infrastructure remain internal. `PublicRefreshOptimizationApiContractTests`, the dependency-boundary tests, and the package-only consumer machine-guard that decision.

Detailed record: `docs/T708-Benchmark-API-Package-and-Regret-Gate.md`. Public contract: `docs/Public-API-Baseline-0.7.md`.

The exact `0.7.0-rc.1` head `637cd6151251f9c247045ee671c12236208d5988` passed Windows, Linux, macOS, and package/fresh-consumer validation.

**Gate T708:** complete.

---

# 12. T709 — Stable 0.7.0 Closure

T709 is release closure only. No runtime feature or public API change is permitted.

The green `0.7.0-rc.1` contract has been promoted unchanged to:

```text
Version         0.7.0
PackageVersion  0.7.0
AssemblyVersion 0.7.0.0
```

The stable optimization/public contract is frozen in:

- `docs/T701-Refresh-Cost-Foundation-and-Baseline.md` through `docs/T708-Benchmark-API-Package-and-Regret-Gate.md`;
- `docs/Public-API-Baseline-0.7.md`;
- `docs/T709-0.7.0-Stable-Release-Closure.md`.

The remaining T709 work is the stable-source merge gate and, after a later explicit merge, the post-merge Release/tag/publication gates:

1. require the exact stable-source PR head to pass Windows/Linux/macOS Staging build/tests and package/fresh-consumer validation;
2. leave that validated branch untouched except for PR metadata;
3. merge only that exact green head;
4. require the exact merged `main` commit to pass the full Release x64/ARM64 matrix;
5. create `v0.7.0` only after that exact main commit is green;
6. verify NuGet.org, GitHub Packages, symbols, checksums, and GitHub Release assets.

This branch does not perform the merge or any post-merge publication step.

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