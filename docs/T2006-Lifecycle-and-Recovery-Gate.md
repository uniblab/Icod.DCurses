# T2006 Lifecycle and Recovery Gate

**Status:** pending executable verification and remaining lifecycle/raster qualification. T2007 is not authorized by this document.

**Starting PR head:** `1996a033ee6683837d14f4a1ed0f523c7bb9e69b`, accepted T2005 gate. The first T2006 test slice was committed as `86044b8d6ed3ae01f211acbf1883113b7d31fe5d`. The later suspend-reset witness in this document remains local and unverified; no T2006 acceptance is claimed.

## Boundary and intended evidence

DCurses retains the desired screen, damage, and repaint policy. Terminal owns the transaction body, serialization, synchronized framing, cleanup, flush, output epoch, and raster identity. A failed commit cannot publish speculative physical state or mark captured damage clean. The caller must explicitly initiate recovery, and a lost raster token cannot be recreated by DCurses.

| Condition | Existing witness | New local witness / remaining check |
|---|---|---|
| Partial write during ordinary text | `CursesOutputFailureHardeningTests.PartialRefreshFailureInvalidatesPhysicalStateForCompleteRetry` fails an output item after writing it in full | `PartialTextWriteRequiresExplicitCompleteRepaint` writes one byte of `AB`, throws, checks damage, complete explicit retry, and no-op afterward. CI pending. |
| Flush failure after emitted body | `CursesRasterFailureAtomicityTests.FlushFailureAfterRasterCommitInvalidatesPhysicalStateForExplicitRetry` | `FlushFailureLeavesDeliveredBodyUncertainUntilExplicitRepaint` checks ordinary text, original exception identity, damage, full retry, and one flush. CI pending. |
| Primary plus cleanup failure | `CursesOutputFailureHardeningTests.SynchronizedRefreshPreservesBodyAndEndFailures` and `CursesSemanticFailureHardeningTests.HyperlinkTextAndCleanupFailuresAreAggregatedWithoutDeferredRetryState` | Re-run with new tests; no new production cleanup path. |
| Suspend reset failure | `CursesOutputFailureHardeningTests.DisposalRestoresTerminalModeAfterRenditionResetFailure` covers disposal restoration | `FailedSuspendResetReleasesActivityGateForExplicitRepaint` checks exception identity, caller-driven repaint, and another suspend/resume cycle. CI pending. |
| Stale/foreign/released raster | `CursesRasterLifecycleHardeningTests.CleanRetainedRasterThatBecomesStaleIsRejectedWithoutNewOutput`, `CursesRasterRefreshTests.RasterRefreshRejectsCellFromDifferentTerminalSessionBeforeOutput`, and `CursesRasterLifecycleTests` ownership mapping | `StaleRasterRequiresExplicitReplacementBeforeRepaint` checks that only a new caller-provided token permits output. `ReleasedRetainedRasterCannotBeReplayedAfterInvalidation` checks no output after loss of a retained token. CI pending. |
| Suspend/resume, resize, disposal | `CursesLifecycleHardeningTests` covers resize storms, cancelled suspend, disposal while suspended, and repeated resume; `CursesSemanticFailureHardeningTests.SuspendResumeInvalidatesRetainedLinkedContentForRepaint` | `DisposalWaitsForEnteredRefreshBeforeRestoringSession` gates output and awaits both refresh and disposal. `ResizeDuringCommitPreservesLogicalContentForNextRefresh` checks a concurrent resize and repaint of the preserved content. CI pending; mixed-media resize and manager/output gate ordering still need explicit review. |

## First exact-head CI result

[Pull-request workflow 35817295769](https://github.com/uniblab/Icod.DCurses/actions/runs/35817295769) passed package validation and all six runtime jobs on `86044b8`. Every runtime job passed **1,047 tests on each of `net8.0`, `net9.0`, and `net10.0`**, with zero failures and zero skips. The six new tests cover partial text writes, ordinary-text flush failure, stale raster replacement, released raster rejection, disposal during an entered refresh, and resize during commitment. No production code changed in this slice.

The existing synchronization witnesses also cover a blocked suspend while refresh owns the activity gate, concurrent session output waiting behind a mixed synchronized transaction, and input waits that do not block refresh. Existing raster failure witnesses cover a failed raster write, failed raster flush, and explicit retry. These witnesses exercise the manager, activity, and output gate ordering without taking raw output ownership into DCurses.

## Pending acceptance checks

The local environment has no `dotnet` executable; the package endpoints needed to install it are inaccessible. The connected GitHub app's attempted upload of the **additional** suspend-reset witness and this updated gate document was rejected by automatic approval review because those changes exceeded the specifically approved patch. After explicit approval of that follow-up, run the `pull_request` workflow for its exact head: package validation and all six runtime jobs, each on `net8.0`, `net9.0`, and `net10.0`. Record exact commit SHA, workflow URL, test counts/failures/skips, and any corrections here before changing the roadmap status.

Review the final diff against the T2005 head. Verify that `README.md`, `docs/Public-API-Fingerprint-2.0.json`, package/assembly versions, and direct dependencies are unchanged. Do not remove the temporary TermInfo reference until T2007.
