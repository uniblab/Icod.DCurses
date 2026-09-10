# Panels and Layers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add independent retained panels with deterministic z-order composition to Icod.DCurses 1.2.0 without changing ordinary `CursesWindow` shared-view semantics.

**Architecture:** Panels own private logical backing surfaces edited through existing `CursesWindow` APIs. A screen-owned panel stack tracks identity, visibility, position, and bottom-to-top order, while a logical compositor merges the base screen and visible panels into the desired screen image before the existing retained physical renderer runs.

**Tech Stack:** C# 13; .NET 8/9/10; xUnit; existing Icod.DCurses virtual-screen/damage/metadata infrastructure; Icod.Terminal 1.8.1 and Icod.TermInfo 1.10.0 as project-declared dependencies.

**Spec:** `Icod.DCurses-1.2.0-Development-Roadmap.md`

## Global Constraints

- Stable compatibility floor is `1.0.0`; accepted 1.1 API remains additive floor.
- `AssemblyVersion` remains `1.0.0.0`.
- Existing `CursesWindow` shared-view semantics do not change.
- No public raw Terminal protocol/routing types.
- No second input reader or terminal lifecycle owner.
- No panel-related unconditional per-cell storage when no panels exist.
- Semantic metadata moves with the cell state it annotates.
- Wide-cell footprints must remain valid after composition and clipping.
- Package validation must not hard-code sibling package versions.
- All supported TFMs and Windows/Linux/macOS x64/ARM64 gates remain required before stable closure.

---

### Task 1: Deterministic internal order engine

**Files:**
- Create: `src/Internal/CursesPanelOrder.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelOrderTests.cs`

**Interfaces:**
- Produces internal `CursesPanelOrder<T> where T : class` with `Count`, `Add`, `Remove`, `MoveToTop`, `MoveToBottom`, `MoveAbove`, `MoveBelow`, and `SnapshotBottomToTop`.
- Ordering uses reference identity, not `Equals`/`GetHashCode` value equality.

- [ ] **Step 1: Write failing tests** covering insertion order, equal-but-distinct reference identity, duplicate-reference rejection, removal, top/bottom movement, relative movement, missing-member rejection, and self-relative rejection.
- [ ] **Step 2: Run PR CI and verify red** because `CursesPanelOrder<T>` does not yet exist.
- [ ] **Step 3: Implement the minimal order engine** using one list and reference-identity lookup; no visibility or composition policy belongs here.
- [ ] **Step 4: Run PR CI and verify green** across the normal matrix.
- [ ] **Step 5: Commit/update T1201 status** only after the implementation head is green.

### Task 2: Independent retained panel surface

**Files:**
- Create: `src/CursesPanel.cs`
- Create: `src/Internal/CursesPanelSurface.cs`
- Modify: `src/CursesScreen.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelSurfaceTests.cs`

**Interfaces:**
- `CursesScreen.CreatePanel(int row, int column, int rows, int columns)` creates one screen-owned panel.
- `CursesPanel.ContentWindow` exposes the existing `CursesWindow` editing model over a private backing screen.
- `CursesPanel.Row`, `Column`, `Rows`, `Columns`, and `IsVisible` expose logical panel state.

- [ ] **Step 1: Write failing creation/independence tests** proving panel writes do not mutate the destination base screen before composition.
- [ ] **Step 2: Verify red.**
- [ ] **Step 3: Implement private backing surface and creation ownership.**
- [ ] **Step 4: Verify focused tests and full matrix green.**
- [ ] **Step 5: Record the provisional public API for later regret review.**

### Task 3: Visibility, movement, and z-order operations

**Files:**
- Modify: `src/CursesPanel.cs`
- Create: `src/Internal/CursesPanelStack.cs`
- Modify: `src/CursesScreen.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelStackTests.cs`

**Interfaces:**
- Public operations equivalent to `Show`, `Hide`, `Move`, `MoveToTop`, `MoveToBottom`, `MoveAbove`, and `MoveBelow`.
- Hide/show preserves full stack membership and therefore remembered z-order.
- Cross-screen relative ordering is rejected.

- [ ] **Step 1: Write failing state/order tests.**
- [ ] **Step 2: Verify red.**
- [ ] **Step 3: Implement stack ownership over Task 1 order engine.**
- [ ] **Step 4: Verify idempotent operations do not report false changes.**
- [ ] **Step 5: Run full matrix.**

### Task 4: Opaque logical composition and clipping

**Files:**
- Create: `src/Internal/CursesPanelCompositor.cs`
- Modify: `src/CursesScreen.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelCompositionTests.cs`

**Interfaces:**
- Compositor reads the base frame plus visible panels in bottom-to-top order.
- Opaque panel blank cells cover lower content.
- Off-screen portions clip without mutating panel content.

- [ ] **Step 1: Write failing overlap/clipping tests.**
- [ ] **Step 2: Verify red.**
- [ ] **Step 3: Implement minimal opaque compositor.**
- [ ] **Step 4: Repair/validate wide footprints after composition.**
- [ ] **Step 5: Verify green.**

### Task 5: Explicit transparent-blank policy

**Files:**
- Create: `src/CursesPanelTransparency.cs`
- Modify: `src/CursesPanel.cs`
- Modify: `src/Internal/CursesPanelCompositor.cs`
- Modify: `tests/Icod.DCurses.Tests/src/CursesPanelCompositionTests.cs`

**Interfaces:**
- Public enum values: `Opaque`, `BlankCellsTransparent`.
- Default is `Opaque`.
- In transparent mode an ordinary blank contributes neither visual cell nor semantic metadata.

- [ ] **Step 1: Write failing transparency tests including styled blank and metadata cases.**
- [ ] **Step 2: Verify red.**
- [ ] **Step 3: Implement panel-scoped policy.**
- [ ] **Step 4: Verify transparent cells reveal lower retained state exactly.**
- [ ] **Step 5: Run full matrix.**

### Task 6: Damage and incremental recomposition

**Files:**
- Create: `src/Internal/CursesPanelDamage.cs`
- Modify: `src/Internal/CursesPanelStack.cs`
- Modify: `src/Internal/CursesPanelCompositor.cs`
- Modify: `src/Integration/CursesSession.Screen.Terminal.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelDamageTests.cs`

**Interfaces:**
- Damage is driven by panel content revisions plus geometry/visibility/order changes.
- Hidden panel content updates remain panel-local until visibility exposes them.

- [ ] **Step 1: Write failing hidden/occluded/damaged-area tests.**
- [ ] **Step 2: Verify red.**
- [ ] **Step 3: Implement rectangular/row-range invalidation sufficient for tested cases.**
- [ ] **Step 4: Verify no-op operations generate no destination damage.**
- [ ] **Step 5: Run full matrix.**

### Task 7: Unicode, wide-cell, line-glyph, and metadata hardening

**Files:**
- Modify: `src/Internal/CursesPanelCompositor.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelSemanticCompositionTests.cs`

**Interfaces:**
- Composition treats `CursesCell` plus optional `CursesCellMetadata` as one logical state.
- Partial wide footprints are repaired deterministically after clipping/occlusion.

- [ ] **Step 1: Write failing width-2/metadata/line-glyph cases.**
- [ ] **Step 2: Verify red.**
- [ ] **Step 3: Reuse existing logical-cell-state and footprint repair helpers.**
- [ ] **Step 4: Verify metadata disappears exactly when its content footprint disappears.**
- [ ] **Step 5: Run full matrix.**

### Task 8: Resize, lifecycle, and live-session integration

**Files:**
- Modify: `src/CursesScreen.cs`
- Modify: `src/Integration/CursesSession.Screen.Terminal.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelLifecycleTests.cs`

**Interfaces:**
- Destination resize clips panels but does not resize or discard their retained content.
- Existing refresh/synchronized-output/lifecycle machinery remains the sole physical owner.

- [ ] **Step 1: Write failing resize/suspend/resume/session tests.**
- [ ] **Step 2: Verify red.**
- [ ] **Step 3: Integrate compositor invalidation with screen/session lifecycle.**
- [ ] **Step 4: Verify retained panel content survives lifecycle invalidation.**
- [ ] **Step 5: Run full matrix.**

### Task 9: Application and performance acceptance

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelApplicationAcceptanceTests.cs`
- Create: `docs/T1208-Panel-Application-Performance-and-Allocation-Acceptance.md`

**Interfaces:**
- No new public APIs; this task proves the accepted shape.

- [ ] **Step 1: Add modal-help, command-palette, completion-popup, context-menu, transient-status, and movable-dialog scenarios.**
- [ ] **Step 2: Add sparse-panel allocation/work measurements.**
- [ ] **Step 3: Exercise repeated z-order changes and hidden updates.**
- [ ] **Step 4: Run full matrix and record exact evidence.**

### Task 10: API/package/documentation regret gate and stable closure

**Files:**
- Modify: `docs/Public-API-Fingerprint-1.2.json`
- Create/modify: `docs/Public-API-Baseline-1.2.md`
- Modify: `tools/package-smoke/Program.cs`
- Create: `samples/Icod.DCurses.Panel.Sample/*`
- Modify: `README.md`
- Modify: current roadmap files and package metadata
- Create: `docs/T1209-Public-API-Package-Documentation-and-Regret-Gate.md`
- Create: `docs/T1210-RC-and-Stable-Closure.md`

**Interfaces:**
- Final public API is additive over the accepted 1.1 fingerprint.

- [ ] **Step 1: Generate and compare the compiler-derived API fingerprint on all TFMs.**
- [ ] **Step 2: Audit overloads, nullability, ownership, and Terminal type leakage.**
- [ ] **Step 3: Extend fresh NuGet-only consumer and focused sample.**
- [ ] **Step 4: Promote through RC only after all previous gates are green.**
- [ ] **Step 5: Qualify exact stable source on the seven-job matrix before any merge/tag/publication.**
