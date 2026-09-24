# Icod.DCurses 2.1 Core Presentation and Text Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Apply superpowers:test-driven-development within every production task and superpowers:verification-before-completion before each tranche gate.

**Goal:** Implement the accepted T2102-T2108 core presentation and text APIs as additive 2.1 contracts, preserving the published 2.0 surface and the direct `Icod.DCurses -> Icod.Terminal 1.18.0` boundary.

**Architecture:** Pure source/layout/mapping/viewport/track values live outside terminal integration. `CursesWindow` alone projects layout and prepared cells into retained state. `CursesSession` exposes one optional immutable snapshot of DCurses refresh work without exposing Terminal plans or bytes. Each tranche adds one independently reviewable public increment and begins with an observed failing test.

**Tech Stack:** C# 13, .NET 8/9/10, xUnit 2.9.2, Icod.Terminal 1.18.0, PowerShell 5.1-compatible repository automation, GitHub Actions. No Python.

**Design authority:** `docs/2.1-Core-Presentation-and-Text-API-Design.md`  
**Evidence:** `docs/T2101-2.0-Core-Presentation-Baseline.md` and `docs/T2101-Core-Presentation-and-Text-Foundation-Gate.md`

## Global implementation rules

- Implement public names, types, defaults, validation, capacities, ownership, complexity, clipping, failure atomicity, and damage exactly as frozen in the design authority. Amend the design explicitly before deviating.
- Preserve every 2.0 public member. Keep `AssemblyVersion` at `2.0.0.0` and the sole production package reference at `Icod.Terminal 1.18.0`.
- Advance `Version` and `PackageVersion` to `2.1.0-alpha.1` only in T2102. Later tasks retain that development identity.
- Keep pure APIs free of `CursesSession`, retained-screen, Icod.Terminal, and Icod.TermInfo references.
- Never retain caller spans or mutable caller collections. Validate complete input before allocating a published result or mutating retained state.
- Use checked or widened arithmetic before narrowing. Enforce the design capacities before excessive allocation.
- Add XML documentation to every public member in the same commit that introduces it. Staging and Release treat documentation warnings as errors.
- Run the focused RED and GREEN command on `net10.0`, then the project test command across all TFMs. Before accepting a tranche, run the full solution in Debug and Staging or obtain equivalent exact-head CI evidence.
- Keep T2109/T2110 samples and T2111/T2112 release closure outside this plan.

---

### Task 1: T2102 development identity and source/text-element foundation

**Files:**
- Modify: `Icod.DCurses.csproj`
- Create: `src/CursesTextPosition.cs`
- Create: `src/CursesTextSpan.cs`
- Create: `src/Internal/CursesTextElementScanner.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextSourceContractTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextSourceUnicodeTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/PublicTwoOneDevelopmentIdentityTests.cs`
- Modify: `Icod.DCurses-2.1.0-Development-Roadmap.md`

**Exact public increment:** `CursesTextPosition(int offset)` with `Offset`; `CursesTextSpan(CursesTextPosition start, int length, CursesStyle style, CursesCellMetadata? metadata = null)` with `Start`, `Length`, checked `End`, `Style`, and `Metadata`.

- [x] **Step 1: Restore the durable RED as a permanent contract test**

Create `CursesTextSourceContractTests` with the T2101 witness plus validation tests:

```csharp
[Fact]
public void RichTextSpanCarriesValidatedSourcePresentation() {
	CursesTextPosition start = new( 2 );
	CursesTextSpan span = new( start, 3, CursesStyle.Default );

	Assert.Equal( 2, span.Start.Offset );
	Assert.Equal( 3, span.Length );
	Assert.Equal( 5, span.End.Offset );
	Assert.Equal( CursesStyle.Default, span.Style );
	Assert.Null( span.Metadata );
}
```

Add theories proving negative positions, zero/negative lengths, and end overflow throw the exact exceptions in the design. Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug -f net10.0 \
  --filter FullyQualifiedName~CursesTextSourceContractTests
```

Expected RED: CS0246 for `CursesTextPosition` and `CursesTextSpan`, matching the T2101 workflow.

- [x] **Step 2: Implement the smallest immutable values**

Implement the two public files with ordinary explicit constructors and get-only properties. Convert checked `Start.Offset + Length` overflow to `ArgumentOutOfRangeException(nameof(length))`. Do not add layout behavior to these types.

Run the focused command. Expected GREEN.

- [x] **Step 3: Add the shared internal text-element scanner test-first**

In `CursesTextSourceUnicodeTests`, use `InternalsVisibleTo` already provided by the project and specify the scanner contract through the public results it will later support. Cover:

- empty, start, end, and ordinary boundaries;
- CR, LF, and indivisible CRLF;
- combining, emoji ZWJ, keycap, flag, and width-two elements;
- one U+FFFD semantic element per malformed surrogate code unit while source offsets stay stable;
- tab identification and rejection of other C0/C1 controls;
- leading and attached zero-width elements;
- both built-in ambiguous-width providers.

First assert one known boundary list and observe RED because the scanner is absent. Then implement `CursesTextElementScanner` as a single-pass internal analyzer over one supplied string/provider. Reuse existing Unicode normalization and width policy helpers rather than forking Unicode tables. Store compact element records with source start/end, width, and hard-break/tab flags. No terminal or retained-surface reference is allowed.

- [x] **Step 4: Advance development identity and guard the boundary**

Set:

```xml
<Version>2.1.0-alpha.1</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<PackageVersion>2.1.0-alpha.1</PackageVersion>
```

Update package release notes to describe development status without claiming unimplemented T2103-T2108 features. In `PublicTwoOneDevelopmentIdentityTests`, assert those values, the target-framework list, the sole `Icod.Terminal` package reference at `1.18.0`, and no production `Icod.TermInfo` reference.

- [x] **Step 5: Verify and commit T2102**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug \
  --filter 'FullyQualifiedName~CursesTextSource|FullyQualifiedName~PublicTwoOneDevelopmentIdentityTests'
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug
dotnet build Icod.DCurses.sln -c Staging -p:ContinuousIntegrationBuild=true
git diff --check
```

Expected: all tests pass on net8.0/net9.0/net10.0; Staging has no warning/error; fingerprint baseline proves the change is additive.

```sh
git add Icod.DCurses.csproj src tests/Icod.DCurses.Tests/src \
  Icod.DCurses-2.1.0-Development-Roadmap.md
git commit -m "feat: add DCurses 2.1 text source contracts"
```

---

### Task 2: T2103 immutable rich-text layout

**Files:**
- Create: `src/CursesTextLayoutContracts.cs`
- Create: `src/CursesTextLayoutOptions.cs`
- Create: `src/CursesTextLayout.cs`
- Create: `src/Internal/CursesTextLayoutBuilder.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextLayoutTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextLayoutUnicodeTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextLayoutCapacityTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextLayoutPerformanceTests.cs`
- Modify: `Icod.DCurses-2.1.0-Development-Roadmap.md`

**Exact public increment:** `CursesTextWrapMode`, `CursesTextAlignment`, `CursesTextOverflow`; `CursesTextLayoutOptions`; `CursesTextLayout.Create(string, CursesTextLayoutOptions, IReadOnlyList<CursesTextSpan>? = null)` and its result properties; immutable `CursesTextVisualLine` and `CursesTextFragment` properties exactly as declared in the design.

- [x] **Step 1: Write the first layout RED**

```csharp
[Fact]
public void CreateProducesOneStyledVisualLine() {
	CursesTextLayout layout = CursesTextLayout.Create(
		"abc",
		new CursesTextLayoutOptions( 5 )
	);

	CursesTextVisualLine line = Assert.Single( layout.Lines );
	Assert.Equal( 3, line.Columns );
	Assert.Equal( "abc", Assert.Single( line.Fragments ).Text );
	Assert.Equal( 3, layout.CellCount );
	Assert.False( layout.IsTruncated );
}
```

Run the focused net10.0 test and observe missing layout contracts.

- [x] **Step 2: Implement options, outputs, validation, and the no-wrap path**

Add enum values/defaults and the exact option limits. `CursesTextLayout.Create` copies options and spans, calls the T2102 scanner once, validates sorted non-overlapping legal boundaries, and builds private arrays exposed through read-only views. Implement empty text, hard lines, default/style span splitting, starting column, no-wrap clip, cell/fragment counts, row limit, and `IsTruncated` first.

Add tests for nulls, enum values, all capacity edges, checked overflow, caller-list mutation after creation, empty text, trailing break, and span precedence. Observe each focused failure before its minimal implementation.

- [x] **Step 3: Add wrapping and alignment through table-driven tests**

In `CursesTextLayoutTests`, generate the complete cross-product of three wrap modes, three alignments, and two overflow modes over fixed ASCII cases. Assert exact source ranges, columns, soft/hard flags, clipping, and odd center remainder. Implement text-element wrap, word-preferred wrap with fallback, and per-line alignment without rescanning earlier source.

- [x] **Step 4: Add Unicode, tab, and ellipsis semantics**

In `CursesTextLayoutUnicodeTests`, freeze the design cases: absolute tab stops at multiple `StartingColumn` values; indivisible CRLF; malformed input replacement with stable offsets; combining/emoji/ambiguous/wide elements; leading/attached zero-width elements; too-wide element clipping; ellipsis style/metadata and empty source range; ellipsis that cannot fit.

Implement only after each group fails. Preserve complete text elements and use the configured provider. Never normalize caller text or split a width-two cell.

- [x] **Step 5: Qualify capacity and cost**

Assert source/span/row/column/fragment/cell limits fail before result publication. Add an application-shaped 80x40 editor slice with 10,000 caller-owned lines and prove only supplied visible text is inspected. Use the T2101 minimum-of-eight convention; require linear counts and a broad portable allocation ceiling, not elapsed-time success.

- [x] **Step 6: Verify and commit T2103**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug -f net10.0 \
  --filter FullyQualifiedName~CursesTextLayout
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
git diff --check
git add src tests/Icod.DCurses.Tests/src Icod.DCurses-2.1.0-Development-Roadmap.md
git commit -m "feat: add immutable rich text layout"
```

---

### Task 3: T2104 caret, hit-testing, and selection geometry

**Files:**
- Create: `src/CursesTextGeometryContracts.cs`
- Create: `src/CursesTextLayout.Geometry.cs`
- Modify: `src/CursesTextLayout.cs`
- Modify: `src/Internal/CursesTextLayoutBuilder.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextGeometryTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextGeometryUnicodeTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextGeometryPerformanceTests.cs`
- Modify: `Icod.DCurses-2.1.0-Development-Roadmap.md`

**Exact public increment:** `CursesTextAffinity`; `CursesTextVisualPosition`; `CursesTextHitTestResult`; `CursesTextSelection`; and the eight `CursesTextLayout` geometry methods from design section 5.1.

- [x] **Step 1: Write the soft-wrap affinity RED**

```csharp
[Fact]
public void SoftWrapBoundaryHonorsAffinity() {
	CursesTextLayout layout = CursesTextLayout.Create(
		"abcd",
		new CursesTextLayoutOptions( 2 ) {
			WrapMode = CursesTextWrapMode.TextElement
		}
	);

	Assert.Equal(
		new CursesTextVisualPosition( 1, 0, CursesTextAffinity.Leading ),
		layout.GetVisualPosition( new CursesTextPosition( 2 ) )
	);
	Assert.Equal(
		new CursesTextVisualPosition( 0, 2, CursesTextAffinity.Trailing ),
		layout.GetVisualPosition(
			new CursesTextPosition( 2 ),
			CursesTextAffinity.Trailing
		)
	);
}
```

Run the focused test and observe missing geometry types/methods.

- [x] **Step 2: Build indexes during layout, then map both directions**

Extend the layout builder with compact legal-boundary and per-line fragment indexes. Implement source-to-visual by binary-searching lines/local fragments and hit testing by binary-searching the requested line. Cover ordinary edges, outside-line clamping, hard breaks, clipped/ellipsis mapping, and validation. Do not rescan the source string.

- [x] **Step 3: Implement navigation and Unicode edge behavior**

Add failing cases for previous/next legal position, line start/end, saturating vertical delta, stable preferred column, width-two leader/continuation, attached zero-width, leading zero-width, and CRLF interior rejection. Implement exact affinity from the design.

- [x] **Step 4: Implement half-open selection rectangles**

Start with reversed anchor/active across three wrapped lines. Assert normalized `Start`/`End`, clipping to requested line range, no rectangle for an empty selection, and no cells for hidden source. Return a newly owned array sized to exact output.

- [x] **Step 5: Prove mapping complexity**

Use a large accepted layout and repeated local caret movement. Freeze operation counts where instrumentable and a portable allocation ceiling. `GetPreviousPosition`/`GetNextPosition` allocate zero; point mapping allocates zero; selection allocates only its returned array.

- [x] **Step 6: Verify and commit T2104**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug -f net10.0 \
  --filter FullyQualifiedName~CursesTextGeometry
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
git diff --check
git add src tests/Icod.DCurses.Tests/src Icod.DCurses-2.1.0-Development-Roadmap.md
git commit -m "feat: add text mapping and selection geometry"
```

---

### Task 4: T2105 retained layout presentation and evidence-backed bulk mutation

**Accepted:** exact executable head `b26b5a12333319f60a6cfbf9f0954629e892088c`, workflow 35947857085 (14/14). The local environment lacked `dotnet`; the exact-head workflow supplied the full .NET 8/9/10 platform/configuration gate. See `docs/T2105-Retained-Presentation-and-Bulk-Cells-Gate.md`.

**Files:**
- Create: `src/CursesWindow.TextLayout.cs`
- Create: `src/CursesWindow.BulkCells.cs`
- Modify: `src/CursesWindow.cs`
- Modify: `src/CursesWindow.Metadata.cs`
- Modify: `src/CursesWindow.Raster.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTextPresentationTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesBulkCellWriteTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesBulkCellWritePerformanceTests.cs`
- Modify: `Icod.DCurses-2.1.0-Development-Roadmap.md`

**Exact public increment:** `PresentTextLayout(CursesTextLayout, int, int, int, int)` and the row/rectangular `WriteCells` overloads from design section 6.1. No style-only, metadata-cell, raster-cell, or unified retained-value overload is authorized.

- [x] **Step 1: Write a retained-presentation RED**

```csharp
[Fact]
public void PresentTextLayoutWritesOnlyTheSelectedVisualLines() {
	CursesScreen screen = new( 4, 8 );
	CursesTextLayout layout = CursesTextLayout.Create(
		"one\ntwo",
		new CursesTextLayoutOptions( 3 )
	);

	screen.StandardWindow.PresentTextLayout( layout, 1, 1, 2, 2 );

	Assert.Equal( "t", screen.VirtualScreen[ 2, 2 ].Content );
	Assert.Equal( "w", screen.VirtualScreen[ 2, 3 ].Content );
	Assert.Equal( "o", screen.VirtualScreen[ 2, 4 ].Content );
}
```

Observe the missing member failure before implementation.

- [x] **Step 2: Implement layout projection using shared retained mutation**

Validate the complete visual-line range before mutation. Map `StartingColumn` to destination column, clip destination rows/columns, preserve the logical cursor, fill requested row width with blanks, write fragment cells/metadata, and remove replaced metadata/raster state through existing ownership helpers. Add failure-atomic, negative destination, clipping, wide-cell, metadata, raster, no-refresh, and exact-damage tests.

- [x] **Step 3: Write the row bulk RED and implement coherent input validation**

```csharp
[Fact]
public void WriteCellsWritesPreparedRowAndPreservesCursor() {
	CursesScreen screen = new( 2, 4 );
	CursesWindow window = screen.StandardWindow;
	window.Move( 1, 1 );
	window.WriteCells( 0, 1, [ new CursesCell( "a" ), new CursesCell( "b" ) ] );

	Assert.Equal( "a", screen.VirtualScreen[ 0, 1 ].Content );
	Assert.Equal( "b", screen.VirtualScreen[ 0, 2 ].Content );
	Assert.Equal( 1, window.CursorRow );
	Assert.Equal( 1, window.CursorColumn );
}
```

Implement row validation first: complete in-bounds destination, self-contained wide footprints, no leading continuation, no retained caller span, validation before mutation, and exact changed-value damage.

- [x] **Step 4: Add rectangular stride, alias, boundary repair, and ownership cases**

Observe failures for insufficient/overflowing source length, `sourceStride < columns`, padding ignored, zero dimensions, out-of-bounds rejection, an existing wide footprint crossing the destination edge, metadata clearing, raster removal/ownership failure, and source alias behavior. Implement rectangular mutation through one private prepared-block helper. Do not route each cell through the scalar public API.

- [x] **Step 5: Demonstrate material benefit**

Compare the accepted 80x24 full-frame and nine-cell workloads against T2101. Assert 1 call versus 1,920 public calls for a full frame, zero new damage for an identical bulk frame, identical final retained cells, and materially lower allocation/work counters under a broad portable bound. Keep wall-clock numbers informational.

- [x] **Step 6: Verify and commit T2105**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug -f net10.0 \
  --filter 'FullyQualifiedName~CursesTextPresentation|FullyQualifiedName~CursesBulkCellWrite'
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
git diff --check
git add src tests/Icod.DCurses.Tests/src Icod.DCurses-2.1.0-Development-Roadmap.md
git commit -m "feat: add retained text and bulk cell presentation"
```

---

### Task 5: T2106 large-content viewport geometry

**Files:**
- Create: `src/CursesCellPosition.cs`
- Create: `src/CursesViewport.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesViewportTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesViewportBoundaryTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesViewportPerformanceTests.cs`
- Modify: `Icod.DCurses-2.1.0-Development-Roadmap.md`

**Exact public increment:** `CursesCellPosition`; immutable `CursesViewport` constructor/properties plus its 13 methods from design section 7.1.

- [x] **Step 1: Write the clamp/visibility RED**

```csharp
[Fact]
public void ConstructorClampsOriginAndReportsVisibleContent() {
	CursesViewport viewport = new( 100, 200, 20, 40, 99, 199 );

	Assert.Equal( 80, viewport.OriginRow );
	Assert.Equal( 160, viewport.OriginColumn );
	Assert.Equal( new CursesRectangle( 80, 160, 20, 40 ), viewport.VisibleContent );
}
```

Observe missing types, then implement constructor validation, clamping, value equality, and `VisibleContent` only.

- [x] **Step 2: Add immutable resize/move/pan/page operations test-first**

Cover content shrink, oversized/zero viewport, empty content, start/end, negative/extreme deltas, and page multiplication at numeric boundaries. Use widened intermediates and return new values; allocate no heap objects.

- [x] **Step 3: Add ensure-visible, overscan, and translations test-first**

Cover point/rectangle validation, smallest movement, leading-edge rule for oversized rectangles, impossible zero axis, symmetric overscan clipping, translations at every visible edge, and default out value on false.

- [x] **Step 4: Prove virtualized application shape**

Drive a 10,000,000-row synthetic document and a 2,048x2,048 algorithmic world through viewport calculations while materializing only 80x40 or 80x24 cells plus explicit overscan. Assert viewport operations stay constant-time by deterministic operation counts and allocate zero after warmup.

- [x] **Step 5: Verify and commit T2106**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug -f net10.0 \
  --filter FullyQualifiedName~CursesViewport
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
git diff --check
git add src tests/Icod.DCurses.Tests/src Icod.DCurses-2.1.0-Development-Roadmap.md
git commit -m "feat: add large-content viewport geometry"
```

---

### Task 6: T2107 stateless track layout

**Files:**
- Create: `src/CursesTrack.cs`
- Create: `src/CursesLayout.Tracks.cs`
- Modify: `src/CursesLayout.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTrackLayoutTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTrackLayoutApplicationTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesTrackLayoutPerformanceTests.cs`
- Modify: `Icod.DCurses-2.1.0-Development-Roadmap.md`

**Exact public increment:** `CursesTrackKind`, `CursesTrackDistribution`, `CursesTrack.Fixed`, `CursesTrack.Weighted`, their four properties, and `CursesLayout.ArrangeRows`/`ArrangeColumns` from design section 8.1.

- [x] **Step 1: Write the fixed/weighted RED**

```csharp
[Fact]
public void ArrangeColumnsAssignsFixedThenWeightedSpace() {
	CursesRectangle[] result = CursesLayout.ArrangeColumns(
		new CursesRectangle( 0, 0, 10, 20 ),
		[ CursesTrack.Fixed( 4 ), CursesTrack.Weighted(), CursesTrack.Weighted( 2 ) ],
		gap: 1
	);

	Assert.Equal( 4, result[ 0 ].Columns );
	Assert.Equal( 5, result[ 1 ].Columns );
	Assert.Equal( 9, result[ 2 ].Columns );
}
```

Observe missing track contracts. Make `CursesLayout` partial and implement factories/validation plus zero/fixed-only/simple weighted arrangements.

- [x] **Step 2: Freeze caps, minima, deterministic remainder, and distributions**

Add table-driven tests for minimum/maximum saturation, low-index weighted remainder, explicit gaps, all six distributions, zero/one track special cases, odd center remainder, infeasible minima, 4,096/4,097 tracks, and arithmetic overflow. Allocate the result only after validation and feasibility.

Implement capped apportionment without iterating once per free cell; runtime must depend on track count, not rectangle extent. Use widened arithmetic for weights and spacing.

- [x] **Step 3: Prove row/column symmetry and application layouts**

Transpose identical cases across axes. Add the editor document/status/prompt arrangement and roguelike map/sidebar/message arrangement over resize sequences. Assert complete non-overlap, bounds containment, deterministic remainder, and no retained tree/window mutation.

- [x] **Step 4: Qualify allocation and commit T2107**

Assert one exact result-array allocation plus bounded scratch proportional to at most 4,096 tracks. Repeated existing `Dock`/split helpers retain their T2101 allocation behavior.

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug -f net10.0 \
  --filter FullyQualifiedName~CursesTrackLayout
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Debug
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
git diff --check
git add src tests/Icod.DCurses.Tests/src Icod.DCurses-2.1.0-Development-Roadmap.md
git commit -m "feat: add stateless track layout"
```

---

### Task 7: T2108 bounded refresh diagnostics and performance qualification

**Files:**
- Create: `src/CursesRefreshDiagnostics.cs`
- Modify: `src/CursesSessionOptions.cs`
- Modify: `src/Integration/CursesSession.Refresh.Terminal.cs`
- Modify: `src/Integration/CursesSession.Terminal.cs`
- Modify: `src/Internal/CursesRefreshEngine.cs`
- Modify: `src/Internal/CursesPreparedRefresh.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRefreshDiagnosticsTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRefreshDiagnosticsInvalidationTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesRefreshDiagnosticsPerformanceTests.cs`
- Create: `tests/Icod.DCurses.Tests/src/CorePresentationTextPerformanceQualificationTests.cs`
- Modify: `Icod.DCurses-2.1.0-Development-Roadmap.md`

**Exact public increment:** `CursesRefreshOutcome`; flagged `CursesRefreshOperationKinds`; immutable `CursesRefreshDiagnosticsSnapshot`; `CursesSessionOptions.EnableRefreshDiagnostics`; `CursesSession.LatestRefreshDiagnostics` from design section 9.1.

- [ ] **Step 1: Write the disabled/default RED**

```csharp
[Fact]
public void RefreshDiagnosticsAreDisabledAndEmptyByDefault() {
	CursesSessionOptions options = new();
	Assert.False( options.EnableRefreshDiagnostics );

	using TerminalScreenTestSession context = TerminalScreenTestSession.Create( options );
	Assert.Null( context.Session.LatestRefreshDiagnostics );
}
```

Adapt fixture construction to the existing async lifetime pattern. Observe missing properties, then add declarations/default only. Do not collect counters yet.

- [ ] **Step 2: Add an internal value accumulator without disabled-path allocation**

Pass a nullable/by-ref internal accumulator through existing refresh planning and commit code. When diagnostics are disabled, retain the exact 2.0/T2107 call path and allocate nothing attributable to diagnostics. When enabled, count examined/changed cells, damaged rows/regions, prepared items, application payloads, raster cells, full repaint, and operation-kind union. Saturate public counters as specified.

- [ ] **Step 3: Publish one truthful immutable snapshot after each outcome**

Add failing integration tests for success, cancellation before output, cancellation after output may begin, output failure before/after write, physical invalidation, logical publication, and forced repaint retry. Construct at most one snapshot after the attempt and publish it atomically. Retain only the latest snapshot. Preserve existing exception/cancellation propagation exactly.

- [ ] **Step 4: Prove semantic boundary and bounded state**

Reflection/public-contract tests must show no Terminal or TermInfo types and no serialized byte/string payloads in the snapshot. Repeated refresh retains one snapshot only. Operation categories report semantic work actually prepared, not claimed physical support.

- [ ] **Step 5: Run final application-shaped performance qualification**

In `CorePresentationTextPerformanceQualificationTests`, rerun and compare:

- rich layout and mapping over the representative Unicode editor slice;
- editor typing, vertical scroll, and horizontal scroll;
- roguelike full-frame and nine-cell local update;
- viewport calculations at large content extents;
- track recomputation for 160x48 layouts;
- diagnostics disabled versus enabled.

Require the frozen complexity properties, zero attributable disabled-path allocation after warmup, at most one enabled snapshot per refresh, visible-slice memory independence from total content, and the accepted bulk improvement. Use deterministic counts and broad portable byte ceilings; elapsed values are informational.

- [ ] **Step 6: Verify the production-plan gate and commit T2108**

```sh
dotnet restore Icod.DCurses.sln
dotnet build Icod.DCurses.sln -c Debug --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Debug --no-build --no-restore
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Staging --no-build --no-restore
pwsh ./packaging/Invoke-Build.ps1 -Section validate -Configuration Staging
rg -n 'Icod\.TermInfo|TerminalDescription|StringCapability' src Icod.DCurses.csproj
git diff --check
```

Expected: all supported TFMs and configurations pass; package validation passes; production search has no direct TermInfo/capability match; version remains `2.1.0-alpha.1`, assembly `2.0.0.0`, and Terminal `1.18.0`.

```sh
git add src tests/Icod.DCurses.Tests/src Icod.DCurses-2.1.0-Development-Roadmap.md
git commit -m "feat: add bounded refresh diagnostics"
git push origin 2.1.0-roadmap
```

Require the ordinary exact-head Windows/Linux/macOS PR matrix before marking T2108 accepted. Do not begin the roguelike/editor sample plans until this production-plan gate is green.

## Completion handoff

After T2108, write separate plans for T2109 and T2110 against the now-observable public surface. T2111 owns README/sample-index/release documentation, adversarial closure, and the final 2.1 public fingerprint. T2112 owns RC/stable-source identity and matrix evidence. Merge, tag, GitHub Release, and NuGet publication remain maintainer actions outside automated implementation.
