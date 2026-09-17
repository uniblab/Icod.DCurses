# Icod.DCurses 1.6.0 Retained Mixed-Media Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make published `Icod.Terminal 1.15.0` Unicode raster-placeholder content participate in DCurses retained windows, pads, viewports, panels, clipping, scrolling, damage, and sparse refresh without leaking protocol-private raster identity or turning DCurses into a widget/scene framework.

**Architecture:** Add a DCurses-shaped ownership facade over Terminal persistent resources/placeholders and a separate lazy row-sparse retained-raster plane beside the existing dense cell and sparse semantic-metadata planes. Extend the existing logical-state, panel-composition, damage, physical-state, and refresh pipelines so raster placeholder cells move and redraw with ordinary retained content while Terminal remains the sole live graphics ownership/encoding/output authority.

**Tech Stack:** C# 13; .NET `net8.0`, `net9.0`, `net10.0`; xUnit; published `Icod.Terminal 1.15.0`; published `Icod.TermInfo 1.14.0`; existing DCurses sparse-plane, panel, damage, refresh, package-smoke, and compiler-derived API fingerprint infrastructure.

**Spec:** `docs/superpowers/specs/2026-09-15-icod-dcurses-1.6-retained-mixed-media-presentation-design.md`

## Global Constraints

- Published DCurses compatibility floor remains `1.0.0`; the complete published `1.5.0` public contract is the additive baseline.
- `AssemblyVersion` remains exactly `1.0.0.0`.
- Starting production dependencies remain exactly `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0` unless a later tranche explicitly qualifies a dependency update.
- `Icod.Terminal` remains the sole live raster protocol/ownership/acknowledgement/encoding/output authority.
- DCurses owns retained logical coordinates, windows, pads, viewports, panels, clipping, scrolling, composition, damage, and refresh ordering.
- Applications own durable source-image data and higher-level policy; DCurses adds no hidden source-image cache, automatic re-upload, or replay.
- No automatic Sixel fallback, backend ranking, terminal-brand heuristic, or raw Kitty/Sixel protocol surface is added.
- Retained raster state is separate from `CursesCell` and `CursesCellMetadata`; applications that never use mixed media pay no unconditional per-cell raster-reference cost.
- Cross-session Terminal raster ownership is never silently transplanted.
- A media-bearing panel coordinate is visually present even when its ordinary cell is blank; semantic metadata alone does not make a blank coordinate opaque.
- Version 1.6 adds no widget framework, retained application event tree, automatic focus policy, retained flex/grid/constraint layout, animation scheduler, terminal emulator, or PTY/process host.
- Existing single-reader input ownership and single serialized refresh/output path remain unchanged.
- All production behavior changes follow RED -> GREEN TDD; test-only RED commits must fail for the expected missing behavior before production code is added.
- Stable closure requires the existing package candidate plus Windows/Linux/macOS x64/ARM64 matrix and package-only consumers on `net8.0`, `net9.0`, and `net10.0`.

---

### Task 1 / T1601: Freeze representation, public API candidate, and dependency boundary

**Files:**
- Create: `docs/T1601-1.6.0-Architecture-Representation-Public-API-and-Dependency-Regret-Gate.md`
- Modify: `Icod.DCurses-1.6.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`

**Interfaces:**
- Freezes public types `CursesRasterResource`, `CursesRasterPlaceholder`, `CursesRasterCell`, `CursesRasterOwnershipStatus`, `CursesRasterOwnershipLossReason`, and `CursesRasterOwnershipState`.
- Freezes exactly one new lower-layer type in public signatures: `Icod.Terminal.TerminalRasterImage`.
- `CursesRasterOwnershipStatus` values are `Current = 0`, `Stale = 1`, `Released = 2`, `Disposed = 3`.
- `CursesRasterOwnershipLossReason` values are `None = 0`, `SessionStateLost = 1`, `ResourceMissing = 2`, `ParentPlacementLost = 3`, `AncestorReleased = 4`, `ResourceReleased = 5`, `ExplicitDisposal = 6`.
- Mapping from Terminal ownership state is exhaustive by semantic enum name, never numeric cast.
- Candidate creation surface:

```csharp
public ValueTask<TerminalControlResult<CursesRasterResource>> CreateRasterResourceAsync(
	TerminalRasterImage image,
	CancellationToken cancellationToken = default
);
```

- Candidate resource surface:

```csharp
public sealed class CursesRasterResource : IAsyncDisposable {
	public CursesRasterOwnershipState OwnershipState { get; }
	public ValueTask<TerminalControlResult<CursesRasterPlaceholder>> CreatePlaceholderAsync(
		int columns,
		int rows,
		CancellationToken cancellationToken = default
	);
	public ValueTask DisposeAsync();
}
```

- Candidate placeholder surface:

```csharp
public sealed class CursesRasterPlaceholder : IAsyncDisposable {
	public int Columns { get; }
	public int Rows { get; }
	public CursesRasterOwnershipState OwnershipState { get; }
	public CursesRasterCell GetCell( int row, int column );
	public ValueTask DisposeAsync();
}
```

- Candidate cell is an opaque readonly value with public `Row` and `Column`, no public constructor, and internal association with one placeholder/session.
- Logical surface candidate:

```csharp
public CursesRasterCell? GetRasterCell( int row, int column );
public void SetRasterCell( int row, int column, CursesRasterCell? rasterCell );
```

on `CursesVirtualScreen` and `CursesWindow`, plus:

```csharp
public void WriteRasterCell( CursesRasterCell rasterCell );
```

on `CursesWindow` at the current cursor, advancing exactly one column under existing wrap/scroll policy.

- [ ] **Step 1: Record the exact contract and rationale** in T1601, including the sparse plane, public dependency allow-list, ownership projection, cross-session rule, panel transparency rule, and explicit exclusions.
- [ ] **Step 2: Keep production/package identity at 1.5.0 during this documentation-only freeze**; T1601 changes no source API and no package identity.
- [ ] **Step 3: Run the ordinary PR matrix** and require an exact-head green result before any T1602 RED commit.
- [ ] **Step 4: Mark T1601 complete in the roadmaps** only after exact-head qualification.

### Task 2 / T1602: Session-owned raster resource and placeholder facade

**Files:**
- Create: `src/CursesRasterOwnershipStatus.cs`
- Create: `src/CursesRasterOwnershipLossReason.cs`
- Create: `src/CursesRasterOwnershipState.cs`
- Create: `src/CursesRasterResource.cs`
- Create: `src/CursesRasterPlaceholder.cs`
- Create: `src/CursesRasterCell.cs`
- Create: `src/Integration/CursesSession.Raster.Terminal.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterPublicApiCandidateTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterOwnershipFacadeTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/PublicDependencyBoundaryTests.cs`
- Modify: `docs/Public-API-Fingerprint-1.6.json` after the candidate is green
- Modify: `tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj`
- Modify: `Icod.DCurses.csproj` only after the candidate implementation is accepted, setting `Version`/`PackageVersion` to `1.6.0-alpha.1` and updating release notes.

**Interfaces:**
- Only `TerminalRasterImage` is newly permitted as a Terminal type in DCurses public signatures.
- `TerminalRasterResource`, `TerminalRasterPlaceholder`, `TerminalRasterPlaceholderCell`, `TerminalRasterOwnershipState`, status/reason enums, protocol ids, and generation ids remain internal implementation details.
- `CursesRasterCell` retains internal owner/session identity so later logical storage/refresh can reject foreign-session use.

- [ ] **Step 1: RED — add reflection/API tests** requiring all T1601 exported types, enum numerics, no public constructors on resource/placeholder/cell wrappers except the ownership-state value constructor, and the exact creation/member signatures above.
- [ ] **Step 2: RED — update the dependency-boundary expectation** to include `Icod.Terminal.TerminalRasterImage`; verify the branch fails because the new candidate surface does not exist yet.
- [ ] **Step 3: Verify RED in PR CI** and confirm failures are specifically missing 1.6 types/signatures or the expected dependency-surface delta.
- [ ] **Step 4: GREEN — implement minimal real wrappers** that delegate resource creation, placeholder creation, `GetCell`, ownership observation, and disposal to Terminal 1.15 without stubs or protocol duplication.
- [ ] **Step 5: Map ownership state exhaustively by enum member name** with a default failure for an unknown future Terminal value rather than numeric casting.
- [ ] **Step 6: Verify focused facade/API/dependency tests and full PR matrix green.**
- [ ] **Step 7: Generate the compiler-derived 1.6 alpha fingerprint**, wire it as current-development baseline, and record T1602 evidence.

### Task 3 / T1603: Separate sparse retained-raster plane and core logical operations

**Files:**
- Modify: `src/CursesVirtualScreen.cs`
- Modify: `src/Internal/CursesLogicalCellState.cs`
- Create: `src/CursesWindow.Raster.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterRepresentationBaselineTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterLogicalSurfaceTests.cs`
- Modify: `docs/Public-API-Fingerprint-1.6.json`

**Interfaces:**
- `CursesVirtualScreen` adds an optional `CursesSparseCellPlane<CursesRasterCellReference>` internal plane; `CursesRasterCellReference` is a small internal reference wrapper because `CursesSparseCellPlane<T>` stores reference types.
- `CursesLogicalCellState` becomes the transient triple `(CursesCell Cell, CursesCellMetadata? Metadata, CursesRasterCellReference? Raster)`.
- Public `GetRasterCell`/`SetRasterCell` expose nullable `CursesRasterCell` values; public callers never see the internal reference wrapper.
- `SetCell`/ordinary text replacement clears raster state at the replaced coordinate but does not silently clear unrelated semantic metadata beyond existing text-footprint rules.

- [ ] **Step 1: RED — representation tests** repeat the established `2048 x 256` scale and prove an unconditional raster reference would cost 4 MiB while ten sparse raster rows materialize only 36 KiB of reference slots on the supported 64-bit matrix.
- [ ] **Step 2: RED — logical tests** require get/set/remove, dirty/revision tracking, `Fill`/`Clear` raster removal, current-cursor `WriteRasterCell`, one-column advance, wrap/scroll behavior, and ordinary text replacement clearing raster at the destination.
- [ ] **Step 3: Verify RED.**
- [ ] **Step 4: GREEN — add the lazy raster plane and public logical APIs** reusing `CursesSparseCellPlane<T>` rather than adding fields to `CursesCell`.
- [ ] **Step 5: Extend `CursesLogicalCellState`** so later editing/composition moves all three axes together.
- [ ] **Step 6: Verify no-raster screens allocate no raster row storage and all existing 1.5 tests remain green.**

### Task 4 / T1604: Editing, scrolling, copy/overlay, subwindow, pad, and viewport propagation

**Files:**
- Modify: `src/CursesWindow.cs`
- Modify: `src/CursesWindow.Editing.cs`
- Modify: `src/CursesWindow.Composition.cs`
- Modify: `src/CursesPad.cs`
- Modify: `src/CursesPadViewport.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterEditingPropagationTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterPadViewportTests.cs`

**Interfaces:**
- Every operation already moving `CursesLogicalCellState` now moves raster references automatically with cell + metadata snapshots.
- Destructive copy transfers blank+raster combinations; transparent overlay treats raster-bearing coordinates as present even when the ordinary cell is blank.
- Same-session copies retain the same semantic placeholder cell.
- A copy/presentation operation targeting a session-owned screen with a foreign-session raster reference rejects before partial destination mutation.

- [ ] **Step 1: RED — cover insert/delete cells, insert/delete lines, scroll up/down, clear/erase, overlapping copy, overlay, subwindow projection, pad presentation, viewport pan/re-present, and clipping.**
- [ ] **Step 2: RED — add a foreign-session transfer witness** proving no partial destination mutation occurs.
- [ ] **Step 3: Verify RED.**
- [ ] **Step 4: GREEN — propagate the raster member through existing snapshot/commit paths** rather than building a second editing engine.
- [ ] **Step 5: Add session-ownership validation at the narrowest shared transfer boundary** used by session-owned destinations; standalone unbound logical surfaces may retain raster intent but cannot become a live output authority by themselves.
- [ ] **Step 6: Run focused propagation tests and full matrix.**

### Task 5 / T1605: Panel composition, transparency, clipping, z-order, and resize coherence

**Files:**
- Modify: `src/Internal/CursesPanelCompositor.cs`
- Modify: `src/Internal/CursesPanelCompositionState.cs`
- Modify: `src/Internal/CursesPanelSurface.cs`
- Modify: `src/CursesPanel.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelRasterCompositionTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesPanelResizeTests.cs`

**Interfaces:**
- Base and panel composition carries raster state with cell/metadata state.
- `BlankCellsTransparent` is transparent only when the panel coordinate has a blank ordinary cell **and no raster reference**.
- Semantic metadata alone still does not make a blank cell visually opaque.
- Topmost visible raster-bearing panel wins at its coordinate; move/hide/show/dispose/resize invalidates the correct composed area.

- [ ] **Step 1: RED — add blank+raster transparency, overlap, z-order, hide/show, move, dispose, clipping, resize-preservation, and resize-discard tests.**
- [ ] **Step 2: Verify RED.**
- [ ] **Step 3: GREEN — update `ResolveCell`, full composition, incremental composition, and transparency checks** to use the three-axis logical state.
- [ ] **Step 4: Preserve retained raster references in the surviving upper-left rectangle during panel resize** and drop references outside the new bounds.
- [ ] **Step 5: Run panel tests and full matrix.**

### Task 6 / T1606: Terminal 1.15 placeholder refresh integration and physical/rendition tracking

**Files:**
- Modify: `src/Internal/CursesPhysicalScreenState.cs`
- Modify: `src/Internal/CursesRefreshEngine.cs`
- Modify: `src/Integration/CursesSession.Refresh.Terminal.cs`
- Create: `src/Terminal/ITerminalRasterPlaceholderOutput.cs`
- Modify: `src/Terminal/TerminalSessionCursesOutput.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterRefreshTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterRefreshRenditionTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterSynchronizedRefreshTests.cs`

**Interfaces:**
- The Terminal-backed output adapter gains a typed internal raster-placeholder emission interface; the refresh engine never constructs Kitty/APC bytes.
- Physical state tracks nullable retained raster reference beside cell + metadata.
- Presence of logical or physical raster state disables line-shift, character-shift, and erase optimizations just as semantic metadata currently does until those optimizations are separately proven raster-safe.
- Rendering a raster cell positions the cursor to the coordinate, delegates the exact `TerminalRasterPlaceholderCell` to Terminal, marks physical raster state only after successful emission, and invalidates cached rendition conservatively so the next ordinary cell reasserts desired style.

- [ ] **Step 1: RED — add tests proving first refresh emits raster placeholder once, unchanged refresh emits nothing, sparse damage re-emits only damaged media, removal restores ordinary content, and clipping/order remain deterministic.**
- [ ] **Step 2: RED — add style-transition tests** showing ordinary text after raster output has required rendition reasserted rather than trusting stale cached foreground/underline state.
- [ ] **Step 3: RED — add synchronized-output tests** proving mixed text/metadata/raster output remains inside one existing refresh transaction.
- [ ] **Step 4: Verify RED.**
- [ ] **Step 5: GREEN — extend physical state and refresh diffing** and delegate placeholder emission through the typed Terminal-backed output adapter.
- [ ] **Step 6: Fail closed on foreign-session/unusable tokens before output.**
- [ ] **Step 7: Run byte/semantic refresh tests plus full matrix.**

### Task 7 / T1607: Lifecycle, suspend/resume, stale/released ownership, and disposal hardening

**Files:**
- Modify: `src/CursesRasterResource.cs`
- Modify: `src/CursesRasterPlaceholder.cs`
- Modify: `src/Internal/CursesRefreshEngine.cs`
- Modify: `src/Integration/CursesSession.Lifecycle.Terminal.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterLifecycleTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterDisposalConcurrencyTests.cs`

**Interfaces:**
- Logical raster references may remain after Terminal ownership becomes stale/released; refresh treats them as unusable intent and emits no stale identity.
- Physical raster knowledge is invalidated when the session generation/lifecycle invalidates terminal state.
- No reverse index from resource/placeholder to all logical cells is introduced; validation is lazy at refresh/use boundaries.
- Explicit disposal is idempotent and never recreates ownership.

- [ ] **Step 1: RED — cover session invalidation, suspend/resume, resource disposal, placeholder disposal, resource release of placeholder, concurrent ownership observation/disposal, and repeated refresh after loss.**
- [ ] **Step 2: Verify RED.**
- [ ] **Step 3: GREEN — connect lifecycle invalidation to physical raster invalidation and validate ownership immediately before emission.**
- [ ] **Step 4: Assert no hidden Terminal create/upload call occurs after loss or disposal.**
- [ ] **Step 5: Run lifecycle/concurrency tests and full matrix.**

### Task 8 / T1608: Application acceptance, package consumer, and optional TermInfo planning sample

**Files:**
- Create: `samples/Icod.DCurses.MixedMedia.Sample/*`
- Modify: `samples/README.md` if present, otherwise the repository sample index used by current samples
- Modify: `tools/package-smoke/Program.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesMixedMediaApplicationAcceptanceTests.cs`
- Create: `docs/T1608-Mixed-Media-Application-Acceptance-and-Package-Consumer.md`

**Interfaces:**
- Sample uses only public DCurses APIs for logical placement/composition and Terminal's public backend-neutral `TerminalRasterImage` as the creation input.
- Package smoke constructs/inspects the 1.6 public raster facade without raw Kitty ids, APC commands, or backend branches.
- Optional planning demonstration may reference `Icod.TermInfo.Inspection` only in a sample/test project; it does not become a production dependency of DCurses.

- [ ] **Step 1: RED — add package-only reflection/compile witnesses for every new exported type/member and the one intentional `TerminalRasterImage` dependency exposure.**
- [ ] **Step 2: Add a realistic mixed-media application scenario** with a scrolling/pannable retained raster region, panel overlay, clipping, sparse refresh, and 1.5 interaction regions over the same geometry.
- [ ] **Step 3: Add optional TermInfo planning demonstration** that keeps backend preference explicit and does not claim TermInfo 1.14 plans Unicode-placeholder semantics.
- [ ] **Step 4: Verify package-only consumers on all three TFMs and normal PR matrix.**

### Task 9 / T1609: Adversarial, capacity, allocation, and failure-atomicity hardening

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAdversarialTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterAllocationRegressionTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRasterCapacityTests.cs`
- Create: `docs/T1609-Retained-Mixed-Media-Adversarial-Performance-and-Allocation-Gate.md`

**Interfaces:**
- No public API additions are expected.
- Tests exercise existing Terminal live ownership ceilings; DCurses does not create a second larger unbounded registry.

- [ ] **Step 1: Exercise maximum placeholder dimensions, many sparse rows, repeated set/remove churn, panel churn, copy churn, resize churn, and deterministic replay.**
- [ ] **Step 2: Exercise malformed/default wrapper misuse through public/reflection-reachable shapes and verify controlled local failure.**
- [ ] **Step 3: Measure no-media baseline allocations and sparse-media overhead** to prove ordinary workloads retain their established cost shape.
- [ ] **Step 4: Inject output failure/cancellation before and after commitment** and verify physical-state invalidation, no blind replay, and no partial logical mutation.
- [ ] **Step 5: Run full matrix and record exact evidence.**

### Task 10 / T1610: Public API, package, documentation, dependency, and licensing regret gate

**Files:**
- Finalize: `docs/Public-API-Fingerprint-1.6.json`
- Create: `docs/Public-API-Baseline-1.6.md`
- Modify: `tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj`
- Modify: `tests/Icod.DCurses.Tests/src/PublicDependencyBoundaryTests.cs`
- Modify: `tools/package-smoke/Program.cs`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `Icod.DCurses.csproj`
- Modify: roadmap files
- Create: `docs/T1610-Public-API-Package-Documentation-Dependency-and-Licensing-Regret-Gate.md`

**Interfaces:**
- Final public surface must remain additive over 1.5 and contain no Terminal raster type except `TerminalRasterImage` plus the already-published five dependency types.
- Final exact API fingerprint must match on `net8.0`, `net9.0`, and `net10.0`.

- [ ] **Step 1: Regenerate/compare compiler-derived API fingerprint across all TFMs.**
- [ ] **Step 2: Audit public constructors, nullability, enum numerics, disposal ownership, cross-session behavior, dependency exposure, and package metadata.**
- [ ] **Step 3: Audit package contents/readme/license/icon/repository metadata and fresh NuGet-only consumer.**
- [ ] **Step 4: Rewrite release-facing README sections by current capability, not tranche chronology, and update CHANGELOG/package release notes.**
- [ ] **Step 5: Require full exact-head green matrix before RC promotion.**

### Task 11 / T1611: RC and stable-source 1.6.0 closure

**Files:**
- Modify: `Icod.DCurses.csproj`
- Modify: release-facing documentation/roadmaps
- Create: `docs/T1611-RC-and-Stable-1.6.0-Closure.md`

**Interfaces:**
- No new feature/API is accepted after T1610.
- RC identity becomes `1.6.0-rc.1`; stable-source becomes `1.6.0` only after unchanged implementation/API qualification.

- [ ] **Step 1: Promote the T1610-qualified implementation to `1.6.0-rc.1` and run the complete seven-job Staging matrix plus package consumers.**
- [ ] **Step 2: Correct only release-blocking test/document/package defects; any production/API change returns to T1610 regret review.**
- [ ] **Step 3: Promote the unchanged accepted implementation/API to stable-source `1.6.0`.**
- [ ] **Step 4: Run one final exact-head seven-job qualification and record commit/workflow/package evidence.**
- [ ] **Step 5: Leave merge, post-merge Release validation, `v1.6.0` tagging, GitHub Release creation, and package publication as separate explicit maintainer actions.**
