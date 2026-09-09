# T1106 — Semantic Output Lifecycle, Failure, Cancellation, and Recovery Hardening

**Project:** `Icod.DCurses`  
**Release:** `1.1.0`  
**Tranche:** T1106  
**Development version:** `1.1.0-alpha.6`  
**Assembly version:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Status:** implementation qualified; documentation-synchronized alpha.6 gate required

## 1. Objective

T1106 hardens the 1.1 semantic renderer against output failure, cancellation, synchronized-output restoration failure, lifecycle transitions, and uncertain Terminal hyperlink cleanup.

The 1.0 failure rule remains authoritative:

> If output may have diverged from retained physical knowledge, DCurses must invalidate that knowledge before allowing a later repaint path.

Semantic output adds an additional ownership constraint: DCurses must not invent a recovery operation for Terminal-managed protocol state that Terminal's public API does not expose.

## 2. Two different retained-cleanup models

Terminal 1.6.0 exposes two relevant ownership models that must not be conflated.

### Synchronized output

`TerminalSynchronizedOutputLease.DisposeAsync()` is explicitly retryable. If final release fails, the lease remains owned and a later disposal attempt can retry cleanup.

DCurses owns the lease object for its refresh transaction, so T1106 retains a failed lease in:

```text
pendingSynchronizedOutputCleanup
```

Before any later synchronized refresh begins, DCurses retries that exact lease. Rendition reset during lifecycle/disposal also retries it first.

### Bounded hyperlink output

`TerminalSession.WriteHyperlinkAsync(...)` owns a synthetic hyperlink lease internally. If application text or OSC 8 cleanup fails, Terminal may retain that internal lease for later cleanup.

The bounded public method does not return that synthetic lease to DCurses. Therefore DCurses cannot safely infer from an arbitrary non-cancellation exception whether:

- the hyperlink begin never committed;
- begin committed but application text failed;
- application text completed but hyperlink close failed;
- cleanup remains owned internally by Terminal.

A blind retry could nest above a retained Terminal hyperlink lease and later restore back to that uncertain state.

T1106 therefore chooses a conservative **fail-closed** rule for application text after a non-cancellation bounded hyperlink failure.

## 3. Hyperlink fail-closed rule

`TerminalSessionCursesOutput` latches the first non-cancellation failure reported by Terminal's bounded hyperlink operation.

After the latch:

- no later ordinary application text is emitted through that curses output;
- no later hyperlink application text is emitted;
- Terminal control strings remain available so curses/Terminal cleanup can proceed;
- flush remains available;
- the original failure is retained as the inner exception of the fail-closed diagnostic;
- callers are directed to dispose the owning `CursesSession` rather than attempting continued application output.

This policy is intentionally more conservative than attempting to classify the exception by message or type. Terminal's public bounded hyperlink contract does not expose a trustworthy stage discriminator.

Terminal session disposal remains the authoritative final cleanup path. Terminal's hyperlink manager retries canonical hyperlink close when cleanup remains pending.

A future Terminal public neutralization/recovery operation could allow DCurses to relax this policy. T1106 does not depend on such a future API.

## 4. Caller cancellation before semantic transmission

Caller cancellation remains cancellation rather than an output fault.

When Terminal reports `OperationCanceledException` and the supplied caller token is canceled, `TerminalSessionCursesOutput` does not latch semantic uncertainty.

This aligns with Terminal's bounded hyperlink contract: caller cancellation is observed before the hyperlink transaction begins transmitting. A later uncancelled semantic operation is therefore permitted.

## 5. Cancellation after a completed semantic run

Cancellation may become observable after one bounded linked run has completed but before the complete curses refresh has finished.

In that case:

1. the completed linked transaction remains a valid Terminal transaction;
2. the later refresh operation observes cancellation;
3. `CursesRefreshEngine` invalidates all retained physical knowledge;
4. the `OperationCanceledException` remains caller cancellation;
5. a later refresh with a fresh token performs a complete safe repaint.

T1106 includes a deterministic output-seam test that cancels immediately after a successful linked run and proves the later repaint includes both semantic and ordinary content.

## 6. Hyperlink primary plus cleanup failure

Terminal already preserves independently meaningful bounded hyperlink failures. T1106 verifies this through the real Terminal-backed curses path.

When application text fails and hyperlink cleanup also fails:

- Terminal produces an `AggregateException` containing both failures;
- DCurses does not flatten, replace, or hide that aggregate;
- retained curses physical state is invalidated;
- the semantic output fault becomes fail-closed for further application output;
- disposing the `CursesSession` allows Terminal to retry the retained hyperlink cleanup.

## 7. Synchronized-output restoration failure

T1106 corrects an existing retained-state gap.

Previously, if the curses refresh body completed successfully but final synchronized-output release failed, `RefreshAsync()` propagated the failure while leaving the refresh engine's newly committed physical knowledge intact.

That was too optimistic. The outer presentation transaction had not completed successfully.

The new rule is:

- synchronized-output release failure invalidates retained physical state;
- the failed `TerminalSynchronizedOutputLease` is retained by DCurses;
- the same lease is retried before any later synchronized refresh transaction is acquired;
- if retry fails again, the later refresh body does not begin;
- once cleanup succeeds, DCurses acquires a new synchronized-output lease and repaints because retained physical state is invalid.

This prevents a new presentation transaction from being layered over a Terminal manager that still reports pending mode-2026 cleanup.

## 8. Refresh plus synchronized-output dual failure

The existing 1.0 dual-failure rule is retained.

If the refresh body fails and synchronized-output restoration also fails, DCurses throws an `AggregateException` containing both meaningful failures.

The failed synchronized-output lease is still retained for cleanup retry and physical state is invalidated.

## 9. Lifecycle and disposal

The existing Terminal lifecycle participant remains authoritative for suspend/resume integration.

T1106 preserves these rules:

- preparing for terminal suspend resets curses rendition state under Terminal lifecycle ordering;
- resuming invalidates retained physical screen knowledge;
- visible linked content therefore repaints after resume;
- pending synchronized-output cleanup is retried before rendition reset during lifecycle/disposal;
- disposal continues to run Terminal-owned cleanup even when curses semantic application output has failed closed;
- repeated `CursesSession.DisposeAsync()` continues to share one restoration operation under the established 1.0 lifetime contract.

No second raw output or lifecycle owner is introduced.

## 10. Focused tests

`CursesSemanticFailureHardeningTests` proves:

- hyperlink application-text plus hyperlink-cleanup dual failure remains visible through `CursesSession.RefreshAsync()`;
- Terminal disposal later retries failed hyperlink cleanup;
- hyperlink cleanup failure causes later curses application output to fail closed;
- caller cancellation before hyperlink transmission does not poison later output;
- synchronized-output end failure is retried before a new synchronized transaction begins;
- repeated synchronized-output cleanup failure blocks the new refresh body;
- successful cleanup retry is followed by a complete retained repaint;
- suspend/resume causes linked content to repaint.

`CursesSemanticCancellationHardeningTests` proves cancellation after one completed linked run invalidates the partial refresh and a later fresh-token refresh repaints the complete logical image.

## 11. Implementation qualification

The initial T1106 failure/cleanup implementation and focused suite passed exact head:

```text
3a4c33e26a256408058bb68a13d20e6bc2500949
```

Workflow #501 (`34415613066`) passed Windows/Linux/macOS x64/ARM64 plus package validation.

The additional post-semantic-run cancellation coverage passed exact head:

```text
8aaeb5f5c188a1a42dd95f4097071e720e83f30e
```

Workflow #502 (`34415923464`) passed the same complete seven-job matrix.

## 12. Public API impact

T1106 adds no public type or member.

The provisional 1.1 compiled public contract remains:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

The new recovery/fault state is internal implementation policy.

## 13. Exit gate

T1106 is complete when one documentation-synchronized `1.1.0-alpha.6` head passes:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- package/fresh-consumer validation.

After that gate, T1107 may begin application-shaped performance, allocation, and semantic optimization acceptance.
