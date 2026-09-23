# Icod.DCurses 2.1.0 T2101 Core Presentation and Text Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Freeze the Icod.DCurses 2.0 contract and representative editor/roguelike costs, then accept an exact public API design and implementation sequence for the 2.1 core presentation and text work before changing production behavior.

**Architecture:** T2101 changes documentation and tests only. It treats the published 2.0 package as the compatibility baseline, measures the cost of composing editor and roguelike workloads from current public APIs, and converts the approved 2.1 roadmap into exact immutable, terminal-independent API candidates. A deliberately uncompilable test proves the first missing contract, then is reverted while its exact-head CI failure remains linked as evidence. Production source, package identity, assembly identity and dependencies remain untouched until T2102.

**Tech Stack:** C# 13, .NET 8/9/10, xUnit 2.9.2, Icod.Terminal 1.18.0, PowerShell 5.1-compatible repository automation, GitHub Actions. No Python.

**Spec:** `Icod.DCurses-2.1.0-Development-Roadmap.md`, especially sections 2-14 and tranche T2101.

## Global Constraints

- Do not modify `src/**`, `Icod.DCurses.csproj`, package versions, `AssemblyVersion`, production package references, samples or public API fingerprints in T2101.
- Preserve the published 2.0 fingerprint: 75 exported types, 559 canonical contract lines and SHA-256 `1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65`.
- Preserve the production dependency boundary `Icod.DCurses -> Icod.Terminal 1.18.0`; `Icod.TermInfo` remains allowed only as the existing private test-fixture dependency.
- Keep layout, mapping and viewport candidates pure and terminal-session independent. Application content, editing policy, game policy, event loops and persistence remain caller-owned.
- Use UTF-16 source offsets only at validated Unicode text-element boundaries. Do not normalize Unicode. Preserve the existing malformed-UTF-16 replacement and width-provider behavior.
- Treat allocation measurements as application-shaped evidence. Freeze deterministic counts and broad portable bounds; do not introduce a wall-clock threshold based on one machine.
- Use the minimum of repeated same-thread allocation samples after warmup, following the existing performance-test convention. Avoid the 256-byte noise floor that has already shown platform variance.
- Run all accepted tests on `net8.0`, `net9.0` and `net10.0`. Use exact-head GitHub Actions evidence if the working environment does not provide every SDK.
- Stop if the API design needs a live terminal feature absent from Terminal 1.18.0. Record the smallest upstream contract instead of reaching into TermInfo.

## Review Focus

- Are the proposed types mechanisms shared by both application goals, rather than editor or game policy?
- Can every layout and mapping result be computed without a terminal session or retained surface?
- Are source ranges, wrapping, tabs, overflow, affinity, clipping, capacities and arithmetic failure rules exact?
- Do bulk-operation candidates have measured benefit over current scalar calls?
- Can diagnostics remain bounded and allocation-free while disabled?
- Does each later tranche have one independently reviewable public-contract increment and a clear RED test?

---

### Task 1: Freeze the published 2.0 identity and dependency baseline

**Files:**
- Create: `docs/T2101-2.0-Core-Presentation-Baseline.md`
- Create: `tests/Icod.DCurses.Tests/src/T2101CorePresentationBaselineTests.cs`

**Interfaces:**
- Consumes: `docs/Public-API-Fingerprint-2.0.json`, `Icod.DCurses.csproj`, `tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj`, and the published 2.0 public surface.
- Produces: executable guards for the exact compatibility and dependency starting point, plus a human-readable baseline for later 2.1 gates.

- [ ] **Step 1: Write the failing baseline guard before the document exists**

Create `T2101CorePresentationBaselineTests.cs` with a test that reads the new document from the repository root and asserts its fixed identity markers. Resolve the root from `AppContext.BaseDirectory` in the same manner as existing documentation-contract tests; do not hard-code a machine path.

```csharp
[Fact]
public void TwoZeroCorePresentationBaselineIsFrozen() {
	string text = File.ReadAllText(
		FindRepositoryFile( "docs/T2101-2.0-Core-Presentation-Baseline.md" )
	);

	Assert.Contains( "75 exported types", text, StringComparison.Ordinal );
	Assert.Contains( "559 canonical contract lines", text, StringComparison.Ordinal );
	Assert.Contains(
		"1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65",
		text,
		StringComparison.Ordinal
	);
	Assert.Contains( "Icod.Terminal 1.18.0", text, StringComparison.Ordinal );
	Assert.Contains( "no direct production Icod.TermInfo dependency", text, StringComparison.Ordinal );
}
```

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug \
  --filter FullyQualifiedName~T2101CorePresentationBaselineTests.TwoZeroCorePresentationBaselineIsFrozen
```

Expected: RED because `docs/T2101-2.0-Core-Presentation-Baseline.md` does not exist.

- [ ] **Step 2: Write the baseline document**

Record:

- package `2.0.0` and assembly `2.0.0.0`;
- `net8.0`, `net9.0`, `net10.0`;
- 75 exported types, 559 canonical contract lines and the exact SHA-256 above;
- direct production dependency `Icod.Terminal 1.18.0` and no direct production TermInfo dependency;
- current public text facilities: `ICursesTextWidthProvider`, `UnicodeCursesTextWidthProvider`, `CursesText`, `CursesWindow.Write`, `WriteWithMetadata`, `WriteCell`, `FillRectangle`, `CopyRectangleTo`, `OverlayRectangleTo`, `CursesPad`, `CursesPadViewport` and `CursesLayout`;
- current coordinate domains and known gaps for rich spans, source/visual mapping, virtualized content geometry, bulk row/block input, track distribution and refresh diagnostics;
- named existing behavior witnesses that later tranches must preserve.

State that `docs/Public-API-Fingerprint-2.0.json` remains the machine-readable authority and is never rewritten for 2.1.

- [ ] **Step 3: Add executable package-boundary assertions**

In the same test class, parse the project XML and assert:

```csharp
[Fact]
public void T2101LeavesProductionIdentityAndDependenciesAtTwoZero() {
	XDocument project = XDocument.Load(
		FindRepositoryFile( "Icod.DCurses.csproj" )
	);
	string[] packageReferences = project.Descendants( "PackageReference" )
		.Select( element => (string?)element.Attribute( "Include" ) )
		.Where( static value => value is not null )
		.Cast<string>()
		.ToArray();

	Assert.Equal( "2.0.0", ReadProperty( project, "Version" ) );
	Assert.Equal( "2.0.0", ReadProperty( project, "PackageVersion" ) );
	Assert.Equal( "2.0.0.0", ReadProperty( project, "AssemblyVersion" ) );
	Assert.Equal( [ "Icod.Terminal" ], packageReferences );
	Assert.Equal(
		"1.18.0",
		(string?)project.Descendants( "PackageReference" ).Single()
			.Attribute( "Version" )
	);
}
```

Use private helpers with explicit failure messages for missing files and duplicate/missing project properties.

- [ ] **Step 4: Verify GREEN and commit**

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug \
  --filter FullyQualifiedName~T2101CorePresentationBaselineTests
git diff --check
```

Expected: all T2101 baseline tests pass and no whitespace errors.

```sh
git add docs/T2101-2.0-Core-Presentation-Baseline.md \
  tests/Icod.DCurses.Tests/src/T2101CorePresentationBaselineTests.cs
git commit -m "test: freeze DCurses 2.0 presentation baseline"
```

### Task 2: Measure current editor and roguelike composition costs

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CorePresentationTextWorkloadBaselineTests.cs`
- Modify: `docs/T2101-2.0-Core-Presentation-Baseline.md`

**Interfaces:**
- Consumes: only the published 2.0 APIs named in Task 1.
- Produces: deterministic workload shapes, mutation/damage counts and portable allocation measurements that justify or reject later 2.1 APIs.

- [ ] **Step 1: Add shared same-thread measurement helpers**

Use these constants and method shape in the new class:

```csharp
private const int AllocationSamples = 8;
private const int MeasurementIterations = 256;
private const long MeasurementNoiseAllowance = 64L * 1024L;
private const int WarmupIterations = 32;

private static long MeasureMinimumAllocatedBytes( Action operation ) {
	ArgumentNullException.ThrowIfNull( operation );
	for ( int index = 0; index < WarmupIterations; index++ ) {
		operation();
	}

	long minimum = long.MaxValue;
	for ( int sample = 0; sample < AllocationSamples; sample++ ) {
		int thread = Environment.CurrentManagedThreadId;
		long before = GC.GetAllocatedBytesForCurrentThread();
		for ( int index = 0; index < MeasurementIterations; index++ ) {
			operation();
		}
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.Equal( thread, Environment.CurrentManagedThreadId );
		minimum = Math.Min( minimum, allocated );
	}
	return minimum;
}
```

Do not share mutable fixtures between tests. Keep the operation synchronous so the thread-local allocation counter remains meaningful.

- [ ] **Step 2: Characterize editor visible-slice composition**

Construct 10,000 application-owned logical lines using a repeating corpus that contains ASCII, tabs represented by application-expanded spaces, combining text, `界`, emoji and ambiguous-width text. Render only 40 visible lines into an 80x40 `CursesScreen` by calling `CursesText.SliceByColumns`, `Move` and `Write`.

The test must establish these deterministic facts:

- only 40 document lines are inspected per frame;
- no `CursesPad` proportional to the document is created;
- the resulting `DirtyCellCount` is bounded by 3,200 cells;
- a second identical frame produces zero newly changed retained cells after `MarkClean`;
- horizontal origin 1 never returns half of `界` or another width-two element;
- the minimum allocation is recorded for 256 visible-slice frames with a broad guard of measured baseline plus `MeasurementNoiseAllowance`.

First write the assertion using an intentionally zero allocation ceiling and run it to capture the actual Debug/Staging values. Replace zero with a documented portable ceiling derived from the largest measured TFM/OS value plus the fixed allowance. Do not use elapsed time as a pass/fail assertion.

- [ ] **Step 3: Characterize roguelike local-update composition**

Represent a 2,048x2,048 synthetic world algorithmically; do not allocate a four-million-cell pad. Render an 80x24 visible viewport into a screen with scalar `WriteCell` calls, then mark it clean and apply a fixed nine-cell neighborhood update.

Assert:

```csharp
Assert.Equal( 80 * 24, fullFrameWrites );
Assert.Equal( 9, sparseWrites );
Assert.InRange( screen.VirtualScreen.DirtyCellCount, 1, 9 );
```

Measure the full-frame scalar path and the nine-cell path separately. Record allocations per 256 operations, and record the repeated public-call counts that a future bulk API would remove. The test must not assume that lower call count alone proves a new public API is warranted.

- [ ] **Step 4: Characterize pad, text-width and layout-helper alternatives**

Add focused measurements for:

- a 2,048x256 `CursesPad` and an 80x24 viewport, reusing the existing scale-test shape;
- 10,000 calls each to `MeasureColumns`, `TruncateToColumns` and `SliceByColumns` over the representative Unicode corpus;
- 100,000 existing `CursesLayout.Dock`/`SplitFixed`/`SplitProportional` calculations using the established 160x48 application geometry.

Freeze deterministic dimensions, dirty-cell counts and output strings. Use the minimum-of-eight allocation convention and `MeasurementNoiseAllowance`; preserve elapsed measurements only as informational numbers in the baseline document.

- [ ] **Step 5: Record the measurements and decision thresholds**

Extend `docs/T2101-2.0-Core-Presentation-Baseline.md` with a table containing, for each runtime/configuration actually measured:

- commit SHA and command;
- workload dimensions and iterations;
- inspected lines/cells and public call counts;
- minimum allocated bytes;
- informational elapsed time, if collected;
- the portable CI ceiling;
- the specific 2.1 decision it informs.

Freeze these rules:

- layout/mapping work must be linear in inspected text plus produced fragments/cells;
- viewport calculations must be constant-time and independent of content extent;
- bulk mutation must show a material improvement in an application-shaped workload before it becomes public;
- virtualized presentation memory must depend on visible rows plus explicit overscan, not total document/map size;
- disabled diagnostics must add zero attributable allocation after warmup.

- [ ] **Step 6: Verify and commit**

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug \
  --filter FullyQualifiedName~CorePresentationTextWorkloadBaselineTests
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~CorePresentationTextWorkloadBaselineTests
git diff --check
```

Expected: all workload tests pass on the installed TFMs without a wall-clock gate.

```sh
git add docs/T2101-2.0-Core-Presentation-Baseline.md \
  tests/Icod.DCurses.Tests/src/CorePresentationTextWorkloadBaselineTests.cs
git commit -m "test: measure 2.0 editor and roguelike workloads"
```

### Task 3: Freeze the exact 2.1 public API candidate and semantics

**Files:**
- Create: `docs/2.1-Core-Presentation-and-Text-API-Design.md`

**Interfaces:**
- Consumes: the approved roadmap and Task 2 evidence.
- Produces: the sole T2102-T2108 public design authority, including exact signatures, validation, complexity and ownership rules.

- [ ] **Step 1: Define the source and rich-text foundation**

Document exact candidate declarations, including constructor validation and equality semantics. The first implementation contract is:

```csharp
public readonly record struct CursesTextPosition {
	public CursesTextPosition( int offset );
	public int Offset { get; }
}

public sealed record CursesTextSpan {
	public CursesTextSpan(
		CursesTextPosition start,
		int length,
		CursesStyle style,
		CursesCellMetadata? metadata = null
	);
	public CursesTextPosition Start { get; }
	public int Length { get; }
	public CursesStyle Style { get; }
	public CursesCellMetadata? Metadata { get; }
}
```

`CursesTextPosition` rejects negative offsets. `CursesTextSpan` rejects negative length and checked end overflow. A layout validates that start/end are legal text-element boundaries, spans are sorted and non-overlapping, and the end does not exceed the source length. Uncovered text uses explicit default style/metadata. Zero-length spans are rejected because they cannot affect presentation.

The design must decide whether the accepted name is `CursesTextSpan` or a less ambiguous alternative before the RED witness is committed. Search the 2.0 API and .NET BCL vocabulary and record the result; do not leave both names provisional.

- [ ] **Step 2: Define layout input, output and mapping**

Freeze one exact declaration for each of these families:

- `CursesTextLayoutOptions`;
- wrap, alignment and overflow enums;
- immutable layout result, visual line and fragment values;
- source-to-visual and visual-to-source mapping values;
- leading/trailing affinity;
- caret movement and half-open selection geometry;
- bounded visual-line enumeration/presentation.

For every constructor, property and method specify:

- exact public name, type, nullability and default;
- eager validation and exception type;
- whether caller collections are copied;
- complexity and allocation contract;
- behavior for empty text, trailing hard breaks, CR/LF/CRLF, tabs, zero-width elements, width-two elements, clipping and ellipsis;
- behavior when a source boundary appears on both sides of a soft wrap;
- maximum source length, span count, columns, rows, fragments and cells.

No output object may retain a `CursesSession`, Terminal object, mutable caller collection or retained screen.

- [ ] **Step 3: Define viewport, bulk, track and diagnostic candidates**

Freeze exact declarations and validation for:

- a nonnegative two-dimensional content extent and viewport state;
- content/viewport/destination translations, clamp, pan and `EnsureVisible`;
- row-span and rectangular retained-cell writes only where Task 2 shows material benefit;
- fixed/weighted/minimum/maximum track definitions and deterministic remainder distribution;
- bounded opt-in refresh diagnostic options and immutable snapshots.

The document must explicitly reject:

- a document model, edit commands, undo/redo or clipboard policy;
- a game world/entity model or pathfinding;
- callback-owned virtual content, background loading or hidden full-content caching;
- a retained widget/layout tree;
- an unbounded diagnostic event log;
- any direct TermInfo or raw escape-sequence path.

- [ ] **Step 4: Freeze shared edge rules**

Include normative tables for:

- coordinate domains and conversions;
- checked arithmetic and capacity failure timing;
- span precedence and metadata inheritance;
- wrap/alignment/overflow combinations;
- tab stops anchored at absolute layout column zero;
- caret affinity and hit testing at wide/zero-width elements;
- viewport behavior for empty content, oversized viewport and numeric boundaries;
- bulk clipping/rejection, overlap, failure atomicity, metadata/raster preservation and damage;
- diagnostic snapshot behavior after success, cancellation, output failure and invalidation.

Each rule must name the first tranche and test class that will enforce it.

- [ ] **Step 5: Confirm the Terminal boundary and self-review**

Run:

```sh
rg -n 'Icod\.TermInfo|TerminalDescription|StringCapability|escape sequence' \
  docs/2.1-Core-Presentation-and-Text-API-Design.md
rg -n '\bT(ODO|BD)\b|provisional|to be decided|implementation-defined' \
  docs/2.1-Core-Presentation-and-Text-API-Design.md
git diff --check
```

Expected: TermInfo/raw-terminal terms appear only in explicit prohibitions, and there are no unresolved placeholders or whitespace errors.

Review the design against Terminal 1.18.0. Record that no new Terminal feature is required, or stop and record the exact missing upstream signature and semantics.

- [ ] **Step 6: Commit the accepted design**

Do not commit this task until the API design has been reviewed as one coherent contract.

```sh
git add docs/2.1-Core-Presentation-and-Text-API-Design.md
git commit -m "docs: freeze DCurses 2.1 presentation API design"
```

### Task 4: Prove the first missing public contract with a reversible RED witness

**Files:**
- Temporarily create: `tests/Icod.DCurses.Tests/src/T2101CorePresentationRedWitnessTests.cs`
- Modify after the RED run: `docs/T2101-Core-Presentation-and-Text-Foundation-Gate.md`
- Remove after the RED run: `tests/Icod.DCurses.Tests/src/T2101CorePresentationRedWitnessTests.cs`

**Interfaces:**
- Consumes: the final accepted type name and signature from Task 3.
- Produces: exact-head CI evidence that T2102 begins with a missing contract rather than already-present behavior.

- [ ] **Step 1: Add the smallest compile-time witness**

After substituting the one accepted span type name from Task 3, create:

```csharp
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class T2101CorePresentationRedWitnessTests {
	[Fact]
	public void RichTextSpanCarriesValidatedSourcePresentation() {
		CursesTextPosition start = new( 0 );
		CursesTextSpan span = new(
			start,
			1,
			CursesStyle.Default
		);

		Assert.Equal( 0, span.Start.Offset );
		Assert.Equal( 1, span.Length );
		Assert.Equal( CursesStyle.Default, span.Style );
		Assert.Null( span.Metadata );
	}
}
```

Do not add production stubs to make this compile.

- [ ] **Step 2: Commit and push the intentional RED head**

```sh
git add tests/Icod.DCurses.Tests/src/T2101CorePresentationRedWitnessTests.cs
git commit -m "test: witness missing 2.1 rich text contract"
git push origin 2.1.0-roadmap
```

Wait for the PR workflow and verify that the compiler reports only the intended missing `CursesTextPosition`/accepted span type. A restore failure, syntax error, helper failure or unrelated test failure does not satisfy the RED gate.

- [ ] **Step 3: Record and revert the witness**

Create `docs/T2101-Core-Presentation-and-Text-Foundation-Gate.md` and record:

- RED commit SHA;
- workflow and job URLs;
- exact compiler diagnostics;
- accepted missing signatures;
- why T2102 owns the implementation;
- confirmation that no production source changed.

Then revert the intentional RED commit with a normal commit:

```sh
git revert --no-edit HEAD
git add docs/T2101-Core-Presentation-and-Text-Foundation-Gate.md
git commit -m "docs: record DCurses T2101 red contract witness"
git push origin 2.1.0-roadmap
```

Expected: the active PR returns to a compilable state while preserving the RED workflow as durable evidence.

### Task 5: Write the accepted T2102-T2108 implementation sequence

**Files:**
- Create: `docs/superpowers/plans/2026-09-23-icod-dcurses-2.1-core-presentation-text.md`

**Interfaces:**
- Consumes: the accepted Task 3 design, Task 2 measurements and Task 4 RED witness.
- Produces: the task-by-task TDD implementation plan for production tranches T2102-T2108.

- [ ] **Step 1: Invoke the writing-plans workflow against the accepted design**

The plan must have separate tasks and commits for:

1. T2102 development identity and source/text-element foundation;
2. T2103 rich text layout;
3. T2104 caret, hit-testing and selection geometry;
4. T2105 retained presentation and evidence-backed bulk mutation;
5. T2106 large-content viewport geometry;
6. T2107 stateless track layout;
7. T2108 bounded refresh diagnostics and performance qualification.

For each task, specify exact source/test files, exact declarations from the design, the first failing test, minimal implementation step, focused verification command, all-TFM verification command and commit command.

- [ ] **Step 2: Preserve tranche boundaries**

The plan must not advance `Version`/`PackageVersion` before T2102, add a bulk API before its Task 2 evidence supports it, or couple diagnostic work to text layout. T2109/T2110 samples and T2111/T2112 release closure remain outside this production plan and receive their own plans when their prerequisites are green.

- [ ] **Step 3: Self-review and commit**

Run:

```sh
rg -n '\bT(ODO|BD)\b|placeholder|decide later' \
  docs/superpowers/plans/2026-09-23-icod-dcurses-2.1-core-presentation-text.md
git diff --check
```

Expected: no unresolved decisions and no whitespace errors.

```sh
git add docs/superpowers/plans/2026-09-23-icod-dcurses-2.1-core-presentation-text.md
git commit -m "docs: plan DCurses 2.1 presentation implementation"
```

### Task 6: Close the T2101 gate and return the PR to green

**Files:**
- Modify: `docs/T2101-Core-Presentation-and-Text-Foundation-Gate.md`
- Modify: `Icod.DCurses-2.1.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`

**Interfaces:**
- Consumes: Tasks 1-5 exact-head evidence.
- Produces: an accepted T2101 checkpoint that authorizes T2102, or a precise blocked checkpoint that authorizes no production work.

- [ ] **Step 1: Run the complete local gate**

```sh
dotnet restore Icod.DCurses.sln
dotnet build Icod.DCurses.sln -c Debug --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Debug --no-build --no-restore
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Staging --no-build --no-restore
pwsh ./packaging/Invoke-Build.ps1 -Section validate -Configuration Staging
```

Expected: zero build errors, zero test failures and successful package validation on the post-revert head.

- [ ] **Step 2: Record exact-head CI evidence**

Push the branch and require the ordinary PR matrix to pass on the same SHA across Windows, Linux and macOS and all configured TFMs/configurations. Record workflow/job URLs, command summaries and test totals in the gate document.

- [ ] **Step 3: Apply the acceptance rule**

Mark T2101 accepted only when all of the following are true:

- 2.0 API/package/dependency identity is frozen;
- editor, roguelike, pad, text-width and layout-helper baselines are recorded;
- exact public candidate signatures and edge semantics are reviewed;
- Terminal 1.18.0 is sufficient;
- the first missing contract has an exact intended RED run and has been reverted;
- the T2102-T2108 implementation plan is reviewed;
- the active exact head is green with no production source or package identity change.

Otherwise mark T2101 blocked, name the smallest corrective action and leave T2102 pending.

- [ ] **Step 4: Update both roadmaps**

In the detailed roadmap, change T2101 from planning to accepted and link the baseline, design, implementation plan and gate. Keep T2102 pending. In the main roadmap, update the 2.1 entry to say foundation accepted and implementation pending.

- [ ] **Step 5: Final self-review and commit**

```sh
rg -n '\bT(ODO|BD)\b' \
  docs/T2101-2.0-Core-Presentation-Baseline.md \
  docs/2.1-Core-Presentation-and-Text-API-Design.md \
  docs/T2101-Core-Presentation-and-Text-Foundation-Gate.md \
  docs/superpowers/plans/2026-09-23-icod-dcurses-2.1-core-presentation-text.md
git diff --check
git status --short
```

Expected: no placeholders, no whitespace errors, and only the intended gate/roadmap files pending.

```sh
git add docs Icod.DCurses-2.1.0-Development-Roadmap.md \
  Icod.DCurses-Development-Roadmap.md
git commit -m "docs: accept DCurses 2.1 presentation foundation"
git push origin 2.1.0-roadmap
```

Do not begin T2102 until this gate is green and explicitly accepted.
