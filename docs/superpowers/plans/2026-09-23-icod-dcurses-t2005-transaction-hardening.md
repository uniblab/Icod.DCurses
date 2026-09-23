# Icod.DCurses T2005 Transaction Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task in the current session. Do not delegate to subagents. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exhaustively qualify DCurses's Terminal-owned screen transaction boundary for capacity, cancellation, synchronization, ordering, stale epochs, lease conflicts, and prepare/commit/publish state safety, while keeping obsolete raw-output and capability-writer shims deleted.

**Architecture:** Keep the T2003/T2004 vertical cutover: DCurses prepares one ordered `CursesPreparedRefresh` over one `TerminalScreenOutputTransaction`, mutates only a detached speculative physical-state clone, commits once, and publishes the clone plus captured damage only after success. Terminal remains solely responsible for output serialization, synchronized framing, hyperlink cleanup, commitment-time cancellation policy, flush, and output epochs. T2005 adds adversarial witnesses and the smallest corrections exposed by those witnesses; it does not add a backend abstraction or split a refresh into multiple transactions.

**Tech Stack:** C# 13; .NET `net8.0`, `net9.0`, and `net10.0`; `Icod.Terminal 1.18.0`; xUnit; PowerShell 5.1-compatible packaging; cmd/sh. No Python.

**Spec:** `Icod.DCurses-2.0.0-Development-Roadmap.md`, especially sections 6-8 and T2005; `docs/superpowers/specs/2026-09-18-icod-dcurses-t2003-vertical-cutover-design.md`, especially T2005 acceptance.

**Execution status:** The first source-boundary slice is complete on PR head `5465b1bba685eafb078e4a7123835525b5c9f426`, qualified by workflow `35806253187`. Tasks 2-8 remain pending. No T2005 acceptance claim exists until the final exact-head matrix and gate document pass.

## Global Constraints

- Work inline in the current session. Do not delegate any task to a subagent.
- Keep `Version` and `PackageVersion` at `2.0.0-alpha.1` and `AssemblyVersion` at `2.0.0.0`.
- Keep direct dependencies at `Icod.Terminal 1.18.0` and temporary `Icod.TermInfo 1.15.0`. T2007 owns final direct package/reference removal and package-boundary proof.
- Production code must not reference `Icod.TermInfo`, `TerminalDescription`, capability identifiers, expansion, padding, `TermInfoOutput`, raw Terminal output, or direct flush APIs.
- Keep `src/Integration/TerminalOutputShim.cs`, `src/Internal/TerminalCapabilityWriter.cs`, and `src/Integration/ITerminalRasterPlaceholderOutput.cs` deleted.
- Do not expose `TerminalScreenOutputTransaction`, `ITerminalOutput`, or arbitrary control strings through a new DCurses production surface.
- Keep one Terminal screen transaction and one Terminal-owned flush for each non-empty successful refresh. A no-op refresh emits and flushes nothing. Never silently split an over-capacity refresh.
- Preparation emits no bytes. Publish speculative physical state and mark only the captured desired-screen revision clean after successful commitment.
- Once Terminal commitment begins, caller cancellation does not interrupt body output or required semantic cleanup. Cancellation before commitment emits nothing and leaves logical damage available for a fresh retry.
- A failed, stale, over-capacity, or lease-conflicting transaction leaves desired content intact and physical state conservatively unknown. Never replay a consumed transaction.
- Preserve the root `README.md` attribution/copyright/license text and `docs/Public-API-Fingerprint-2.0.json` exactly.
- Do not merge, tag, release, publish a package, or remove the temporary direct TermInfo package reference.

## Review Focus

1. Capacity failure must happen before output and must not mark damage clean; ordinary application-shaped workloads must remain below Terminal's 65,536-item and 64-MiB application-payload limits.
2. Cancellation before commitment and cancellation after commitment are intentionally different; tests must prove both without timing sleeps.
3. Terminal's output gate must keep the whole DCurses transaction contiguous with respect to other session output, including synchronized framing, hyperlink scopes, raster placeholders, cleanup, and one flush.
4. Desired-screen mutation or explicit invalidation while commitment is blocked must survive the first successful publication and force the correct next refresh.
5. Failures and ownership conflicts must retain logical content and invalidate physical certainty; successful direct cursor/alert/reset operations must use the same semantic transaction boundary.

---

### Task 1: Freeze the T2005 production boundary and remove obsolete output shims

**Files:**
- Delete: `src/Integration/TerminalOutputShim.cs`
- Delete: `src/Internal/TerminalCapabilityWriter.cs`
- Delete: `src/Integration/ITerminalRasterPlaceholderOutput.cs`
- Create: `tests/Icod.DCurses.Tests/src/T2005TransactionalBoundaryContractTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/ILegacyTerminalOutputFixture.cs`
- Modify: legacy tests that formerly implemented the deleted production test seam

- [x] **Step 1: Delete the three obsolete production seams**

The per-write output adapter, raw capability writer, and raster-placeholder output interface are no longer reachable after T2003/T2004. Delete them rather than retaining compatibility aliases.

- [x] **Step 2: Add the production-wide source contract**

`T2005TransactionalBoundaryContractTests.ProductionSourceContainsNoRawOutputOrTermInfoSeam` must enumerate every production `.cs` file and reject:

```text
Icod.TermInfo
TerminalDescription
StringCapability
TermInfoParameter
TermInfoOutput
TerminalCapabilityWriter
TerminalSessionCursesOutput
WriteTerminalStringAsync
ITerminalOutput
ITerminalHyperlinkOutput
ITerminalRasterPlaceholderOutput
.Output.FlushAsync
```

It must also assert that all three deleted files remain absent.

- [x] **Step 3: Record and correct the compile-RED witness**

The initial deletion exposed old tests still implementing production `ITerminalOutput`. Replace that testing convenience with the explicitly test-only `ILegacyTerminalOutputFixture`, and update semantic cancellation coverage to exercise `CursesPreparedRefresh`/the real Terminal transaction.

- [x] **Step 4: Verify the corrected boundary**

Workflow `35806253187` passed package validation and all six runtime jobs; Linux x64 passed 1,015 tests for each target framework with zero failures and zero skips.

**Commit history:** `28566e2335a2d0f3840efa6d806f2e88386a468f` (deletion/RED) and `5465b1bba685eafb078e4a7123835525b5c9f426` (test-only fixture correction/GREEN).

---

### Task 2: Qualify prepared-refresh capacity and output epochs

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesPreparedRefreshHardeningTests.cs`
- Modify only if a RED witness requires it: `src/Internal/CursesPreparedRefresh.cs`

**Interfaces:**
- Consumes: `TerminalSession.CreateScreenOutputTransaction`, `TerminalScreenOutputTransaction.WriteText`, and `CommitAsync` through `CursesPreparedRefresh`.
- Produces: proof that DCurses preserves Terminal's item/payload bounds, single-use rule, and stale-epoch rejection without exposing the underlying transaction.

- [ ] **Step 1: Write the item-limit RED witness**

Add `ItemLimitRejectsTheNextPreparedItemBeforeOutput`. Add exactly 65,536 empty text items through `CursesPreparedRefresh.WriteText`, assert that the next addition throws `InvalidOperationException` with Terminal's item-limit message, and assert zero output and zero flushes. Empty text keeps the witness focused on retained-item count rather than payload size.

- [ ] **Step 2: Write the payload-limit RED witness**

Add `PayloadLimitRejectsTheNextPreparedByteBeforeOutput`. Write exactly 64 MiB of UTF-8 application payload through `CursesPreparedRefresh`, assert that one further ASCII byte is rejected, and assert zero output and zero flushes. Allocate the boundary payload once; do not copy it into assertion diagnostics.

- [ ] **Step 3: Write the stale-epoch RED witness**

Create a prepared refresh containing `"stale"`, then perform an intervening session-owned semantic text write. `prepared.CommitAsync()` must throw Terminal's stale-transaction exception without emitting the prepared body or adding a flush. A fresh `CursesPreparedRefresh` must then commit successfully.

- [ ] **Step 4: Run the focused tests**

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~CursesPreparedRefreshHardeningTests
```

Expected before any correction: the tests should pass if the wrapper is transparent. Any failure is a boundary defect; correct the wrapper with the smallest semantic delegation change and never duplicate Terminal's counters or epoch logic in DCurses.

- [ ] **Step 5: Commit the task**

```sh
git add src/Internal/CursesPreparedRefresh.cs \
  tests/Icod.DCurses.Tests/src/CursesPreparedRefreshHardeningTests.cs
git commit -m "test: harden prepared refresh boundaries"
```

---

### Task 3: Prove cancellation phases, cleanup, and one-flush commitment

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/CursesSemanticCancellationHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesPreparedRefreshTests.cs`
- Modify only if a RED witness requires it: `src/Internal/CursesPreparedRefresh.cs`
- Modify only if a RED witness requires it: `src/Internal/CursesRefreshEngine.cs`

- [ ] **Step 1: Add the pre-commit cancellation witness**

Create a dirty screen and call `CursesRefreshEngine.RefreshAsync` with an already-cancelled token. Assert `OperationCanceledException`, zero writes, zero flushes, and no synchronized-output begin frame. Retry with a fresh token and assert that the complete dirty content is emitted once.

- [ ] **Step 2: Strengthen the post-commit cancellation witness**

Retain `CancellationAfterLinkedRunDoesNotInterruptEnteredTransaction`, but record exact ordering and flush count. The output fixture cancels the caller token after linked body output begins. Assert that the trailing ordinary text, hyperlink close, synchronized-output end when enabled, and one flush still occur. A subsequent unchanged refresh must be a no-op.

- [ ] **Step 3: Prove cancellation does not make a transaction reusable**

For a pre-cancelled `CursesPreparedRefresh.CommitAsync`, assert no output and then assert the same prepared transaction cannot be committed again. Build a fresh prepared refresh for recovery. DCurses must not hide Terminal's single-use contract.

- [ ] **Step 4: Run focused cancellation and prepared-refresh tests**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter "FullyQualifiedName~CursesSemanticCancellationHardeningTests|FullyQualifiedName~CursesPreparedRefreshTests"
```

- [ ] **Step 5: Implement only evidence-driven corrections**

Keep `cancellationToken.ThrowIfCancellationRequested()` before gate acquisition and pass the token into Terminal commitment. Do not add cancellation checks between prepared items, and do not implement cleanup or flush in DCurses.

- [ ] **Step 6: Commit the task**

```sh
git add src/Internal/CursesPreparedRefresh.cs \
  src/Internal/CursesRefreshEngine.cs \
  tests/Icod.DCurses.Tests/src/CursesPreparedRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesSemanticCancellationHardeningTests.cs
git commit -m "test: qualify refresh cancellation phases"
```

---

### Task 4: Prove publication and invalidation races

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesRefreshPublicationHardeningTests.cs`
- Modify only if a RED witness requires it: `src/Internal/CursesRefreshEngine.cs`
- Modify only if a RED witness requires it: `src/CursesVirtualScreen.cs`

**Test fixture:** Add a private `BlockingTerminalOutput` using run-continuations-asynchronously `TaskCompletionSource` instances for `FirstWriteStarted` and `ReleaseFirstWrite`. Do not use sleeps or elapsed-time assertions.

- [ ] **Step 1: Block a refresh during Terminal commitment**

Start a refresh for cell A, await `FirstWriteStarted`, and assert the refresh task remains incomplete. While blocked, mutate cell B and capture its revision. Release output and await the first refresh.

- [ ] **Step 2: Prove captured-revision publication**

After the first commit, assert cell A is clean but cell B remains dirty with a revision greater than the first captured revision. A second refresh must emit B; a third unchanged refresh must emit and flush nothing.

- [ ] **Step 3: Prove explicit invalidation survives commitment**

Block a refresh, call `engine.Invalidate()` while commitment is in progress, release it, and then refresh again. The second refresh must repaint from unknown physical state instead of treating the first speculative snapshot as authoritative.

- [ ] **Step 4: Prove blocked failure publishes nothing**

Configure the blocking output to throw after release. Assert that the original exception remains observable and the next successful refresh emits the complete desired screen, not merely cells changed after the failed attempt.

- [ ] **Step 5: Apply the smallest state-machine correction if RED**

The required publication order is:

```text
prepare detached state
commit one Terminal transaction
clear completed scroll-region recovery, if any
publish speculative physical state
mark desired cells clean through captured revision
```

If invalidation arrives after the pre-refresh exchange but before publication, publication must not erase that pending invalidation. Do not hold a lock across application mutation.

- [ ] **Step 6: Run and commit**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~CursesRefreshPublicationHardeningTests
git add src/Internal/CursesRefreshEngine.cs src/CursesVirtualScreen.cs \
  tests/Icod.DCurses.Tests/src/CursesRefreshPublicationHardeningTests.cs
git commit -m "test: harden refresh publication races"
```

---

### Task 5: Prove lease conflicts and whole-batch output serialization

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesRefreshSynchronizationHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesTerminalIntegrationTests.cs`
- Modify only if a RED witness requires it: `src/Internal/CursesPreparedRefresh.cs`
- Modify only if a RED witness requires it: `src/Internal/CursesRefreshEngine.cs`

- [ ] **Step 1: Freeze synchronized-output conflict behavior**

Promote the existing outer-lease scenario into exact T2005 assertions: an active `TerminalSynchronizedOutputLease` causes a framed DCurses refresh to fail before any DCurses body byte; desired damage remains; release of the outer lease performs only its own close/flush; a fresh refresh then succeeds.

- [ ] **Step 2: Add the hyperlink-lease conflict witness**

Acquire `TerminalHyperlinkLease` with `TerminalSession.AcquireHyperlinkAsync`, prepare a DCurses refresh containing a hyperlink, and assert commitment rejects the ownership conflict before the DCurses body. Dispose the outer lease, retry from fresh preparation, and assert the complete hyperlink body and close frame appear exactly once.

- [ ] **Step 3: Add the no-interleaving witness**

Block the first DCurses transaction write. While blocked, start `TerminalSession.WriteTextAsync("outside")`. Assert the outside task does not complete and `"outside"` is absent. Release the DCurses write, await both operations, and assert all DCurses begin/body/hyperlink/raster/end output is contiguous before `"outside"`.

- [ ] **Step 4: Assert exact flush ownership**

For a successful mixed semantic refresh, assert one synchronized begin, ordered plan/text/hyperlink/raster content, one synchronized end, and one flush. For a no-op refresh, assert no new bytes and no new flush. DCurses must not call a second flush.

- [ ] **Step 5: Run and commit**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter "FullyQualifiedName~CursesRefreshSynchronizationHardeningTests|FullyQualifiedName~CursesTerminalIntegrationTests"
git add src/Internal/CursesPreparedRefresh.cs src/Internal/CursesRefreshEngine.cs \
  tests/Icod.DCurses.Tests/src/CursesRefreshSynchronizationHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/CursesTerminalIntegrationTests.cs
git commit -m "test: harden refresh synchronization"
```

---

### Task 6: Qualify direct cursor, alert, and reset operations

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesDirectSemanticOperationHardeningTests.cs`
- Modify only if a RED witness requires it: `src/Internal/CursesRefreshEngine.cs`
- Modify only if a RED witness requires it: `src/Integration/CursesSession.Presentation.Terminal.cs`
- Modify only if a RED witness requires it: `src/Integration/CursesSession.Refresh.Terminal.cs`

- [ ] **Step 1: Test successful direct operations**

Exercise `SetCursorPositionAsync`, alert planning/commit, and rendition reset through public `CursesSession` operations. Assert each non-empty operation commits through Terminal framing policy and exactly one flush, and that cursor/rendition physical state is published only after success.

- [ ] **Step 2: Test pre-cancellation**

For each direct operation, use an already-cancelled token and assert no output, no flush, and no physical-state publication. A fresh retry must succeed.

- [ ] **Step 3: Test failure invalidation**

Inject a write or flush failure into each operation family. Assert the failure remains observable. The next screen refresh must recover from unknown physical state and re-establish cursor/rendition rather than trusting the failed direct operation.

- [ ] **Step 4: Keep one internal semantic route**

`CommitPlanAsync`, `SetCursorPositionAsync`, and `ResetRenditionAsync` may share a private helper only if tests demonstrate equivalent state publication. Do not create a public backend or a raw-plan execution API.

- [ ] **Step 5: Run and commit**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~CursesDirectSemanticOperationHardeningTests
git add src/Internal/CursesRefreshEngine.cs \
  src/Integration/CursesSession.Presentation.Terminal.cs \
  src/Integration/CursesSession.Refresh.Terminal.cs \
  tests/Icod.DCurses.Tests/src/CursesDirectSemanticOperationHardeningTests.cs
git commit -m "test: qualify direct semantic operations"
```

---

### Task 7: Prove realistic workloads fit and adversarial overflow is failure-atomic

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/CursesScaleHardeningTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTransactionCapacityIntegrationTests.cs`
- Modify only if a RED witness requires coalescing: `src/Internal/CursesRefreshEngine.cs`

- [ ] **Step 1: Add application-shaped mixed workloads**

Cover a 160x60 full repaint and sparse updates containing ordinary text, alternating styles, strict hyperlinks, and retained raster placeholders. Include the five already-approved application shapes: roguelike, full-screen editor, pixel-art HUD, tile-based map, and sprite/HUD scene. Assert every non-empty refresh uses one flush and every unchanged refresh uses zero.

- [ ] **Step 2: Add an adversarial item-overflow witness**

Construct a large screen whose alternating style/metadata pattern defeats ordinary run coalescing and exceeds Terminal's 65,536 retained-item bound during preparation. Assert `InvalidOperationException`, zero bytes, zero flushes, and all desired damage retained. Simplify the same desired screen to a coalescible representation and prove a fresh retry succeeds.

- [ ] **Step 3: Record capacity disposition**

If any realistic workload exceeds a bound, stop T2005 and document an upstream Terminal blocker; do not split the refresh, raise constants locally, or fall back to raw output. If only the deliberate adversarial case exceeds the bound, record that failure-atomic rejection is the accepted behavior.

- [ ] **Step 4: Run and commit**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter "FullyQualifiedName~CursesScaleHardeningTests|FullyQualifiedName~CursesTransactionCapacityIntegrationTests"
git add src/Internal/CursesRefreshEngine.cs \
  tests/Icod.DCurses.Tests/src/CursesScaleHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/CursesTransactionCapacityIntegrationTests.cs
git commit -m "test: qualify refresh transaction capacity"
```

---

### Task 8: Run the T2005 gate and publish exact evidence

**Files:**
- Create: `docs/T2005-Transactional-Refresh-Hardening-Gate.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Modify: `Icod.DCurses-2.0.0-Development-Roadmap.md`
- Modify: PR #32 body after exact-head qualification

- [ ] **Step 1: Run static and protected-file checks**

```sh
git diff --check
rg -n "Icod\.TermInfo|TerminalDescription|StringCapability|TermInfoParameter|TermInfoOutput|TerminalCapabilityWriter|TerminalSessionCursesOutput|WriteTerminalStringAsync|ITerminalOutput|ITerminalHyperlinkOutput|ITerminalRasterPlaceholderOutput|\.Output\.FlushAsync" src
git diff --exit-code 5465b1bba685eafb078e4a7123835525b5c9f426 -- README.md
git diff --exit-code 5465b1bba685eafb078e4a7123835525b5c9f426 -- docs/Public-API-Fingerprint-2.0.json
```

Expected: `rg` returns no production matches; both protected-file comparisons are empty. The temporary `Icod.TermInfo` package reference remains explicitly allowed until T2007.

- [ ] **Step 2: Run targeted T2005 tests**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter "FullyQualifiedName~T2005|FullyQualifiedName~CursesPreparedRefresh|FullyQualifiedName~CursesSemanticCancellationHardeningTests|FullyQualifiedName~CursesRefreshPublicationHardeningTests|FullyQualifiedName~CursesRefreshSynchronizationHardeningTests|FullyQualifiedName~CursesDirectSemanticOperationHardeningTests|FullyQualifiedName~CursesTransactionCapacityIntegrationTests"
```

- [ ] **Step 3: Run the full local qualification available to the executor**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Release
pwsh -NoProfile -File ./packaging/Invoke-Build.ps1 -Section validate -Configuration Staging
```

If no local .NET SDK is available, record that limitation and use exact-head GitHub Actions as the executable authority; do not claim local tests ran.

- [ ] **Step 4: Push and qualify the exact PR head**

Require package validation plus all six runtime jobs on `net8.0`, `net9.0`, and `net10.0`, with zero failures and zero skips. Inspect failed logs before changing code. Do not accept a superseded green workflow.

- [ ] **Step 5: Write the gate document**

Record:

- exact executable commit, workflow, and job links;
- per-framework test totals;
- item/payload boundary evidence;
- pre-/post-commit cancellation evidence;
- whole-batch ordering, one-flush, stale-epoch, and lease-conflict evidence;
- publication/invalidation race evidence;
- direct cursor/alert/reset semantic-boundary evidence;
- realistic workload and adversarial overflow disposition;
- deleted-shim and production source-boundary evidence;
- unchanged API identity and protected files;
- remaining T2006 lifecycle/failure work and T2007 direct TermInfo package/reference removal.

- [ ] **Step 6: Update roadmaps only after all criteria pass**

Mark T2005 accepted and T2006 next in both roadmaps. Do not mark T2007 dependency removal complete. Update PR #32 with the accepted executable head and exact workflow.

- [ ] **Step 7: Verify final documentation head**

Push the gate/roadmap commit and require the same seven-job matrix on that exact head. A documentation-only commit does not waive exact-head verification for tranche acceptance.

- [ ] **Step 8: Stop at the T2005 boundary**

Report the accepted head, workflow, test totals, remaining debt, and next T2006 checkpoint. Do not merge, tag, release, publish, remove the TermInfo package reference, or begin T2006 without maintainer approval.

## Completion Criteria

T2005 is complete only when all of the following are true:

- [ ] Every non-empty refresh and direct cursor/alert/reset operation uses one Terminal screen transaction; no parallel per-write production path exists.
- [ ] Text, opaque operation plans, strict hyperlinks, and raster placeholders preserve exact semantic order within one serialized commitment.
- [ ] Synchronized framing, hyperlink cleanup, cancellation-after-commit behavior, output serialization, and flush are Terminal-owned.
- [ ] Item/payload overflow, stale epochs, active lease conflicts, and pre-commit cancellation emit no DCurses transaction bytes and retain logical damage.
- [ ] Failure after commitment begins remains observable, invalidates physical certainty, attempts Terminal-owned cleanup, and permits complete recovery from desired state.
- [ ] Physical-state publication and `MarkCleanThrough(capturedRevision)` occur only after success; later mutation and invalidation survive commitment.
- [ ] Realistic roguelike, editor, pixel-art, tile-based, and sprite/HUD workloads fit one bounded transaction; deliberate overflow fails atomically.
- [ ] The obsolete raw-output/capability-writer/raster seams remain deleted, and production source contains no direct TermInfo or raw-output token.
- [ ] The approved 2.0 API fingerprint, version identities, dependencies, root attribution/license, and PowerShell 5.1 compatibility remain unchanged.
- [ ] Exact-head package validation and all six runtime jobs pass before T2005 is marked accepted.
