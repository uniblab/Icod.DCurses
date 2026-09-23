# Icod.DCurses T2006 Recovery Qualification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Qualify failure, lifecycle, raster ownership, and recovery at the Terminal-only output boundary before T2007 removes the direct TermInfo reference.

**Architecture:** Exercise the real Terminal screen transaction with faulting output and deterministic gates. DCurses keeps logical intent and invalidates physical certainty after failure; Terminal retains encoding, serialization, cleanup, and flush ownership. Make production corrections only for witnessed defects, never add raw output.

**Tech Stack:** C# 13, xUnit, .NET 8/9/10, Icod.Terminal 1.18.0, GitHub Actions six-platform matrix. No Python.

**Spec:** `Icod.DCurses-2.0.0-Development-Roadmap.md`, T2006 and sections 6-8; `docs/T2005-Transactional-Refresh-Hardening-Gate.md`.

## Global Constraints

- Keep `2.0.0-alpha.1`, assembly `2.0.0.0`, Terminal `1.18.0`, and temporary direct TermInfo `1.15.0` unchanged.
- Preserve `README.md` and `docs/Public-API-Fingerprint-2.0.json` exactly, including attribution and license wording.
- Never interpret capabilities or write/flush raw output in production; one successful non-empty refresh commits one Terminal transaction and flushes once.
- Retain logical damage after failure, do not replay a consumed transaction automatically, and fail closed on stale raster identity.
- No merge, release, tag, publication, or TermInfo reference removal in T2006.

## Review Focus

1. An output sink that writes only a prefix before throwing leaves the entire desired row pending and a later explicit refresh redraws it.
2. A flush failure after the body leaves physical state uncertain even though the bytes reached the sink.
3. A body failure combined with hyperlink or synchronized cleanup failure exposes both exceptions and permits a fresh caller-driven recovery.
4. Foreign, stale, or released raster identities fail before output; a later explicit replacement can repaint without reviving the old identity.
5. Suspend/resume, resize, and concurrent disposal cannot deadlock the activity gate or publish an outdated screen.

---

### Task 1: Partial-write and flush-failure recovery

**Files:** Modify `tests/Icod.DCurses.Tests/src/CursesOutputFailureHardeningTests.cs`; modify `src/Internal/CursesRefreshEngine.cs` only for a demonstrated failure.

**Interfaces:** Consume `CursesRefreshEngine.RefreshAsync(CursesScreen, int, int, CancellationToken)` and `ITerminalOutput.WriteAsync/FlushAsync`. Produce evidence that a failed commit retains screen damage and that explicit retry makes one complete commit.

- [ ] Add a prefix-writing fault to the test output and a test that observes the original `IOException`, dirty desired cells, incomplete first output, a complete explicit retry, then an unchanged no-op.
- [ ] Add a one-shot flush fault and test that observes the original failure after body output, dirty desired cells, complete retry, and one successful flush.
- [ ] Run the two focused tests against the PR branch. If a behavioral assertion fails, correct the narrow owner and rerun.
- [ ] Run package validation and six runtime jobs at the exact pushed head; record results.

### Task 2: Cleanup, raster ownership, and lifecycle races

**Files:** Extend `CursesSemanticFailureHardeningTests.cs`, `CursesRasterLifecycleHardeningTests.cs`, and `CursesLifecycleHardeningTests.cs` only for missing witnesses; production lifecycle/raster integration only for demonstrated defects.

**Interfaces:** Consume Terminal-owned transaction cleanup, `CursesRasterPlaceholder.OwnershipState`, `CursesSession.LifecycleParticipant`, and `CursesSession.DisposeAsync()`. Produce fault and ordering evidence without introducing a new output seam.

- [ ] Verify the existing body-plus-cleanup aggregation tests against a partial body and a cleanup fault; add a missing witness if necessary.
- [ ] Add deterministic foreign/stale/disposed raster tests that distinguish no-output rejection from explicit replacement recovery.
- [ ] Gate an active refresh while suspend, resize, and disposal compete, assert bounded completion and retained logical intent, then explicitly refresh the current dimensions.
- [ ] Run focused tests and the full matrix; fix only witnessed production defects.

### Task 3: Gate and roadmap closure

**Files:** Create `docs/T2006-Lifecycle-and-Recovery-Gate.md`; update `Icod.DCurses-2.0.0-Development-Roadmap.md` and PR body only after acceptance.

**Interfaces:** Consume exact-head workflow results. Produce a reviewable T2006 acceptance decision and handoff to T2007.

- [ ] Record test names, failure observations, exact head SHA, package job and six runtime job outcomes, API/dependency identity, and remaining risks.
- [ ] Update T2006 checklist only when all witnesses and CI pass; preserve the explicit T2007 dependency-removal handoff.
- [ ] Verify changed-file diff, protected blobs, and final workflow again before claiming acceptance.

## Self-review

The tasks cover both output uncertainty and identity/lifecycle recovery, with one independent test and review gate per slice. All new claims require exact-head executable evidence. The task boundaries preserve the accepted T2005 transaction design and T2007 dependency scope.
