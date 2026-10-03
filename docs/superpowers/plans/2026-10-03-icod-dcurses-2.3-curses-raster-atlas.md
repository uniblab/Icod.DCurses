# Icod.DCurses 2.3 `CursesRasterAtlas` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for inline execution. Apply superpowers:test-driven-development to every production task and superpowers:verification-before-completion before each tranche gate.

**Goal:** Add a first-class, cell-aligned raster atlas that coordinates Terminal 1.24 geometry, resource/placeholder ownership, two-frame tile updates and DCurses retained presentation without absorbing game or protocol responsibilities.

**Architecture:** `CursesRasterAtlas` owns one DCurses facade over one Terminal resource, one placeholder grid and two opaque frames. `CursesSession` performs geometry, creation and presentation under its existing terminal-activity gate. `CursesWindow` projects atlas cells into the existing sparse retained-raster plane. Each present copies front to back, applies a bounded deterministic tile-change set, selects last and swaps only after acknowledgement.

**Tech stack:** C# 13, .NET 8/9/10, xUnit 2.9.2, Icod.Terminal 1.24.1, PowerShell 5.1-compatible repository automation, GitHub Actions. No Python.

**Design authority:** `docs/superpowers/specs/2026-10-03-icod-dcurses-2.3-curses-raster-atlas-design.md`  
**Release authority:** `Icod.DCurses-2.3.0-Development-Roadmap.md`

## Global rules

- Preserve every 2.2 public member and `AssemblyVersion 2.0.0.0`.
- Keep the sole direct production dependency at exactly `Icod.Terminal 1.24.1`; never add direct TermInfo access or raw control strings.
- Add XML documentation with each public member. Staging and Release warnings are errors.
- Observe a focused RED before production behavior in every task.
- Validate and copy all caller data before retained mutation or terminal output.
- Use checked/widened arithmetic and exact divisibility; never round pixel geometry.
- Serialize every Terminal call through `CursesSession.AcquireTerminalActivityAsync`.
- Never retain caller image data after creation/presentation, expose Terminal ids, or blind-retry after ambiguous commitment.
- Run focused tests on `net10.0`, then all project TFMs. Obtain exact-head full-matrix CI before accepting a tranche.
- Record each accepted/deferred gate and exact commit in the release roadmap or a linked `docs/T23xx-*.md` file.

---

### Task 1: T2301 public-contract RED and API freeze

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasPublicContractTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasValidationTests.cs`
- Create: `docs/T2301-CursesRasterAtlas-API-Gate.md`
- Modify: `docs/superpowers/specs/2026-10-03-icod-dcurses-2.3-curses-raster-atlas-design.md`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Add compile-time witnesses for `CursesRasterAtlasGeometry`, `CursesRasterAtlasTileUpdate`, `CursesRasterAtlasPresentationStatus`, `CursesRasterAtlasPresentationResult`, `CursesRasterAtlas`, the two `CursesSession` methods and `CursesWindow.WriteRasterAtlas`.
2. Add reflection assertions for exact enum values, const value 4096, property mutability, method defaults and absence of public Terminal resource/frame/placeholder identities.
3. Add validation vectors for zero/negative/257 dimensions, multiplication overflow, invalid timeout, nulls, out-of-range tile coordinates, duplicate coordinates, format mismatch and tile-size mismatch.
4. Run the focused test and capture expected CS0246/CS1061 failures only:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug -f net10.0 \
  --filter FullyQualifiedName~CursesRasterAtlas
```

5. Review exact names and semantics against the design. Amend the design first if needed, then accept T2301 without adding production types.

---

### Task 2: T2302 geometry and planning

**Files:**
- Create: `src/CursesRasterAtlasGeometry.cs`
- Create: `src/Integration/CursesSession.RasterAtlasGeometry.Terminal.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasGeometryTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasGeometryIntegrationTests.cs`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Keep the T2301 geometry tests RED; add direct-query and fallback scripted-terminal tests.
2. Implement the immutable geometry value with positive values and checked `PixelWidth`/`PixelHeight`.
3. Implement `QueryRasterAtlasGeometryAsync` under the terminal-activity gate:
   - validate rows, columns and timeout before I/O;
   - call `QueryCellPixelDimensionsAsync` first;
   - catch only `TimeoutException` from that direct call;
   - query terminal pixels, capture current character dimensions, and call `TerminalPixelGeometry.TryDeriveCellDimensions`;
   - reject non-exact derivation without guessing;
   - return a fresh observation and never cache it.
4. Test cancellation-before-output, malformed response, transport loss, second-query timeout, resize followed by re-query and checked atlas-pixel overflow.
5. Assert `GetRasterPlanningSnapshot()` is consulted only by creation in Task 3, not treated as geometry authority.
6. Run focused and full tests, Staging build and `git diff --check`; record T2302 evidence.

---

### Task 3: T2303 atlas creation and ownership

**Files:**
- Create: `src/CursesRasterAtlas.cs`
- Create: `src/CursesRasterAtlasCreation.cs`
- Create: `src/Integration/CursesSession.RasterAtlas.Terminal.cs`
- Create: `src/Internal/CursesRasterAtlasState.cs`
- Modify: `src/CursesRasterResource.cs`
- Modify: `src/CursesRasterPlaceholder.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasCreationTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasCreationFailureTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasOwnershipTests.cs`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Add RED tests for exact divisibility, immutable properties, `GetCell`, planning-capacity early result, creation call order, second frame, conservative ownership and idempotent disposal.
2. Expose intrinsic pixel width/height internally through `CursesRasterResource`; do not expose its Terminal resource publicly.
3. Implement `CreateRasterAtlasAsync` under one activity lease:
   - validate image/grid before I/O;
   - read the advisory Terminal planning snapshot;
   - create resource, placeholder, then second frame initialized from the same image;
   - map unsupported/unavailable/failed results without publishing a partial atlas;
   - clean partial state in reverse order and preserve cleanup errors.
4. Store only resource/placeholder facades, animation/frame tokens and immutable geometry. Prove with reflection/allocation tests that image bytes are not retained.
5. Implement conservative `OwnershipState`, `RequiresRecreation`, `GetCell` and idempotent placeholder-before-resource disposal.
6. Test every failure point, disposal failure aggregation, wrong-session retained use and lifecycle generation loss.
7. Verify focused/full/Staging and record T2303.

---

### Task 4: T2304 retained atlas projection

**Files:**
- Create: `src/CursesWindow.RasterAtlas.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasWindowTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasPadViewportTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasPanelCompositionTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasDamageTests.cs`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Write RED tests for a rectangular atlas slice projected at a destination without cursor movement.
2. Validate atlas/session ownership, complete source bounds and complete logical destination bounds before changing the retained raster plane.
3. Implement a row-major projection using the existing `CursesRasterCell` plane and damage path. Do not introduce a parallel map or alter visual cells/metadata.
4. Test zero-size rectangles, subwindows clipped by screen geometry, pads/viewports, scroll/edit/copy/overlay, transparent/opaque panels, z-order, resize and failure atomicity.
5. Confirm an unchanged atlas cell creates no broader damage than existing per-cell semantics and ordinary screens without atlases have no new retained allocation.
6. Verify and record T2304.

---

### Task 5: T2305 deterministic double-buffered presentation

**Files:**
- Create: `src/CursesRasterAtlasTileUpdate.cs`
- Create: `src/CursesRasterAtlasPresentationStatus.cs`
- Create: `src/CursesRasterAtlasPresentationResult.cs`
- Create: `src/CursesRasterAtlas.Presentation.cs`
- Modify: `src/Integration/CursesSession.RasterAtlas.Terminal.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasPresentationTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasPresentationOrderingTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasPresentationValidationTests.cs`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Add a scripted-terminal RED for exact order: full front-to-back replacement composition, sorted tile updates, frame selection, internal swap.
2. Implement immutable update/result contracts and copy a caller list into bounded internal entries.
3. Reject over 4096 entries, duplicates, invalid coordinates, non-RGB24/RGBA32 formats and non-tile-sized images before output. Sort by row then column.
4. Implement `PresentAsync` through an owner-session core method under one terminal-activity lease. For empty work return `NoChanges` without acquiring output or calling Terminal.
5. Require successful full-frame `ComposeFrameAsync(..., Replace)` before updates. Apply updates with checked pixel origins; select last; swap only after successful selection.
6. Return exact controlled status/message and requested/completed counts for unsupported/unavailable/failed operations. Do not select after any earlier failure.
7. Test repeated sparse changes across alternating buffers to prove pixels from two frames ago cannot reappear.
8. Verify and record T2305.

---

### Task 6: T2306 refresh, lifecycle and ambiguous-failure hardening

**Files:**
- Modify: `src/CursesRasterAtlas.cs`
- Modify: `src/CursesRasterAtlas.Presentation.cs`
- Modify: `src/Integration/CursesSession.RasterAtlas.Terminal.cs`
- Modify: `src/Integration/CursesSession.Refresh.Terminal.cs` only if evidence requires a shared helper; do not duplicate gates
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasRefreshSynchronizationTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasLifecycleTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasAmbiguousFailureTests.cs`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Add concurrency RED tests proving refresh, atlas present and lifecycle transitions never interleave Terminal output inside an atlas transaction.
2. At each compose/update/select step, distinguish controlled failure from an exception after possible commitment.
3. On controlled failure, retain known front identity and allow a later call to re-copy front into back. On ambiguous exception, atomically set `RequiresRecreation`, invalidate physical-screen certainty and rethrow.
4. Reject later present calls before output when recreation is required or any owned component is non-current.
5. Test cancellation before the activity lease separately from cancellation/timeout after output, transport loss, missing-resource acknowledgement, generation advance, suspend/resume and concurrent dispose.
6. Prove retained stale cells are rejected by the existing refresh lifecycle path until the application replaces them.
7. Verify and record T2306.

---

### Task 7: T2307 adversarial bounds and workload measurement

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasCapacityTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasAllocationTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasWorkloadTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasPackageConsumerTests.cs`
- Create: `docs/Raster-Atlas-Measurement-2.3.md`
- Create: `docs/T2307-Raster-Atlas-Hardening-Gate.md`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Exercise every dimension/update boundary, large caller collections, duplicate-heavy input, invalid images, checked pixel origins and cleanup under failure.
2. Measure allocations after warmup using the repository's established minimum-of-eight convention. Require work proportional to the submitted visible update list, never the application map/world.
3. Run package-only scripted workloads for 1, 4, 16, 64, 121 and 256 changed tiles for RGB24 and RGBA32 where supported. Record commands, package hash, payload/operation counts and environment; do not use elapsed time as a portable pass condition.
4. Inspect Terminal focused operation evidence after acknowledged success and prove format-specific separation.
5. Decide from real package evidence whether Terminal batching or Indexed8 region replacement deserves a later proposal. Do not add either to 2.3.
6. Verify full Debug/Staging and exact-head PR matrix; record T2307.

---

### Task 8: T2308 Ultima-style public sample and fallback

**Files:**
- Create: `samples/Icod.DCurses.RasterAtlas.Sample/Icod.DCurses.RasterAtlas.Sample.csproj`
- Create: `samples/Icod.DCurses.RasterAtlas.Sample/Program.cs`
- Create: `samples/Icod.DCurses.RasterAtlas.Sample/README.md`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAtlasSampleTests.cs`
- Modify: `Icod.DCurses.sln`
- Modify: repository workflow/package-consumer scripts as required
- Create: `docs/T2308-Raster-Atlas-Application-Acceptance-Gate.md`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Build an original, small top-down map model with no copyrighted art/data and deterministic movement/camera tests.
2. Generate or embed minimal immutable RGB24/RGBA32 tile pixels in the sample; keep decoding and durable asset ownership in sample/application code.
3. Query geometry, create the atlas, project a viewport, submit sparse tile changes, layer status/help panels, handle resize by explicit recreation and replace retained cells.
4. Provide `--text` to force text fallback and automatically choose documented fallback when raster setup/presentation is unavailable. The same model and movement rules drive both paths.
5. Add headless tests for movement, update lists, overlays, fallback, resize recreation and clean exit; add a package-only consumer mode.
6. Complete a manual checklist on a representative raster-capable terminal and a fallback terminal. Record what was observed and what remains unproven.
7. Verify and record T2308.

---

### Task 9: T2309 API, package and documentation freeze

**Files:**
- Modify: `README.md`
- Modify: `Icod.DCurses.csproj`
- Create: `docs/Public-API-Baseline-2.3.md`
- Create: `docs/Public-API-Fingerprint-2.3.json`
- Create: `docs/T2309-Adversarial-Package-Documentation-and-API-Gate.md`
- Modify: package/API/dependency tests and scripts
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`

1. Update README feature, ownership, minimum dependency, usage, fallback, resize and recreation guidance. Link the sample and measurement report.
2. Generate the normalized .NET 8/9/10 API baseline/fingerprint and prove the 2.3 delta is additive over published 2.2.
3. Verify every public member has accurate XML docs including exceptions, limits, I/O, acknowledgement and non-atomicity.
4. Pack `2.3.0-alpha.1`; inspect the nuspec and lock/deps outputs for exactly one direct runtime dependency, `Icod.Terminal >= 1.24.1`, and no direct TermInfo.
5. Run fresh NuGet-only consumers on all TFMs and cross-platform workflow scripts. Check licensing/readme/icon/symbol/source-link artifacts.
6. Freeze the public contract and record T2309. No new feature family enters afterward.

---

### Task 10: T2310 RC and stable-source closure

**Files:**
- Modify: `Icod.DCurses.csproj`
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Modify: `Icod.DCurses-2.3.0-Development-Roadmap.md`
- Create: `docs/T2310-RC-Qualification.md`
- Create: `docs/T2310-Stable-Source-Release-Gate.md`

1. Advance `Version` and `PackageVersion` together to `2.3.0-rc.1`; update release notes without changing `AssemblyVersion`.
2. Run clean restore/build/test/pack, package-only consumers, full Staging PR matrix and manual raster/text-fallback checklist on the exact RC head.
3. Fix only acceptance defects, repeat exact-head evidence, and record the RC artifact hashes.
4. Advance together to `2.3.0`, regenerate/verify package metadata and prove the stable source differs from accepted RC only in authorized identity/release text.
5. Run the stable-source matrix on the exact head and record hashes, API fingerprint and live evidence.
6. Stop before merge, tag, GitHub Release or NuGet publication. Those remain separate maintainer-authorized actions after the post-merge Release workflow is green.

## Standard verification commands

Use the repository's exact scripts where they supersede these direct commands:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug -f net10.0 \
  --filter FullyQualifiedName~CursesRasterAtlas
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug
dotnet build Icod.DCurses.sln -c Staging -p:ContinuousIntegrationBuild=true
dotnet pack Icod.DCurses.csproj -c Staging --no-restore -p:ContinuousIntegrationBuild=true
git diff --check
git status --short
```

The implementation environment must have the required .NET SDKs. If local SDKs are unavailable, do not claim local success; push an exact commit and use the complete GitHub Actions matrix as the executable evidence.
