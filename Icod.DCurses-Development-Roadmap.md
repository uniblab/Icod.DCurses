# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Accepted stable compatibility floor:** published `1.2.0`  
**Current development package:** `1.3.0`  
**Assembly version:** `1.0.0.0`  
**Current declared runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.3.0` — deterministic geometry, layout allocation, retained panel resizing, explicit live relayout  
**Status:** T1301-T1310 complete; T1311 RC qualified; stable-source dependency refresh qualification in progress

---

## Current authorities

- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- `Icod.DCurses-1.3.0-Development-Roadmap.md`
- `docs/Public-API-Fingerprint-1.3.json`
- `docs/Public-API-Baseline-1.3.md`
- `docs/T1309-Layout-Application-Performance-and-Allocation-Acceptance.md`
- `docs/T1310-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/T1311-RC-and-Stable-Closure.md`

Historical 1.0-1.2 tranche records remain historical compatibility authorities and are not rewritten to simulate current development state.

## Release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Historical stable baseline |
| `1.1.0` | Semantic metadata and hyperlinks | Historical stable baseline |
| `1.2.0` | Panels/layers/z-order composition | Published stable baseline |
| `1.3.0` | Layout and resize primitives | stable-source dependency refresh qualification active |
| `1.4.0` | Focus/interaction/gestures/hit testing/pointer semantics | Approved future release |

## API policy

Published 1.2 contract:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Frozen 1.3 contract:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

Four exported types are added over 1.2: `CursesRectangle`, `CursesInsets`, `CursesDockEdge`, and `CursesLayout`. Existing screen/window/panel types receive additive bounds/resize application members. `AssemblyVersion` remains `1.0.0.0`.

The stable-source dependency refresh to `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0` changes the package dependency graph only. It does not change the frozen DCurses public API fingerprint above.

## 1.3 accepted architecture

Geometry is immutable and terminal-independent. `CursesLayout` performs pure fixed/proportional/docking/clipping calculations and retains no application state.

`CursesWindow` remains a shared logical view. `CursesPanel` remains an independent retained screen-owned surface. Version 1.3 adds retained panel resizing plus explicit rectangle application without changing those ownership models.

Live terminal resize remains explicit application policy:

```text
session.Screen.Bounds
    -> application layout calculation
    -> window/panel SetBounds or panel Resize
    -> retained RefreshAsync
```

No retained layout tree or automatic layout owner exists.

## Qualified 1.3 checkpoints

| Tranche | Exact head | Workflow | Result |
|---|---|---|---|
| T1301 | `fb77341ac844ae9345917a36f4e4d33febe46125` | #633 / `34598939553` | seven jobs green |
| T1302 | `acb3f5beee8003cc952f6f5062eefd8ed7f70eb1` | #637 / `34603146171` | seven jobs green |
| T1303 | `a10a58bb37e17a86401a1ccabb863458b516cb21` | #643 / `34613814570` | seven jobs green |
| T1304 | `2c80b1dceca475b81690baa6c2c00f30167b24a6` | #647 / `34614953520` | seven jobs green |
| T1305 | `89409d7e323657bd8f86cfd80bedd882486fba4a` | #652 / `34621558815` | seven jobs green |
| T1306 | `aa29fd378ba96e879cfb28fee05fefa37cdf3774` | #659 / `34622698523` | seven jobs green |
| T1307 | `1c8ad631fd68fba4e56a9afd4cbc2bca832769d4` | #665 / `34623947573` | seven jobs green |
| T1308 | `976f677a24a07cacfdde21c2bb8aed61b6b7be89` | #666 / `34624445542` | seven jobs green |
| T1309 | `c209780cf4a0afbf71991e34a1d8912373426922` | #671 / `34625614322` | seven jobs green |
| T1310 | `c8d6a6313b9f6ca124a255c12b912f8ed89ffda7` | #681 / `34694609178` | seven jobs green |
| T1311 RC | `2ab949a64f63759ad9cf4e93e45a368c50ab6e49` | #689 / `34694881296` | seven jobs green |

## Current sequence

```text
T1301-T1310  implementation + acceptance + regret gate           complete
T1311 RC      1.3.0-rc.1 exact-head qualification                complete
T1311 stable  Terminal 1.11.1 + TermInfo 1.11.0 refresh          qualification in progress
```

The final stable-source gate must prove the packed `1.3.0` package and complete runtime matrix against the declared `Icod.Terminal 1.11.1` / `Icod.TermInfo 1.11.0` graph. A green exact head completes repository-side 1.3 development qualification.

Merge, main Release qualification, tagging, GitHub Release creation, and NuGet publication remain explicit separate actions and are not implied by source completion.
