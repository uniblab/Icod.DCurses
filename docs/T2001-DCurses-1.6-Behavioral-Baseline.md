# T2001 DCurses 1.6 Behavioral Migration Baseline

**Date:** 2026-09-18.\
**Source behavior:** `Icod.DCurses 1.6.0`.\
**Migration target:** `Icod.DCurses 2.0.0`.\
**Purpose:** freeze observable behavior before the renderer and dependency-boundary migration.

## Contract identity

The 2.0 migration begins from the frozen 1.6 package contract:

| Contract item | 1.6 baseline |
|---|---|
| Package version | `1.6.0` |
| Assembly version | `1.0.0.0` |
| Target frameworks | `net8.0`, `net9.0`, `net10.0` |
| Exported types | 75 |
| Canonical public-contract lines | 559 |
| Public API SHA-256 | `266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458` |
| Production dependencies | `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0` |

`docs/Public-API-Fingerprint-1.6.json`, `PublicApiFingerprintTests`, and
`PublicOneSixApiBaselineTests` are the machine-readable source of truth. The only
approved 2.0 public breaks are those frozen in `docs/2.0-API-Break-Manifest.md`.

## Representative parity witnesses

These existing tests are migration acceptance witnesses. T2003-T2009 must keep
their observable result, or document and review an intentional behavior change.

| Behavior family | Existing witness | Frozen invariant |
|---|---|---|
| Full repaint | `CursesOptimizationRegretTests.LargeFullRepaintHasDeterministicOutputCost` | A 160x60 cursor-only repaint emits 9,661 bytes in 121 writes and one flush. |
| Sparse repaint | `CursesOptimizationRegretTests.ThousandSmallUpdatesRemainDeterministicAndBounded` | 1,000 one-cell updates emit 2,000 bytes in 2,000 writes and 1,000 flushes. |
| No-op refresh | `CursesScaleHardeningTests.RepeatedNoOpRefreshDoesNotGrowTerminalWriteCount` | 512 unchanged refreshes add no writes; refresh still flushes once per call. |
| Cursor placement | `CursesCursorMotionIntegrationTests.KnownCursorUsesCheaperRelativeMotion`; `AbsoluteAddressRemainsFallback`; `EqualCostTieRetainsAbsoluteAddress` | Known-position relative motion wins only when strictly cheaper; absolute addressing is fallback and wins ties. |
| Rendition minimization | `CursesRenditionTransitionTests.AdditiveAttributeTransitionAvoidsResetAndColorRestore`; `NondefaultColorChangeAvoidsResetAndOriginalPairRestore`; `AttributeRemovalRetainsResetFirstSafety`; `LogicalStylesResolvingToSamePhysicalStyleDoNotRepeatReset` | Additive transitions avoid unnecessary reset, destructive transitions reset safely, and physically equivalent styles do not re-emit rendition. |
| ACS/Unicode fallback | `CursesSemanticLineRefreshTests.ConsecutiveSemanticLineCellsShareOneAcsRun`; `MissingAcsUsesCanonicalUnicodeLineContent`; `UnsafeUnicodeLineWidthUsesAsciiFallback` | Semantic line cells coalesce ACS state and degrade deterministically through Unicode to ASCII. |
| Erase selection | `CursesEraseIntegrationTests.ShortBlankTailUsesLiteralFallbackWhenEraseLineCostsMore`; `DefaultBlankScreenTailUsesEraseToEndOfScreenAndRetainsKnowledge`; `WholeDefaultBlankScreenUsesClearAndRetainsKnowledge` | Erase capabilities are selected only when cheaper and safe, while physical knowledge remains exact. |
| Character shifts | `CursesCharacterShiftRefreshTests.InsertCellsUsesPhysicalInsertCharactersAndRetainsExactRow`; `DeleteCellsUsesPhysicalDeleteCharactersAndRetainsExactRow`; `MissingCharacterShiftCapabilitiesFallsBackToOrdinaryRewrite` | Insert/delete character plans preserve the exact retained row; unavailable plans fall back to rewrite. |
| Line shifts | `CursesLineShiftRefreshTests.DeleteLinesUsesPhysicalDeleteLinesAndRetainsExactScreen`; `InteriorFullWidthWindowUsesTemporaryScrollRegionAndRestoresIt`; `PartialWidthWindowDoesNotClaimTerminalScrollRegionOwnership` | Safe line shifts preserve the exact screen, bound temporary scroll regions, and reject unsafe ownership claims. |
| Hyperlinks | `CursesHyperlinkRefreshTests.AdjacentEquivalentLinkedCellsUseOneBoundedHyperlinkRun`; `UnchangedSemanticContentProducesNoSecondPayloadWrite`; `InvalidationRepaintsLinkedContentThroughSemanticOutput` | Equivalent cells share one bounded OSC 8 run, unchanged links are silent, and invalidation repaints semantically. |
| Raster placeholders | `CursesRasterRefreshTests.FirstRefreshEmitsRasterInsteadOfFallbackTextAndUnchangedRefreshIsSilent`; `DirtyRasterCoordinateReemitsOnlyThatCoordinate`; `RasterEmissionForcesRenditionReassertionBeforeFollowingText` | Typed raster output replaces fallback text, remains sparse, is silent when clean, and invalidates rendition knowledge before text. |
| Synchronized mixed media | `CursesRasterSynchronizedRefreshTests.MixedRasterAndTextStayInsideOneSynchronizedRefreshTransaction` | One begin frame precedes raster and text; one end frame follows both. |
| Output failure | `CursesOutputFailureHardeningTests.PartialRefreshFailureInvalidatesPhysicalStateForCompleteRetry`; `SynchronizedRefreshPreservesBodyAndEndFailures` | Uncertain output invalidates physical knowledge; body and cleanup failures remain observable. |
| Lifecycle | `CursesLifecycleHardeningTests.ResizeStormSynchronizesLogicalScreenDimensions`; `RepeatedSuspendResumeCyclesAlwaysReleaseBlockedRefresh`; `CursesSemanticFailureHardeningTests.SuspendResumeInvalidatesRetainedLinkedContentForRepaint` | Resize converges, suspend blocks activity without leaking gates, resume forces safe repaint. |
| Panels | `CursesPanelApplicationAcceptanceTests.SparseVisibleEditKeepsDamageBounded`; `CursesPanelReleaseHardeningTests.DisposingLastLivePanelRestoresBaseAndReturnsToCleanNoPanelRefresh` | Sparse panel edits retain bounded damage; removing the final panel restores the base and converges to a clean refresh. |
| Pads/viewports | `CursesPadViewportTests.MultipleViewportsPanIndependentlyOverOnePad`; `CursesPadPresentationTests.PresentToNormalizesWideGlyphCutAtSourceBoundary`; `CursesSemanticPropagationTests.PadViewportObservesAndPresentsSemanticOnlyChange` | Viewports pan independently, repair wide-cell boundaries, and propagate semantic-only changes. |
| Large surfaces | `CursesScaleHardeningTests.LargePadSupportsRepeatedTwoAxisPanningWithStableFootprints`; `LargeScreenAndHighFrequencySparseRefreshRemainStable` | A 2,048x256 pad survives 256 two-axis pans with valid footprints; 1,000 sparse live refreshes remain stable. |

## Deterministic optimization costs

The following literal release gates are especially important while replacing raw
TermInfo expansion with Terminal operation plans:

| Workload | Optimized | Fallback | Required result |
|---|---:|---:|---|
| 32-column editor insertion | 2 bytes / 1 write | 34 bytes / 3 writes | Character-shift plan wins and final rows match. |
| Six-row pager deletion | 4 bytes / 3 writes | 166 bytes / 11 writes | Line-shift plan wins and final rows match. |
| 160x60 full repaint | 9,661 bytes / 121 writes / 1 flush | Not applicable | Output cost stays deterministic. |
| 1,000 one-cell refreshes | 2,000 bytes / 2,000 writes / 1,000 flushes | Not applicable | Per-refresh work remains bounded. |

The expected byte and write counts are asserted by `CursesOptimizationRegretTests`.
They are correctness gates, not timing benchmarks.

## Allocation and representation ceilings

The migration must preserve the current retained-data and steady-state ceilings:

- `CursesLayoutApplicationAcceptanceTests.RepeatedPureGeometryCalculationIsAllocationFree`
  measures zero bytes across 100,000 pure geometry calculations.
- `SteadyStateLayoutApplyAndCompositionStayWithinAllocationCeiling` permits at most
  262,144 bytes across 1,024 retained layout/composition iterations and ends clean.
- `CursesPanelApplicationAcceptanceTests.NoPanelPresenceCheckIsAllocationFree`
  measures zero bytes across 10,000 checks.
- `SteadyStateSparseCompositionReusesFrameWithinAllocationCeiling` permits at most
  131,072 bytes across 1,024 compositions and reuses the retained frame.
- `CursesRasterAllocationHardeningTests.OrdinaryTextMutationDoesNotMaterializeRasterStorageOrAllocatePerWrite`
  permits only the 1,024-byte measurement allowance across 10,000 mutations and
  leaves raster storage unmaterialized.
- `MaterializedSparseRasterReplacementStaysWithinOneSmallWrapperPerMutation`
  permits at most 128 bytes per mutation plus the 1,024-byte allowance.
- `CursesSemanticMetadataRepresentationBaselineTests.ReferencePadMaterializesOnlySemanticRowsAtAcceptedScale`
  freezes 4,608 reference slots, or 36 KiB on the qualified 64-bit runtime, for
  the reference sparse pad workload.

Timing measurements remain informational. No elapsed-time threshold is introduced
by T2001 because shared CI scheduling is not deterministic.

## Migration use

The Terminal-only renderer may change internal batching and therefore individual
low-level write boundaries. Any such change must preserve final bytes/order and
state semantics, and must meet or improve the deterministic cost gates above unless
the roadmap records a reviewed replacement metric. Failure, cancellation, cleanup,
stale-state and lifecycle tests remain strict safety contracts.

This baseline does not authorize T2002 by itself. T2001 is accepted only when the
published Terminal 1.18.0 readiness witnesses, this existing behavioral suite,
package validation, and the complete platform matrix are green on one exact head.
