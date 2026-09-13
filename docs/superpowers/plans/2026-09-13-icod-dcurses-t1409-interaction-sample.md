# T1409 Interaction Acceptance Sample Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a public-DCurses-only interactive 1.4 acceptance sample that demonstrates focus traversal, local/global command routing, mouse hit testing, panel precedence, explicit pointer-shape leases, resize relayout, and terminal-focus/logical-focus independence.

**Architecture:** Introduce one new executable sample project, `Icod.DCurses.Interaction.Sample`, under the existing `samples` solution folder. The sample owns one `CursesSession`, one `CursesInteractionRouter`, five retained interaction regions, one retained popup panel, three DCurses input-protocol leases, and at most one active DCurses pointer-shape lease. All command execution, focus changes, layout recomputation, pointer application, and rendering stay in application code; no production library source changes are expected.

**Tech Stack:** C# 13; .NET `net8.0;net9.0;net10.0`; `Icod.DCurses 1.4.0-alpha.1` source project; xUnit; Visual Studio solution format 12.00; GitHub Actions Staging matrix.

**Spec:** `docs/superpowers/specs/2026-09-12-icod-dcurses-t1409-interaction-sample-design.md`

## Global Constraints

- Work only on branch `1.4.0-interaction-routing` / PR #29.
- Preserve package identity `1.4.0-alpha.1` and `AssemblyVersion 1.0.0.0`.
- Preserve public API fingerprint exactly: 62 exported types / 491 canonical contract lines / SHA-256 `8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147`.
- Do not modify production library source unless the sample proves a concrete public-API defect; any such defect requires a separate documented correction before T1409 may close.
- The new sample targets `net8.0;net9.0;net10.0`, configurations `Debug;Staging;Release`, `IsPackable=false`, and references only `../../Icod.DCurses.csproj`.
- The sample must not reference or import `Icod.Terminal` or `Icod.TermInfo` directly.
- Use the repository GPL-3.0-or-later header for executable sample/test source and project files.
- Maintain existing 1TBS C# formatting: braces on every `if`/`else`, multiline call closing parenthesis on its own line, parameter validation at method entry.
- Minimum terminal geometry is exactly 64 columns x 16 rows; popup geometry is exactly 28 columns x 8 rows.
- Use `CursesLayout.Dock(...)` and `CursesLayout.SplitColumnsProportional(...)`; do not introduce retained/automatic layout ownership.
- Exactly five interaction regions: header, left, right, footer, popup. Left/right/popup are focusable; header/footer are not.
- Frozen command names: `focus.next`, `focus.previous`, `popup.toggle`, `escape`, `left.action`, `right.action`, `global.x`, `quit`.
- Frozen bindings: global Tab, global Shift+Tab, global F2, global Escape, left-local `x`, right-local `r`, global `x`, global `q`.
- Frozen pointer preferences: header `Default`, left `Text`, right `Crosshair`, footer `Pointer`, popup `Move`.
- Acquire rich input only through public DCurses input-protocol leases: keyboard event types, focus reporting, mouse button events.
- The sample owns exactly one event loop; never create a competing reader or parse raw escape sequences.

---

### Task 1: Project and solution wiring acceptance gate

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs`
- Create: `samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj`
- Create: `samples/Icod.DCurses.Interaction.Sample/Program.cs` (minimal compile placeholder only for this task)
- Modify: `Icod.DCurses.sln`

**Interfaces:**
- Consumes: existing root `Icod.DCurses.csproj`; existing `samples` solution folder `{4A1B4462-96F7-4E95-95A4-059D416B73E7}`.
- Produces: one solution-buildable executable project named `Icod.DCurses.Interaction.Sample`, with project GUID `{B427973D-98EF-4F7E-BB0D-6AE640753A21}`.

- [ ] **Step 1: Write the failing project contract test**

Create `tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs` with the GPL test header and this first test/helper:

```csharp
using System.Xml.Linq;
using Xunit;

namespace Icod.DCurses.Tests;

public sealed class InteractionSampleProjectContractTests {
    [Fact]
    public void InteractionSampleProjectIsWiredIntoTheSolution() {
        string root = FindRepositoryRoot();
        string projectPath = Path.Combine(
            root,
            "samples",
            "Icod.DCurses.Interaction.Sample",
            "Icod.DCurses.Interaction.Sample.csproj"
        );
        Assert.True( File.Exists( projectPath ) );

        XDocument project = XDocument.Load( projectPath );
        Assert.Equal(
            "net8.0;net9.0;net10.0",
            project.Descendants( "TargetFrameworks" ).Single().Value
        );
        Assert.Equal(
            "Debug;Staging;Release",
            project.Descendants( "Configurations" ).Single().Value
        );
        Assert.Equal(
            "false",
            project.Descendants( "IsPackable" ).Single().Value
        );

        XElement reference = project.Descendants( "ProjectReference" ).Single();
        Assert.Equal(
            @"..\..\Icod.DCurses.csproj",
            reference.Attribute( "Include" )!.Value
        );

        string solution = File.ReadAllText(
            Path.Combine(
                root,
                "Icod.DCurses.sln"
            )
        );
        Assert.Contains(
            @"samples\Icod.DCurses.Interaction.Sample\Icod.DCurses.Interaction.Sample.csproj",
            solution,
            StringComparison.Ordinal
        );
    }

    private static string FindRepositoryRoot() {
        DirectoryInfo? current = new( AppContext.BaseDirectory );
        while ( current is not null ) {
            if ( File.Exists(
                Path.Combine(
                    current.FullName,
                    "Icod.DCurses.sln"
                )
            ) ) {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException( "Repository root not found." );
    }
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```text
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging --filter FullyQualifiedName~InteractionSampleProjectIsWiredIntoTheSolution
```

Expected: FAIL because `samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj` does not yet exist.

- [ ] **Step 3: Create the sample project**

Create `samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj` matching the existing executable-sample convention:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- GPL sample header -->
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
        <AssemblyName>Icod.DCurses.Interaction.Sample</AssemblyName>
        <RootNamespace>Icod.DCurses.Interaction.Sample</RootNamespace>
        <IsPackable>false</IsPackable>
        <Configurations>Debug;Staging;Release</Configurations>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\Icod.DCurses.csproj" />
    </ItemGroup>
</Project>
```

Create a temporary GPL-headed `Program.cs` containing only:

```csharp
using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
return 0;
```

- [ ] **Step 4: Add the project to `Icod.DCurses.sln`**

Add:

```text
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Icod.DCurses.Interaction.Sample", "samples\Icod.DCurses.Interaction.Sample\Icod.DCurses.Interaction.Sample.csproj", "{B427973D-98EF-4F7E-BB0D-6AE640753A21}"
EndProject
```

Add Debug/Staging/Release `ActiveCfg` + `Build.0` entries for `{B427973D-98EF-4F7E-BB0D-6AE640753A21}` and nest it under solution folder `{4A1B4462-96F7-4E95-95A4-059D416B73E7}`.

- [ ] **Step 5: Run the focused contract test and solution build**

Run:

```text
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging --filter FullyQualifiedName~InteractionSampleProjectIsWiredIntoTheSolution
dotnet build Icod.DCurses.sln -c Staging
```

Expected: PASS; solution builds all three sample TFMs with zero warnings/errors.

- [ ] **Step 6: Commit the wiring gate**

```text
git add tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs samples/Icod.DCurses.Interaction.Sample Icod.DCurses.sln
git commit -m "Add T1409 interaction sample project"
```

---

### Task 2: Interactive sample behavior and public-only source contract

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs`
- Replace: `samples/Icod.DCurses.Interaction.Sample/Program.cs`
- Create: `samples/Icod.DCurses.Interaction.Sample/InteractionSampleState.cs`

**Interfaces:**
- Consumes public DCurses signatures:
  - `new CursesInteractionRouter(CursesScreen)`
  - `router.RegisterRegion(CursesInteractionRegionOptions)`
  - `router.BindGlobalGesture(CursesKeyGesture, CursesCommand)`
  - `region.BindGesture(CursesKeyGesture, CursesCommand)`
  - `router.Route(CursesInputEvent)`
  - `router.Focus(CursesInteractionRegion)` / `MoveFocus(CursesFocusDirection)`
  - `CursesKeyGesture.ForKey(...)`, `ForCharacter(...)`, `ForFunctionKey(...)`
  - `session.AcquireInputProtocolsAsync(CursesInputProtocolOptions)`
  - `session.AcquirePointerShapeAsync(CursesPointerShape)`
  - `session.SynchronizeDimensions()` / `session.Invalidate()` / `session.RefreshAsync()` / `session.ReadEventAsync()`
- Produces: one public-API-only interactive sample implementing every manual acceptance behavior frozen in the spec.

- [ ] **Step 1: Add a failing source-contract test**

Append this test to `InteractionSampleProjectContractTests`:

```csharp
[Fact]
public void InteractionSampleUsesTheFrozenPublicInteractionContract() {
    string root = FindRepositoryRoot();
    string programPath = Path.Combine(
        root,
        "samples",
        "Icod.DCurses.Interaction.Sample",
        "Program.cs"
    );
    string source = File.ReadAllText( programPath );

    Assert.DoesNotContain( "Icod.Terminal", source, StringComparison.Ordinal );
    Assert.DoesNotContain( "Icod.TermInfo", source, StringComparison.Ordinal );

    string[] requiredMarkers = [
        "CursesInteractionRouter",
        "CursesInteractionRegionOptions",
        "BindGlobalGesture",
        "BindGesture",
        "CursesKeyGesture.ForKey",
        "CursesKeyGesture.ForCharacter",
        "CursesKeyGesture.ForFunctionKey",
        "AcquireInputProtocolsAsync",
        "CursesKeyboardReportingMode.EventTypes",
        "FocusReporting = true",
        "CursesMouseTrackingMode.ButtonEvents",
        "router.Route",
        "MoveFocus",
        "AcquirePointerShapeAsync",
        "CursesPointerShapeLease",
        "SynchronizeDimensions",
        "CursesLayout.SplitColumnsProportional",
        "focus.next",
        "focus.previous",
        "popup.toggle",
        "left.action",
        "right.action",
        "global.x",
        "quit"
    ];

    foreach ( string marker in requiredMarkers ) {
        Assert.Contains( marker, source, StringComparison.Ordinal );
    }
}
```

- [ ] **Step 2: Run the focused source-contract test and verify RED**

Run:

```text
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging --filter FullyQualifiedName~InteractionSampleUsesTheFrozenPublicInteractionContract
```

Expected: FAIL against the temporary two-line sample.

- [ ] **Step 3: Add sample-local state**

Create GPL-headed `InteractionSampleState.cs`:

```csharp
namespace Icod.DCurses.Interaction.Sample;

using Icod.DCurses;

internal sealed class InteractionSampleState {
    internal bool Running { get; set; } = true;
    internal bool PopupVisible { get; set; }
    internal string Status { get; set; } = "Ready";
    internal CursesFocusState? TerminalFocus { get; set; }
    internal string RoutedTarget { get; set; } = "-";
    internal CursesPointerShape? AppliedPointerShape { get; set; }
}
```

- [ ] **Step 4: Replace `Program.cs` with the full session/protocol/router setup**

Use only `using Icod.DCurses;` plus BCL namespaces. Open the session and materialize `screen` / `standard`; set `standard.WrapMode = CursesWrapMode.Clip`.

Acquire three DCurses input-protocol leases via a helper equivalent to the existing Input.Showcase pattern:

```csharp
await TryAcquireInputProtocolAsync(
    session,
    new CursesInputProtocolOptions {
        KeyboardReportingMode = CursesKeyboardReportingMode.EventTypes
    },
    protocolLeases,
    state
);
await TryAcquireInputProtocolAsync(
    session,
    new CursesInputProtocolOptions {
        FocusReporting = true
    },
    protocolLeases,
    state
);
await TryAcquireInputProtocolAsync(
    session,
    new CursesInputProtocolOptions {
        MouseTrackingMode = CursesMouseTrackingMode.ButtonEvents
    },
    protocolLeases,
    state
);
```

Create one router and eight command identities with the exact frozen names. Bind global gestures:

```csharp
router.BindGlobalGesture(
    CursesKeyGesture.ForKey( CursesKey.Tab ),
    FocusNext
);
router.BindGlobalGesture(
    CursesKeyGesture.ForKey(
        CursesKey.Tab,
        CursesKeyModifiers.Shift
    ),
    FocusPrevious
);
router.BindGlobalGesture(
    CursesKeyGesture.ForFunctionKey( 2 ),
    PopupToggle
);
router.BindGlobalGesture(
    CursesKeyGesture.ForKey( CursesKey.Escape ),
    Escape
);
router.BindGlobalGesture(
    CursesKeyGesture.ForCharacter( new Rune( 'x' ) ),
    GlobalX
);
router.BindGlobalGesture(
    CursesKeyGesture.ForCharacter( new Rune( 'q' ) ),
    Quit
);
```

After creating left/right regions, bind `x -> left.action` and `r -> right.action` locally. Explicitly focus the left region at startup.

- [ ] **Step 5: Implement frozen layout and retained geometry**

Define constants:

```csharp
const int MinimumColumns = 64;
const int MinimumRows = 16;
const int PopupColumns = 28;
const int PopupRows = 8;
```

Implement `ComputeLayout(...)` with:

```csharp
CursesLayout.Dock(
    bounds,
    CursesDockEdge.Top,
    1,
    out header,
    out CursesRectangle afterHeader
);
CursesLayout.Dock(
    afterHeader,
    CursesDockEdge.Bottom,
    2,
    out footer,
    out CursesRectangle body
);
CursesLayout.SplitColumnsProportional(
    body,
    1,
    1,
    out left,
    out right
);
```

Center a 28x8 popup within `body`. Create header/left/right/footer windows once while the initial screen is large enough; create the popup `CursesPanel` once and hide it initially. Register exactly five regions with the frozen focusability/pointer-shape values; popup region bounds are panel-local `(0,0,PopupRows,PopupColumns)`.

`ApplyLayout(...)` must update the four window bounds, popup panel bounds, and four ordinary region bounds. The popup region stays panel-local/full-panel.

When below 64x16, hide the popup and draw only the resize message on the standard screen without destroying router/region/binding state.

- [ ] **Step 6: Implement structured command handling**

For every input event:

```csharp
CursesInteractionResult routed = router.Route( input );
```

Switch on `routed.Command?.Name`:

- `focus.next` -> `router.MoveFocus( CursesFocusDirection.Forward )`;
- `focus.previous` -> backward;
- `popup.toggle` -> show/focus popup or hide and rely on repair;
- `escape` -> hide popup if visible, otherwise `state.Running = false`;
- `left.action`, `right.action`, `global.x` -> update status text so the precedence result is visible;
- `quit` -> stop.

For `Targeted` mouse results, update `state.RoutedTarget` with region name, original screen row/column, local row/column, and `Hit.PointerShape`.

Do not call `router.Focus(...)` from mouse routing.

- [ ] **Step 7: Implement explicit pointer lease replacement**

Keep:

```csharp
CursesPointerShapeLease? pointerLease = null;
CursesPointerShape? appliedPointerShape = null;
```

On a mouse result with a desired shape different from the current shape:

```csharp
CursesPointerShapeLease replacement =
    await session.AcquirePointerShapeAsync( desiredShape );
CursesPointerShapeLease? prior = pointerLease;
pointerLease = replacement;
appliedPointerShape = desiredShape;
state.AppliedPointerShape = desiredShape;
if ( prior is not null ) {
    await prior.DisposeAsync();
}
```

On no hit/no desired shape, dispose the current lease and clear the observation. Catch only acquisition failures, keep the prior successfully owned lease, and place the exception message in `state.Status`.

- [ ] **Step 8: Implement lifecycle, focus-report, draw, and cleanup behavior**

Handle normalized `CursesInputEventKind.Focus` by updating `state.TerminalFocus = input.Focus?.State`; never mutate router logical focus from that event.

Handle `Resize` / `Resumed` lifecycle events by:

```csharp
_ = session.SynchronizeDimensions();
ApplyLayoutIfLargeEnough(...);
session.Invalidate();
```

Interrupt/termination stop the loop. Draw visibly distinct focused pane/popup state using public `CursesStyle`/text APIs; header shows terminal-focus observation and status, footer shows controls.

Use `try/finally` to dispose, in order:

1. current pointer lease;
2. input-protocol leases in reverse acquisition order;
3. router/regions/panel through normal `using` ownership;
4. session via `await using`.

- [ ] **Step 9: Run focused source contract and full Staging build/tests**

Run:

```text
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging --filter FullyQualifiedName~InteractionSampleProjectContractTests
dotnet build Icod.DCurses.sln -c Staging
dotnet test Icod.DCurses.sln -c Staging --no-build
```

Expected: all pass, with the public API fingerprint still 62 / 491 / `8afe72de...`.

- [ ] **Step 10: Commit the interactive behavior**

```text
git add samples/Icod.DCurses.Interaction.Sample tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs
git commit -m "Implement T1409 interaction acceptance sample"
```

---

### Task 3: Sample documentation and T1409 closure evidence

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs`
- Modify: `samples/README.md`
- Create: `docs/T1409-Interaction-Acceptance-Sample.md`
- Modify: PR #29 body after exact-head qualification (metadata only, no source SHA change)

**Interfaces:**
- Consumes: implemented `Icod.DCurses.Interaction.Sample` and exact T1409 CI evidence.
- Produces: repository-visible run instructions, manual acceptance checklist, and exact-head tranche record.

- [ ] **Step 1: Add a failing documentation-contract test**

Append:

```csharp
[Fact]
public void InteractionSampleIsDocumentedForUsers() {
    string root = FindRepositoryRoot();
    string readme = File.ReadAllText(
        Path.Combine(
            root,
            "samples",
            "README.md"
        )
    );

    Assert.Contains( "## Icod.DCurses.Interaction.Sample", readme, StringComparison.Ordinal );
    Assert.Contains(
        "dotnet run --project samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj",
        readme,
        StringComparison.Ordinal
    );
    Assert.Contains( "Tab", readme, StringComparison.Ordinal );
    Assert.Contains( "Shift+Tab", readme, StringComparison.Ordinal );
    Assert.Contains( "F2", readme, StringComparison.Ordinal );
    Assert.Contains( "pointer", readme, StringComparison.OrdinalIgnoreCase );
}
```

- [ ] **Step 2: Verify documentation test RED**

Run the single test; expected FAIL because `samples/README.md` does not yet contain the Interaction.Sample section.

- [ ] **Step 3: Update `samples/README.md`**

Change “eight executable samples” to “nine executable samples”. Add a section after `Layout.Sample` describing:

- 1.4 interaction routing;
- Tab / Shift+Tab focus traversal;
- F2 popup toggle;
- local-vs-global command precedence;
- mouse/local coordinates and topmost popup precedence;
- explicit pointer-shape application;
- explicit resize relayout;
- terminal focus reports remaining distinct from logical focus;
- the exact `dotnet run --project ...` command.

- [ ] **Step 4: Run documentation contract and full tests**

Expected: PASS, public API fingerprint unchanged.

- [ ] **Step 5: Commit user-facing documentation**

```text
git add samples/README.md tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs
git commit -m "Document T1409 interaction sample"
```

- [ ] **Step 6: Run/observe the exact implementation documentation head through the full PR matrix**

Required seven jobs:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

Do not write closure evidence until this exact source/documentation head is green.

- [ ] **Step 7: Create `docs/T1409-Interaction-Acceptance-Sample.md`**

Record:

- approved spec path;
- RED project/source/docs checkpoints;
- exact implementation commits;
- public-only dependency audit;
- 64x16 / 28x8 geometry contract;
- command and pointer-shape demonstration matrix;
- exact unchanged 62 / 491 fingerprint;
- exact workflow run and seven-job results;
- manual acceptance checklist and note that CI validates compile/test/package behavior while terminal visual interaction remains a human-run acceptance activity.

- [ ] **Step 8: Qualify the documentation-complete closure head**

Run/observe the full seven-job PR matrix again on the closure-doc commit. T1409 is complete only when that exact head is green.

- [ ] **Step 9: Update PR #29 progress ledger**

Mark T1409 complete with exact closure SHA/workflow, preserve T1401–T1408 evidence, and mark T1410 as next. This metadata update must not change the qualified source SHA.
