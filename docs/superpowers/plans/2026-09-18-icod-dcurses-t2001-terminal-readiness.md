# Icod.DCurses 2.0.0 T2001 Terminal Readiness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Freeze the complete DCurses-to-TermInfo coupling inventory and prove that the published Terminal screen API can support DCurses recovery and bounded transactional refresh before any renderer migration begins.

**Architecture:** T2001 changes documentation and downstream acceptance tests only. The production package remains version 1.6.0 with its existing dependencies. The test project resolves published Terminal 1.17.0 directly so readiness is measured against the intended dependency rather than the production project's 1.15.0 floor. A missing safe unknown-rendition recovery operation is a blocking upstream result, not permission to reinterpret `PlanRenditionReset(current)` or emit raw terminal strings.

**Tech Stack:** C# 13, .NET 8/9/10, xUnit, PowerShell 5.1-compatible repository automation, GitHub Actions. No Python.

**Spec:** `Icod.DCurses-2.0.0-Development-Roadmap.md`, especially sections 4-8 and tranche T2001.

## Global Constraints

- Production code, `Version`, `PackageVersion`, `AssemblyVersion`, and production package references remain unchanged in T2001.
- The intended dependency direction is `Icod.DCurses -> Icod.Terminal -> Icod.TermInfo`.
- TermInfo is allowed only in explicitly inventoried test-fixture construction during T2001; it remains forbidden as the eventual production boundary.
- Readiness tests consume published `Icod.Terminal 1.17.0`; a required upstream change must be published before a later DCurses tranche consumes it.
- Use exact-head GitHub Actions evidence because the current isolated executor has no installed .NET SDK.
- Stop at the first genuine upstream blocker. Record it precisely and do not begin T2002 or reinterpret an existing Terminal contract.

---

### Task 1: Freeze the coupling inventory and public break manifest

**Files:**
- Create: `docs/2.0-TermInfo-Coupling-Inventory.md`
- Create: `docs/2.0-API-Break-Manifest.md`

**Interfaces:**
- Consumes: source snapshot `c6c365fecb1257b2fed931a57f00e8d516af277f` and published 1.6 API artifacts.
- Produces: a classified source/test/tool/document inventory and the complete approved 2.0 public break set used by T2002-T2007.

- [x] **Step 1: Generate the raw references**

Run:

```sh
rg -n --glob '*.cs' --glob '*.csproj' --glob '*.props' --glob '*.targets' \
  'Icod\.TermInfo|TerminalDescription|TerminalSize|StringCapability|BooleanCapability|NumericCapability|TermInfoParameter|TermInfoOutput|WriteTerminalStringAsync|\.GetSize\(|terminalEvent\.Size' \
  src tests samples tools packaging Icod.DCurses.csproj
```

Classify every result as public API, production implementation, test-fixture bootstrap, package/tooling, or historical/current documentation. Record exact paths and the Terminal replacement for each production family.

- [x] **Step 2: Freeze the public break manifest**

Record these approved replacements and enumerate every affected canonical signature from `docs/Public-API-Baseline-1.6.md`:

```text
CursesSession.Terminal
    remove
CursesSession.Profile
    add as Icod.Terminal.TerminalProfile
CursesSession.GetDimensions()
    return TerminalControlResult<Icod.Terminal.TerminalDimensions>
CursesSession.SynchronizeDimensions()
    return TerminalControlResult<Icod.Terminal.TerminalDimensions>
CursesLifecycleEvent.Dimensions
    become Icod.Terminal.TerminalDimensions?
AssemblyVersion
    become 2.0.0.0 in T2002
```

State explicitly that all other public changes require a roadmap amendment and review.

- [x] **Step 3: Self-check the documents**

Run:

```sh
rg -n '\bT(ODO|BD)\b' docs/2.0-TermInfo-Coupling-Inventory.md docs/2.0-API-Break-Manifest.md
git diff --check
```

Expected: no placeholder matches and no whitespace errors.

- [x] **Step 4: Commit the evidence**

```sh
git add docs/2.0-TermInfo-Coupling-Inventory.md docs/2.0-API-Break-Manifest.md
git commit -m "docs: freeze DCurses 2.0 dependency break inventory"
```

### Task 2: Establish the Terminal 1.17 test-only readiness floor

**Files:**
- Modify: `tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj`
- Create: `tests/Icod.DCurses.Tests/src/TerminalScreenReadinessTests.cs`

**Interfaces:**
- Consumes: published `Icod.Terminal 1.17.0`, existing `ITerminalControlProvider`, `ITerminalInput`, and `ITerminalOutput` public test seams.
- Produces: a downstream acceptance witness for a safe unconditional rendition baseline.

- [x] **Step 1: Add the test-only Terminal floor**

Add to the test project's package references:

```xml
<PackageReference Include="Icod.Terminal" Version="1.17.0" />
```

Do not modify `Icod.DCurses.csproj` in T2001.

- [x] **Step 2: Write the failing unknown-rendition recovery test**

Create `TerminalScreenReadinessTests.cs` with a real in-memory Terminal session whose synthetic profile advertises `ExitAttributeMode = "<sgr0>"` and `OriginalColorPair = "<op>"`. The test must express the required semantic API directly:

```csharp
[Fact]
public async Task UnknownRenditionCanEstablishAVisibleSafeBaseline() {
	RecordingOutput output = new();
	await using TerminalSession session = await OpenSessionAsync( output );
	TerminalScreenOperationPlan plan = session.Screen.PlanRenditionBaseline()
		?? throw new InvalidOperationException(
			"The selected profile cannot establish a rendition baseline."
		);
	TerminalScreenOutputTransaction transaction =
		session.CreateScreenOutputTransaction();
	transaction.Add( plan );

	await transaction.CommitAsync();

	Assert.Equal( "<sgr0><op>", output.Text );
}
```

The test helpers must use real public Terminal session behavior and a complete test transport. Do not call `PlanRenditionReset(TerminalScreenRendition.Default)`: default is a claimed known current state, not unknown physical state.

- [ ] **Step 3: Verify RED on the exact pushed head**

Run through the normal PR workflow:

```sh
dotnet test Icod.DCurses.sln -c Staging --logger trx
```

Expected on Terminal 1.17.0: build failure naming the missing `TerminalScreenPlanner.PlanRenditionBaseline()` API. Confirm the failure is the intended missing semantic operation and not package restore, syntax, helper, or unrelated test failure.

- [ ] **Step 4: Stop on the upstream blocker**

If Step 3 fails as expected, do not change DCurses production code and do not proceed to Tasks 3-4. Proceed directly to Task 5's blocked closure: record the exact DCurses witness head, workflow/job, compiler diagnostic, required Terminal signature, semantics, and minimum proposed Terminal release in `docs/T2001-Terminal-Boundary-and-Readiness-Gate.md`. Revert the intentionally uncompilable witness from the active PR with a normal follow-up commit while preserving the red-run link as evidence.

The minimum upstream contract is:

```csharp
public TerminalScreenOperationPlan? PlanRenditionBaseline();
```

It must plan a safe unconditional return to Terminal's normalized default rendition using available attribute and original-color restoration capabilities, return an empty valid plan only when the selected profile exposes no state that Terminal could have changed, remain side-effect free, and be emitted only through a same-session output transaction.

### Task 3: Harden the readiness witnesses after the blocker is resolved

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/TerminalScreenReadinessTests.cs`
- Modify: `tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj` only to select the published Terminal version containing the approved fix

**Interfaces:**
- Consumes: the published Terminal recovery contract accepted from Task 2.
- Produces: downstream characterization of transaction capacity, stale epochs, and plan costs.

- [ ] **Step 1: Verify GREEN for the retained baseline test**

Run:

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~TerminalScreenReadinessTests.UnknownRenditionCanEstablishAVisibleSafeBaseline
```

Expected: one passing test and output bytes exactly `<sgr0><op>`.

- [ ] **Step 2: Write the transaction-limit test**

Add a test which creates a transaction, performs 65,536 one-byte `WriteText("x")` additions, verifies the 65,537th addition throws before commit, and asserts the recording output remains empty. The break it catches is partial emission or an undocumented change to the published 1.17 item bound.

- [ ] **Step 3: Write the stale-epoch test**

Create a transaction containing `"transaction"`, perform a separate session-owned `WriteTextAsync("outside")`, then verify transaction commit throws `InvalidOperationException` and output equals only `"outside"`. The break it catches is stale transactional output being appended or blindly replayed.

- [ ] **Step 4: Write the semantic cost test**

Create a profile whose parameterized cursor move and repeated relative moves have hand-computed costs. Assert `PlanCursorMove(...).ByteCount` is the exact shorter encoded byte count and commit emits the expected literal bytes. The expected byte sequence must be a literal, not generated with TermInfo expansion in the assertion.

- [ ] **Step 5: Verify each RED/GREEN cycle and the focused class**

```sh
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging \
  --filter FullyQualifiedName~TerminalScreenReadinessTests
```

Expected: all readiness tests pass with no warnings.

- [ ] **Step 6: Commit the accepted witness**

```sh
git add tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj \
  tests/Icod.DCurses.Tests/src/TerminalScreenReadinessTests.cs
git commit -m "test: qualify Terminal screen boundary for DCurses 2.0"
```

### Task 4: Capture the 1.6 behavioral migration baseline

**Files:**
- Create: `docs/T2001-DCurses-1.6-Behavioral-Baseline.md`

**Interfaces:**
- Consumes: existing refresh/optimization/lifecycle/mixed-media tests and published 1.6 API fingerprint.
- Produces: named parity witnesses and metrics that T2003-T2009 must retain or explicitly review.

- [ ] **Step 1: Record representative existing witnesses**

List exact tests for full and sparse repaint, cursor placement, rendition minimization, ACS/Unicode fallback, erase/character/line shifts, hyperlinks, raster placeholders, synchronized framing, failure invalidation, lifecycle suspend/resume, panels, pads and large-screen behavior.

- [ ] **Step 2: Record measurable baselines**

For deterministic fixture workloads, record literal output order, write/flush counts, chosen optimization and public API fingerprint. Keep timing-only measurements informational and out of pass/fail CI gates.

- [ ] **Step 3: Verify the complete suite and package validation**

```sh
dotnet restore Icod.DCurses.sln
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Staging --no-build --no-restore --logger trx
pwsh ./packaging/Invoke-Build.ps1 -Section validate -Configuration Staging
```

Expected: zero build errors, zero test failures, and successful package validation.

- [ ] **Step 4: Commit the baseline**

```sh
git add docs/T2001-DCurses-1.6-Behavioral-Baseline.md
git commit -m "docs: capture DCurses 1.6 migration baseline"
```

### Task 5: Close or explicitly block T2001

**Files:**
- Create or complete: `docs/T2001-Terminal-Boundary-and-Readiness-Gate.md`
- Modify: `Icod.DCurses-2.0.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`

**Interfaces:**
- Consumes: Tasks 1-4 exact-head evidence.
- Produces: either an accepted T2001 checkpoint authorizing T2002 or a precise blocked checkpoint that authorizes no dependent work.

- [ ] **Step 1: Record evidence**

Record exact commit SHA, workflow and job URLs, commands, counts, package versions, test-only TermInfo fixture exceptions, API break manifest, and every readiness result.

- [ ] **Step 2: Apply the gate**

Mark T2001 accepted only if unknown-rendition recovery, transaction bounds, stale epochs, semantic cost, inventory, API break manifest, baseline suite, package validation, and required platform CI are all green at the same exact head. Otherwise mark T2001 blocked with the smallest owning-repository correction and leave T2002 pending.

- [ ] **Step 3: Self-review**

```sh
rg -n '\bT(ODO|BD)\b' \
  docs/2.0-TermInfo-Coupling-Inventory.md \
  docs/2.0-API-Break-Manifest.md \
  docs/T2001-DCurses-1.6-Behavioral-Baseline.md \
  docs/T2001-Terminal-Boundary-and-Readiness-Gate.md
git diff --check
```

Expected: no placeholders and no whitespace errors.

- [ ] **Step 4: Commit the gate**

```sh
git add docs Icod.DCurses-2.0.0-Development-Roadmap.md Icod.DCurses-Development-Roadmap.md
git commit -m "docs: record DCurses T2001 readiness gate"
```

Do not begin T2002 until T2001 is explicitly accepted.
