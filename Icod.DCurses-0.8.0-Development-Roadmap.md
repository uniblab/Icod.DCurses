# Icod.DCurses 0.8.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.8.0`  
**Merged source baseline:** `0.7.0`  
**Dependency baseline:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Release theme:** Production hardening  
**Status:** T801-T809 complete; exact stable source validated and awaiting explicit merge

---

## 1. Release Objective

`Icod.DCurses 0.8.0` SHALL turn the feature-complete pre-1.0 library into a production-grade runtime component without introducing another major public feature family.

The release SHALL harden ownership, lifetime, cancellation, lifecycle recovery, failure recovery, repeated entry/exit, large-surface behavior, high-frequency refresh, and architecture coverage while preserving the logical screen/window/pad and refresh contracts frozen through `0.7.0`.

Correctness and deterministic restoration remain more important than throughput claims.

---

## 2. Frozen Concurrency Direction

The 0.8 concurrency model SHALL be explicit:

- logical `CursesScreen`, `CursesWindow`, `CursesPad`, and `CursesPadViewport` mutation is **single-writer** unless a specific member explicitly documents otherwise;
- callers may perform terminal input waits concurrently with refresh/output activity;
- terminal-mutating presentation, protocol-acquisition, refresh, alert, cursor, suspend, and disposal work remains internally serialized at the session terminal-activity boundary;
- DCurses SHALL NOT add per-cell or pervasive surface locks merely to claim transparent thread safety;
- concurrent independent event consumers remain unsupported as an ownership pattern because `ReadEventAsync(...)` and `ReadLifecycleEventAsync(...)` ultimately consume Terminal-owned event/lifecycle streams;
- session disposal SHALL make new terminal activity fail deterministically and SHALL unblock pending DCurses input/lifecycle waits without abandoning terminal restoration.

Terminal remains the authoritative byte-stream reader, lifecycle observer, presentation owner, synchronized-output owner, and rich-input protocol owner.

---

## 3. Development Sequence

```text
T801  package/version + hardening/concurrency contract foundation         complete
  -> T802  session lifetime + pending-read/lifecycle disposal semantics   complete
  -> T803  input/refresh concurrency and cancellation stress              complete
  -> T804  resize/suspend/resume storms + rich-input lease coordination   complete
  -> T805  partial-write/output-failure/restoration recovery              complete
  -> T806  repeated session entry/exit and Terminal 1.4 ownership soak    complete
  -> T807  large pads/screens + high-frequency/allocation-pressure stress complete
  -> T808  x64/ARM64, package, API, documentation, and regret gate        complete
  -> T809  stable 0.8.0 closure                                           complete
```

---

# 4. T801 — Hardening and Concurrency Foundation

T801 SHALL:

- set `<Version>` and `<PackageVersion>` to `0.8.0-alpha.1`;
- set `<AssemblyVersion>` to `0.8.0.0`;
- retain `Icod.Terminal 1.4.0` and `Icod.TermInfo 1.10.0`;
- publish this roadmap;
- update the authoritative 1.0 roadmap so `0.7.0` is the merged baseline and `0.8.0` is active;
- document the single-writer logical-surface rule and session-level concurrency model;
- establish deterministic stress/failure helpers without adding public diagnostics APIs.

**Gate T801:** satisfied.

---

# 5. T802 — Session Lifetime and Pending-Wait Disposal

T802 SHALL give `CursesSession` an explicit lifetime cancellation path for operations which wait on Terminal input/lifecycle activity without owning the terminal-activity gate.

Required behavior:

- operations started after disposal begins throw `ObjectDisposedException`;
- a pending `ReadEventAsync(...)` or `ReadLifecycleEventAsync(...)` is unblocked when disposal begins;
- caller-requested cancellation remains distinguishable from session disposal;
- timeout/deadline semantics remain unchanged;
- disposal still waits for in-flight terminal-mutating activity and performs restoration exactly once;
- no second byte reader or cancellation path may discard a fragmented Terminal input sequence.

**Gate T802:** satisfied.

---

# 6. T803 — Concurrent Input and Refresh Stress

T803 SHALL prove that the supported concurrency pattern works repeatedly:

- one Terminal-owned event consumer may wait while refresh activity proceeds;
- cancellation of an input wait does not cancel the underlying Terminal decoder state;
- cancellation before/during refresh does not corrupt retained physical state;
- refresh and presentation operations remain serialized against suspend/disposal;
- independent competing event-reader loops are documented unsupported rather than made implicitly thread-safe.

Tests SHALL use deterministic in-memory Terminal transports and bounded loops; they SHALL NOT depend on wall-clock sleeps when a controllable signal can be used.

**Gate T803:** satisfied.

---

# 7. T804 — Lifecycle Storms and Rich-Input Coordination

T804 SHALL harden resize, suspend, resume, and rich-input ownership interactions.

Coverage SHALL include:

- resize storms with repeated logical-screen synchronization and physical invalidation;
- suspend while refresh/presentation activity is pending;
- resume invalidation and release of the suspended terminal-activity lease;
- repeated suspend/resume callbacks;
- rich-input lease ownership before DCurses entry, during a DCurses session, and after DCurses disposal;
- lifecycle participant close/disposal while suspended;
- lifecycle callback cancellation/failure without leaving the terminal-activity gate permanently held.

The acceptance model SHOULD align with the downstream DCurses hardening soak already maintained by `Icod.Terminal 1.4.0`.

**Gate T804:** satisfied.

---

# 8. T805 — Output Failure and Restoration Recovery

T805 SHALL inject failures at terminal output seams and prove safe recovery.

Coverage SHALL include:

- cancellation during control and refresh writes;
- partial-progress failure during refresh;
- failure while synchronized-output framing is active;
- failure during temporary scroll-region restoration;
- failure during rendition reset;
- failure while releasing presentation leases;
- simultaneous primary-operation and restoration failure;
- physical-screen invalidation after uncertain output;
- a later refresh returning through the safe renderer after invalidation.

DCurses SHALL preserve all independently meaningful failures, using aggregate exceptions where the existing contract already requires dual-failure preservation.

**Gate T805:** satisfied. The failure-injection harness is intentionally chunk-agnostic and does not require application text to be coalesced into a particular `WriteAsync(...)` call.

---

# 9. T806 — Repeated Entry/Exit and Ownership Soak

T806 SHALL run repeated complete ownership cycles using an in-memory duplex Terminal transport and control provider.

Each cycle SHALL verify:

- Terminal baseline mode is entered/restored exactly once;
- pre-existing rich-input leases are temporarily handed off and restored in correct order around full-screen DCurses ownership;
- DCurses refresh remains functional during the cycle;
- semantic key, focus, and bracketed-paste input remain decodable;
- final DCurses disposal restores presentation state;
- later disposal of stale outer protocol leases emits no duplicate cleanup.

The test SHALL be bounded and deterministic, suitable for ordinary CI.

**Gate T806:** satisfied.

---

# 10. T807 — Large Surface, Frequency, and Allocation Pressure

T807 SHALL exercise production-shaped scale without turning microbenchmark noise into correctness failures.

Required workloads:

- large logical screens;
- very large pads with small moving viewports;
- repeated vertical and horizontal panning;
- repeated sparse refreshes;
- repeated no-op refreshes;
- high-frequency one-cell updates;
- wide-cell and semantic-line content under large-surface operations;
- bounded allocation observations around repeated steady-state refresh/pan loops.

Correctness gates SHALL use deterministic final-screen equivalence, bounded retained state, byte/write counts where stable, and absence of unbounded growth. Wall-clock time and GC allocation measurements MAY be recorded as observations but SHALL NOT be brittle pass/fail thresholds unless a repeatable regression bound is demonstrated.

**Gate T807:** satisfied.

---

# 11. T808 — Architecture, Package, API, and Regret Gate

T808 is complete.

The PR workflow uses the same six-architecture runtime set as `main`:

- Windows x64 and ARM64;
- Linux x64 and ARM64;
- macOS x64 and ARM64.

Each runtime job validates `net8.0`, `net9.0`, and `net10.0` under Staging warnings-as-errors. Package validation verifies package structure, exact dependencies, and a package-only fresh consumer.

The public API regret decision is **zero new public API** for 0.8. The accepted 0.7 public surface remains the 0.8 public surface.

The pre-RC implementation head `015b028167337cfacdd39f9a38550548644f6059` passed the complete gate.

The exact `0.8.0-rc.1` head `6247b9dc290089e5db82f83929d07b4b674b8d8b` also passed Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64, and package/fresh-consumer validation.

The formal RC record is maintained in `docs/T808-Hardening-Regret-and-Release-Candidate-Gate.md`, and the public surface decision is frozen in `docs/Public-API-Baseline-0.8.md`.

**Gate T808:** satisfied.

---

# 12. T809 — Stable 0.8.0 Closure

The accepted release candidate has been promoted unchanged to:

```text
Version         0.8.0
PackageVersion  0.8.0
AssemblyVersion 0.8.0.0
```

The stable closure record is maintained in `docs/T809-0.8.0-Stable-Release-Closure.md`.

Exact stable-source head `533f2cd78b1eecdaad936678f09790955da6308b` passed:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- package/fresh-consumer validation.

**Gate T809:** satisfied.

The validated branch SHALL now remain untouched except for PR metadata. The next permitted source-history action is an explicit merge of exact green head `533f2cd78b1eecdaad936678f09790955da6308b` into `main`. After merge, require the exact `main` merge commit to pass the full Release matrix before `v0.8.0` tagging/publication.

---

## 13. Explicit 0.8 Non-Goals

`0.8.0` does not include:

- transparent multi-writer thread safety for logical screens/windows/pads;
- per-cell locking;
- a second terminal input reader;
- a public scheduler/event-loop abstraction;
- a public performance/diagnostics API without demonstrated consumer need;
- native `ncurses` ABI or source compatibility;
- terminal emulation;
- widget/layout systems;
- a new output optimization family unrelated to hardening.
