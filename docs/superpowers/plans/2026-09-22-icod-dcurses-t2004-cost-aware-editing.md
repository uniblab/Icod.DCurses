# Icod.DCurses T2004 Cost-Aware Editing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task in the current session. Do not delegate to subagents. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore erase, character-shift, line-shift, and scroll-region optimizations through `Icod.Terminal 1.18.0` opaque plans while removing TermInfo interpretation from the migrated optimization path.

**Architecture:** Preserve the existing DCurses resolvers and deterministic eligibility policy, but replace raw terminal strings with an ordered `CursesTerminalPlanSequence` of same-session `TerminalScreenOperationPlan` values. Re-enable the resolvers inside T2003's detached prepare/commit/publish refresh, use regional retained-state safety, and record fail-closed full-region restoration after uncertain temporary-region commitment.

**Tech Stack:** C# 13; .NET `net8.0`, `net9.0`, and `net10.0`; `Icod.Terminal 1.18.0`; xUnit; PowerShell 5.1-compatible packaging; cmd/sh. No Python.

**Spec:** `docs/superpowers/specs/2026-09-22-icod-dcurses-t2004-cost-aware-editing-design.md`

**Execution status:** Tasks 1-8 complete. The accepted executable head is `049843eff8535718f5a7a3c9b10399b75880fd4e`, qualified by workflow `35803855740`; the final documentation head must pass the same seven-job matrix before handoff. See `docs/T2004-Cost-Aware-Editing-Cutover-Gate.md`.

## Global Constraints

- Keep `Version` and `PackageVersion` at `2.0.0-alpha.1` and `AssemblyVersion` at `2.0.0.0`.
- Keep the direct dependency versions at `Icod.Terminal 1.18.0` and temporary `Icod.TermInfo 1.15.0`; final package-reference removal remains T2007.
- Production editing resolvers, cost model, and refresh integration must not use `Icod.TermInfo`, `TerminalDescription`, capability identifiers, expansion, raw terminal strings, `TermInfoOutput`, `TerminalCapabilityWriter`, or borrowed Terminal output.
- Terminal owns operation encoding, padding, `ByteCount`, and `AffectedLines`; DCurses owns exact-match eligibility and candidate selection.
- Treat a zero-byte erase, character-shift, line-shift, or scroll-region operation plan as unavailable. Zero-byte cursor/rendition setup plans may remain in an otherwise executable sequence.
- Preserve strict cheaper-than selection. Equal cost retains ordinary rewrite; stable first-candidate order breaks ties between optimizations.
- Retained metadata or raster content blocks only an operation whose safety region contains it.
- Keep one Terminal screen transaction and one flush for each non-empty successful refresh; no-op refresh remains zero writes and zero flushes.
- Publish speculative physical state and clean captured damage only after successful commitment.
- Keep `README.md` attribution and `docs/Public-API-Fingerprint-2.0.json` unchanged.
- Do not merge, tag, release, or publish.

## Review Focus

- A failed temporary-region transaction may have emitted setup but not restoration; Task 6 proves the next explicit refresh restores the full region before any body output or fails closed.
- Raster or hyperlink metadata immediately outside an editing region must not disable a safe text-only candidate; Task 7 proves regional rather than global gating.
- A raster or metadata cell inside any destination/source footprint must reject the operation; Tasks 2-4 and 7 prove no terminal edit moves retained ownership.
- Wide characters may move only as complete rows; Task 3 rejects row-local splitting and Task 4 preserves complete line footprints.
- Candidate byte-count overflow or an unavailable Terminal plan must fall back without output or state publication; Tasks 1-4 cover checked totals and null plans.

---

### Task 1: Freeze the T2004 source boundary and ordered Terminal plan sequence

**Files:**
- Create: `src/Internal/CursesTerminalPlanSequence.cs`
- Create: `src/Internal/CursesEditingRegionSafety.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTerminalPlanSequenceTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesEditingRegionSafetyTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/T2004EditingCutoverContractTests.cs`

**Interfaces:**
- Consumes: same-session `TerminalScreenOperationPlan` values from `TerminalScreenPlanner` and desired/physical retained screen planes.
- Produces: `CursesTerminalPlanSequence`, with ordered `Plans`, checked `ByteCount`, and `UsesTemporaryScrollRegion`; `CursesEditingRegionSafety.IsRetainedStateFree(...)` for Tasks 2-4.

- [x] **Step 1: Write the RED source-boundary contract**

Add a contract test that reads these production files and rejects the frozen token set:

```csharp
[Fact]
public void EditingCutoverPathsContainNoTermInfoOrRawTerminalTokens() {
	string root = FindRepositoryRoot();
	string[] paths = [
		"src/Internal/CursesEraseResolver.cs",
		"src/Internal/CursesCharacterShiftResolver.cs",
		"src/Internal/CursesLineShiftResolver.cs",
		"src/Internal/CursesOutputCostModel.cs",
		"src/Internal/CursesRefreshEngine.cs"
	];
	string[] forbidden = [
		"Icod.TermInfo",
		"TerminalDescription",
		"StringCapability",
		"TermInfoOutput",
		"TerminalCapabilityWriter",
		"WriteTerminalStringAsync"
	];
	foreach ( string path in paths ) {
		string source = File.ReadAllText(
			Path.Combine(
				root,
				path.Replace( '/', Path.DirectorySeparatorChar )
			)
		);
		foreach ( string token in forbidden ) {
			Assert.DoesNotContain( token, source, StringComparison.Ordinal );
		}
	}
}
```

Copy the private `FindRepositoryRoot()` helper already present in `T2003VerticalCutoverContractTests.cs`; do not invent another environment-variable convention.

- [x] **Step 2: Run the contract test and record RED**

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~T2004EditingCutoverContractTests
```

Expected: failure identifies TermInfo/raw-cost tokens in the retained resolvers and cost model. If the executor has no SDK, push this isolated test and retain the expected-red workflow link before production changes.

- [x] **Step 3: Write ordered-sequence tests**

Open a real `TerminalSession` with `TerminalScreenTestSession`, obtain two cursor plans, and assert:

```csharp
CursesTerminalPlanSequence sequence = new(
	[ first, second ],
	usesTemporaryScrollRegion: true
);

Assert.Equal( [ first, second ], sequence.Plans );
Assert.Equal( checked( first.ByteCount + second.ByteCount ), sequence.ByteCount );
Assert.True( sequence.UsesTemporaryScrollRegion );
Assert.Throws<ArgumentException>(
	() => new CursesTerminalPlanSequence( [ default ] )
);
```

Also prove an empty sequence is rejected and that the input collection is copied rather than retained mutably.

- [x] **Step 4: Implement `CursesTerminalPlanSequence`**

Use this exact surface:

```csharp
internal sealed class CursesTerminalPlanSequence {
	internal CursesTerminalPlanSequence(
		IEnumerable<TerminalScreenOperationPlan> plans,
		bool usesTemporaryScrollRegion = false
	);

	internal IReadOnlyList<TerminalScreenOperationPlan> Plans { get; }
	internal int ByteCount { get; }
	internal bool UsesTemporaryScrollRegion { get; }
}
```

Materialize once into an array, reject zero plans and any plan with `AffectedLines <= 0`, and sum `ByteCount` in a checked context.

- [x] **Step 5: Write regional retained-state tests**

Create desired and physical 4x3 screens and cover:

```csharp
Assert.True( CursesEditingRegionSafety.IsRetainedStateFree(
	desired, physical, topRow: 1, bottomRowExclusive: 2,
	startColumn: 1, endColumnExclusive: 3
) );
```

Then independently place desired metadata, physical metadata, desired raster, and physical raster inside the range and expect `false`. Place each immediately outside the range and expect `true`. Mark one physical coordinate unknown and expect `false`. Assert null screens throw `ArgumentNullException`; mismatched dimensions throw `ArgumentException` for `physical`; invalid `topRow`/`bottomRowExclusive` throw `ArgumentOutOfRangeException`; and invalid `startColumn`/`endColumnExclusive` throw `ArgumentOutOfRangeException`.

- [x] **Step 6: Implement regional safety**

Use this exact signature:

```csharp
internal static class CursesEditingRegionSafety {
	internal static bool IsRetainedStateFree(
		CursesVirtualScreen desired,
		CursesPhysicalScreenState physical,
		int topRow,
		int bottomRowExclusive,
		int startColumn,
		int endColumnExclusive
	);
}
```

Validate matching dimensions and a non-empty in-bounds rectangle. For every coordinate, require `physical.TryGetCell(...)`, then require null desired/physical metadata and null desired/physical raster state.

- [x] **Step 7: Run focused tests and commit the foundation**

Run the two new helper test classes. The source-boundary test remains expected-red until Tasks 2-4 remove the legacy tokens.

```sh
git add src/Internal/CursesTerminalPlanSequence.cs \
  src/Internal/CursesEditingRegionSafety.cs \
  tests/Icod.DCurses.Tests/src/CursesTerminalPlanSequenceTests.cs \
  tests/Icod.DCurses.Tests/src/CursesEditingRegionSafetyTests.cs \
  tests/Icod.DCurses.Tests/src/T2004EditingCutoverContractTests.cs
git commit -m "test: freeze DCurses T2004 editing boundary"
```

---

### Task 2: Move erase selection and application-text cost through Terminal

**Files:**
- Modify: `src/Internal/CursesOutputCostModel.cs`
- Modify: `src/Internal/CursesEraseResolver.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesOutputCostModelTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesEraseResolverTests.cs`

**Interfaces:**
- Consumes: Task 1 plan sequences/safety, `TerminalScreenPlanner.PlanErase`, `CursesPresentationResolver`, and `CursesCursorMotionResolver`.
- Produces: `CursesErasePlan` carrying an opaque ordered sequence and post-operation cursor/style observations for Task 5.

- [x] **Step 1: Rewrite cost-model tests around application text only**

Keep UTF-8/UTF-16 application payload assertions and remove every test of terminal strings or padding. Assert null input throws `ArgumentNullException` and non-ASCII payloads use the configured encoding's exact `GetByteCount` result. The only production method after this task is:

```csharp
internal int GetApplicationTextByteCount( string value );
```

- [x] **Step 2: Rewrite erase resolver tests against a real planner**

Convert tests to `async Task`, open a real session over `RecordingTerminalOutput`, and construct:

```csharp
CursesEraseResolver resolver = new(
	session.Screen,
	new CursesOutputCostModel( Encoding.UTF8 )
);
```

Update `Resolve` calls to provide retained style/cursor state:

```csharp
CursesErasePlan? result = resolver.Resolve(
	desired,
	physical,
	row,
	startColumn,
	CursesStyle.Default,
	currentCursorRow: row,
	currentCursorColumn: startColumn
);
```

Assert the selected `Kind`, `Sequence.ByteCount`, operation `Kind == TerminalScreenOperationKind.Erase`, post-cursor state, and no output during planning. Commit every plan in `Sequence.Plans` and assert exact output afterward.

Retain literal-rewrite, erase-line, erase-screen, clear-screen, styled-blank, and application-encoding cases. Add null Terminal operation fallback; metadata/raster inside rejection; metadata/raster outside acceptance; unknown physical rejection; and clear-screen cursor-unknown coverage.

- [x] **Step 3: Verify RED**

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesOutputCostModelTests|FullyQualifiedName~CursesEraseResolverTests'
```

Expected: compile failures because the resolver still requires `TerminalDescription` and exposes raw `Sequence` strings.

- [x] **Step 4: Remove terminal-string cost from `CursesOutputCostModel`**

Delete `using Icod.TermInfo;` and `GetTerminalStringByteCount`. Retain the constructor and application-text method unchanged apart from XML wording that makes Terminal plan cost ownership explicit.

- [x] **Step 5: Convert the erase plan and resolver**

Use this result surface:

```csharp
internal readonly record struct CursesErasePlan(
	CursesEraseKind Kind,
	CursesTerminalPlanSequence Sequence,
	CursesStyle StyleAfter,
	int? CursorAfterRow,
	int? CursorAfterColumn
);
```

Change the constructor to `(TerminalScreenPlanner planner, CursesOutputCostModel costModel)`. Construct presentation/cursor adapters from the same planner. Build candidate plan lists in this order:

```text
non-clear: default-rendition setup, cursor motion, erase operation
clear:     default-rendition setup, clear-screen operation
```

Request only `PlanErase(ToEndOfLine, 1)`, `PlanErase(ToEndOfScreen, rows-row)`, or `PlanErase(Screen, rows)`. Null means unavailable. Use `CursesEditingRegionSafety` for each exact affected rectangle. Compare the checked complete sequence cost strictly against the existing encoded blank rewrite lower bound. Preserve erase-line, erase-screen, clear-screen candidate order.

Reject a returned erase operation whose `ByteCount` is zero before constructing the sequence; a zero-byte semantic edit cannot justify publishing the erased speculative region.

- [x] **Step 6: Run focused tests and source checks**

Run the Step 3 command and:

```sh
rg -n 'Icod\.TermInfo|TerminalDescription|StringCapability|TermInfoOutput|GetTerminalStringByteCount' \
  src/Internal/CursesEraseResolver.cs src/Internal/CursesOutputCostModel.cs
```

Expected: focused tests pass and source search is empty.

- [x] **Step 7: Commit erase migration**

```sh
git add src/Internal/CursesOutputCostModel.cs \
  src/Internal/CursesEraseResolver.cs \
  tests/Icod.DCurses.Tests/src/CursesOutputCostModelTests.cs \
  tests/Icod.DCurses.Tests/src/CursesEraseResolverTests.cs
git commit -m "refactor: plan erase operations through Terminal"
```

---

### Task 3: Move row-local character shifts through Terminal

**Files:**
- Modify: `src/Internal/CursesCharacterShiftResolver.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesCharacterShiftResolverTests.cs`

**Interfaces:**
- Consumes: Task 1 sequence/safety, Task 2 application-text cost model, `PlanCharacterShift`, presentation and cursor adapters.
- Produces: `CursesCharacterShiftPlan` with opaque plans and post-operation state for Task 5.

- [x] **Step 1: Rewrite resolver tests against Terminal plans**

Use real sessions and construct the resolver with `session.Screen`. Preserve tests for parameterized insert/delete, Terminal's repeated-single-operation choice, strict equal-cost fallback, application encoding, unknown physical cells, nondefault inserted blanks, and wide-cell rejection.

Add cases proving:

- continuation, width-two, and line-glyph cells reject the complete row tail;
- metadata/raster inside the row tail reject selection;
- metadata/raster in another row do not reject selection;
- unavailable cursor/default-rendition/character-shift plans return null without output; and
- checked candidate totals cannot publish a partial plan.

Commit the returned plans through one Terminal transaction and assert exact bytes only after commit.

- [x] **Step 2: Verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~CursesCharacterShiftResolverTests
```

Expected: compile failures from the old constructor/raw plan contract.

- [x] **Step 3: Convert the character-shift result**

Use:

```csharp
internal readonly record struct CursesCharacterShiftPlan(
	CursesCharacterShiftKind Kind,
	int Row,
	int Column,
	int Count,
	CursesTerminalPlanSequence Sequence,
	CursesStyle StyleAfter,
	int CursorAfterRow,
	int CursorAfterColumn
);
```

- [x] **Step 4: Replace capability resolution with planner calls**

Change the constructor to `(TerminalScreenPlanner planner, CursesOutputCostModel costModel)`. For each exact-match insertion/deletion candidate:

```csharp
TerminalScreenOperationPlan? operation = planner.PlanCharacterShift(
	kind == CursesCharacterShiftKind.Insert
		? TerminalScreenCharacterShiftKind.Insert
		: TerminalScreenCharacterShiftKind.Delete,
	count
);
```

Build default-rendition setup, cursor movement to `(row, firstDifference)`, then operation. Compare the complete sequence `ByteCount` strictly against `GetChangedNonblankByteCount`. Preserve insertion-before-deletion stable tie behavior. Replace the old `TryResolveOperation` and all TermInfo/string-building logic.

Use `CursesEditingRegionSafety` over `[row,row+1) x [firstDifference,columns)` in addition to existing simple-cell and exact-match checks.

Reject a returned character-shift operation whose `ByteCount` is zero before constructing the sequence.

- [x] **Step 5: Run focused tests and source checks**

Run Step 2 and:

```sh
rg -n 'Icod\.TermInfo|TerminalDescription|StringCapability|TermInfoOutput|StringBuilder' \
  src/Internal/CursesCharacterShiftResolver.cs
```

Expected: no source match and all focused tests pass.

- [x] **Step 6: Commit character-shift migration**

```sh
git add src/Internal/CursesCharacterShiftResolver.cs \
  tests/Icod.DCurses.Tests/src/CursesCharacterShiftResolverTests.cs
git commit -m "refactor: plan character shifts through Terminal"
```

---

### Task 4: Move line shifts and scroll regions through Terminal

**Files:**
- Modify: `src/Internal/CursesLineShiftResolver.cs`
- Delete: `src/Internal/CursesLegacyCursorMotionResolver.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesLineShiftResolverTests.cs`

**Interfaces:**
- Consumes: Task 1 sequence/safety, Task 2 cost model, `PlanLineShift`, `PlanScrollRegion`, and planner-backed presentation/cursor adapters.
- Produces: `CursesLineShiftPlan` containing one complete ordered operation bundle for Task 6.

- [x] **Step 1: Rewrite line-shift tests around opaque ordered bundles**

Preserve direct insert/delete to screen bottom, repeated-operation selection, interior temporary region, full-screen forward/reverse scrolling, wide-row preservation, styled-vacated-row rejection, unknown physical rejection, and equal-cost rewrite.

For the interior case, assert committed order:

```text
<set-region><move-from-unknown><line-operation><restore-full-region><final-move-from-unknown>
```

Assert `UsesTemporaryScrollRegion`, checked aggregate `ByteCount`, each operation's semantic `Kind`, and padding-sensitive `AffectedLines`. Add metadata/raster inside-region rejection and outside-region acceptance.

- [x] **Step 2: Verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~CursesLineShiftResolverTests
```

Expected: compile failures from raw strings and legacy cursor motion.

- [x] **Step 3: Reduce the result to semantic state plus an opaque sequence**

Use:

```csharp
internal readonly record struct CursesLineShiftPlan(
	CursesLineShiftKind Kind,
	CursesLineShiftOperation Operation,
	int TopRow,
	int BottomRow,
	int Count,
	CursesTerminalPlanSequence Sequence,
	CursesStyle StyleAfter,
	int CursorAfterRow,
	int CursorAfterColumn
);
```

- [x] **Step 4: Replace every line/region/cursor capability path**

Change the constructor to `(TerminalScreenPlanner planner, CursesOutputCostModel costModel)`. Map operations exactly:

```csharp
CursesLineShiftOperation.InsertLines  => TerminalScreenLineShiftKind.Insert
CursesLineShiftOperation.DeleteLines  => TerminalScreenLineShiftKind.Delete
CursesLineShiftOperation.ScrollForward => TerminalScreenLineShiftKind.ScrollForward
CursesLineShiftOperation.ScrollReverse => TerminalScreenLineShiftKind.ScrollReverse
```

Request `PlanLineShift(mappedKind, count, regionHeight)`. For temporary regions, request both `PlanScrollRegion(top,bottom,regionHeight)` and `PlanScrollRegion(0,rows-1,rows)`, use cursor movement from unknown after each region change, and mark the sequence temporary. For non-temporary candidates, use the known current cursor for operation motion and the operation cursor for final motion.

Prepend the safe default-rendition setup plan when needed. Sum the complete sequence through Task 1. Compare strictly with the conservative changed-nonblank rewrite lower bound. Use regional safety over all columns and `[topRow,bottomRow+1)`.

Reject any zero-byte line-shift or scroll-region operation plan before constructing the sequence. A temporary-region candidate is unavailable unless both the temporary and full-screen region plans are non-null and non-empty.

- [x] **Step 5: Delete the obsolete legacy cursor resolver**

After `rg -n 'CursesLegacyCursorMotion' src tests` shows only the legacy file, delete it. Do not relocate its TermInfo logic.

- [x] **Step 6: Run focused tests and the complete source-boundary contract**

Run Step 2 plus:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~T2004EditingCutoverContractTests
```

Expected: all pass; the original Task 1 RED boundary is now green.

- [x] **Step 7: Commit line-shift migration**

```sh
git add src/Internal/CursesLineShiftResolver.cs \
  src/Internal/CursesLegacyCursorMotionResolver.cs \
  tests/Icod.DCurses.Tests/src/CursesLineShiftResolverTests.cs
git commit -m "refactor: plan line shifts through Terminal"
```

---

### Task 5: Re-enable erase and character-shift transaction preparation

**Files:**
- Modify: `src/Internal/CursesRefreshEngine.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesEraseIntegrationTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesCharacterShiftRefreshTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesRefreshCostBaselineTests.cs`

**Interfaces:**
- Consumes: Tasks 2-3 resolver results and T2003 `CursesPreparedRefresh`/speculative state.
- Produces: transaction-backed erase/character optimization with correct speculative publication.

- [x] **Step 1: Change the T2003 fallback expectations to T2004 RED expectations**

Rename the erase tests to require `<el>`, `<ed>`, and `<clear>` only when their complete candidate is cheaper. Change character insertion/deletion tests to require the selected Terminal plan and exact final row. Preserve the missing-capability ordinary-rewrite case.

For every optimized test assert:

```csharp
Assert.Equal( 1, output.FlushCount );
Assert.Contains( expectedOperation, output.Text, StringComparison.Ordinal );
```

Then run an unchanged second refresh and assert zero additional writes/flushes. Retain output-failure retry coverage and assert no speculative state was published before success.

- [x] **Step 2: Verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesEraseIntegrationTests|FullyQualifiedName~CursesCharacterShiftRefreshTests|FullyQualifiedName~CursesRefreshCostBaselineTests'
```

Expected: T2003 ordinary rewrite does not emit editing plans.

- [x] **Step 3: Construct the migrated resolvers in `CursesRefreshEngine`**

Create one UTF-8 application cost model and initialize:

```csharp
CursesOutputCostModel costModel = new(
	new UTF8Encoding( encoderShouldEmitUTF8Identifier: false )
);
this.eraseResolver = new CursesEraseResolver( this.planner, costModel );
this.characterShiftResolver = new CursesCharacterShiftResolver( this.planner, costModel );
this.lineShiftResolver = new CursesLineShiftResolver( this.planner, costModel );
```

- [x] **Step 4: Add semantic plan-sequence preparation**

Add:

```csharp
private static void AddPlanSequence(
	Preparation preparation,
	CursesTerminalPlanSequence sequence
) {
	foreach ( TerminalScreenOperationPlan plan in sequence.Plans ) {
		preparation.Prepared.AddPlan( plan );
		preparation.HasOutput = true;
	}
}
```

Add a `CopyDesiredRange` helper that writes desired cells/metadata/raster into the speculative physical screen only after the resolver has proved the operation.

- [x] **Step 5: Reintroduce row character-shift and span erase selection**

Inside the existing row loop, try the character resolver before scanning spans. If selected, add its sequence, copy the complete row, set speculative style/cursor from the result, and continue to the next row.

At each changed span, try erase before ordinary `PrepareSpan`. If selected, add its sequence, copy its exact affected range, set speculative style/cursor, and either end the row or end the screen traversal according to erase kind.

Do not restore the old global `retainedStatePresent` gate; resolver-region safety owns the decision.

- [x] **Step 6: Run focused tests and commit**

Run Step 2. Expected: erase and character-shift integration pass with one transaction/flush, correct retry, and clean no-op follow-up.

```sh
git add src/Internal/CursesRefreshEngine.cs \
  tests/Icod.DCurses.Tests/src/CursesEraseIntegrationTests.cs \
  tests/Icod.DCurses.Tests/src/CursesCharacterShiftRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesRefreshCostBaselineTests.cs
git commit -m "feat: restore Terminal-planned erase and character shifts"
```

---

### Task 6: Re-enable line shifts with fail-closed scroll-region recovery

**Files:**
- Modify: `src/Internal/CursesRefreshEngine.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesLineShiftRefreshTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesScrollRegionRecoveryTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesOutputFailureHardeningTests.cs`

**Interfaces:**
- Consumes: Task 4 line result and Task 5 plan preparation.
- Produces: one-transaction line-shift application plus persistent `scrollRegionResetRequired` recovery state.

- [x] **Step 1: Change line-shift integration tests to RED**

Require direct delete/insert plans, full-screen scroll selection, and interior ordered temporary-region output. Assert exact final desired cells, final cursor, one flush, and silent second refresh.

- [x] **Step 2: Write uncertain-region recovery tests**

Use an output fixture that throws after accepting the temporary region setup bytes. Assert:

1. the first refresh throws and logical damage remains;
2. no speculative physical state is published;
3. the next explicit refresh begins with the full-screen region plan before rendition/cursor/body output;
4. a successful retry clears the requirement; and
5. a profile without full-screen `PlanScrollRegion` throws controlled `NotSupportedException` before retry output and retains damage.

Also cover conservative recovery when the original transaction fails before any bytes.

- [x] **Step 3: Verify RED**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesLineShiftRefreshTests|FullyQualifiedName~CursesScrollRegionRecoveryTests|FullyQualifiedName~CursesOutputFailureHardeningTests'
```

Expected: line shifts remain disabled and no pending-region recovery exists.

- [x] **Step 4: Apply a line-shift bundle before the row loop**

Resolve once against the detached speculative state. If selected, add the entire ordered sequence, copy `[TopRow,BottomRow]` from desired, publish result style/final cursor into speculative state, set `Preparation.UsesTemporaryScrollRegion`, and skip the ordinary row loop.

When the selected sequence is temporary, also set `Preparation.RestoresFullScrollRegion = true` because the ordered bundle contains the full-screen restoration before its final cursor plan.

- [x] **Step 5: Implement persistent region recovery**

Add:

```csharp
private bool scrollRegionResetRequired;
```

When this is true at refresh preparation, require and prepend:

```csharp
this.planner.PlanScrollRegion( 0, desired.Rows - 1, desired.Rows )
```

Mark speculative cursor unknown afterward and set `Preparation.RestoresFullScrollRegion`.

Immediately before `CommitAsync`, set `scrollRegionResetRequired = true` if the batch uses a temporary region. Clear it only after a successful commit when `Preparation.RestoresFullScrollRegion` is true. Both a recovery prefix and a complete temporary-region bundle set that restoration marker. On any commitment failure, leave the persistent flag true. A preparation failure before a temporary-region batch reaches commitment must not create a new recovery requirement.

- [x] **Step 6: Run focused tests and commit**

Run Step 3. Expected: all line-shift/recovery/failure tests pass.

```sh
git add src/Internal/CursesRefreshEngine.cs \
  tests/Icod.DCurses.Tests/src/CursesLineShiftRefreshTests.cs \
  tests/Icod.DCurses.Tests/src/CursesScrollRegionRecoveryTests.cs \
  tests/Icod.DCurses.Tests/src/CursesOutputFailureHardeningTests.cs
git commit -m "feat: restore Terminal-planned line shifts"
```

---

### Task 7: Qualify regional safety and the five application shapes

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/CursesVerticalCutoverApplicationTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesOptimizationRegretTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesSemanticApplicationAcceptanceTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesScaleHardeningTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesPanelRasterRefreshProjectionTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/T2004EditingCutoverContractTests.cs`

**Interfaces:**
- Consumes: complete Tasks 1-6 editing path.
- Produces: workload and boundary evidence needed for the T2004 gate.

- [x] **Step 1: Update roguelike and editor witnesses**

Change only output expectations that now deliberately select cheaper Terminal plans. The roguelike witness must exercise a textual message-log scroll without touching map glyph footprints. The editor witness must exercise character insertion/deletion, line insertion/deletion, line-tail erase, and an interior scrolling region while preserving final text/cursor/rendition.

- [x] **Step 2: Retain pixel-art and sprite-like safety witnesses**

Keep raster viewport/sprite movement on semantic repaint. Add a text-only HUD region outside the raster safety region and prove it may optimize independently. Add a negative case with a raster cell inside the proposed shifted region and assert no character/line operation token is emitted.

- [x] **Step 3: Add the tile-based witness**

Create a cell-aligned tile map containing text/Unicode/ACS tiles and at least one raster tile. Assert sparse text-tile updates remain correct, the raster tile is repainted semantically, and no edit operation moves a region containing that tile.

- [x] **Step 4: Update cost/regret and scale expectations**

Replace the accepted T2003 rewrite-only editor/pager expectations with exact cheaper editing-plan output. Preserve deterministic byte counts, bounded allocations, high-frequency sparse refresh, and no-op write/flush counts. Do not add timing-only thresholds.

- [x] **Step 5: Expand the source-boundary contract**

Add `src/Internal/CursesTerminalPlanSequence.cs` and `src/Internal/CursesEditingRegionSafety.cs` to the migrated-path search. Also assert `src/Internal/CursesLegacyCursorMotionResolver.cs` no longer exists.

- [x] **Step 6: Run application and hardening tests**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesVerticalCutoverApplicationTests|FullyQualifiedName~CursesOptimizationRegretTests|FullyQualifiedName~CursesSemanticApplicationAcceptanceTests|FullyQualifiedName~CursesScaleHardeningTests|FullyQualifiedName~CursesPanelRasterRefreshProjectionTests|FullyQualifiedName~T2004EditingCutoverContractTests'
```

Expected: all five workload classes pass, unrelated text can optimize beside retained media, and affected media regions always fall back.

- [x] **Step 7: Commit application qualification**

```sh
git add tests/Icod.DCurses.Tests/src/CursesVerticalCutoverApplicationTests.cs \
  tests/Icod.DCurses.Tests/src/CursesOptimizationRegretTests.cs \
  tests/Icod.DCurses.Tests/src/CursesSemanticApplicationAcceptanceTests.cs \
  tests/Icod.DCurses.Tests/src/CursesScaleHardeningTests.cs \
  tests/Icod.DCurses.Tests/src/CursesPanelRasterRefreshProjectionTests.cs \
  tests/Icod.DCurses.Tests/src/T2004EditingCutoverContractTests.cs
git commit -m "test: qualify T2004 editing workloads"
```

---

### Task 8: Qualify and close T2004

**Files:**
- Create: `docs/T2004-Cost-Aware-Editing-Cutover-Gate.md`
- Modify: `Icod.DCurses-2.0.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Modify: `docs/superpowers/plans/2026-09-22-icod-dcurses-t2004-cost-aware-editing.md`
- Verify unchanged: `README.md`
- Verify unchanged: `docs/Public-API-Fingerprint-2.0.json`

**Interfaces:**
- Consumes: Tasks 1-7 and exact-head workflow evidence.
- Produces: accepted T2004 evidence authorizing T2005, or one precise blocker.

- [x] **Step 1: Run complete static boundary checks**

```sh
rg -n '<Version>|<PackageVersion>|<AssemblyVersion>|Icod\.Terminal|Icod\.TermInfo' Icod.DCurses.csproj
rg -n 'Icod\.TermInfo|TerminalDescription|StringCapability|TermInfoParameter|TermInfoOutput|TerminalCapabilityWriter|WriteTerminalStringAsync|TerminalSessionCursesOutput' \
  src/Internal/CursesEraseResolver.cs \
  src/Internal/CursesCharacterShiftResolver.cs \
  src/Internal/CursesLineShiftResolver.cs \
  src/Internal/CursesOutputCostModel.cs \
  src/Internal/CursesTerminalPlanSequence.cs \
  src/Internal/CursesEditingRegionSafety.cs \
  src/Internal/CursesRefreshEngine.cs
test ! -e src/Internal/CursesLegacyCursorMotionResolver.cs
git diff --check
git diff 70c391d9f2722d05c073ff16ddc1a59ce436fba2 -- README.md docs/Public-API-Fingerprint-2.0.json
```

Expected: exact identities/dependencies, no migrated-path token output, deleted legacy resolver, clean whitespace, and no protected-file diff.

- [x] **Step 2: Run focused and full qualification**

When an SDK executor is available:

```sh
dotnet restore Icod.DCurses.sln
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Staging --no-build --no-restore --logger trx
pwsh ./packaging/Invoke-Build.ps1 -Section validate -Configuration Staging
```

If the executor lacks .NET, use exact-head PR CI and do not claim local execution.

- [x] **Step 3: Verify API identity**

Require the active/historical API tests to retain:

```text
75 exported types
559 canonical declared contract lines
sha256 1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65
```

Stop on any public contract or dependency-identity change.

- [x] **Step 4: Require the exact-head matrix**

Require all seven jobs on the same commit:

```text
Package candidate
Runtime Windows x64
Runtime Windows ARM64
Runtime Linux x64
Runtime Linux ARM64
Runtime macOS x64
Runtime macOS ARM64
```

Record per-framework totals from Linux x64 and inspect at least one second architecture. Do not accept a rerun from another head.

- [x] **Step 5: Record and commit the T2004 gate**

The gate must include exact commit/workflow/job links, identities, test totals, source-boundary result, operation/cost/fallback evidence, regional retained-state evidence, temporary-region recovery behavior, all five workload witnesses, and remaining T2005-T2007 debt. State explicitly that no package is published.

Update both roadmaps to T2004 accepted and T2005 next only after every criterion passes.

```sh
git add docs/T2004-Cost-Aware-Editing-Cutover-Gate.md \
  docs/superpowers/plans/2026-09-22-icod-dcurses-t2004-cost-aware-editing.md \
  Icod.DCurses-2.0.0-Development-Roadmap.md \
  Icod.DCurses-Development-Roadmap.md
git commit -m "docs: record DCurses T2004 editing cutover gate"
```

- [x] **Step 6: Requalify the final documentation head**

Require the complete seven-job matrix again on the final PR head. Report the accepted executable parent and final documentation head separately. Do not merge, tag, release, or publish.
