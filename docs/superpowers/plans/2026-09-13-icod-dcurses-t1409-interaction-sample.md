# T1409 Interaction Acceptance Sample Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a public-DCurses-only interactive 1.4 acceptance sample demonstrating focus traversal, local/global commands, mouse hit testing, panel precedence, explicit pointer-shape leases, resize relayout, and terminal-focus/logical-focus independence.

**Architecture:** Add one executable sample project, `Icod.DCurses.Interaction.Sample`, under the existing `samples` solution folder. The sample owns one `CursesSession`, one `CursesInteractionRouter`, exactly five interaction regions, one retained popup panel, three DCurses input-protocol leases, and at most one active DCurses pointer-shape lease. Application code owns command execution, focus changes, layout recomputation, pointer application, and rendering; no production library change is expected.

**Tech Stack:** C# 13; .NET `net8.0;net9.0;net10.0`; xUnit; existing DCurses public API; Visual Studio solution format 12.00; GitHub Actions Staging matrix.

**Spec:** `docs/superpowers/specs/2026-09-12-icod-dcurses-t1409-interaction-sample-design.md`

## Global Constraints

- Branch: `1.4.0-interaction-routing`; PR #29.
- Preserve package `1.4.0-alpha.1`, `AssemblyVersion 1.0.0.0`, and public fingerprint **62 / 491 / `8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147`**.
- No production source change unless the sample proves a real public-API defect.
- Sample project: `net8.0;net9.0;net10.0`; `Debug;Staging;Release`; `IsPackable=false`; only `../../Icod.DCurses.csproj` as a project reference.
- No direct `Icod.Terminal` or `Icod.TermInfo` import/reference.
- GPL-3.0-or-later headers for sample/test artifacts; existing 1TBS formatting conventions.
- Minimum terminal size: **64x16**. Popup: **28x8**.
- Layout: `CursesLayout.Dock(...)` plus `CursesLayout.SplitColumnsProportional(...)` only; no retained layout owner.
- Exactly five regions: header, left, right, footer, popup. Focusable: left/right/popup only.
- Commands: `focus.next`, `focus.previous`, `popup.toggle`, `escape`, `left.action`, `right.action`, `global.x`, `quit`.
- Bindings: global Tab, Shift+Tab, F2, Escape, `x`, `q`; left-local `x`; right-local `r`.
- Pointer preferences: header `Default`, left `Text`, right `Crosshair`, footer `Pointer`, popup `Move`.
- Acquire keyboard event types, focus reporting, and mouse button events only through DCurses input-protocol leases.
- One event loop; no competing reader and no raw escape parsing.

---

### Task 1: Add a RED project/solution contract and wire the sample project

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs`
- Create: `samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj`
- Create: `samples/Icod.DCurses.Interaction.Sample/Program.cs` (temporary compile placeholder)
- Modify: `Icod.DCurses.sln`

**Interfaces:**
- Consumes root `Icod.DCurses.csproj` and solution folder GUID `{4A1B4462-96F7-4E95-95A4-059D416B73E7}`.
- Produces solution project GUID `{B427973D-98EF-4F7E-BB0D-6AE640753A21}`.

- [ ] Add `InteractionSampleProjectIsWiredIntoTheSolution()` which finds the repository root by walking parents from `AppContext.BaseDirectory`, then asserts:
  - the sample `.csproj` exists;
  - `TargetFrameworks == "net8.0;net9.0;net10.0"`;
  - `Configurations == "Debug;Staging;Release"`;
  - `IsPackable == "false"`;
  - exactly one `ProjectReference` with `Include="..\..\Icod.DCurses.csproj"`;
  - `Icod.DCurses.sln` contains the sample project path.
- [ ] Run the focused test and verify RED because the sample project does not exist.
- [ ] Create the sample `.csproj` matching `Icod.DCurses.Layout.Sample` conventions and GPL header.
- [ ] Create temporary GPL-headed `Program.cs`:

```csharp
using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
return 0;
```

- [ ] Add the project to `Icod.DCurses.sln`, including all Debug/Staging/Release `ActiveCfg` and `Build.0` entries and nesting under the `samples` solution folder.
- [ ] Run the focused test and `dotnet build Icod.DCurses.sln -c Staging`; require PASS and zero warnings/errors.
- [ ] Commit: `Add T1409 interaction sample project`.

---

### Task 2: Add a RED source-contract test and implement the public-only sample

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs`
- Replace: `samples/Icod.DCurses.Interaction.Sample/Program.cs`
- Create: `samples/Icod.DCurses.Interaction.Sample/InteractionSampleState.cs`

**Interfaces consumed:**
- `CursesInteractionRouter`, `CursesInteractionRegionOptions`, `BindGlobalGesture`, `BindGesture`, `Route`, `Focus`, `MoveFocus`.
- `CursesKeyGesture.ForKey`, `.ForCharacter`, `.ForFunctionKey`.
- `CursesSession.AcquireInputProtocolsAsync`, `.AcquirePointerShapeAsync`, `.SynchronizeDimensions`, `.Invalidate`, `.RefreshAsync`, `.ReadEventAsync`.

- [ ] Add `InteractionSampleUsesTheFrozenPublicInteractionContract()` which reads `Program.cs`, rejects `Icod.Terminal` / `Icod.TermInfo`, and requires these markers: `CursesInteractionRouter`, `CursesInteractionRegionOptions`, `BindGlobalGesture`, `BindGesture`, all three gesture factories, `AcquireInputProtocolsAsync`, keyboard event types, focus reporting, button mouse tracking, `router.Route`, `MoveFocus`, `AcquirePointerShapeAsync`, `CursesPointerShapeLease`, `SynchronizeDimensions`, `CursesLayout.SplitColumnsProportional`, and every frozen command name.
- [ ] Run the focused test and verify RED against the temporary program.
- [ ] Create GPL-headed `InteractionSampleState.cs` with:

```csharp
internal sealed class InteractionSampleState {
    internal bool Running { get; set; } = true;
    internal bool PopupVisible { get; set; }
    internal string Status { get; set; } = "Ready";
    internal CursesFocusState? TerminalFocus { get; set; }
    internal string RoutedTarget { get; set; } = "-";
    internal CursesPointerShape? AppliedPointerShape { get; set; }
}
```

- [ ] Replace `Program.cs` with the full application. Use `System.Text` for `Rune` plus `Icod.DCurses`; no Terminal/TermInfo imports.
- [ ] Open one `CursesSession`; materialize `screen` and `standard`; set `standard.WrapMode = Clip`.
- [ ] Acquire three DCurses input-protocol leases using the existing Input.Showcase controlled-result pattern:
  - keyboard `EventTypes`;
  - `FocusReporting = true`;
  - `MouseTrackingMode = ButtonEvents`.
- [ ] Create eight `CursesCommand` instances with the exact frozen names and bind the exact gestures. Local left `x` must shadow global `x`; local right `r` is right-only.
- [ ] Create one router. To support startup below 64x16 without rebuilding identities, create four **valid 1x1 placeholder windows** and one **valid 1x1 hidden popup panel** at `(0,0)` immediately. Register header/left/right/footer with `CursesRectangle.Empty` (or an equivalent zero-size rectangle) so they are initially ineligible; register popup region panel-relative as full `(0,0,8,28)` while the panel stays hidden. These objects are created once and retained for the whole sample.
- [ ] Apply frozen pointer preferences and focusability. Do not attempt initial left focus until a valid 64x16 layout has been applied; after the first valid layout, explicitly focus left if no eligible focus exists.
- [ ] Implement `ComputeLayout(...)`:

```csharp
CursesLayout.Dock( bounds, CursesDockEdge.Top, 1, out header, out CursesRectangle afterHeader );
CursesLayout.Dock( afterHeader, CursesDockEdge.Bottom, 2, out footer, out CursesRectangle body );
CursesLayout.SplitColumnsProportional( body, 1, 1, out left, out right );
```

Center an exact 28x8 popup in `body`.
- [ ] Implement `ApplyLayout(...)`: set all four window bounds, popup panel bounds, and the four ordinary region bounds. Keep popup region panel-local and full-panel.
- [ ] When current screen is below 64x16: hide popup, set the four ordinary region bounds to empty/ineligible rectangles, render only a resize message on `standard`, preserve all objects/bindings, and continue reading events. On later growth, apply the real layout and resume normal rendering.
- [ ] Route every input through `router.Route(input)`. Execute commands in application code:
  - next/previous -> `MoveFocus`;
  - F2 toggle -> show/focus popup or hide and rely on repair;
  - Escape -> hide popup if shown, else quit;
  - local/global actions -> status text;
  - quit -> stop.
- [ ] Mouse routing updates status with target name, screen coordinates, region-local coordinates, and pointer preference. Mouse does not call `Focus`.
- [ ] Maintain one active pointer lease. When desired shape changes: acquire replacement first; if successful, swap ownership then dispose prior lease. When there is no hit/preference, dispose current lease. Scope acquisition exception handling narrowly around the acquisition call and preserve prior ownership on failure.
- [ ] Normalize terminal focus reports into `state.TerminalFocus` only; never mutate logical focus from terminal focus.
- [ ] On `Resize` / `Resumed`: `SynchronizeDimensions()`, apply minimum-size/layout policy, then `session.Invalidate()`.
- [ ] Draw focused left/right/popup state visibly; header shows terminal-focus/status; footer shows controls.
- [ ] In `finally`, dispose pointer lease, then protocol leases in reverse acquisition order. Router/regions/panel/session use structured `using` / `await using` ownership.
- [ ] Run focused sample contract, full Staging build, and full Staging tests. Require fingerprint unchanged at 62 / 491 / `8afe72de...`.
- [ ] Commit: `Implement T1409 interaction acceptance sample`.

---

### Task 3: Add a RED documentation contract, document the sample, and close T1409

**Files:**
- Modify: `tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs`
- Modify: `samples/README.md`
- Create: `docs/T1409-Interaction-Acceptance-Sample.md`

- [ ] Add `InteractionSampleIsDocumentedForUsers()` asserting `samples/README.md` contains:
  - `## Icod.DCurses.Interaction.Sample`;
  - exact `dotnet run --project samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj` command;
  - `Tab`, `Shift+Tab`, `F2`, and pointer-shape wording.
- [ ] Run that focused test and verify RED.
- [ ] Update sample count from eight to nine and add a full Interaction.Sample section after Layout.Sample describing focus traversal, local/global precedence, F2 popup, mouse/local coordinates, panel precedence, explicit pointer-shape application, explicit resize relayout, and terminal-focus/logical-focus distinction.
- [ ] Run focused contract, full Staging build/tests; require unchanged fingerprint.
- [ ] Commit: `Document T1409 interaction sample`.
- [ ] Qualify that exact implementation/documentation head through all seven PR jobs before writing closure evidence.
- [ ] Create `docs/T1409-Interaction-Acceptance-Sample.md` recording approved spec/plan, RED checkpoints, implementation commits, public-only audit, 64x16/28x8 geometry, command/pointer matrix, unchanged API fingerprint, exact workflow evidence, and manual acceptance checklist.
- [ ] Qualify the closure-doc head through all seven jobs again.
- [ ] Update PR #29 metadata with the exact T1409 closure SHA/workflow and mark T1410 next; do not alter the qualified source SHA.
