# Icod.DCurses 1.3 Public API Baseline

**Release:** `1.3.0`  
**Current source package identity:** `1.3.0`  
**Compatibility floor:** published `1.2.0` contract  
**AssemblyVersion:** `1.0.0.0`  
**Declared runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Qualified T1310 head:** `c8d6a6313b9f6ca124a255c12b912f8ed89ffda7`  
**T1310 workflow:** #681 / `34694609178`  
**Qualified RC head:** `2ab949a64f63759ad9cf4e93e45a368c50ab6e49`  
**RC workflow:** #689 / `34694881296`  

## Published 1.2 floor

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

## 1.3 stable-source contract

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

The compiler-derived fingerprint is represented by `docs/Public-API-Fingerprint-1.3.json` and is required to remain identical on `net8.0`, `net9.0`, and `net10.0`.

Exactly four exported types are added over 1.2:

```text
Icod.DCurses.CursesDockEdge
Icod.DCurses.CursesInsets
Icod.DCurses.CursesLayout
Icod.DCurses.CursesRectangle
```

No published 1.2 exported type is removed.

## Geometry contract

`CursesRectangle` is an immutable zero-based terminal-cell rectangle. It permits empty geometry results, validates non-negative coordinates/dimensions and overflow-sensitive exclusive bounds, and exposes deterministic containment, intersection, and inset behavior.

`CursesInsets` is an immutable non-negative top/right/bottom/left value with derived horizontal and vertical totals.

`CursesDockEdge` identifies `Top`, `Right`, `Bottom`, and `Left` allocation edges.

`CursesLayout` is a stateless static utility. Its public operations are pure and terminal-independent:

```text
SplitTop
SplitBottom
SplitLeft
SplitRight
SplitRowsProportional
SplitColumnsProportional
Dock
Clip
```

Fixed allocation clips oversized requests to available space. Proportional allocation requires positive weights, uses deterministic integer division for the first region, and assigns the remainder to the second. Layout operations retain no screen/window/panel ownership.

## Geometry application contract

Version 1.3 adds these members to existing public types:

```text
CursesScreen.Bounds
CursesWindow.Bounds
CursesWindow.SetBounds(CursesRectangle)
CursesPanel.Bounds
CursesPanel.Resize(int rows, int columns)
CursesPanel.SetBounds(CursesRectangle)
```

`CursesScreen.Bounds` is always `(0, 0, Rows, Columns)`.

A root non-standard window reports screen-relative bounds. A nested window reports bounds relative to its immediate parent. `CursesWindow.SetBounds` is rejected for the standard window, validates the complete final rectangle before mutation, preserves the existing shared-window model, and clamps the cursor when a shrink would otherwise leave it outside the window.

Panel bounds are screen-relative. `CursesPanel.Resize` and `SetBounds` preserve surviving upper-left retained cells and semantic metadata, repair width-two footprints at shrink boundaries, clamp the retained cursor, preserve visibility/z-order/transparency state, and validate owner-screen containment before mutation.

Empty rectangles remain legal geometry values but cannot be applied to windows or panels because retained window/panel dimensions remain positive.

## Resize ownership model

Version 1.3 deliberately does not retain layout rules. Terminal lifecycle synchronization updates `CursesSession.Screen` dimensions; applications then explicitly derive new rectangles from `Screen.Bounds`, explicitly call `SetBounds`/`Resize`, and refresh through the existing retained compositor.

No widget framework, retained layout tree, automatic layout owner, constraint solver, focus router, hit-test system, pointer policy, animation layer, or raster placement contract is added by 1.3.

## Regret-gate conclusions

T1310 reviewed the candidate surface for naming, mutability, overload ambiguity, ownership, and planned 1.4 reuse. No corrective API break was required before RC promotion:

- the geometry names use existing curses terminology and are explicit about rows/columns rather than pixels;
- immutable record structs prevent hidden geometry ownership;
- `SetBounds` complements the already-established `MoveTo`/`Resize` operations without changing their semantics;
- rectangles provide the reusable coordinate substrate planned for 1.4 focus/hit-test/interaction work;
- no overload introduces source ambiguity with the published 1.2 API;
- `AssemblyVersion` remains `1.0.0.0`.

## Package evidence

The fresh NuGet-only package smoke consumer compiles and executes representative use of:

- `CursesRectangle` and `CursesInsets`;
- fixed, proportional, docking, and clipping layout operations;
- `CursesScreen.Bounds`;
- `CursesWindow.Bounds` and `SetBounds`;
- retained `CursesPanel.Resize`;
- `CursesPanel.Bounds` and `SetBounds` with retained content preservation.

The package-only consumer has no project reference to repository source. T1310 alpha qualification and the exact `1.3.0-rc.1` qualification both passed package candidate validation with the then-declared `Icod.Terminal 1.9.0` / `Icod.TermInfo 1.10.0` dependency graph.

Before final stable-source closure, the declared dependencies are explicitly refreshed to `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0`. This package-graph update does not change the compiler-derived DCurses public API fingerprint, but final package evidence must be regenerated against the new dependency graph.

## Stable-source rule

The implementation and public API remain identical to the fully qualified RC above. The dependency-refreshed `1.3.0` source is accepted as repository-side stable only after its exact head passes package validation plus Windows/Linux/macOS x64/ARM64 testing across all supported target frameworks, with the NuGet-only consumer restoring against `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0`.

Merge, tagging, GitHub Release creation, and NuGet publication remain separate explicit actions.
