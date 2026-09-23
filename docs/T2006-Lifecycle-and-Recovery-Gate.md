# T2006 Lifecycle and Recovery Gate

**Status:** pending executable verification and remaining lifecycle/raster qualification. T2007 is not authorized by this document.

**Starting PR head:** `1996a033ee6683837d14f4a1ed0f523c7bb9e69b`, accepted T2005 gate. The tests described below are local changes; no test result or new GitHub head is claimed.

## Boundary and intended evidence

DCurses retains the desired screen, damage, and repaint policy. Terminal owns the transaction body, serialization, synchronized framing, cleanup, flush, output epoch, and raster identity. A failed commit cannot publish speculative physical state or mark captured damage clean. The caller must explicitly initiate recovery, and a lost raster token cannot be recreated by DCurses.

| Condition | Existing witness | New local witness / remaining check |
|---|---|---|
| Partial write during ordinary text | `CursesOutputFailureHardeningTests.PartialRefreshFailureInvalidatesPhysicalStateForCompleteRetry` fails an output item after writing it in full | `PartialTextWriteRequiresExplicitCompleteRepaint` writes one byte of `AB`, throws, checks damage, complete explicit retry, and no-op afterward. CI pending. |
| Flush failure after emitted body | `CursesRasterFailureAtomicityTests.FlushFailureAfterRasterCommitInvalidatesPhysicalStateForExplicitRetry` | `FlushFailureLeavesDeliveredBodyUncertainUntilExplicitRepaint` checks ordinary text, original exception identity, damage, full retry, and one flush. CI pending. |
| Primary plus cleanup failure | `CursesOutputFailureHardeningTests.SynchronizedRefreshPreservesBodyAndEndFailures` and `CursesSemanticFailureHardeningTests.HyperlinkTextAndCleanupFailuresAreAggregatedWithoutDeferredRetryState` | Re-run with new tests; no new production cleanup path. |
| Stale/foreign/released raster | `CursesRasterLifecycleHardeningTests.CleanRetainedRasterThatBecomesStaleIsRejectedWithoutNewOutput`, `CursesRasterRefreshTests.RasterRefreshRejectsCellFromDifferentTerminalSessionBeforeOutput`, and `CursesRasterLifecycleTests` ownership mapping | `StaleRasterRequiresExplicitReplacementBeforeRepaint` checks that only a new caller-provided token permits output. `ReleasedRetainedRasterCannotBeReplayedAfterInvalidation` checks no output after loss of a retained token. CI pending. |
| Suspend/resume, resize, disposal | `CursesLifecycleHardeningTests` covers resize storms, cancelled suspend, disposal while suspended, and repeated resume; `CursesSemanticFailureHardeningTests.SuspendResumeInvalidatesRetainedLinkedContentForRepaint` | `DisposalWaitsForEnteredRefreshBeforeRestoringSession` gates output and awaits both refresh and disposal. `ResizeDuringCommitPreservesLogicalContentForNextRefresh` checks a concurrent resize and repaint of the preserved content. CI pending; mixed-media resize and manager/output gate ordering still need explicit review. |

## Pending acceptance commands

The local environment has no `dotnet` executable; the package endpoints needed to install it are inaccessible. The connected GitHub app's attempted upload was rejected by automatic approval review, so no PR CI run exists for these changes. After approved upload, run the `pull_request` workflow for an exact head: package validation and all six runtime jobs, each on `net8.0`, `net9.0`, and `net10.0`. Record exact commit SHA, workflow URL, test counts/failures/skips, and any corrections here before changing the roadmap status.

Review the final diff against the T2005 head. Verify that `README.md`, `docs/Public-API-Fingerprint-2.0.json`, package/assembly versions, and direct dependencies are unchanged. Do not remove the temporary TermInfo reference until T2007.
