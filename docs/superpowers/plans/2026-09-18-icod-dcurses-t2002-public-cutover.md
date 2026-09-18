# Icod.DCurses 2.0 T2002 Public Cutover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish the 2.0 development identity and replace the four approved TermInfo-bearing public contracts with Terminal 1.18-owned profile and dimensions contracts, without beginning renderer migration.

**Architecture:** Keep the existing 1.6 renderer and its temporary internal `TerminalDescription` access intact while removing that type from the public surface. Delegate live dimensions directly to `TerminalSession.GetDimensions()`, project lifecycle dimensions from `TerminalLifecycleEvent.Dimensions`, and freeze the resulting 2.0 API in a new fingerprint without modifying historical 1.x artifacts.

**Tech Stack:** C# 13, .NET 8/9/10, xUnit, NuGet, PowerShell 5.1-compatible packaging automation, GitHub Actions. No Python.

**Spec:** `Icod.DCurses-2.0.0-Development-Roadmap.md` T2002 and `docs/2.0-API-Break-Manifest.md`.

## Global Constraints

- Set both `Version` and `PackageVersion` to `2.0.0-alpha.1`; set `AssemblyVersion` to `2.0.0.0`.
- Select published `Icod.Terminal 1.18.0` in production.
- Keep the direct production `Icod.TermInfo 1.15.0` reference required by Terminal 1.18 until T2007; T2002 removes public leaks, not all implementation coupling.
- Implement exactly four approved public contract replacements: `Terminal` to `Profile`, two dimensions return types, and lifecycle `Dimensions`.
- Do not add an obsolete shim, overload, conversion, type forwarder, duplicate profile/dimensions model, or new public break.
- Preserve `TerminalControlResult<T>` status, message, native-error and value semantics by delegating to Terminal's public dimensions API.
- Preserve all historical `docs/Public-API-Fingerprint-*.json` artifacts byte-for-byte and add a separate 2.0 development artifact.
- Do not migrate presentation planning, refresh batching, output transactions, raw-output shims, samples, or package dependency groups in this tranche.
- Use exact-head GitHub Actions evidence because the isolated executor has no local .NET SDK.
- Do not merge, tag, publish, or create a release.

---

### Task 1: Establish a runtime RED contract witness

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/PublicTwoZeroApiContractTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/PublicDependencyBoundaryTests.cs`

**Interfaces:**
- Consumes: the frozen 1.6 assembly and `docs/2.0-API-Break-Manifest.md`.
- Produces: reflection-based tests for `CursesSession.Profile`, removed public `Terminal`, both Terminal-owned dimensions methods, lifecycle dimensions, assembly identity, and a public dependency set containing no TermInfo type.

- [ ] **Step 1: Write the reflection-based failing API test**

Create `PublicTwoZeroApiContractTests` with a single contract test that obtains declared public members by reflection so it compiles against the 1.6 surface:

```csharp
[Fact]
public void PublicSurfaceMatchesTheApprovedTwoZeroBreakManifest() {
	Type session = typeof( CursesSession );
	PropertyInfo? profile = session.GetProperty(
		"Profile",
		BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
	);
	PropertyInfo? terminal = session.GetProperty(
		"Terminal",
		BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
	);
	MethodInfo getDimensions = Assert.IsAssignableFrom<MethodInfo>(
		session.GetMethod( "GetDimensions", Type.EmptyTypes )
	);
	MethodInfo synchronizeDimensions = Assert.IsAssignableFrom<MethodInfo>(
		session.GetMethod( "SynchronizeDimensions", Type.EmptyTypes )
	);
	PropertyInfo lifecycleDimensions = Assert.IsAssignableFrom<PropertyInfo>(
		typeof( CursesLifecycleEvent ).GetProperty( "Dimensions" )
	);
	Type dimensionsResult = typeof( TerminalControlResult<TerminalDimensions> );

	Assert.NotNull( profile );
	Assert.Equal( typeof( TerminalProfile ), profile.PropertyType );
	Assert.Null( terminal );
	Assert.Equal( dimensionsResult, getDimensions.ReturnType );
	Assert.Equal( dimensionsResult, synchronizeDimensions.ReturnType );
	Assert.Equal( typeof( TerminalDimensions? ), lifecycleDimensions.PropertyType );
	Assert.Equal(
		new Version( 2, 0, 0, 0 ),
		typeof( CursesSession ).Assembly.GetName().Version
	);
}
```

- [ ] **Step 2: Change the public dependency expectation to Terminal-only**

Set `ExpectedDependencyTypes` in `PublicDependencyBoundaryTests` to exactly:

```csharp
{
	"Icod.Terminal.TerminalControlResult`1",
	"Icod.Terminal.TerminalDimensions",
	"Icod.Terminal.TerminalEndpoint",
	"Icod.Terminal.TerminalProfile",
	"Icod.Terminal.TerminalRasterImage",
	"Icod.Terminal.TerminalSession"
}
```

Rename the test and summary to state that the public API exposes only approved Terminal types and no TermInfo types. Keep recursive generic/type traversal unchanged.

- [ ] **Step 3: Push and verify the intended RED result**

Run locally when an SDK is available:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~PublicTwoZeroApiContractTests|FullyQualifiedName~PublicDependencyBoundaryTests'
```

In the current executor, commit and push the tests, then inspect the exact-head PR workflow. Expected: tests build, then fail because `Profile` is absent, public `Terminal` is present, dimensions expose `TerminalSize`, AssemblyVersion is `1.0.0.0`, and the dependency set still contains two TermInfo types. Any compile failure is a malformed witness and must be corrected before implementation.

- [ ] **Step 4: Commit the RED witness**

```sh
git add tests/Icod.DCurses.Tests/src/PublicTwoZeroApiContractTests.cs \
  tests/Icod.DCurses.Tests/src/PublicDependencyBoundaryTests.cs
git commit -m "test: freeze DCurses 2.0 public cutover"
```

### Task 2: Establish 2.0 identity and implement the four public replacements

**Files:**
- Modify: `Icod.DCurses.csproj`
- Modify: `src/Integration/CursesSession.Terminal.cs`
- Modify: `src/Integration/CursesSession.Screen.Terminal.cs`
- Modify: `src/Integration/CursesLifecycleEvent.Terminal.cs`
- Modify: `src/Integration/CursesSession.Lifecycle.Terminal.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesTerminalIntegrationTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesInteractionCoherenceTests.cs`

**Interfaces:**
- Consumes: `TerminalSession.Profile`, `TerminalSession.GetDimensions()`, `TerminalLifecycleEvent.Dimensions`, and the exact signatures frozen by Task 1.
- Produces: `CursesSession.Profile : TerminalProfile`, `GetDimensions()` and `SynchronizeDimensions()` returning `TerminalControlResult<TerminalDimensions>`, and `CursesLifecycleEvent.Dimensions : TerminalDimensions?`.

- [ ] **Step 1: Update development and dependency metadata**

Change the project metadata to:

```xml
<Version>2.0.0-alpha.1</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<PackageVersion>2.0.0-alpha.1</PackageVersion>
```

Replace the release notes with a concise alpha statement naming the Terminal-only public cutover and unfinished internal decoupling. Change only the production Terminal reference:

```xml
<PackageReference Include="Icod.Terminal" Version="1.18.0" />
<PackageReference Include="Icod.TermInfo" Version="1.15.0" />
```

- [ ] **Step 2: Replace the public terminal-description property**

In `CursesSession.Terminal.cs`, add:

```csharp
/// <summary>Gets the Terminal-owned semantic profile selected for this session.</summary>
public TerminalProfile Profile => this.terminalSession.Profile;
```

Change the existing `Terminal` property from `public` to `internal` and update its summary to identify it as a temporary renderer-migration seam. Keep its `TerminalDescription` return type and all existing internal callers during T2002. This removes the public leak without pulling T2003 presentation work into the tranche.

- [ ] **Step 3: Delegate live dimensions to Terminal-owned values**

Replace `CursesSession.GetDimensions()` with:

```csharp
public TerminalControlResult<TerminalDimensions> GetDimensions() {
	return this.terminalSession.GetDimensions();
}
```

In `CursesSession.Screen.Terminal.cs`, remove the TermInfo using and change the local/result types in lazy screen creation and `SynchronizeDimensions()` from `TerminalSize` to `TerminalDimensions`. Keep the existing resize and invalidation logic unchanged.

- [ ] **Step 4: Change lifecycle dimensions without changing event policy**

In `CursesLifecycleEvent.Terminal.cs`, replace `TerminalSize?` with `TerminalDimensions?` in the internal constructor and public property, and use `Icod.Terminal` rather than `Icod.TermInfo`.

In `CursesSession.Lifecycle.Terminal.cs`, use `terminalEvent.Dimensions` for resize/resume checks, logical resize, and `CursesLifecycleEvent` construction:

```csharp
TerminalDimensions? dimensions = terminalEvent.Dimensions;
if (
	terminalEvent.Kind is TerminalLifecycleEventKind.Resize or TerminalLifecycleEventKind.Resumed
	&& dimensions.HasValue
) {
	_ = this.ResizeLogicalScreen(
		dimensions.Value.Columns,
		dimensions.Value.Rows
	);
}
return new CursesLifecycleEvent( kind, dimensions );
```

Do not change repaint invalidation or lifecycle kind mapping.

- [ ] **Step 5: Update strongly typed integration assertions**

In `CursesTerminalIntegrationTests.DimensionsComeDirectlyFromTerminalSession`, assert:

```csharp
Assert.Same( terminalSession.Profile, session.Profile );
TerminalControlResult<TerminalDimensions> dimensions = session.GetDimensions();
Assert.True( dimensions.IsAvailable );
Assert.Equal( new TerminalDimensions( 101, 37 ), dimensions.GetRequiredValue() );
```

In `CursesInteractionCoherenceTests`, change only the two `SynchronizeDimensions()` result variables and expected values to `TerminalControlResult<TerminalDimensions>`/`TerminalDimensions`. Keep `TerminalSize` in fake `ITerminalControlProvider.GetSize()` implementations because that is Terminal's legacy provider seam, not DCurses public API.

- [ ] **Step 6: Verify GREEN on the public and focused integration tests**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~PublicTwoZeroApiContractTests|FullyQualifiedName~PublicDependencyBoundaryTests|FullyQualifiedName~CursesTerminalIntegrationTests|FullyQualifiedName~CursesInteractionCoherenceTests'
```

Expected: all selected tests pass; no warning; the Task 1 reflection witness is green.

- [ ] **Step 7: Commit the public cutover**

```sh
git add Icod.DCurses.csproj src/Integration \
  tests/Icod.DCurses.Tests/src/CursesTerminalIntegrationTests.cs \
  tests/Icod.DCurses.Tests/src/CursesInteractionCoherenceTests.cs
git commit -m "feat: establish DCurses 2.0 public Terminal boundary"
```

### Task 3: Harden dimensions result and lifecycle parity

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/CursesTerminalIntegrationTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesLifecycleHardeningTests.cs`

**Interfaces:**
- Consumes: the Terminal-owned dimensions contracts implemented in Task 2.
- Produces: behavioral proof for available/unavailable/unsupported/failed dimensions and nullable lifecycle dimensions, while preserving screen resize/invalidation and ownership behavior.

- [ ] **Step 1: Add controlled-result parity cases**

Extend the terminal integration test provider with a configurable `TerminalControlResult<TerminalSize> SizeResult`, defaulting to its current available size. Add a theory or four focused facts that call `session.GetDimensions()` and assert:

```text
Available   -> TerminalDimensions value has identical columns/rows
Unavailable -> status, message and native error code are preserved
Unsupported -> status and message are preserved; no value
Failed      -> status, message and native error code are preserved
```

Use literal messages and native codes in the fixture. Do not reconstruct a DCurses result; the production method must remain a direct delegation.

- [ ] **Step 2: Add synchronization behavior cases**

Create a materialized screen, change the fake provider from 80x24 to 40x12, call `SynchronizeDimensions()`, and assert the returned `TerminalDimensions` and screen dimensions match. Then set a failed result, call `SynchronizeDimensions()`, and assert the existing logical screen remains 40x12 and the exact failure metadata is returned.

- [ ] **Step 3: Add lifecycle dimensions cases**

In `CursesLifecycleHardeningTests`, drive one resize event with dimensions and one interrupt event without dimensions. Assert the DCurses event exposes `new TerminalDimensions(columns, rows)` for resize, `null` for interrupt, preserves kind, and retains existing resize/repaint behavior.

- [ ] **Step 4: Verify the focused behavior tests**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~CursesTerminalIntegrationTests|FullyQualifiedName~CursesLifecycleHardeningTests|FullyQualifiedName~CursesInteractionCoherenceTests'
```

Expected: all selected tests pass on net8.0, net9.0 and net10.0.

- [ ] **Step 5: Commit the parity hardening**

```sh
git add tests/Icod.DCurses.Tests/src/CursesTerminalIntegrationTests.cs \
  tests/Icod.DCurses.Tests/src/CursesLifecycleHardeningTests.cs
git commit -m "test: harden Terminal-owned dimensions parity"
```

### Task 4: Freeze the 2.0 development API without rewriting history

**Files:**
- Create: `docs/Public-API-Fingerprint-2.0.json`
- Create: `docs/Public-API-Baseline-2.0.md`
- Create: `tests/Icod.DCurses.Tests/src/PublicTwoZeroApiBaselineTests.cs`
- Modify: `tests/Icod.DCurses.Tests/src/PublicApiFingerprintTests.cs`
- Modify: `tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj`
- Preserve unchanged: `docs/Public-API-Fingerprint-0.9.json` through `docs/Public-API-Fingerprint-1.6.json`
- Preserve unchanged: `tests/Icod.DCurses.Tests/src/PublicOneSixApiBaselineTests.cs`

**Interfaces:**
- Consumes: the compiled Task 2 public surface and frozen 1.6 fingerprint.
- Produces: a 2.0-alpha fingerprint selected by the active development guard and an explicit approved-diff baseline.

- [ ] **Step 1: Select a distinct 2.0 fingerprint file**

Add `docs/Public-API-Fingerprint-2.0.json` to the test project as `Public-API-Fingerprint-2.0.json`. Change `PublicApiFingerprintTests.PublicApiMatchesCurrentDevelopmentFingerprint` to load that filename directly. Leave the historical `Public-API-Fingerprint-1.2.json` alias pointing at the 1.6 artifact so `PublicOneSixApiBaselineTests` remains an immutable historical guard.

- [ ] **Step 2: Capture the compiled fingerprint values**

Create the new JSON as an exact copy of `docs/Public-API-Fingerprint-1.6.json`, then change its identity fields to:

```text
schema = 1
release = 2.0.0-alpha.1
status = development
sha256 = 0000000000000000000000000000000000000000000000000000000000000000
exportedTypeCount = 75
contractLineCount = 559
exportedTypes = the byte-for-byte copied JSON array from Public-API-Fingerprint-1.6.json
```

Push once with an intentionally invalid SHA-256 value and inspect the existing mismatch message for the actual SHA-256, exported type count and contract line count. Replace the invalid value immediately. If either count differs from 75/559, stop and compare reflection output to the break manifest before accepting the artifact.

- [ ] **Step 3: Add an explicit 2.0 artifact guard**

Create `PublicTwoZeroApiBaselineTests` to assert schema `1`, release `2.0.0-alpha.1`, status `development`, the accepted hash/counts, and equality between the artifact's exported types and the frozen 1.6 exported-type list. This proves T2002 changes members but adds/removes no public type.

- [ ] **Step 4: Document the exact API delta**

Create `docs/Public-API-Baseline-2.0.md` containing:

- package/development and assembly identities;
- Terminal 1.18.0 and temporary TermInfo 1.15.0 production references;
- exact hash/counts from the compiled artifact;
- the four old/new signatures from `docs/2.0-API-Break-Manifest.md`;
- confirmation that all 75 exported type names are unchanged;
- confirmation that the public dependency set contains no `Icod.TermInfo` type; and
- an explicit statement that internal TermInfo/renderer decoupling remains T2003-T2007 work.

- [ ] **Step 5: Verify current and historical API guards**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter 'FullyQualifiedName~PublicApiFingerprintTests|FullyQualifiedName~PublicOneSixApiBaselineTests|FullyQualifiedName~PublicTwoZeroApiBaselineTests|FullyQualifiedName~PublicTwoZeroApiContractTests|FullyQualifiedName~PublicDependencyBoundaryTests'
```

Expected: all active 2.0 and immutable 1.6 guards pass.

- [ ] **Step 6: Commit the 2.0 API baseline**

```sh
git add docs/Public-API-Fingerprint-2.0.json docs/Public-API-Baseline-2.0.md \
  tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj \
  tests/Icod.DCurses.Tests/src/PublicApiFingerprintTests.cs \
  tests/Icod.DCurses.Tests/src/PublicTwoZeroApiBaselineTests.cs
git commit -m "docs: freeze DCurses 2.0 development API"
```

### Task 5: Qualify and close T2002

**Files:**
- Create: `docs/T2002-Public-Terminal-Cutover-Gate.md`
- Modify: `Icod.DCurses-2.0.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Modify: `docs/superpowers/plans/2026-09-18-icod-dcurses-t2002-public-cutover.md`

**Interfaces:**
- Consumes: Tasks 1-4 and their exact-head workflow evidence.
- Produces: an accepted T2002 checkpoint authorizing T2003, or a precise blocker with later tranches still pending.

- [ ] **Step 1: Run static boundary checks**

```sh
rg -n 'public .*Icod\.TermInfo|public .*TerminalDescription|public .*TerminalSize' src
rg -n '<Version>|<PackageVersion>|<AssemblyVersion>|Icod\.Terminal|Icod\.TermInfo' Icod.DCurses.csproj
git diff --check
```

Expected: no public TermInfo match; both package versions are `2.0.0-alpha.1`; assembly version is `2.0.0.0`; production references are Terminal 1.18.0 and temporary TermInfo 1.15.0; no whitespace errors.

- [ ] **Step 2: Run complete build, tests and package validation**

```sh
dotnet restore Icod.DCurses.sln
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Staging --no-build --no-restore --logger trx
pwsh ./packaging/Invoke-Build.ps1 -Section validate -Configuration Staging
```

In the current executor, use the exact-head PR workflow. Required matrix: package candidate and Windows/Linux/macOS x64/ARM64 all green. Record per-framework test counts from Linux x64 logs.

- [ ] **Step 3: Review the compiled API diff**

Compare the 1.6 and 2.0 fingerprints plus Task 1 reflection assertions. Accept only:

```text
- CursesSession.Terminal : Icod.TermInfo.TerminalDescription
+ CursesSession.Profile : Icod.Terminal.TerminalProfile
~ CursesSession.GetDimensions() return generic argument
~ CursesSession.SynchronizeDimensions() return generic argument
~ CursesLifecycleEvent.Dimensions property type
~ AssemblyVersion 1.0.0.0 -> 2.0.0.0
```

No exported type, enum value, unrelated signature, nullability, default, constraint, or ownership behavior may change.

- [ ] **Step 4: Record and apply the gate**

Create `docs/T2002-Public-Terminal-Cutover-Gate.md` with exact commit/workflow/job links, package versions, fingerprint hash/counts, test counts, public dependency set, retained internal TermInfo debt, and any blocker. Mark T2002 accepted in both roadmaps only when every Task 5 criterion passes; otherwise leave T2003 pending.

- [ ] **Step 5: Commit the gate**

```sh
git add docs/T2002-Public-Terminal-Cutover-Gate.md \
  docs/superpowers/plans/2026-09-18-icod-dcurses-t2002-public-cutover.md \
  Icod.DCurses-2.0.0-Development-Roadmap.md Icod.DCurses-Development-Roadmap.md
git commit -m "docs: record DCurses T2002 public cutover gate"
```

Do not begin T2003 unless T2002 is explicitly accepted.
