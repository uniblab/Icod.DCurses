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
**Qualified stable-source head:** `b90b444556291434bddb9f56d031f09ac1aabfbf`  
**Stable-source workflow:** #719 / `34708925529`  
**Merged main commit:** `18255d59136922b9e246f4113dc6fdb6a9ea24a3`  
**Main Release workflow:** #17 / `34709142076`  

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

The compiler-derived fingerprint is represented by `docs/Public-API-Fingerprint-1.3.json` and is identical on `net8.0`, `net9.0`, and `net10.0`.

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

## Package and merge evidence

The fresh NuGet-only package smoke consumer compiles and executes representative use of rectangles, insets, fixed/proportional/docking/clipping layout, screen/window bounds, retained panel resize, and atomic panel bounds application.

T1310 and the RC qualification used the then-declared `Icod.Terminal 1.9.0` / `Icod.TermInfo 1.10.0` graph. Before final closure, dependencies were refreshed to `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0` without changing this DCurses API fingerprint.

Final stable-source head `b90b444556291434bddb9f56d031f09ac1aabfbf` passed workflow #719 / `34708925529`, including the fresh NuGet-only consumer against the refreshed dependency graph and all six runtime jobs. PR #27 was merged as `18255d59136922b9e246f4113dc6fdb6a9ea24a3`, and the resulting `main` Release workflow #17 / `34709142076` passed.

## Release state

The 1.3 source/API/package candidate is repository-side complete and Release-qualified on `main`. Public release actions remain separate: tag `v1.3.0`, GitHub Release creation, and NuGet publication have not yet occurred.
