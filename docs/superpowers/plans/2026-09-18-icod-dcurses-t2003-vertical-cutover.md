# Icod.DCurses 2.0 T2003 Vertical Cutover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move DCurses presentation and core screen refresh onto one Terminal 1.18 semantic, transaction-backed output path without retaining a second renderer or changing the accepted 2.0 public API.

**Architecture:** Map curses values explicitly to Terminal-owned profile, rendition, glyph, cursor, and alert contracts. Prepare one ordered `TerminalScreenOutputTransaction` per refresh against a detached speculative physical-state snapshot, commit once, and publish only after success. Until T2004 migrates erase/character-shift/line-shift/scroll planning, reject those optimization candidates and use ordinary rewrite output.

**Tech Stack:** C# 13; .NET 8/9/10; xUnit; `Icod.Terminal 1.18.0`; PowerShell 5.1-compatible packaging; cmd/sh; GitHub Actions. No Python.

**Spec:** `docs/superpowers/specs/2026-09-18-icod-dcurses-t2003-vertical-cutover-design.md`

## Global Constraints

- Preserve `Version` and `PackageVersion` `2.0.0-alpha.1` and `AssemblyVersion` `2.0.0.0`.
- Keep the production references at `Icod.Terminal 1.18.0` and temporary direct `Icod.TermInfo 1.15.0`; T2007 removes the latter.
- Preserve the accepted T2002 fingerprint: 75 exported types, 559 contract lines, SHA-256 `1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65`.
- Do not modify historical public-API fingerprints or the maintainer-restored attribution, copyright, and license text in root `README.md`.
- No migrated production path may inspect TermInfo capabilities, expand controls, call `TPuts`, borrow raw output, or reconstruct an opaque Terminal plan.
- One refresh creates and commits at most one `TerminalScreenOutputTransaction`; preparation emits zero bytes.
- Publish speculative physical state and captured damage only after successful commitment; failure retains damage and invalidates physical certainty.
- Use `TerminalScreenOutputTransactionOptions.UseSynchronizedOutput`; do not wrap a transaction in the existing synchronized-output lease.
- Null essential plans fail closed. Unknown physical rendition uses `PlanRenditionBaseline()`, never an assumed default state.
- T2003 may rewrite instead of using erase/character-shift/line-shift/scroll controls. It must not emit those legacy raw controls.
- Preserve the accepted test-only TermInfo synthetic-profile bootstrap; do not add TermInfo use to production, samples, package smoke, or consumer guidance.
- Mark completed plan steps as `[x]` and include this plan file in every task commit so the branch records execution progress.
- Do not merge, tag, release, or publish.

**Execution note:** Tasks 2-7 are being qualified as one atomic vertical-cutover checkpoint. The
planner-backed cursor resolver returns an opaque session-bound plan, while the pre-cutover refresh
engine and deferred line-shift optimizer consumed raw cursor strings; no independently compilable
Task 2 or Task 3 checkpoint exists without forbidden compatibility logic. The bypassed T2004
line-shift resolver temporarily retains its cursor-cost calculation under the explicitly legacy
`CursesLegacyCursorMotionResolver` name.

---

## File Structure

### New focused production files

- `src/Internal/CursesTerminalScreenMapper.cs`: explicit curses/Terminal enum, color, rendition, glyph, alert, and position conversions.
- `src/Internal/CursesPreparedRefresh.cs`: one semantic wrapper over `TerminalScreenOutputTransaction`; no raw-control API.
- `src/Internal/CursesRefreshPhysicalState.cs`: detached speculative screen/cursor/rendition aggregate used during preparation.

### Existing production files changed

- `src/CursesPresentationCapabilities.cs`: project `TerminalScreenCapabilities`; remove production TermInfo interpretation.
- `src/Internal/CursesPhysicalScreenState.cs`: add a detached copy operation used by speculative refresh state.
- `src/CursesVirtualScreen.cs`: add revision-aware captured-damage publication.
- `src/Internal/CursesPresentationResolver.cs`: normalize and plan rendition through `TerminalScreenPlanner`.
- `src/Internal/CursesLinePresentationResolver.cs`: resolve Terminal glyphs while retaining Unicode/ASCII fallback.
- `src/Internal/CursesCursorMotionResolver.cs`: return opaque cursor plans and Terminal-owned costs.
- `src/Internal/CursesRefreshEngine.cs`: prepare semantic operations into one batch and publish after commit.
- `src/Integration/CursesSession.PresentationCapabilities.cs`: use `Profile.Screen`.
- `src/Integration/CursesSession.Presentation.Terminal.cs`: use Terminal alerts/cursor/rendition plans.
- `src/Integration/CursesSession.Refresh.Terminal.cs`: remove outer synchronized framing and construct the engine from `TerminalSession`.
- `src/Integration/CursesSession.Terminal.cs`: stop constructing the legacy refresh-output adapter.

### Legacy files retained but unused until T2005 deletion

- `src/Integration/TerminalOutputShim.cs`
- `src/Integration/ITerminalRasterPlaceholderOutput.cs`
- `src/Internal/TerminalCapabilityWriter.cs`

T2003 source-boundary tests must prove that the migrated refresh/session files no longer reference these types. T2005 removes the files after all remaining test fixtures have moved.

### New and principal test files

- `tests/Icod.DCurses.Tests/src/T2003VerticalCutoverContractTests.cs`
- `tests/Icod.DCurses.Tests/src/CursesTerminalScreenMapperTests.cs`
- `tests/Icod.DCurses.Tests/src/CursesPreparedRefreshTests.cs`
- `tests/Icod.DCurses.Tests/src/CursesVerticalCutoverApplicationTests.cs`
- Existing presentation, cursor, refresh, hyperlink, raster, synchronization, cancellation, failure, scale, and API test families listed in each task.

---

### Task 1: Freeze the vertical-cutover RED contract and amend tranche responsibilities

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/T2003VerticalCutoverContractTests.cs`
- Modify: `Icod.DCurses-2.0.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Modify: `docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md`

**Interfaces:**
- Consumes: approved T2003 design and current production source tree.
- Produces: an executable source-boundary witness and amended T2003/T2004/T2005 responsibilities.

- [x] **Step 1: Add a source-boundary test with an explicit migrated-path allowlist**

Create `T2003VerticalCutoverContractTests` using the repository-root discovery pattern already used by project contract tests. Assert that these files contain none of the forbidden tokens:

```csharp
private static readonly string[] MigratedProductionPaths = [
	"src/CursesPresentationCapabilities.cs",
	"src/Internal/CursesPresentationResolver.cs",
	"src/Internal/CursesLinePresentationResolver.cs",
	"src/Internal/CursesCursorMotionResolver.cs",
	"src/Internal/CursesRefreshEngine.cs",
	"src/Integration/CursesSession.PresentationCapabilities.cs",
	"src/Integration/CursesSession.Presentation.Terminal.cs",
	"src/Integration/CursesSession.Refresh.Terminal.cs"
];

private static readonly string[] ForbiddenTokens = [
	"Icod.TermInfo",
	"TerminalDescription",
	"StringCapability",
	"TermInfoParameter",
	"TermInfoOutput",
	"TerminalCapabilityWriter",
	"WriteTerminalStringAsync",
	"TerminalSessionCursesOutput"
];
```

Add a second assertion that `CursesSession.Refresh.Terminal.cs` contains
`UseSynchronizedOutput = this.Options.UseSynchronizedOutput` and does not contain
`AcquireSynchronizedOutputAsync`.

- [x] **Step 2: Add a compiled architecture assertion**

Use reflection to assert this exact constructor exists and the legacy constructor does not:

```csharp
ConstructorInfo? terminalConstructor = typeof( CursesRefreshEngine ).GetConstructor(
	BindingFlags.Instance | BindingFlags.NonPublic,
	binder: null,
	[ typeof( TerminalSession ), typeof( bool ) ],
	modifiers: null
);
Assert.NotNull( terminalConstructor );
Assert.DoesNotContain(
	typeof( CursesRefreshEngine ).GetConstructors( BindingFlags.Instance | BindingFlags.NonPublic ),
	constructor => constructor.GetParameters().Any(
		parameter => "Icod.TermInfo.TerminalDescription" == parameter.ParameterType.FullName
	)
);
```

- [x] **Step 3: Amend the release roadmaps**

Record the approved staging precisely:

```text
T2003: transaction-backed vertical cutover for profile/rendition/ACS/cursor/alert and ordinary rewrite refresh
T2004: restore erase/character-shift/line-shift/scroll optimizations with Terminal plans and costs
T2005: exhaustive transaction/capacity/cancellation/synchronization/publication hardening and legacy-shim deletion
```

State that no T2003 package is published and that the temporary rewrite difference is accepted only until T2004.

- [x] **Step 4: Run the focused RED witness**

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~T2003VerticalCutoverContractTests'
```

Expected: compilation or assertions fail because `CursesRefreshEngine` still consumes `TerminalDescription`, migrated paths still contain forbidden raw/TermInfo tokens, and the session still owns an outer synchronized-output lease.

- [x] **Step 5: Push the RED witness and record exact CI evidence**

Commit:

```sh
git add tests/Icod.DCurses.Tests/src/T2003VerticalCutoverContractTests.cs \
  Icod.DCurses-2.0.0-Development-Roadmap.md \
  Icod.DCurses-Development-Roadmap.md \
  docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md
git commit -m "test: freeze DCurses T2003 vertical cutover"
```

Push and retain one expected-red workflow run showing only the new T2003 contract failure. Stop if unrelated existing tests or package validation fail.

    Evidence: published PR commit `c0b0b10772a2467650b9bb396ffdda54dd799496`; [workflow](https://github.com/uniblab/Icod.DCurses/actions/runs/35395082070); representative [Linux x64 job](https://github.com/uniblab/Icod.DCurses/actions/runs/35395082070/job/105761957469); package candidate job `105761957316` succeeded; all six runtime jobs built successfully and failed only in the test step. Linux x64 and Windows ARM64 each reported, for `net8.0`, `net9.0`, and `net10.0`, 912 passed, 3 expected T2003 contract failures, 0 skipped (915 total).

    Review correction: aligned the existing tranche table and detailed T2005-T2007 sections so T2005 owns exhaustive transaction hardening and obsolete output-shim deletion, while T2007 retains final direct `Icod.TermInfo` package/reference removal.

---

### Task 2: Project presentation capabilities from Terminal and add explicit semantic mappings

**Files:**
- Create: `src/Internal/CursesTerminalScreenMapper.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTerminalScreenMapperTests.cs`
- Modify: `src/CursesPresentationCapabilities.cs`
- Modify: `src/Integration/CursesSession.PresentationCapabilities.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesPresentationCapabilitiesTests.cs`
- Test: `tests/Icod.DCurses.Tests/src/PublicPresentationApiContractTests.cs`

**Interfaces:**
- Consumes: `TerminalProfile.Screen`, `TerminalScreenCapabilities`, `TerminalScreenColor`, `TerminalScreenRendition`, `TerminalTextAttributes`, `TerminalLineGlyph`, `TerminalAlertKind`, and `TerminalScreenPosition` from Terminal 1.18.
- Produces: `CursesTerminalScreenMapper` and `CursesPresentationCapabilities.Create(TerminalScreenCapabilities)` for Tasks 3-7.

- [x] **Step 1: Write failing mapper and capability-projection tests**

Cover every flag individually and in combination:

```csharp
[Theory]
[InlineData( CursesTextAttributes.Bold, TerminalTextAttributes.Bold )]
[InlineData( CursesTextAttributes.Dim, TerminalTextAttributes.Dim )]
[InlineData( CursesTextAttributes.Underline, TerminalTextAttributes.Underline )]
[InlineData( CursesTextAttributes.Reverse, TerminalTextAttributes.Reverse )]
[InlineData( CursesTextAttributes.Standout, TerminalTextAttributes.Standout )]
[InlineData( CursesTextAttributes.Italic, TerminalTextAttributes.Italic )]
[InlineData( CursesTextAttributes.Blink, TerminalTextAttributes.Blink )]
[InlineData( CursesTextAttributes.Conceal, TerminalTextAttributes.Conceal )]
[InlineData( CursesTextAttributes.Strikeout, TerminalTextAttributes.Strikeout )]
public void EveryCursesAttributeMapsExplicitly(
	CursesTextAttributes curses,
	TerminalTextAttributes terminal
) {
	Assert.Equal( terminal, CursesTerminalScreenMapper.ToTerminal( curses ) );
	Assert.Equal( curses, CursesTerminalScreenMapper.ToCurses( terminal ) );
}
```

Add tests for default/indexed/RGB colors, all line glyphs, both alert kinds, nullable cursor positions, unknown enum rejection, and combined flags. Update capability tests to derive observations from a real `TerminalSession.Profile.Screen` instead of calling `Create(TerminalDescription)`.

- [x] **Step 2: Run the focused tests to verify RED**

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesTerminalScreenMapperTests|FullyQualifiedName~CursesPresentationCapabilitiesTests|FullyQualifiedName~PublicPresentationApiContractTests'
```

Expected: compilation fails because the mapper and Terminal-owned `Create` overload do not exist.

- [x] **Step 3: Implement the mapper with exhaustive switches and flag composition**

Create these exact members:

```csharp
internal static class CursesTerminalScreenMapper {
	internal static TerminalTextAttributes ToTerminal( CursesTextAttributes value );
	internal static CursesTextAttributes ToCurses( TerminalTextAttributes value );
	internal static TerminalScreenColor ToTerminal( CursesColor value );
	internal static CursesColor ToCurses( TerminalScreenColor value );
	internal static TerminalScreenRendition ToTerminal( CursesStyle value );
	internal static CursesStyle ToCurses( TerminalScreenRendition value );
	internal static TerminalLineGlyph ToTerminal( CursesLineGlyph value );
	internal static TerminalAlertKind ToTerminal( CursesAlertKind value );
	internal static TerminalScreenPosition? ToTerminalPosition( int? row, int? column );
}
```

For flags, reject unknown bits before composing known values. For nullable cursor state, return `null` only when both values are absent and throw `InvalidOperationException` when only one coordinate is present.

- [x] **Step 4: Replace TermInfo capability interpretation**

Change the factory signature to:

```csharp
internal static CursesPresentationCapabilities Create(
	TerminalScreenCapabilities capabilities
)
```

Copy scalar observations and map both attribute sets with `CursesTerminalScreenMapper.ToCurses`. Remove all no-color-video constants and all TermInfo imports from the file. Change the session property to:

```csharp
return CursesPresentationCapabilities.Create( this.Profile.Screen );
```

- [x] **Step 5: Run focused and API tests to verify GREEN**

Run the Step 2 command. Expected: all selected tests pass and the public contract remains unchanged.

- [x] **Step 6: Commit the mapping foundation**

```sh
git add src/Internal/CursesTerminalScreenMapper.cs \
  src/CursesPresentationCapabilities.cs \
  src/Integration/CursesSession.PresentationCapabilities.cs \
  tests/Icod.DCurses.Tests/src/CursesTerminalScreenMapperTests.cs \
  tests/Icod.DCurses.Tests/src/CursesPresentationCapabilitiesTests.cs
git add docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md
git commit -m "refactor: project presentation through Terminal profile"
```

---

### Task 3: Replace rendition, glyph, and cursor interpretation with Terminal plans

**Files:**
- Modify: `src/Internal/CursesPresentationResolver.cs`
- Modify: `src/Internal/CursesLinePresentationResolver.cs`
- Modify: `src/Internal/CursesCursorMotionResolver.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesPresentationResolverTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesLinePresentationResolverTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesCursorMotionResolverTests.cs`
- Test: `tests/Icod.DCurses.Tests/src/TerminalScreenReadinessTests.cs`

**Interfaces:**
- Consumes: Task 2 mapper and one session-bound `TerminalScreenPlanner`.
- Produces: normalized curses styles and opaque baseline/transition/reset/ACS/cursor plans for the prepared refresh.

- [x] **Step 1: Rewrite resolver tests against real session-bound planners**

Use a real `TerminalSession` backed by synthetic test profile/output fixtures. Assert these signatures and behaviors:

```csharp
CursesPresentationResolver presentation = new( session.Screen );
CursesStyle normalized = presentation.Normalize( requested );
TerminalScreenOperationPlan? baseline = presentation.PlanBaseline();
TerminalScreenOperationPlan? transition = presentation.PlanTransition( current, target );
TerminalScreenOperationPlan? reset = presentation.PlanReset( current );

CursesLinePresentationResolver lines = new( session.Screen );
CursesPhysicalLineGlyph glyph = lines.Resolve( CursesLineGlyph.Horizontal, widthProvider );
TerminalScreenOperationPlan? enterAcs = lines.PlanAlternateCharacterSet( enabled: true );

CursesCursorMotionResolver cursor = new( session.Screen );
TerminalScreenOperationPlan plan = cursor.Resolve( currentRow, currentColumn, targetRow, targetColumn );
```

Retain every prior normalization, glyph fallback, cursor candidate, tie-order, and controlled unsupported test. Replace assertions on `Sequence` with transaction-committed literal output and `ByteCount` assertions.

- [x] **Step 2: Run focused tests to verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesPresentationResolverTests|FullyQualifiedName~CursesLinePresentationResolverTests|FullyQualifiedName~CursesCursorMotionResolverTests|FullyQualifiedName~TerminalScreenReadinessTests'
```

Expected: compilation fails because the resolver constructors and methods still expose TermInfo-based contracts.

- [x] **Step 3: Implement `CursesPresentationResolver` as a planner adapter**

Use these members:

```csharp
internal sealed class CursesPresentationResolver {
	private readonly TerminalScreenPlanner planner;

	internal CursesPresentationResolver( TerminalScreenPlanner planner );
	internal CursesStyle Normalize( CursesStyle requested );
	internal TerminalScreenOperationPlan? PlanBaseline();
	internal TerminalScreenOperationPlan? PlanTransition( CursesStyle current, CursesStyle target );
	internal TerminalScreenOperationPlan? PlanReset( CursesStyle current );
}
```

Every method converts with `CursesTerminalScreenMapper`; no local color support, reversibility, or capability logic remains.

- [x] **Step 4: Implement glyph/ACS and cursor planner adapters**

`CursesLinePresentationResolver.Resolve` maps to `TerminalLineGlyph`, accepts Terminal's representation when present, and retains the existing Unicode-width/ASCII fallback when absent. `PlanAlternateCharacterSet` delegates directly.

`CursesCursorMotionResolver.Resolve` validates non-negative targets, maps nullable current position, calls `PlanCursorMove`, and throws this controlled error when null:

```csharp
throw new NotSupportedException(
	$"Terminal '{this.planner.Profile.Name}' does not provide a safe cursor-motion plan for the requested position."
);
```

- [x] **Step 5: Run focused tests to verify GREEN**

Run the Step 2 command. Expected: all selected tests pass with no resolver production reference to `Icod.TermInfo`.

- [x] **Step 6: Commit planner-backed resolvers**

```sh
git add src/Internal/CursesPresentationResolver.cs \
  src/Internal/CursesLinePresentationResolver.cs \
  src/Internal/CursesCursorMotionResolver.cs \
  tests/Icod.DCurses.Tests/src/CursesPresentationResolverTests.cs \
  tests/Icod.DCurses.Tests/src/CursesLinePresentationResolverTests.cs \
  tests/Icod.DCurses.Tests/src/CursesCursorMotionResolverTests.cs
git add docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md
git commit -m "refactor: plan core presentation through Terminal"
```

---

### Task 4: Add detached speculative state and captured-damage publication

**Files:**
- Create: `src/Internal/CursesRefreshPhysicalState.cs`
- Modify: `src/Internal/CursesPhysicalScreenState.cs`
- Modify: `src/CursesVirtualScreen.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRefreshPhysicalStateTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesVirtualScreenChangeTrackingTests.cs`

**Interfaces:**
- Consumes: current known physical cells, metadata, raster state, cursor, style, and logical change revisions.
- Produces: `CursesRefreshPhysicalState.Clone()` and `CursesVirtualScreen.MarkCleanThrough(ulong)` for Task 6.

- [x] **Step 1: Write failing detached-copy tests**

Construct known/unknown cells with metadata/raster state and assert:

```csharp
CursesRefreshPhysicalState copy = original.Clone();
copy.Screen.SetCell( 0, 0, replacement );
copy.CursorRow = 1;
copy.CurrentStyle = CursesStyle.Default;

Assert.Equal( originalCell, ReadKnownCell( original.Screen, 0, 0 ) );
Assert.NotEqual( original.CursorRow, copy.CursorRow );
Assert.NotEqual( original.CurrentStyle, copy.CurrentStyle );
```

Also mutate the original after cloning and prove the copy is detached.

- [x] **Step 2: Write failing captured-damage tests**

Enable tracking, dirty two cells, capture `ulong revision`, mutate one cell after capture, then call:

```csharp
screen.MarkCleanThrough( revision );
```

Assert the pre-capture-only cell is clean and the later mutation remains dirty. Add wrap protection by asserting `RecordChange` continues to reject `ulong.MaxValue` overflow according to the existing revision policy.

- [x] **Step 3: Run focused tests to verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesRefreshPhysicalStateTests|FullyQualifiedName~CursesVirtualScreenChangeTrackingTests'
```

Expected: compilation fails because `Clone` and `MarkCleanThrough` do not exist.

- [x] **Step 4: Implement detached physical-state copying**

Add an internal copy constructor or `Clone` to `CursesPhysicalScreenState` that copies the cell and known arrays and rebuilds sparse metadata/raster rows from `SnapshotRow`. Add:

```csharp
internal sealed class CursesRefreshPhysicalState {
	internal CursesRefreshPhysicalState( CursesPhysicalScreenState screen );
	internal CursesPhysicalScreenState Screen { get; }
	internal CursesStyle? CurrentStyle { get; set; }
	internal int? CursorRow { get; set; }
	internal int? CursorColumn { get; set; }
	internal CursesRefreshPhysicalState Clone();
	internal void Invalidate();
}
```

The clone must preserve whether each coordinate is unknown; it must not materialize unknown cells as blanks.

- [x] **Step 5: Implement revision-aware cleaning**

Add:

```csharp
internal void MarkCleanThrough( ulong capturedRevision )
```

Require change tracking. Iterate dirty coordinates and clear only those whose cell revision is less than or equal to `capturedRevision`, decrementing `dirtyCellCount` exactly once per cleared cell. Do not modify values or revision numbers.

- [x] **Step 6: Run focused tests to verify GREEN and commit**

Run the Step 3 command. Expected: all selected tests pass.

```sh
git add src/Internal/CursesRefreshPhysicalState.cs \
  src/Internal/CursesPhysicalScreenState.cs \
  src/CursesVirtualScreen.cs \
  tests/Icod.DCurses.Tests/src/CursesRefreshPhysicalStateTests.cs \
  tests/Icod.DCurses.Tests/src/CursesVirtualScreenChangeTrackingTests.cs
git add docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md
git commit -m "feat: add speculative refresh state publication"
```

---

### Task 5: Wrap one Terminal screen transaction as a semantic prepared refresh

**Files:**
- Create: `src/Internal/CursesPreparedRefresh.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPreparedRefreshTests.cs`
- Test: `tests/Icod.DCurses.Tests/src/TerminalScreenReadinessTests.cs`

**Interfaces:**
- Consumes: one `TerminalSession` and a `UseSynchronizedOutput` choice.
- Produces: semantic add/write/commit methods used exclusively during one refresh or direct operation.

- [x] **Step 1: Write failing semantic batch tests**

Create a real Terminal session over recording raw output. Assert exact order for:

```csharp
CursesPreparedRefresh prepared = new( session, useSynchronizedOutput: true );
prepared.AddPlan( session.Screen.PlanCursorMove( null, new TerminalScreenPosition( 0, 0 ) )!.Value );
prepared.WriteText( "text" );
prepared.WriteHyperlink( "link", new CursesHyperlink( "https://example.invalid", "id" ) );
prepared.WriteRasterPlaceholderCell( rasterCell );
await prepared.CommitAsync();
```

Assert one synchronized begin/end pair, one flush, plan/text/hyperlink/raster ordering, rejection of default plans/cells, rejection after commit begins, and no raw output before commit.

- [x] **Step 2: Run the prepared-refresh tests to verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesPreparedRefreshTests|FullyQualifiedName~TerminalScreenReadinessTests'
```

Expected: compilation fails because `CursesPreparedRefresh` does not exist.

- [x] **Step 3: Implement the wrapper without a raw-control method**

Create exactly this surface:

```csharp
internal sealed class CursesPreparedRefresh {
	private readonly TerminalScreenOutputTransaction transaction;

	internal CursesPreparedRefresh( TerminalSession session, bool useSynchronizedOutput );
	internal void AddPlan( TerminalScreenOperationPlan plan );
	internal void WriteText( string value );
	internal void WriteHyperlink( string value, CursesHyperlink hyperlink );
	internal void WriteRasterPlaceholderCell( CursesRasterCell cell );
	internal void WriteRasterPlaceholderCells( ReadOnlyMemory<CursesRasterCell> cells );
	internal ValueTask CommitAsync( CancellationToken cancellationToken = default );
}
```

Map raster cells to `TerminalRasterPlaceholderCell[]` before calling the Terminal batch method. Do not expose `TerminalScreenOutputTransaction`, Terminal output, or a method accepting an arbitrary control string.

- [x] **Step 4: Run tests to verify GREEN and commit**

Run the Step 2 command. Expected: all selected tests pass.

```sh
git add src/Internal/CursesPreparedRefresh.cs \
  tests/Icod.DCurses.Tests/src/CursesPreparedRefreshTests.cs
git add docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md
git commit -m "feat: add semantic prepared refresh batch"
```

---

### Task 6: Cut ordinary text, style, ACS, and cursor refresh vertically to one transaction

**Files:**
- Modify: `src/Internal/CursesRefreshEngine.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesRefreshEngineTerminalTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesRenditionTransitionTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesCursorMotionIntegrationTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesSemanticLineRefreshTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesRefreshCostBaselineTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesCharacterShiftRefreshTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesLineShiftRefreshTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesEraseIntegrationTests.cs`

**Interfaces:**
- Consumes: Tasks 2-5 mappings, planner adapters, speculative state, and prepared refresh.
- Produces: `CursesRefreshEngine(TerminalSession, bool)`, one-transaction ordinary refresh, and explicit T2004 rewrite fallback.

- [x] **Step 1: Change engine tests to require a real Terminal session and one commit**

Replace direct `(TerminalDescription, ITerminalOutput)` construction with a helper that opens a real session over recording raw output, then construct:

```csharp
CursesRefreshEngine engine = new(
	terminalSession,
	useSynchronizedOutput: false
);
```

Retain exact output assertions for cursor/rendition/ACS/text. Update the T2004-owned optimization integration expectations so T2003 explicitly asserts ordinary rewrite and absence of `<el>`, insert/delete, line-shift, and scroll-region controls.

- [x] **Step 2: Add RED tests for preparation, commit, and publication**

Add cases proving:

- raw output remains empty while the transaction is blocked before commit;
- one refresh flushes once;
- a no-op refresh commits no transaction and emits no flush;
- commit failure leaves all captured cells dirty and forces a full next repaint;
- mutation while commit is blocked remains dirty after successful publication;
- invalidation while commit is blocked forces the next refresh to repaint fully;
- unknown rendition starts with the exact Terminal baseline plan;
- null baseline/transition/cursor plans throw controlled `NotSupportedException` before output.

- [x] **Step 3: Run core refresh tests to verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesRefreshEngineTerminalTests|FullyQualifiedName~CursesRenditionTransitionTests|FullyQualifiedName~CursesCursorMotionIntegrationTests|FullyQualifiedName~CursesSemanticLineRefreshTests|FullyQualifiedName~CursesRefreshCostBaselineTests|FullyQualifiedName~CursesCharacterShiftRefreshTests|FullyQualifiedName~CursesLineShiftRefreshTests|FullyQualifiedName~CursesEraseIntegrationTests'
```

Expected: compilation/assertion failures because the engine constructor and output model are still legacy and optimization controls are still emitted.

- [x] **Step 4: Change engine ownership and preparation signatures**

Replace TerminalDescription/output fields with:

```csharp
private readonly TerminalSession terminalSession;
private readonly TerminalScreenPlanner planner;
private readonly bool useSynchronizedOutput;
private CursesRefreshPhysicalState? physicalState;

internal CursesRefreshEngine(
	TerminalSession terminalSession,
	bool useSynchronizedOutput
);
```

Construct the three planner-backed resolvers from `terminalSession.Screen`. Remove character-shift, line-shift, erase, output, hyperlink-output, and raster-output fields from this class for T2003.

- [x] **Step 5: Convert refresh helpers from asynchronous writes to semantic preparation**

Pass `CursesPreparedRefresh prepared` and `CursesRefreshPhysicalState speculative` through cursor, style, span, glyph, text, hyperlink, and raster helpers. Use:

```csharp
prepared.AddPlan( requiredPlan );
prepared.WriteText( text );
prepared.WriteHyperlink( text, hyperlink );
prepared.WriteRasterPlaceholderCell( rasterCell );
```

Use `PlanBaseline` when `speculative.CurrentStyle` is unknown, otherwise use `PlanTransition`. Add ACS entry/exit plans only when mode changes. Coalesce adjacent application text in the existing `StringBuilder` before adding an item.

- [x] **Step 6: Disable legacy optimization selection without deleting its independent resolvers**

Remove calls to `lineShiftResolver.Resolve`, `characterShiftResolver.Resolve`, and `eraseResolver.Resolve` from `CursesRefreshEngine`. The ordinary `RenderSpan` path becomes the only T2003 refresh path. Preserve the resolver source/tests for T2004; do not copy their raw sequences into the prepared refresh.

- [x] **Step 7: Implement prepare/commit/publish**

At refresh start, enable logical change tracking and capture:

```csharp
ulong capturedRevision = desired.ChangeRevision;
CursesRefreshPhysicalState speculative = this.physicalState!.Clone();
CursesPreparedRefresh prepared = new( this.terminalSession, this.useSynchronizedOutput );
```

Prepare without awaiting output. If no changes and the final cursor is already correct, return without constructing/committing a transaction. Otherwise commit once. After success:

```csharp
this.physicalState = speculative;
desired.MarkCleanThrough( capturedRevision );
```

On any preparation or commit exception, invalidate retained physical certainty, preserve logical damage, set `invalidationRequested`, and rethrow.

- [x] **Step 8: Run core refresh tests to verify GREEN**

Run the Step 3 command. Expected: all selected tests pass, optimization tests assert rewrite fallback, and there is one flush for each non-empty refresh.

- [x] **Step 9: Commit the ordinary vertical cutover**

```sh
git add src/Internal/CursesRefreshEngine.cs \
  tests/Icod.DCurses.Tests/src/CursesRefreshEngineTerminalTests.cs \
  tests/Icod.DCurses.Tests/src/CursesRenditionTransitionTests.cs \
  tests/Icod.DCurses.Tests/src/CursesCursorMotionIntegrationTests.cs \
  tests/Icod.DCurses.Tests/src/CursesSemanticLineRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesRefreshCostBaselineTests.cs \
  tests/Icod.DCurses.Tests/src/CursesCharacterShiftRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesLineShiftRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesEraseIntegrationTests.cs
git add docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md
git commit -m "feat: cut refresh to Terminal transactions"
```

---

### Task 7: Move mixed-media ordering, synchronized framing, and direct operations to the transaction path

**Files:**
- Modify: `src/Integration/CursesSession.Terminal.cs`
- Modify: `src/Integration/CursesSession.Refresh.Terminal.cs`
- Modify: `src/Integration/CursesSession.Presentation.Terminal.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesHyperlinkRefreshTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesRasterRefreshTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesRasterSynchronizedRefreshTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesTerminalHyperlinkIntegrationTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesTerminalIntegrationTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesOutputFailureHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesSemanticFailureHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesPanelReleaseHardeningTests.cs`

**Interfaces:**
- Consumes: Task 6 transaction-backed engine and Terminal planner methods `PlanAlert`, `PlanCursorMove`, `PlanRenditionBaseline`, and `PlanRenditionReset`.
- Produces: session-level one-transaction refresh and direct semantic operations without the old output adapter or outer synchronized lease.

- [x] **Step 1: Add/update RED integration tests**

Require:

- text, hyperlink, raster placeholder, final cursor, and synchronized end appear in exact order;
- exactly one begin/end frame and one flush for synchronized refresh;
- `AlertAsync` emits preferred/fallback Terminal plan and returns false when null;
- `SetCursorPositionAsync` checks `Profile.Screen.SupportsAbsoluteCursorAddressing`, commits one cursor plan, and preserves logical intent on failure;
- `ResetRenditionAsync` commits known reset or unknown baseline and returns false when no safe plan exists;
- a body failure does not trigger the old pending synchronized-lease retry state;
- no migrated integration file constructs or calls `TerminalSessionCursesOutput`.

- [x] **Step 2: Run focused integration tests to verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesHyperlinkRefreshTests|FullyQualifiedName~CursesRasterRefreshTests|FullyQualifiedName~CursesRasterSynchronizedRefreshTests|FullyQualifiedName~CursesTerminalHyperlinkIntegrationTests|FullyQualifiedName~CursesTerminalIntegrationTests|FullyQualifiedName~CursesOutputFailureHardeningTests|FullyQualifiedName~CursesSemanticFailureHardeningTests|FullyQualifiedName~CursesPanelReleaseHardeningTests'
```

Expected: failures show the current outer lease, legacy adapter construction, and raw direct controls.

- [x] **Step 3: Remove the outer synchronized-output flow**

Simplify `RefreshAsync` to acquire DCurses activity and call `RefreshCoreAsync`. Delete `pendingSynchronizedOutputCleanup`, `RetryPendingSynchronizedOutputCleanupAsync`, and the outer lease/aggregate-cleanup logic. Pass `Options.UseSynchronizedOutput` into `CursesRefreshEngine` construction.

- [x] **Step 4: Stop creating the legacy refresh adapter**

Remove `refreshOutput` and `new TerminalSessionCursesOutput(terminalSession)` from `CursesSession.Terminal.cs`. Construct the engine as:

```csharp
this.refreshEngine ??= new CursesRefreshEngine(
	this.HostSession,
	this.Options.UseSynchronizedOutput
);
```

- [x] **Step 5: Migrate direct alert/cursor/reset operations**

Replace all `StringCapability` use with explicit mapper/planner calls. Route serialized direct plans through these exact engine methods:

```csharp
internal ValueTask CommitPlanAsync(
	TerminalScreenOperationPlan plan,
	CancellationToken cancellationToken
);

internal ValueTask SetCursorPositionAsync(
	TerminalScreenOperationPlan plan,
	int row,
	int column,
	CancellationToken cancellationToken
);

internal ValueTask ResetRenditionAsync(
	TerminalScreenOperationPlan plan,
	CancellationToken cancellationToken
);
```

`CommitPlanAsync` is used for alerts and does not change retained physical state. The cursor method publishes the supplied cursor only after successful commit. The rendition method publishes normalized default rendition only after successful commit. Every method invalidates physical certainty on failure. Keep presentation-lease code unchanged.

- [x] **Step 6: Run focused integration tests to verify GREEN**

Run the Step 2 command. Expected: all selected tests pass through Terminal transactions with one synchronized frame owned by Terminal.

- [x] **Step 7: Commit session/mixed-media cutover**

```sh
git add src/Integration/CursesSession.Terminal.cs \
  src/Integration/CursesSession.Refresh.Terminal.cs \
  src/Integration/CursesSession.Presentation.Terminal.cs \
  src/Internal/CursesRefreshEngine.cs \
  tests/Icod.DCurses.Tests/src/CursesHyperlinkRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesRasterRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesRasterSynchronizedRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesTerminalHyperlinkIntegrationTests.cs \
  tests/Icod.DCurses.Tests/src/CursesTerminalIntegrationTests.cs \
  tests/Icod.DCurses.Tests/src/CursesOutputFailureHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/CursesSemanticFailureHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/CursesPanelReleaseHardeningTests.cs
git add docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md
git commit -m "feat: route session presentation through screen transactions"
```

---

### Task 8: Add application-shaped acceptance and complete the migrated boundary

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesVerticalCutoverApplicationTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesSemanticApplicationAcceptanceTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesScaleHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesConcurrentInputRefreshHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesLifecycleHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesRasterLifecycleHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/T2003VerticalCutoverContractTests.cs`

**Interfaces:**
- Consumes: complete Tasks 2-7 vertical output path.
- Produces: roguelike, pixel-art/raster, sprite-like placeholder, editor, scale, concurrency, and lifecycle evidence plus a green T2003 source boundary.

- [x] **Step 1: Add the four approved workload witnesses**

Implement these exact test cases:

```csharp
[Fact] public async Task RoguelikeFrameCommitsSparseStyledUnicodeAndAcsContentAtomically();
[Fact] public async Task PixelArtFrameOrdersTextPanelsAndRasterPlaceholdersInOneCommit();
[Fact] public async Task SpriteLikeCellAlignedRasterMovementClipsAndRepaintsDeterministically();
[Fact] public async Task EditorFramePreservesSparseTextCursorAndRenditionWithRewriteFallback();
```

Each uses a real Terminal session and recording raw output. Assert final literal ordering, one flush, no output before commit, retained damage on induced failure, and no erase/shift/scroll controls in the editor/roguelike T2003 fallback.

- [x] **Step 2: Adapt scale/concurrency/lifecycle fixtures to real sessions**

Replace remaining direct `Icod.DCurses.Terminal.ITerminalOutput` refresh-engine fixtures in the listed files with Terminal raw-output fixtures under real sessions. Preserve existing test intent and counts. Update no-op expectations to zero additional writes and zero additional flushes.

- [x] **Step 3: Run application and hardening tests**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesVerticalCutoverApplicationTests|FullyQualifiedName~CursesSemanticApplicationAcceptanceTests|FullyQualifiedName~CursesScaleHardeningTests|FullyQualifiedName~CursesConcurrentInputRefreshHardeningTests|FullyQualifiedName~CursesLifecycleHardeningTests|FullyQualifiedName~CursesRasterLifecycleHardeningTests|FullyQualifiedName~T2003VerticalCutoverContractTests'
```

Expected: all selected tests pass; the T2003 migrated-path boundary contains no forbidden token.

- [x] **Step 4: Run all TermInfo/raw-output source checks**

```sh
rg -n 'Icod\.TermInfo|TerminalDescription|StringCapability|TermInfoParameter|TermInfoOutput|TerminalCapabilityWriter|WriteTerminalStringAsync|TerminalSessionCursesOutput' \
  src/CursesPresentationCapabilities.cs \
  src/Internal/CursesPresentationResolver.cs \
  src/Internal/CursesLinePresentationResolver.cs \
  src/Internal/CursesCursorMotionResolver.cs \
  src/Internal/CursesRefreshEngine.cs \
  src/Integration/CursesSession.PresentationCapabilities.cs \
  src/Integration/CursesSession.Presentation.Terminal.cs \
  src/Integration/CursesSession.Refresh.Terminal.cs
```

Expected: no output.

- [x] **Step 5: Commit application acceptance**

```sh
git add tests/Icod.DCurses.Tests/src/CursesVerticalCutoverApplicationTests.cs \
  tests/Icod.DCurses.Tests/src/CursesSemanticApplicationAcceptanceTests.cs \
  tests/Icod.DCurses.Tests/src/CursesScaleHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/CursesConcurrentInputRefreshHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/CursesLifecycleHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/CursesRasterLifecycleHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/T2003VerticalCutoverContractTests.cs
git add docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md
git commit -m "test: qualify DCurses T2003 application workloads"
```

---

### Task 9: Qualify and close T2003

**Files:**
- Create: `docs/T2003-Semantic-Presentation-Vertical-Cutover-Gate.md`
- Modify: `Icod.DCurses-2.0.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Modify: `docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md`
- Verify unchanged: `README.md`
- Verify unchanged: `docs/Public-API-Fingerprint-2.0.json`

**Interfaces:**
- Consumes: Tasks 1-8 and exact-head workflow evidence.
- Produces: accepted T2003 evidence authorizing T2004, or a precise blocker.

- [x] **Step 1: Run static identity, API, and boundary checks**

```sh
rg -n '<Version>|<PackageVersion>|<AssemblyVersion>|Icod\.Terminal|Icod\.TermInfo' Icod.DCurses.csproj
rg -n 'Icod\.TermInfo|TerminalDescription|StringCapability|TermInfoParameter|TermInfoOutput|TerminalCapabilityWriter|WriteTerminalStringAsync|TerminalSessionCursesOutput' \
  src/CursesPresentationCapabilities.cs \
  src/Internal/CursesPresentationResolver.cs \
  src/Internal/CursesLinePresentationResolver.cs \
  src/Internal/CursesCursorMotionResolver.cs \
  src/Internal/CursesRefreshEngine.cs \
  src/Integration/CursesSession.PresentationCapabilities.cs \
  src/Integration/CursesSession.Presentation.Terminal.cs \
  src/Integration/CursesSession.Refresh.Terminal.cs
git diff --check
git diff origin/2.0.0-roadmap -- README.md docs/Public-API-Fingerprint-2.0.json
```

Expected: identities/dependencies remain exact; the migrated-path search and protected-file diff are empty; whitespace is clean.

- [x] **Step 2: Run complete local qualification when an SDK executor is available**

```sh
dotnet restore Icod.DCurses.sln
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Staging --no-build --no-restore --logger trx
pwsh ./packaging/Invoke-Build.ps1 -Section validate -Configuration Staging
```

Expected: zero warnings/errors; all tests and package validation pass. If the current executor lacks `dotnet`, use the exact-head PR workflow and do not claim local execution.

- [x] **Step 3: Review the complete API fingerprint**

Run the active/historical API and public-dependency tests. Expected T2002 values remain:

```text
75 exported types
559 canonical declared contract lines
sha256 1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65
```

Stop on any public contract or public dependency change.

- [x] **Step 4: Push and require the exact-head matrix**

Required jobs:

```text
Package candidate
Runtime Windows x64
Runtime Windows ARM64
Runtime Linux x64
Runtime Linux ARM64
Runtime macOS x64
Runtime macOS ARM64
```

Record per-framework totals from Linux x64 and inspect at least one second architecture's logs. Do not accept a rerun from a different head.

- [x] **Step 5: Record the T2003 gate**

Create the gate with exact commit/workflow/job links, package/API identities, test totals, migrated-path search result, transaction/flush/order/failure evidence, application-shaped witnesses, and the explicit temporary T2004 optimization debt. State that direct TermInfo removal remains T2007 and no package is published.

- [x] **Step 6: Mark T2003 accepted only after every criterion passes**

Update both roadmaps to show T2003 accepted and T2004 next. If any criterion fails, retain T2003 as blocked and record the exact blocker instead.

- [x] **Step 7: Commit the gate**

```sh
git add docs/T2003-Semantic-Presentation-Vertical-Cutover-Gate.md \
  docs/superpowers/plans/2026-09-18-icod-dcurses-t2003-vertical-cutover.md \
  Icod.DCurses-2.0.0-Development-Roadmap.md \
  Icod.DCurses-Development-Roadmap.md
git commit -m "docs: record DCurses T2003 vertical cutover gate"
```

Do not begin T2004 unless T2003 is explicitly accepted. Do not merge, tag, release, or publish.
