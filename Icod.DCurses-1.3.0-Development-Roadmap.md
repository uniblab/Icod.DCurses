# Icod.DCurses 1.3.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.3.0`  
**Published baseline:** `1.2.0`  
**Baseline commit:** `16c148a1b84064a05d64f974cbbf2122e1bda236`  
**Current source package:** `1.3.0-rc.1`  
**Assembly version:** `1.0.0.0`  
**Declared dependencies:** `Icod.Terminal 1.9.0`; `Icod.TermInfo 1.10.0`  
**Theme:** deterministic geometry, layout allocation, retained panel resizing, and explicit live resize recomputation  
**Status:** T1301-T1310 complete and qualified; T1311 release-candidate qualification in progress

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

## Frozen 1.3 public contract

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

Four exported types are added over published 1.2:

```text
CursesRectangle
CursesInsets
CursesDockEdge
CursesLayout
```

Existing-type additions are:

```text
CursesScreen.Bounds
CursesWindow.Bounds
CursesWindow.SetBounds(CursesRectangle)
CursesPanel.Bounds
CursesPanel.Resize(int rows, int columns)
CursesPanel.SetBounds(CursesRectangle)
```

`AssemblyVersion` remains `1.0.0.0`.

## Qualified checkpoints

| Tranche | Exact head | Workflow | Result |
|---|---|---|---|
| T1301 geometry foundation | `fb77341ac844ae9345917a36f4e4d33febe46125` | #633 / `34598939553` | seven jobs green |
| T1302 fixed layout | `acb3f5beee8003cc952f6f5062eefd8ed7f70eb1` | #637 / `34603146171` | seven jobs green |
| T1303 proportional layout + negotiation isolation | `a10a58bb37e17a86401a1ccabb863458b516cb21` | #643 / `34613814570` | seven jobs green |
| T1304 docking | `2c80b1dceca475b81690baa6c2c00f30167b24a6` | #647 / `34614953520` | seven jobs green |
| T1305 retained panel resize | `89409d7e323657bd8f86cfd80bedd882486fba4a` | #652 / `34621558815` | seven jobs green |
| T1306 rectangle application | `aa29fd378ba96e879cfb28fee05fefa37cdf3774` | #659 / `34622698523` | seven jobs green |
| T1307 live resize recomputation + sample | `1c8ad631fd68fba4e56a9afd4cbc2bca832769d4` | #665 / `34623947573` | seven jobs green |
| T1308 Unicode/metadata/wide-cell hardening | `976f677a24a07cacfdde21c2bb8aed61b6b7be89` | #666 / `34624445542` | seven jobs green |
| T1309 application/performance/allocation acceptance | `c209780cf4a0afbf71991e34a1d8912373426922` | #671 / `34625614322` | seven jobs green |
| T1310 API/package/docs/licensing regret gate | `c8d6a6313b9f6ca124a255c12b912f8ed89ffda7` | #681 / `34694609178` | seven jobs green |

T1303 includes the test-isolation correction required by the macOS bounded Terminal negotiation acceptance tests; production timeout behavior was not changed.

## Accepted implementation

T1301-T1304 provide immutable geometry plus fixed, proportional, docking, and clipping allocation. T1305-T1306 add retained panel resize and explicit rectangle application to screens/windows/panels. T1307 demonstrates explicit live relayout from synchronized `Screen.Bounds`. T1308 hardens Unicode/metadata/wide-cell resize behavior. T1309 freezes application-shaped state and allocation behavior. T1310 freezes the reviewed API/package/documentation/licensing candidate and proves the packed artifact through a fresh NuGet-only 1.3 consumer.

Authorities:

- `docs/Public-API-Fingerprint-1.3.json`
- `docs/Public-API-Baseline-1.3.md`
- `docs/T1309-Layout-Application-Performance-and-Allocation-Acceptance.md`
- `docs/T1310-Public-API-Package-Documentation-and-Regret-Gate.md`

## T1311 — current gate

The unchanged T1310-qualified implementation/API has been promoted to source/package identity `1.3.0-rc.1`.

Only release/package/status metadata changed. Runtime dependencies remain `Icod.Terminal 1.9.0` and `Icod.TermInfo 1.10.0`; `AssemblyVersion` remains `1.0.0.0`.

The RC exact head must pass the normal seven-job PR matrix before any stable-source promotion. After a green RC, the unchanged implementation/API may be promoted to source identity `1.3.0` and must pass a second exact-head seven-job matrix.

Authority: `docs/T1311-RC-and-Stable-Closure.md`.

## Remaining workflow

```text
T1301 foundation + immutable geometry contract                 complete
T1302 fixed splits / insets / clipping                        complete
T1303 proportional row/column allocation                      complete
T1304 docking contract                                         complete
T1305 retained panel resize                                    complete
T1306 rectangle application conveniences                       complete
T1307 live resize recomputation acceptance + sample            complete
T1308 Unicode / metadata / wide-cell resize hardening          complete
T1309 application / performance / allocation acceptance        complete
T1310 API / package / docs / licensing regret gate             complete
T1311 RC / stable-source closure                               RC qualification in progress
```

## Non-goals

Version 1.3 does not add widgets, focus traversal, keyboard gestures, hit testing, pointer-shape ownership, automatic layout trees, CSS/flexbox behavior, general constraint solving, animation, raster placement, terminal pixel layout, PTYs, or process hosting.

## Design and implementation authorities

- `docs/superpowers/specs/2026-09-11-icod-dcurses-1.3-layout-resize-design.md`
- `docs/superpowers/plans/2026-09-11-icod-dcurses-1.3-layout-resize.md`
- `docs/Public-API-Fingerprint-1.3.json`
- `docs/Public-API-Baseline-1.3.md`
- `docs/T1309-Layout-Application-Performance-and-Allocation-Acceptance.md`
- `docs/T1310-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/T1311-RC-and-Stable-Closure.md`

Merge, tagging, GitHub Release creation, and NuGet publication remain separate explicit actions after repository-side stable-source qualification.
