# T2006 Lifecycle and Recovery Gate

**Status:** accepted on exact executable head `5d25874aacb3235a25087ce442b4c7ed1fe04678`. T2007 may begin.

**Starting PR head:** `1996a033ee6683837d14f4a1ed0f523c7bb9e69b`, accepted T2005 gate. The first T2006 test slice was committed as `86044b8d6ed3ae01f211acbf1883113b7d31fe5d`; the suspend-reset witness followed at `5d25874aacb3235a25087ce442b4c7ed1fe04678`.

## Boundary and intended evidence

DCurses retains the desired screen, damage, and repaint policy. Terminal owns the transaction body, serialization, synchronized framing, cleanup, flush, output epoch, and raster identity. A failed commit cannot publish speculative physical state or mark captured damage clean. The caller must explicitly initiate recovery, and a lost raster token cannot be recreated by DCurses.

| Condition | Existing witness | T2006 witness |
|---|---|---|
| Partial write during ordinary text | `CursesOutputFailureHardeningTests.PartialRefreshFailureInvalidatesPhysicalStateForCompleteRetry` fails an output item after writing it in full | `PartialTextWriteRequiresExplicitCompleteRepaint` writes one byte of `AB`, throws, checks damage, complete explicit retry, and no-op afterward. |
| Flush failure after emitted body | `CursesRasterFailureAtomicityTests.FlushFailureAfterRasterCommitInvalidatesPhysicalStateForExplicitRetry` | `FlushFailureLeavesDeliveredBodyUncertainUntilExplicitRepaint` checks ordinary text, original exception identity, damage, full retry, and one flush. |
| Primary plus cleanup failure | `CursesOutputFailureHardeningTests.SynchronizedRefreshPreservesBodyAndEndFailures` and `CursesSemanticFailureHardeningTests.HyperlinkTextAndCleanupFailuresAreAggregatedWithoutDeferredRetryState` | Both primary and cleanup exceptions remain observable through Terminal's transaction; no new production cleanup path. |
| Suspend reset failure | `CursesOutputFailureHardeningTests.DisposalRestoresTerminalModeAfterRenditionResetFailure` covers disposal restoration | `FailedSuspendResetReleasesActivityGateForExplicitRepaint` checks exception identity, caller-driven repaint, and another suspend/resume cycle. |
| Stale/foreign/released raster | `CursesRasterLifecycleHardeningTests.CleanRetainedRasterThatBecomesStaleIsRejectedWithoutNewOutput`, `CursesRasterRefreshTests.RasterRefreshRejectsCellFromDifferentTerminalSessionBeforeOutput`, and `CursesRasterLifecycleTests` ownership mapping | `StaleRasterRequiresExplicitReplacementBeforeRepaint` checks that only a new caller-provided token permits output. `ReleasedRetainedRasterCannotBeReplayedAfterInvalidation` checks no output after loss of a retained token. |
| Suspend/resume, resize, disposal | `CursesLifecycleHardeningTests` covers resize storms, cancelled suspend, disposal while suspended, and repeated resume; `CursesSemanticFailureHardeningTests.SuspendResumeInvalidatesRetainedLinkedContentForRepaint` | `DisposalWaitsForEnteredRefreshBeforeRestoringSession` gates output and awaits both refresh and disposal. `ResizeDuringCommitPreservesLogicalContentForNextRefresh` checks a concurrent resize and repaint of preserved content. Mixed raster/text transaction ordering and session-output exclusion are exercised by `CursesRefreshSynchronizationHardeningTests`. |

## First exact-head CI result

[Pull-request workflow 35817295769](https://github.com/uniblab/Icod.DCurses/actions/runs/35817295769) passed package validation and all six runtime jobs on `86044b8`. Every runtime job passed **1,047 tests on each of `net8.0`, `net9.0`, and `net10.0`**, with zero failures and zero skips. The six new tests cover partial text writes, ordinary-text flush failure, stale raster replacement, released raster rejection, disposal during an entered refresh, and resize during commitment. No production code changed in this slice.

The existing synchronization witnesses also cover a blocked suspend while refresh owns the activity gate, concurrent session output waiting behind a mixed synchronized transaction, and input waits that do not block refresh. Existing raster failure witnesses cover a failed raster write, failed raster flush, and explicit retry. These witnesses exercise the manager, activity, and output gate ordering without taking raw output ownership into DCurses.

## Final exact-head verification

[Pull-request workflow 35817879433](https://github.com/uniblab/Icod.DCurses/actions/runs/35817879433) passed package validation and all six runtime jobs on exact head `5d25874aacb3235a25087ce442b4c7ed1fe04678`. Every successful runtime job passed **1,048 tests** on each target framework (`net8.0`, `net9.0`, `net10.0`) with zero failures and zero skips. No production code or public API changed in T2006.

The initial Windows x64 attempt had one failure on `net8.0`: the existing `CursesRichInputAcceptanceTests.RichInputFamiliesAndModifiedKeysUseSingleCursesEventStream` reached its five-second read timeout waiting for paste-begin after a successful focus event. The same test passed on the prior head's Windows x64 run, all other platform/framework jobs on this head, and the rerun of the single Windows x64 job on this **same commit** (1,048/1,048 on all three frameworks). No input or timeout code changed. This isolated timing result is recorded; a repeat requires separate diagnosis and may not be silently counted as a T2006 recovery pass.

The root `README.md`, `docs/Public-API-Fingerprint-2.0.json`, package/assembly versions, and direct dependencies match the accepted T2005 head. The temporary direct `Icod.TermInfo 1.15.0` reference remains explicitly assigned to T2007. No merge, tag, release, or publication is authorized by this gate.

## Acceptance decision

Partial writes, flush and cleanup failures preserve visible exceptions and logical intent; recovery requires a new caller-initiated refresh. Lost raster identity fails closed until the application provides a current token. Suspend failure and disposal/resize races leave the activity gate usable and the next refresh responsible for repaint. The retained concurrent input, mixed-output, synchronization, and lifecycle witnesses cover gate ordering. T2006 is accepted, and T2007 dependency removal may begin.
