# T2501 — Logical Color Retention

## Decision

The existing logical mutation implementation is sufficient. No production code or public API change was required.

All witnesses use styles whose foreground, background, and attributes are deliberately distinct, so losing or recombining either color axis cannot pass accidentally. The tests also validate every affected wide-cell surface with `CursesCellFootprint.Validate`.

## Permanent witnesses

`CursesColorSurfaceLogicalTests` covers:

- `WritesRetainCompleteStyle`: string, rune, text-element, tab, and current-style writes;
- `BackgroundOperationsUseCompleteBackgroundStyle`: fill, line clear, and scrolling synthesis;
- `CopyAndLayoutProjectionRetainCompleteStyle`: destructive copy and styled text-layout projection;
- `PreparedCellsRetainIndependentColorAxes`: bulk prepared-cell writes;
- `EqualCellRewriteDoesNotCreateNewRevision`: retained no-op equality;
- `WideLeaderAndContinuationCarryIdenticalStyle`: complete wide footprint creation;
- `OverwritingContinuationRepairsBothColumnsWithBackgroundStyle`: continuation-boundary repair;
- `ClippedWideCopyLeavesNoStaleColoredHalf`: composition-boundary normalization; and
- `EditingAcrossWideFootprintUsesCompleteBackgroundStyle`: editing normalization and vacated-cell synthesis.

## Failure classification

The first focused run reported 8 passes and 1 failure in `WritesRetainCompleteStyle`. The failing assertion inspected the next row after a tab from column three; tab expansion correctly occupied columns three through seven and did not wrap. This was a test-coordinate error, not a runtime style loss. The witness was corrected to inspect columns zero through eight after a subsequent current-style write. The focused suite then passed 9/9.

No operation lost foreground, background, or attributes. No orphaned continuation or stale colored wide-cell half was observed.

## Verification

- Focused net10.0 logical suite: 9/9 passed.
- Required logical and adjacent suites: 71/71 passed on each of net8.0, net9.0, and net10.0; 213 total target-specific tests.
- Adjacent classes: `CursesColorStyleTests`, `CursesWindowTests`, `CursesWindowRegionEditingTests`, `CursesWindowInsertDeleteTests`, `CursesWindowCompositionTests`, `CursesTextPresentationTests`, and `CursesVirtualScreenChangeTrackingTests`.

This execution environment contains only the .NET 10 runtime. Net8.0 and net9.0 target assemblies were executed with `DOTNET_ROLL_FORWARD=Major`; repository CI remains authoritative for native target-runtime and platform coverage.
