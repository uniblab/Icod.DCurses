# Icod.DCurses 2.5 Color-Surface Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Qualify and harden the complete independent foreground/background color path that Icod.Widgets and dedit will rely on.

**Architecture:** Preserve `CursesColor` → `CursesStyle` → `CursesCell` as the only DCurses color representation. Prove complete-style retention through logical mutation, pads/viewports, panels, damage and refresh; fix only witnessed defects; retain Terminal-owned normalization/encoding and capability-driven caller fallback.

**Tech Stack:** C# 13; net8.0/net9.0/net10.0; Icod.Terminal 1.29.0; xUnit; existing cmd/sh/PowerShell 5.1 build and package-verification tooling.

**Spec:** [2.5 color-surface design](../specs/2026-10-12-icod-dcurses-2.5-color-surface-hardening-design.md). See also the [versioned development roadmap](../../Icod.DCurses-2.5.0-Development-Roadmap.md).

## Global Constraints

- Baseline is published Icod.DCurses 2.4.0 with direct stable Icod.Terminal 1.29.0 and no direct production Icod.TermInfo reference.
- Begin implementation at `Version` and `PackageVersion` `2.5.0-alpha.1`; retain `AssemblyVersion 2.0.0.0`.
- Preserve C# `13.0`, `net8.0;net9.0;net10.0`, AnyCPU where supported, and Windows/Linux/macOS qualification.
- Library code remains `LGPL-3.0-or-later`; samples/tests remain `GPL-3.0-or-later` with the prescribed 2026 Timothy J. Bruce notices.
- Use C#, cmd, sh, and PowerShell 5.1-compatible tooling only; do not introduce Python or native C/C++ tooling.
- Preserve `CursesColor`, `CursesStyle`, `CursesCell`, `CursesPresentationCapabilities`, and existing panel-transparency meanings.
- Unsupported colors continue to degrade to terminal default; do not add hidden quantization, terminal-name detection, raw escape output, or direct TermInfo access.
- Add public API only after a permanent failing public-consumer witness, compatibility analysis, design amendment, and maintainer approval.
- Application owns palette/repaint policy; Widgets owns theme/state/part resolution; Terminal owns normalization, physical plans, and encoding.
- Maintainer alone merges, tags, and publishes. CI does not substitute for live visual terminal evidence.

## Review Focus

1. **Styled blank on a blank-transparent panel:** remain transparent for 2.4 compatibility; an opaque panel must preserve its colored background (Task 3).
2. **Width-two glyph partially overwritten or clipped:** both cells must be repaired with the owning complete background style, with no stale colored continuation (Task 2).
3. **Background-only change with identical content:** mark damage and emit the required rendition transition; an equal rewrite remains quiet (Task 4).
4. **RGB requested on an indexed-only profile or unsafe direct-color value:** degrade to terminal default without throwing, guessing, or corrupting the other color axis (Task 5).
5. **Resize/invalidate/resume after a multicolor frame:** preserve requested logical styles where specified, default new cells, and repaint without stale physical color (Task 5).

---

## File map

| Path | Responsibility |
|---|---|
| `Icod.DCurses.csproj` | 2.5 preview/RC/stable identity and unchanged Terminal 1.29.0 dependency |
| `src/CursesColor.cs`, `src/CursesStyle.cs`, `src/CursesCell.cs` | Existing semantic color/style/cell values; modify only for a demonstrated contract defect |
| `src/CursesWindow.cs`, `src/CursesWindow.Editing.cs`, `src/CursesWindow.BulkCells.cs`, `src/CursesWindow.Composition.cs`, `src/CursesWindow.TextLayout.cs` | Logical writes, background synthesis, editing, prepared cells, copying and layout projection |
| `src/CursesPad*.cs`, `src/CursesPanel*.cs`, `src/Internal/CursesPanel*.cs`, `src/CursesScreen.cs` | Retained projection, composition, transparency, movement and resize |
| `src/CursesVirtualScreen.cs`, `src/Internal/CursesPhysicalScreenState.cs`, `src/Internal/CursesRefresh*.cs` | Complete-style equality, damage and physical-state certainty |
| `src/Internal/CursesTerminalScreenMapper.cs`, `src/Internal/CursesPresentationResolver.cs`, `src/CursesPresentationCapabilities.cs` | Terminal-owned mapping, capability projection and controlled degradation |
| `tests/Icod.DCurses.Tests/src/CursesColorSurfaceLogicalTests.cs` | T2501 logical mutation matrix |
| `tests/Icod.DCurses.Tests/src/CursesColorSurfaceCompositionTests.cs` | T2502 pads/panels/transparency matrix |
| `tests/Icod.DCurses.Tests/src/CursesColorSurfaceRefreshTests.cs` | T2503 damage/physical-output matrix |
| `tests/Icod.DCurses.Tests/src/CursesColorSurfaceLifecycleTests.cs` | T2504 capability/resize/invalidation/lifecycle matrix |
| `samples/Icod.DCurses.Color.Sample/` | Public-only live color witness |
| `tools/package-smoke/Program.cs` | NuGet-only public color-surface consumer witness |
| `docs/T2500-*.md` through `docs/T2507-*.md` | Exact tranche decisions, measurements, limitations and commits |
| `README.md`, `samples/README.md`, `CHANGELOG.md`, `docs/Public-API-*2.5*` | Durable usage, implementation history and frozen package/API evidence |

Historical 2.4 fingerprints, gate records, and release notes remain immutable. Its roadmap release-status summary may record the completed merge, tag, and publication.

### Task 1 — T2500: Baseline, contract inventory and development identity

**Files:** Modify `Icod.DCurses.csproj`; create `tests/Icod.DCurses.Tests/src/PublicTwoFiveDevelopmentIdentityTests.cs`; create `docs/T2500-Color-Surface-Baseline-and-Identity.md`.

**Interfaces:** Produces the exact 2.5 development identity and frozen 2.4 color-surface inventory used by every later task. No color API is added.

- [ ] **Step 1: Write `PreviewIdentityPreservesColorFoundationAndTerminalBoundary`**

  Assert project Version/PackageVersion `2.5.0-alpha.1`, AssemblyVersion `2.0.0.0`, one production `Icod.Terminal` PackageReference at `1.29.0`, no direct production TermInfo reference, and the existing public signatures for `CursesColor`, `CursesStyle`, `CursesCell`, `CursesPresentationCapabilities`, and `CursesPanelTransparency`.

- [ ] **Step 2: Run the focused test against 2.4 and record the expected RED identity mismatch**

  Run: `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~PublicTwoFiveDevelopmentIdentityTests`

  Expected: FAIL only because Version/PackageVersion remain `2.4.0`; public/dependency assertions pass.

- [ ] **Step 3: Advance development identity only**

  Set `<Version>` and `<PackageVersion>` to `2.5.0-alpha.1`. Do not alter AssemblyVersion, target frameworks, dependency versions, package description/release notes, or public runtime code.

- [ ] **Step 4: Inventory existing coverage and record permanent gaps**

  In T2500, map each spec invariant to an existing test or one of the new T2501–T2504 files. Record current public API fingerprint/count/hash, exact baseline commit/tag, dependency graph, and any tests that already prove the behavior. Do not label an untested code inspection as accepted behavior.

- [ ] **Step 5: Verify identity and baseline**

  Run the focused identity test, `dotnet build Icod.DCurses.sln -c Staging`, and the complete net10.0 test project. Expect zero warnings/errors and no unexpected baseline failures.

- [ ] **Step 6: Commit**

  Commit `build: start 2.5 color-surface hardening` with T2500 exact evidence.

### Task 2 — T2501: Complete-style logical mutation and wide-cell repair

**Files:** Create `tests/Icod.DCurses.Tests/src/CursesColorSurfaceLogicalTests.cs`; modify logical source files from the file map only where a new witness fails; create `docs/T2501-Logical-Color-Retention.md`.

**Interfaces:** Consumes existing `CursesStyle`/`CursesCell`. Produces the invariant that logical mutations retain or synthesize one exact complete style without changing cursor or footprint semantics.

- [ ] **Step 1: Write the parameterized logical-retention tests**

  Add tests named `WritesRetainCompleteStyle`, `BackgroundOperationsUseCompleteBackgroundStyle`, `CopyAndLayoutProjectionRetainCompleteStyle`, `PreparedCellsRetainIndependentColorAxes`, and `EqualCellRewriteDoesNotCreateNewRevision`. Use styles with distinct indexed/RGB foreground, background, and attributes so axis loss cannot pass accidentally.

- [ ] **Step 2: Write the wide-cell review-focus tests**

  Add `WideLeaderAndContinuationCarryIdenticalStyle`, `OverwritingContinuationRepairsBothColumnsWithBackgroundStyle`, `ClippedWideCopyLeavesNoStaleColoredHalf`, and `EditingAcrossWideFootprintUsesCompleteBackgroundStyle`. Validate `CursesCellFootprint` after every mutation.

- [ ] **Step 3: Run the focused logical suite and classify each failure**

  Run: `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~CursesColorSurfaceLogicalTests`

  For a passing family, record that existing implementation is sufficient. For a failure, name the exact operation and lost style component in T2501 before editing production code.

- [ ] **Step 4: Implement the smallest witnessed corrections**

  Preserve existing signatures. Fix only the mutation path proven faulty; use the complete source cell or `BackgroundCell`, never reconstruct a partial style. Do not conflate a blank glyph with default styling.

- [ ] **Step 5: Verify focused and adjacent regressions**

  Run the new logical suite plus `CursesColorStyleTests`, `CursesWindowTests`, `CursesWindowRegionEditingTests`, `CursesWindowInsertDeleteTests`, `CursesWindowCompositionTests`, `CursesTextPresentationTests`, and `CursesVirtualScreenChangeTrackingTests` on net8.0/net9.0/net10.0.

- [ ] **Step 6: Commit**

  Commit `test: harden logical color retention` if no runtime fix was needed, or `fix: preserve complete styles through logical mutation` when a production correction was required. Record exact results in T2501.

### Task 3 — T2502: Pads, panels, colored blanks and transparency

**Files:** Create `tests/Icod.DCurses.Tests/src/CursesColorSurfaceCompositionTests.cs`; modify pad/panel/compositor files only for witnessed defects; create `docs/T2502-Color-Composition-and-Transparency-Decision.md`.

**Interfaces:** Consumes T2501 complete-style cell invariants. Produces a frozen composition rule for opaque and blank-transparent surfaces, including movement and resize cleanup.

- [ ] **Step 1: Write pad and viewport style-retention tests**

  Add `PadViewportRetainsCompleteStylesAcrossProjection`, `RepeatedViewportProjectionDoesNotLoseBackground`, and `ViewportBoundaryRepairsWideStyleFootprint`.

- [ ] **Step 2: Write opaque-panel tests**

  Add `OpaquePanelStyledBlankCoversLowerCell`, `OpaquePanelMoveRestoresLowerStyle`, `OpaquePanelHideAndShowRestoresCorrectStyles`, and `PanelResizeLeavesNoStaleColoredCoordinates`.

- [ ] **Step 3: Freeze published transparent-panel behavior**

  Add `BlankTransparentPanelStyledBlankRemainsTransparent`, `BlankTransparentPanelMetadataOnlyBlankRemainsTransparent`, `BlankTransparentPanelRasterBlankRemainsOpaque`, and a width-two boundary test. These assertions intentionally preserve 2.4 semantics.

- [ ] **Step 4: Run the focused composition suite before implementation**

  Run: `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~CursesColorSurfaceCompositionTests`

  Record whether an actual defect exists. If a widget requirement cannot be expressed with `Opaque`, stop and submit a design amendment; do not add an enum value during this task without approval.

- [ ] **Step 5: Implement only demonstrated internal corrections**

  Preserve the public transparency enum and style-independent `BlankCellsTransparent` rule. Correct copying, repair, dirtying, or old-bounds restoration only where a failing test identifies a defect.

- [ ] **Step 6: Verify adjacent panel/pad suites and all target frameworks**

  Run new tests plus existing `CursesPad*`, `CursesPanel*`, `CursesWindowCompositionTests`, and `CursesWindowDamageRangeTests` for net8.0/net9.0/net10.0.

- [ ] **Step 7: Commit**

  Commit `test: freeze colored surface composition` or `fix: preserve colored panel composition`, and record the API-gap decision in T2502.

### Task 4 — T2503: Damage, physical state and rendition transitions

**Files:** Create `tests/Icod.DCurses.Tests/src/CursesColorSurfaceRefreshTests.cs`; modify `CursesVirtualScreen`, physical-state, refresh, mapper, or resolver files only for witnessed defects; create `docs/T2503-Color-Damage-and-Refresh.md`.

**Interfaces:** Consumes complete logical/composed styles. Produces deterministic style-only damage and Terminal-owned transition behavior without new protocol output.

- [ ] **Step 1: Write revision and dirty-state tests**

  Add `ForegroundOnlyChangeMarksCellDirty`, `BackgroundOnlyChangeMarksCellDirty`, `AttributeOnlyChangeMarksCellDirty`, and `EqualCompleteStyleRewriteRemainsClean`.

- [ ] **Step 2: Write physical refresh tests**

  Add `ForegroundOnlyChangeProducesRenditionTransition`, `BackgroundOnlyChangeProducesRenditionTransition`, `ColorPairChangePreservesContentAndMetadata`, `EqualMulticolorFrameProducesNoCellOutput`, and `InvalidatedMulticolorFrameReemitsCompleteStyles` using the existing recording Terminal fixtures.

- [ ] **Step 3: Write mapper/transition-order tests**

  Add `MapperPreservesIndependentForegroundAndBackground`, `ResetRestoresDefaultColorsBeforeLaterDefaultText`, and `ReverseRemainsAttributeWithoutStoredColorSwap`.

- [ ] **Step 4: Run the focused refresh suite and capture output deltas**

  Run: `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~CursesColorSurfaceRefreshTests`

  Expected: exact logical transitions; no assertion should depend on an escape byte owned outside the Terminal screen-plan fixtures.

- [ ] **Step 5: Implement minimal corrections and re-run refresh regressions**

  Fix comparison, physical certainty, plan sequencing, or mapping only where a witness fails. Run `CursesVirtualScreenChangeTrackingTests`, `CursesRefreshPhysicalStateTests`, `CursesRefreshEngineTerminalTests`, `CursesPreparedRefreshTests`, `CursesPresentationResolverTests`, and `CursesTerminalScreenMapperTests` across all target frameworks.

- [ ] **Step 6: Commit**

  Commit `test: harden style-only refresh transitions` or `fix: refresh foreground and background changes`, with exact T2503 evidence.

### Task 5 — T2504: Capability degradation, resize and lifecycle

**Files:** Create `tests/Icod.DCurses.Tests/src/CursesColorSurfaceLifecycleTests.cs`; modify capability/resolver/screen/lifecycle code only for witnessed defects; create `docs/T2504-Color-Capability-and-Lifecycle.md`.

**Interfaces:** Consumes T2503 transition semantics. Produces predictable behavior for constrained terminal profiles and loss of physical certainty.

- [ ] **Step 1: Write the capability matrix tests**

  Add cases for monochrome, indexed 8/16/256, direct RGB, foreground-only selector, background-only selector, missing default restoration, unsafe indexed/RGB values, and no-color-video restrictions. Assert both `CursesPresentationCapabilities` and normalized styles.

- [ ] **Step 2: Pin independent-axis degradation**

  Add `UnsupportedForegroundDoesNotDiscardSupportedBackground`, `UnsupportedBackgroundDoesNotDiscardSupportedForeground`, and `UnsafeRgbDegradesWithoutNearestColorGuess`. If the current lower-layer contract necessarily degrades the whole pair, stop and document the exact Terminal behavior before changing DCurses expectations.

- [ ] **Step 3: Write resize/invalidation/lifecycle tests**

  Add `ResizePreservesOverlappingCompleteStyles`, `ResizeGrowthCreatesDefaultCellsUntilRepaint`, `ResizeWideBoundaryLeavesNoStaleStyle`, `InvalidateRetainsLogicalMulticolorFrame`, and `ResumeRequiresCompleteRepaintWithoutStalePhysicalStyle`.

- [ ] **Step 4: Run focused tests and distinguish DCurses from Terminal defects**

  Run: `dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -f net10.0 --filter FullyQualifiedName~CursesColorSurfaceLifecycleTests`

  If a failure originates in Terminal normalization or plan generation, pause that case and open a separate Terminal issue/PR; do not duplicate its policy or protocol in DCurses.

- [ ] **Step 5: Implement DCurses-owned corrections and run adjacent suites**

  Run new tests plus `CursesPresentationCapabilitiesTests`, `CursesPresentationResolverTests`, `CursesLayoutResizeIntegrationTests`, `CursesPanelResizeTests`, refresh publication/recovery suites, and lifecycle tests on all target frameworks.

- [ ] **Step 6: Commit**

  Commit `test: qualify color capability and lifecycle behavior` or `fix: preserve color state across lifecycle changes`, and record any lower-layer deferral in T2504.

### Task 6 — T2505: Public-only color sample and package consumer

**Files:** Create `samples/Icod.DCurses.Color.Sample/Icod.DCurses.Color.Sample.csproj`, `Program.cs`, and `README.md`; add the project to `Icod.DCurses.sln`; modify `samples/README.md` and `tools/package-smoke/Program.cs`; create `docs/T2505-Color-Surface-Acceptance.md`.

**Interfaces:** Consumes only public Icod.DCurses 2.5 APIs. Produces the runnable downstream witness that Widgets can cite before adding theme inheritance.

- [ ] **Step 1: Add a failing public-only consumer guard**

  Verify the sample project references only `Icod.DCurses.csproj`, contains no `Icod.Terminal`, `Icod.TermInfo`, `InternalsVisibleTo`, raw escape literals, or terminal-name branching, and is present in the solution.

- [ ] **Step 2: Implement the smallest application-owned loop**

  Open one `CursesSession`, inspect `PresentationCapabilities`, select a conservative default/indexed/RGB palette, paint the matrix and opaque panel, refresh, process resize/repaint and Q/Escape/end-of-input, and exit cleanly. Include a width-two grapheme and explicit background-only cells.

- [ ] **Step 3: Make constrained capability behavior visible**

  Render requested palette class and capability facts. On monochrome or restricted profiles, remain usable with terminal defaults and labels; do not claim that requested RGB was displayed.

- [ ] **Step 4: Document and run the sample**

  Document `dotnet run --project samples/Icod.DCurses.Color.Sample/Icod.DCurses.Color.Sample.csproj -c Release -f net10.0`, expected regions, controls, resize behavior, fallback, and a manual observation template. Run noninteractive/unit-level drawing tests where possible; record live runs separately.

- [ ] **Step 5: Extend and run the package-only consumer**

  Add `VerifyTwoFiveColorSurface` to `tools/package-smoke/Program.cs`; construct styled ordinary/blank/continuation cells, fill an opaque panel surface, project it, and assert exact public values without terminal I/O. Pack the candidate, restore the existing smoke project from an isolated local feed with only Icod.DCurses declared, compile/run for net8.0/net9.0/net10.0, and inspect the resolved dependency graph. Do not add new direct PackageReferences; the smoke's existing approved historical Terminal-bearing API checks remain unchanged.

- [ ] **Step 6: Commit**

  Commit `sample: demonstrate multicolor retained surfaces` and record exact commands/results in T2505.

### Task 7 — T2506: Adversarial, performance, API, package and documentation freeze

**Files:** Add focused workload tests under the existing performance-test naming pattern; modify `README.md`, `CHANGELOG.md`, `samples/README.md`, package metadata, and verifier rules as required; create `docs/Public-API-Baseline-2.5.md`, `docs/Public-API-Fingerprint-2.5.json`, and `docs/T2506-API-Package-Documentation-Gate.md`.

**Interfaces:** Consumes the accepted T2500–T2505 implementation. Produces one frozen RC candidate contract; later changes require reopening this gate.

- [ ] **Step 1: Measure representative refreshes**

  Measure an unchanged multicolor frame, one foreground-only cell, one background-only cell, and a bounded colored panel move. Record environment, iterations, allocations, elapsed time, emitted operations/bytes, baseline, and accepted budget. Equal-frame refresh must not emit cell/rendition work.

- [ ] **Step 2: Run adversarial matrices**

  Exercise tiny screens, rapid palette changes, wide glyph at the right edge, clipped panels, repeated resize/invalidate, unsupported color combinations, default restoration absence, and cancellation/output failure. Assert controlled failure and physical-state uncertainty where applicable.

- [ ] **Step 3: Freeze public API**

  Generate and review the 2.5 fingerprint against 2.4. Expected delta is none unless a separately approved amendment exists. Record type/line/hash counts and explain every difference.

- [ ] **Step 4: Make README and sample guidance durable**

  Add a concise color model, capability/fallback example, opaque-panel rule, reverse warning, and runnable sample command. Use full GitHub URLs for packaged links. Keep readiness evidence in T2506 rather than the packaged README.

- [ ] **Step 5: Update release metadata only for implemented behavior**

  Update CHANGELOG and package description/release notes/tags if warranted by actual results. Audit every new C# and sample/test csproj header and GPL/LGPL boundary.

- [ ] **Step 6: Run the complete pre-RC gate**

  Force restore; build/test Staging and Release for all targets; run package verifier, symbol/source checks, sample builds, isolated consumers, formatting/status checks, and the repository's build scripts. Expect zero warnings/errors and an exact dependency/package graph.

- [ ] **Step 7: Commit**

  Commit `build: freeze 2.5 color-surface contract` with T2506 exact-head evidence.

### Task 8 — T2507: RC, stable source and maintainer handoff

**Files:** Modify `Icod.DCurses.csproj`, `CHANGELOG.md`, roadmaps and release evidence only; create `docs/T2507-2.5.0-Stable-Source-Gate.md`.

**Interfaces:** Consumes the frozen T2506 candidate. Produces an exact stable-source commit for maintainer-controlled merge/tag/publication; no new feature behavior.

- [ ] **Step 1: Promote to `2.5.0-rc.1`**

  Change Version and PackageVersion together. Run forced restore, complete all-target Release build/test, pack, package validation and isolated consumers. Record exact commit and artifact hashes.

- [ ] **Step 2: Collect exact-head PR CI**

  Require package validation and the existing Windows/Linux/macOS x64/ARM64 runtime matrix. Classify unavailable jobs or infrastructure failures; do not convert them into passes.

- [ ] **Step 3: Record live terminal observations**

  Capture exact environment and observed sample output for available terminals. Explicitly list unverified live platforms. Do not block solely because a maintainer lacks live macOS/Linux access if the approved release waiver and automated matrix are recorded.

- [ ] **Step 4: Promote unchanged accepted source to `2.5.0`**

  Change only version/release state required for stable source, then rerun the exact release/package/consumer gate. Any runtime change returns to T2506 and a new RC.

- [ ] **Step 5: Commit and hand off**

  Commit `build: prepare Icod.DCurses 2.5.0 stable source`. Update the main/versioned roadmaps and T2507 with the exact stable commit, CI runs, artifact hashes, limitations, and maintainer commands. Do not merge, tag, publish, or claim publication.

## Plan self-review

- Every design requirement maps to T2501–T2505; compatibility/version/package closure maps to T2500/T2506/T2507.
- Each possible runtime edit is preceded by an exact failing test; a passing audit remains valuable test/documentation work and does not force code churn.
- The plan preserves existing public types and names. No later task depends on an unapproved new API.
- The five Review Focus conditions each have named tests in their owning tasks.
- Terminal-owned fallback/encoding failures stop at the repository boundary instead of being reimplemented in DCurses.
- Sample and packaged README work occurs only after behavior is evidenced, preventing stale prerelease claims.
