# Icod.DCurses 1.3.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.3.0`  
**Published baseline:** `1.2.0`  
**Baseline commit:** `16c148a1b84064a05d64f974cbbf2122e1bda236`  
**Assembly version:** `1.0.0.0`  
**Declared dependencies:** `Icod.Terminal 1.9.0`; `Icod.TermInfo 1.10.0`  
**Theme:** deterministic geometry, layout allocation, and retained panel resizing  
**Status:** planning complete; T1301 starting

---

## Release objective

Version 1.3 makes terminal geometry manageable without creating a widget framework or hidden layout owner. Applications receive immutable rectangle/inset primitives and pure layout helpers, explicitly recompute geometry when the screen changes, and explicitly apply resulting bounds to windows and panels.

The 1.2 retained-panel model remains intact. Version 1.3 adds geometry and resize capability on top of it.

## Frozen design rules

- `CursesWindow` remains a shared logical view.
- `CursesPanel` remains an independent retained z-ordered surface.
- Layout operations are pure and terminal-independent.
- No retained layout tree or automatic application-layout ownership.
- Empty rectangles are valid geometry results.
- Windows/panels still require positive dimensions when geometry is applied.
- Panel resize preserves surviving upper-left content and metadata.
- Width-two footprints are repaired at resize boundaries.
- Screen resize recomputation remains explicit application behavior.
- 1.4 will reuse 1.3 rectangles for focus/hit-test/interaction work.

## Workflow

```text
T1301  foundation + immutable geometry contract
  -> T1302  fixed splits / insets / clipping
  -> T1303  proportional row/column allocation
  -> T1304  docking contract
  -> T1305  retained panel resize
  -> T1306  rectangle application conveniences
  -> T1307  live resize recomputation acceptance + sample
  -> T1308  Unicode / metadata / wide-cell resize hardening
  -> T1309  application / performance / allocation acceptance
  -> T1310  API / package / docs / licensing regret gate
  -> T1311  RC / stable-source closure
```

Every tranche follows the same gate:

```text
write contract tests
    -> establish RED when behavior/API is new
    -> implement minimum production change
    -> focused GREEN
    -> full net8/net9/net10 tests
    -> package candidate validation
    -> Windows/Linux/macOS x64/ARM64 qualification
    -> record exact SHA/workflow
```

## Planned public surface

### Geometry

- `CursesRectangle`
- `CursesInsets`
- `CursesDockEdge`
- static `CursesLayout`

### Geometry application

- `CursesScreen.Bounds`
- `CursesWindow.Bounds`
- `CursesWindow.SetBounds(...)`
- `CursesPanel.Bounds`
- `CursesPanel.Resize(...)`
- `CursesPanel.SetBounds(...)`

The exact final API remains provisional until T1310's compiler-derived fingerprint/regret gate.

## T1301 acceptance

T1301 is complete when:

- source/package identity is `1.3.0-alpha.1` while `AssemblyVersion` remains `1.0.0.0`;
- `CursesRectangle` and `CursesInsets` exist with validated immutable semantics;
- empty/intersection/inset behavior is frozen by tests;
- the provisional 1.3 API fingerprint is compiler-derived;
- the exact head passes the normal seven-job PR matrix.

## Non-goals

Version 1.3 does not add widgets, focus traversal, keyboard gestures, hit testing, pointer-shape ownership, automatic layout trees, CSS/flexbox behavior, general constraint solving, animation, raster placement, terminal pixel layout, PTYs, or process hosting.

## Design and implementation authorities

- `docs/superpowers/specs/2026-09-11-icod-dcurses-1.3-layout-resize-design.md`
- `docs/superpowers/plans/2026-09-11-icod-dcurses-1.3-layout-resize.md`
