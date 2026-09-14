# T1409 — Interaction Acceptance Sample

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1409  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** implementation and user documentation qualified; documentation-complete head requires its own exact-head matrix

---

## Objective

T1409 proves that the complete 1.4 interaction-routing surface composes into a realistic interactive terminal application without adding a widget framework, hidden event loop, retained layout owner, command callbacks, or a raw Terminal protocol path.

The acceptance artifact is:

```text
samples/Icod.DCurses.Interaction.Sample/
    Icod.DCurses.Interaction.Sample.csproj
    InteractionSampleState.cs
    Program.cs
```

The sample consumes only public `Icod.DCurses` APIs.

## Design and implementation authorities

The approved written design is:

`docs/superpowers/specs/2026-09-12-icod-dcurses-t1409-interaction-sample-design.md`

Final design-spec head:

`9d167356a072c97908a6969234e69a2cd39aee84`

The implementation plan is:

`docs/superpowers/plans/2026-09-13-icod-dcurses-t1409-interaction-sample.md`

Final reviewed plan head:

`e4450ccbf4acf968aa837e6e08eb82b782ac22e1`

The plan review caught and corrected the startup-small case before implementation: the sample creates valid retained placeholder surfaces once, keeps ordinary interaction regions empty/ineligible until geometry is large enough, and then applies real application bounds when the terminal reaches the frozen minimum size.

## TDD checkpoints

### Project/solution contract RED

Test-only head:

`fae011d310ec9bed9afbb2fa816de7af46dc5655`

The contract required the new sample project metadata and solution entry before either existed. The asserted sample project path was absent at that head, establishing the project-wiring RED condition.

The project and complete solution configuration were then added. The sample is a non-packable executable targeting:

```text
net8.0;net9.0;net10.0
Debug;Staging;Release
```

and references only:

```text
..\..\Icod.DCurses.csproj
```

### Interaction-source contract RED

Test-only head:

`e2b5ff446358595d5ec55f1a2697b8495801a23c`

At that checkpoint `Program.cs` was still the minimal session-open placeholder. The new test required the frozen public 1.4 interaction vocabulary, semantic command bindings, rich-input protocol acquisition, routing, focus traversal, pointer-shape leasing, and explicit resize synchronization; those markers were intentionally absent.

### Documentation contract RED

Test-only head:

`1e03946053551a6140a4e6772c4e724ee45687b0`

At that checkpoint `samples/README.md` still documented eight executable samples and contained no `Icod.DCurses.Interaction.Sample` section. The final user documentation updates the count to nine and documents the new sample, run command, controls, and the 1.4 interaction contracts demonstrated.

## Accepted application behavior

The sample uses one application-owned `CursesInteractionRouter`, one ordinary event loop, and exactly five interaction regions:

- header/status;
- left pane;
- right pane;
- footer/help;
- retained popup panel region.

The left, right, and popup regions are focusable. Header/footer remain non-focusable interaction targets.

The frozen command vocabulary is:

```text
focus.next
focus.previous
popup.toggle
escape
left.action
right.action
global.x
quit
```

The sample demonstrates:

- Tab / Shift+Tab logical-focus traversal;
- F2 retained popup toggle;
- Escape closing the popup before quitting the application;
- left-local `x` winning over global `x` while the left pane is focused;
- right-local `r` routing independently;
- mouse hit testing and region-local coordinates;
- current panel-over-ordinary precedence when the popup overlaps the body;
- terminal focus observation without clearing logical focus;
- explicit application-owned relayout after dimension synchronization;
- recovery after shrinking below and growing back above the 64x16 minimum;
- explicit pointer-shape application through `CursesPointerShapeLease` rather than router I/O.

The popup geometry remains 28x8 when the terminal is large enough.

## Protocol and ownership boundary

The sample acquires rich input only through public DCurses protocol leases:

- keyboard event-type reporting;
- terminal focus reporting;
- mouse button-event tracking.

It never imports or directly calls `Icod.Terminal` or `Icod.TermInfo`.

Pointer preferences are region metadata:

- header: `Default`;
- left: `Text`;
- right: `Crosshair`;
- footer: `Pointer`;
- popup: `Move`.

Hit testing/routing reports the preference without performing output. The sample explicitly acquires a replacement `CursesPointerShapeLease` before disposing the previous successfully owned lease. On no-hit/no-preference input it releases the current DCurses pointer lease and returns physical pointer policy to Terminal ownership.

All protocol and pointer leases are disposed through structured application ownership before session disposal.

## Layout and resize boundary

The minimum supported normal layout is:

```text
64 columns x 16 rows
```

The application derives layout from `CursesScreen.Bounds` using public `CursesLayout.Dock(...)` and `CursesLayout.SplitColumnsProportional(...)`, then explicitly applies resulting rectangles to retained windows, panel, and interaction regions.

No retained layout rules are stored by the router or screen.

The sample creates its retained surfaces once and preserves router/region/binding identity while too small. This demonstrates the T1408 coherence contract at application scale instead of rebuilding interaction state after every resize.

## Automated acceptance

The complete interactive implementation head is:

`581b708cf1be1d4778eb32bb2c459b9621c4f4fe`

Workflow #777 / `34737586564` passed all seven PR jobs:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

On Linux x64 the exact Staging build reported:

```text
0 warnings
0 errors
```

and the test suite reported on each target framework:

```text
718 passed / 0 failed / 0 skipped
```

The README-complete application head is:

`4c0ab569b289e7c8f4628e0262ecd9c4f85834c8`

Workflow #779 / `34737765972` initially ended `cancelled` while GitHub-hosted jobs were interrupted. The branch remained on the same exact SHA. The failed/cancelled job set was rerun without changing source, and workflow #779 then completed successfully with all seven package/runtime jobs green on the same head.

This preserves the evidence distinction between infrastructure interruption and a source/test failure.

## Public API and dependency result

T1409 adds no production library source and no public library API.

The accepted 1.4 alpha fingerprint therefore remains exactly:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

The published 1.3 compatibility floor and the existing Terminal/TermInfo public dependency allow-list remain unchanged.

## Files changed by the tranche

T1409 adds or changes only application/test/documentation/solution artifacts:

```text
Icod.DCurses.sln
samples/README.md
samples/Icod.DCurses.Interaction.Sample/Icod.DCurses.Interaction.Sample.csproj
samples/Icod.DCurses.Interaction.Sample/InteractionSampleState.cs
samples/Icod.DCurses.Interaction.Sample/Program.cs
tests/Icod.DCurses.Tests/src/InteractionSampleProjectContractTests.cs
docs/superpowers/specs/2026-09-12-icod-dcurses-t1409-interaction-sample-design.md
docs/superpowers/plans/2026-09-13-icod-dcurses-t1409-interaction-sample.md
docs/T1409-Interaction-Acceptance-Sample.md
```

No production file under `src/` changed for T1409.

## Scope audit

T1409 introduces no:

- widget/control hierarchy;
- callback dispatcher;
- automatic focus-on-click library policy;
- automatic layout owner;
- modal/nested event loop;
- raw Terminal protocol parser/emitter;
- pointer capture or drag/drop system;
- accessibility framework;
- navigation framework;
- raster scene graph.

The acceptance sample therefore demonstrates that a realistic application can consume the 1.4 primitives directly without bypassing their ownership model.

## Exit gate

T1409 is complete when this documentation-complete head passes the full PR package/runtime matrix on Windows, Linux, and macOS x64/ARM64. After that exact-head qualification, T1410 may begin performance, allocation, bounds, and adversarial hardening.
