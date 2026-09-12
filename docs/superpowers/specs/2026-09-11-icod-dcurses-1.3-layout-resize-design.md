# Icod.DCurses 1.3.0 Layout and Resize Design

**Date:** 2026-09-11  
**Project:** `Icod.DCurses`  
**Baseline:** published `1.2.0`, `main` commit `16c148a1b84064a05d64f974cbbf2122e1bda236`  
**Theme:** deterministic geometry, layout allocation, and retained panel resizing

## Objective

Version 1.3 removes routine row/column/height/width arithmetic from application code without introducing a widget tree, a CSS/flexbox model, a general constraint solver, or hidden layout ownership.

The release adds small immutable geometry value types and pure deterministic layout operations. Applications explicitly recompute geometry when terminal dimensions change and explicitly apply resulting rectangles to windows and panels.

## Compatibility and ownership

The 1.2 panel/window contract remains the compatibility floor. `CursesWindow` remains a shared view into one logical screen. `CursesPanel` remains an independent retained surface in the screen-owned z-order stack. `Icod.Terminal` remains the sole live terminal/session/input/protocol owner; `Icod.TermInfo` remains the immutable capability authority.

No 1.3 layout primitive performs terminal I/O. No retained layout tree is introduced. No layout object owns windows or panels.

## Public geometry model

### `CursesRectangle`

Add an immutable public value type representing a zero-based rectangle:

```csharp
public readonly record struct CursesRectangle(
    int Row,
    int Column,
    int Rows,
    int Columns
);
```

Construction requires non-negative row, column, rows, and columns. Empty rectangles are valid and have either zero rows or zero columns. The type exposes:

- `BottomExclusive`
- `RightExclusive`
- `IsEmpty`
- coordinate containment
- rectangle containment
- intersection/clipping
- inset application

Overflow in exclusive-bound arithmetic is rejected rather than wrapped.

### `CursesInsets`

Add an immutable public value type:

```csharp
public readonly record struct CursesInsets(
    int Top,
    int Right,
    int Bottom,
    int Left
);
```

All values are non-negative. `Horizontal` and `Vertical` totals are exposed. Applying insets to a rectangle never creates negative dimensions; excessive insets yield an empty rectangle deterministically.

## Pure layout operations

Add a public static `CursesLayout` class. All operations are terminal-independent and side-effect free.

The first release surface provides:

- fixed top/bottom allocation plus remainder;
- fixed left/right allocation plus remainder;
- proportional two-way row split;
- proportional two-way column split;
- docking to `Top`, `Bottom`, `Left`, or `Right` through `CursesDockEdge`;
- clipping to a containing rectangle.

Fixed requests larger than the available extent are clipped to the available extent. A remainder may therefore be empty. Negative fixed sizes are programmer errors and throw. Proportional weights must be positive and are evaluated using integer arithmetic with deterministic remainder assignment to the second region.

This deliberately avoids arbitrary N-way solver state in 1.3. N-way layouts can be built by repeated deterministic splits, which keeps the contract small and composable.

## Applying geometry

Add rectangle conveniences rather than replacing established primitive APIs:

- `CursesScreen.Bounds`
- `CursesWindow.Bounds`
- `CursesWindow.SetBounds(CursesRectangle)` for non-standard windows
- `CursesPanel.Bounds`
- `CursesPanel.Resize(int rows, int columns)`
- `CursesPanel.SetBounds(CursesRectangle)`

`CursesWindow.Resize()` and `Reposition()` remain supported low-level operations.

`SetBounds` is atomic from the caller's perspective. It validates the final rectangle against the containing screen/window and must not fail merely because one hypothetical intermediate move-then-resize ordering would be invalid.

## Panel resize semantics

Panel resize is the only new retained-surface mutation introduced by 1.3.

- The surviving upper-left content region is preserved.
- Semantic metadata follows surviving cells.
- Width-two cell footprints are repaired at the new boundary.
- Cursor coordinates are clamped into the resized surface.
- Transparency, visibility, z-order, and ownership are unchanged.
- Old and new destination footprints are invalidated so shrink, grow, and move-resize cannot leave stale physical content.
- A panel must remain wholly contained by its owning screen when resized or assigned new bounds.

The implementation should reuse `CursesScreen.Resize(..., preserveContents: true)` inside the panel-private backing surface instead of introducing a second resize engine.

## Screen resize workflow

Version 1.3 does not automatically remember application layout rules. On terminal resize:

1. `CursesSession` synchronizes the screen dimensions through the existing lifecycle path.
2. The application recomputes rectangles from `session.Screen.Bounds` using `CursesLayout`.
3. The application applies rectangles to windows/panels with `SetBounds`/`Resize`.
4. Existing damage/composition/refresh machinery emits the required repaint.

A focused sample will demonstrate this explicit recomputation model.

## Empty and undersized behavior

Responsive layout shrinkage is ordinary input, not exceptional control flow. Geometry helpers therefore clip fixed allocations and allow empty rectangles.

Mutable window/panel application APIs continue to require positive dimensions because existing windows and retained panel surfaces cannot have zero size. Applications should detect `rectangle.IsEmpty` and hide/skip the corresponding region rather than attempting to apply it.

## Testing strategy

Tests will freeze:

- geometry validation and overflow behavior;
- intersection/inset/empty semantics;
- fixed, proportional, and dock allocation determinism;
- atomic window/panel rectangle application;
- panel grow/shrink content and metadata preservation;
- wide-cell boundary repair;
- composition damage for shrink/grow/move-resize;
- live resize recomputation through `CursesSession`;
- allocation/resource ceilings for steady-state layout arithmetic;
- public API fingerprint and package-only consumption.

All supported target frameworks (`net8.0`, `net9.0`, `net10.0`) and Windows/Linux/macOS x64/ARM64 validation remain required.

## Non-goals

1.3 does not add widgets, focus routing, gesture routing, hit testing, pointer-shape policy, automatic retained layout trees, animation, alpha blending, raster placement, terminal pixel layout, PTY/process hosting, or a general constraint solver. Those remain outside this release, with focus/interaction mechanics reserved for 1.4.
