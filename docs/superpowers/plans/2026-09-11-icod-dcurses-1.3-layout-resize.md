# Icod.DCurses 1.3.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver `Icod.DCurses 1.3.0` with immutable geometry types, deterministic layout helpers, atomic rectangle application, and retained panel resizing over the published 1.2 contract.

**Architecture:** Geometry is represented by small immutable value types. `CursesLayout` performs pure terminal-independent allocation; applications explicitly recompute on terminal resize and explicitly apply rectangles to windows/panels. Panel resizing reuses the existing screen resize engine in the panel-private surface and preserves 1.2 composition/lifecycle semantics.

**Tech Stack:** C# 13, .NET 8/9/10, xUnit, existing Icod.DCurses retained screen/panel engine, GitHub Actions Windows/Linux/macOS x64/ARM64 matrix.

**Spec:** `docs/superpowers/specs/2026-09-11-icod-dcurses-1.3-layout-resize-design.md`

## Global Constraints

- Baseline is published `Icod.DCurses 1.2.0`, `main` commit `16c148a1b84064a05d64f974cbbf2122e1bda236`.
- Preserve `AssemblyVersion 1.0.0.0`.
- Keep `Icod.Terminal 1.9.0` and `Icod.TermInfo 1.10.0` unless a separately approved dependency change occurs.
- Keep all library `.cs` LGPL headers and all executable sample/test GPL headers intact.
- `Icod.DCurses.csproj` line 1 remains exactly `<?xml version="1.0" encoding="utf-8"?>`.
- No widgets, retained layout tree, focus routing, hit testing, pointer policy, raster placement, animation, or general constraint solver in 1.3.
- All public/internal methods validate parameters at entry; all `if`/`else` blocks use braces.
- Tests must not write stdout/stderr except process communication.
- Every tranche receives exact-head package + Windows/Linux/macOS x64/ARM64 qualification before being called complete.

---

### Task 1: T1301 Foundation and Geometry Contract

**Files:**
- Modify: `Icod.DCurses.csproj`
- Create: `src/CursesRectangle.cs`
- Create: `src/CursesInsets.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesGeometryTests.cs`
- Modify: `docs/Public-API-Fingerprint-1.3.json` only after compiler-derived evidence exists

**Interfaces:**
- Produces `CursesRectangle` and `CursesInsets` used by every later task.

- [ ] **Step 1: bump development identity**

Set `Version` and `PackageVersion` to `1.3.0-alpha.1`; retain `AssemblyVersion 1.0.0.0` and current dependency versions.

- [ ] **Step 2: write failing geometry tests**

Freeze construction validation, empty rectangles, exclusive bounds, coordinate/rectangle containment, intersection, and inset behavior.

Representative contract:

```csharp
CursesRectangle bounds = new(
    row: 2,
    column: 3,
    rows: 4,
    columns: 5
);
Assert.Equal( 6, bounds.BottomExclusive );
Assert.Equal( 8, bounds.RightExclusive );
Assert.True( bounds.Contains( 2, 3 ) );
Assert.False( bounds.Contains( 6, 3 ) );
```

- [ ] **Step 3: run the focused test file and confirm RED**

Run the PR build/test path or focused equivalent. Expected failures are missing `CursesRectangle`/`CursesInsets` only.

- [ ] **Step 4: implement immutable value types**

Required signatures:

```csharp
public readonly record struct CursesRectangle {
    public CursesRectangle( int row, int column, int rows, int columns );
    public int Row { get; }
    public int Column { get; }
    public int Rows { get; }
    public int Columns { get; }
    public int BottomExclusive { get; }
    public int RightExclusive { get; }
    public bool IsEmpty { get; }
    public bool Contains( int row, int column );
    public bool Contains( CursesRectangle rectangle );
    public CursesRectangle Intersect( CursesRectangle rectangle );
    public CursesRectangle Inset( CursesInsets insets );
}

public readonly record struct CursesInsets {
    public CursesInsets( int top, int right, int bottom, int left );
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }
    public int Left { get; }
    public int Horizontal { get; }
    public int Vertical { get; }
}
```

- [ ] **Step 5: run focused + full tests and qualify GREEN**

- [ ] **Step 6: derive/update provisional 1.3 API fingerprint and commit T1301**

---

### Task 2: T1302 Fixed Splits, Insets, and Clipping

**Files:**
- Create: `src/CursesLayout.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesLayoutFixedTests.cs`

**Interfaces:**
- Consumes: `CursesRectangle`, `CursesInsets`.
- Produces fixed deterministic layout helpers.

- [ ] Write RED tests for top/bottom/left/right fixed allocation, requests larger than available space, zero-size requests, and empty inputs.
- [ ] Implement:

```csharp
public static class CursesLayout {
    public static void SplitTop( CursesRectangle bounds, int rows, out CursesRectangle first, out CursesRectangle remaining );
    public static void SplitBottom( CursesRectangle bounds, int rows, out CursesRectangle remaining, out CursesRectangle last );
    public static void SplitLeft( CursesRectangle bounds, int columns, out CursesRectangle first, out CursesRectangle remaining );
    public static void SplitRight( CursesRectangle bounds, int columns, out CursesRectangle remaining, out CursesRectangle last );
    public static CursesRectangle Clip( CursesRectangle rectangle, CursesRectangle container );
}
```

Negative requested extents throw; oversized requests clip to available space.

- [ ] Qualify exact head and commit T1302.

---

### Task 3: T1303 Proportional Allocation

**Files:**
- Modify: `src/CursesLayout.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesLayoutProportionalTests.cs`

**Interfaces:**
- Produces proportional two-way row/column splits.

- [ ] Write RED tests covering equal/unequal weights, odd remainders, tiny/empty bounds, and invalid zero/negative weights.
- [ ] Implement:

```csharp
public static void SplitRowsProportional(
    CursesRectangle bounds,
    int firstWeight,
    int secondWeight,
    out CursesRectangle first,
    out CursesRectangle second
);

public static void SplitColumnsProportional(
    CursesRectangle bounds,
    int firstWeight,
    int secondWeight,
    out CursesRectangle first,
    out CursesRectangle second
);
```

Integer division determines the first extent; the second receives the deterministic remainder so the pair always covers the input bounds exactly.

- [ ] Qualify and commit T1303.

---

### Task 4: T1304 Docking Contract

**Files:**
- Create: `src/CursesDockEdge.cs`
- Modify: `src/CursesLayout.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesLayoutDockTests.cs`

**Interfaces:**
- Produces `CursesDockEdge` and generic dock allocation.

- [ ] Write RED tests for `Top`, `Bottom`, `Left`, `Right`, clipping, zero size, empty bounds, invalid enum, and negative size.
- [ ] Implement:

```csharp
public enum CursesDockEdge {
    Top,
    Right,
    Bottom,
    Left
}

public static void Dock(
    CursesRectangle bounds,
    CursesDockEdge edge,
    int size,
    out CursesRectangle docked,
    out CursesRectangle remaining
);
```

- [ ] Qualify and commit T1304.

---

### Task 5: T1305 Retained Panel Resize

**Files:**
- Modify: `src/CursesPanel.cs`
- Modify: `src/Internal/CursesPanelSurface.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesPanelResizeTests.cs`

**Interfaces:**
- Adds `CursesPanel.Resize(int rows, int columns)`.

- [ ] Write RED tests for grow/shrink, retained upper-left content, cursor clamping, metadata preservation, wide-cell boundary repair, visibility/z-order/transparency preservation, and invalid size/containment.
- [ ] Implement panel-surface resize by calling its backing `CursesScreen.Resize(columns, rows, preserveContents: true)`.
- [ ] Add public panel resize validation against the owner screen before mutating the surface.
- [ ] Verify incremental composition invalidates old/new footprints and no stale cells survive shrink/grow.
- [ ] Qualify and commit T1305.

---

### Task 6: T1306 Rectangle Application Convenience

**Files:**
- Modify: `src/CursesScreen.cs`
- Modify: `src/CursesWindow.cs`
- Modify: `src/CursesPanel.cs`
- Create: `tests/Icod.DCurses.Tests/src/CursesBoundsApplicationTests.cs`

**Interfaces:**
- Adds:

```csharp
public CursesRectangle CursesScreen.Bounds { get; }
public CursesRectangle CursesWindow.Bounds { get; }
public void CursesWindow.SetBounds( CursesRectangle bounds );
public CursesRectangle CursesPanel.Bounds { get; }
public void CursesPanel.SetBounds( CursesRectangle bounds );
```

- [ ] RED-test standard/non-standard window behavior, nested relative bounds, atomic move+resize cases, panel atomic move+resize, and invalid/empty rectangles.
- [ ] Implement atomic final-rectangle validation before mutating window/panel fields.
- [ ] Preserve existing cursor-clamp behavior when dimensions shrink.
- [ ] Qualify and commit T1306.

---

### Task 7: T1307 Live Resize Recomputation Acceptance

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesLayoutResizeIntegrationTests.cs`
- Create or extend: `samples/Icod.DCurses.Layout.Sample/Program.cs`
- Create: `samples/Icod.DCurses.Layout.Sample/Icod.DCurses.Layout.Sample.csproj`
- Modify: `Icod.DCurses.sln`
- Modify: `samples/README.md`

**Interfaces:**
- Demonstrates explicit recomputation from `session.Screen.Bounds` after lifecycle resize.

- [ ] Add integration tests proving terminal dimension synchronization -> layout recomputation -> window/panel `SetBounds` -> retained repaint.
- [ ] Add a GPL-headed interactive sample with two regions and one retained panel that recompute on resize.
- [ ] Qualify all TFMs/platforms and commit T1307.

---

### Task 8: T1308 Unicode, Metadata, and Wide-Cell Resize Hardening

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesLayoutUnicodeResizeTests.cs`

- [ ] Add tests for width-two leaders/continuations at new panel boundaries, semantic metadata survival, transparent blank behavior after resize, and repeated grow/shrink cycles.
- [ ] Run `CursesCellFootprint.Validate` after every retained resize state.
- [ ] Fix only demonstrated defects in existing resize/composition machinery.
- [ ] Qualify and commit T1308.

---

### Task 9: T1309 Application, Performance, and Allocation Acceptance

**Files:**
- Create: `tests/Icod.DCurses.Tests/src/CursesLayoutApplicationAcceptanceTests.cs`
- Create: `docs/T1309-Layout-Application-Performance-and-Allocation-Acceptance.md`

- [ ] Exercise representative top/status/sidebar/dialog layouts using chained splits/docking.
- [ ] Freeze allocation ceilings for repeated pure geometry calculations and steady-state resize-free frames.
- [ ] Ensure no retained layout objects/background ownership were introduced.
- [ ] Qualify and commit T1309.

---

### Task 10: T1310 API, Package, Documentation, and Regret Gate

**Files:**
- Modify: `README.md`
- Modify: `Icod.DCurses-Development-Roadmap.md`
- Modify: `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- Modify: `Icod.DCurses-1.3.0-Development-Roadmap.md`
- Modify: `tools/package-smoke/*` as needed
- Create: `docs/Public-API-Fingerprint-1.3.json`
- Create: `docs/Public-API-Baseline-1.3.md`
- Create: `docs/T1310-Public-API-Package-Documentation-and-Regret-Gate.md`

- [ ] Audit every new public type/member for naming, mutability, ambiguity, and future 1.4 reuse.
- [ ] Verify package-only consumer use of rectangles, layout helpers, panel resize, and bounds application.
- [ ] Freeze the compiler-derived 1.3 API fingerprint without rewriting historical baselines.
- [ ] Audit LGPL/GPL headers on all newly created source/project files.
- [ ] Qualify and commit T1310.

---

### Task 11: T1311 RC and Stable-Source Closure

**Files:**
- Modify release/version/status docs and `Icod.DCurses.csproj` only.
- Create: `docs/T1311-RC-and-Stable-Closure.md`

- [ ] Promote unchanged implementation/API to `1.3.0-rc.1` and qualify exact head.
- [ ] If green, promote the same implementation/API to stable-source `1.3.0` and qualify exact head.
- [ ] Record exact SHAs/workflows without making a self-referential post-qualification source commit.
- [ ] Do not merge, tag, create a GitHub Release, or publish NuGet without explicit authorization.
