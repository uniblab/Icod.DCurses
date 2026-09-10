# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Accepted stable compatibility floor:** `1.1.0`  
**1.1 merged baseline commit:** `99aa3a6f95d950e37f729386549dc42817f63bd1`  
**Current source package:** `1.2.0-rc.1`  
**Assembly version:** `1.0.0.0`  
**Current declared runtime dependencies:** `Icod.Terminal 1.8.1`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.2.0` — panels, retained layers, and z-order composition  
**Status:** T1201–T1209 complete and qualified; T1210 `1.2.0-rc.1` exact-head qualification active

---

## Purpose

This is the current roadmap index. Historical pre-1.0 and 1.1 tranche documents remain permanent compatibility/qualification records rather than being rewritten as current-state documents.

Current authorities:

- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md` — approved post-1.0 release train;
- `Icod.DCurses-1.2.0-Development-Roadmap.md` — active detailed 1.2 plan;
- `docs/T1208-Panel-Application-Performance-and-Allocation-Acceptance.md` — application/resource acceptance;
- `docs/T1209-Public-API-Package-Documentation-and-Regret-Gate.md` — completed pre-RC regret gate;
- `docs/T1210-RC-and-Stable-Closure.md` — active release closure record.

## Current release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Historical published baseline |
| `1.1.0` | Semantic cell metadata and hyperlinks | Accepted stable compatibility floor |
| `1.2.0` | Panels, layers, visibility, z-order composition | RC qualification active |
| `1.3.0` | Layout and resize primitives | Approved future release |
| `1.4.0` | Focus, interaction regions, gestures, hit testing, pointer semantics | Approved future release |
| later | Raster graphics over Terminal semantic raster routing | Deferred until a concrete DCurses graphics consumer requires it |

## Compatibility and version policy

Accepted 1.1 contract:

```text
45 exported types
337 canonical contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

Accepted 1.2 candidate contract:

```text
47 exported types
356 canonical contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Compatible additive 1.x releases retain `AssemblyVersion 1.0.0.0`. Dependency versions remain project declarations rather than duplicated verifier policy.

## Qualified 1.2 checkpoints

| Tranche | Exact head | Workflow | Result |
|---|---|---|---|
| T1201 | `b4582beebe9519d5b5feaf5a4f4ada54adc21672` | #564 / `34510470939` | seven jobs green |
| T1202 | `cfb4973992c78273e0edcdff17065e8b8a029af0` | #571 / `34511809356` | seven jobs green |
| T1203 | `85d907f7cf00fa4e8839ed6173255218a9efa17f` | #575 / `34513600585` | seven jobs green |
| T1204 | `09293c42dfa2c46484542601b7c0538babe07684` | #584 / `34515384714` | seven jobs green |
| T1205 | `f18d3a55877115f224f9aca8ccc7cde6cfafd845` | #588 / `34517869687` | seven jobs green |
| T1206 | `8a7ce13d25d3f911b9c17dbe6ce24af0012c2431` | #590 / `34518913849` | seven jobs green |
| T1207 | `c0b7fcd48c9b97eaa924299367ffd28779dfd1d0` | #593 / `34520535815` | seven jobs green |
| T1208 | `bb00707779cf3dc6c2222455a6f942469d036881` | #596 / `34522859308` | seven jobs green |
| T1209 API/lifetime | `866497c9d5015f3a149580d67eacefaf7e121aaf` | #600 / `34524054785` | seven jobs green |
| T1209 docs/sample/package | `3728bf0e576b32747dd3a628ed5d3eca768ac67f` | #603 / `34525966166` | seven jobs green |

T1209 closure built the focused panel sample on all three TFMs, passed fresh package-only panel consumption, and reported 554/554 tests per TFM on the Linux ARM64 evidence leg with zero build warnings/errors.

## 1.2 accepted architecture

Version 1.2 preserves ordinary `CursesWindow` shared-view semantics and adds `CursesPanel` as a distinct independent retained surface.

The accepted shape provides deterministic screen-owned z-order, show/hide, movement, opaque or blank-transparent composition, clipping, incremental damage-bounded recomposition, Unicode/wide-cell/metadata coherence, live session refresh, resize/suspend/resume integration, and one-way `IDisposable` removal for transient panels.

Panel size remains fixed in 1.2. General layout and resize primitives remain assigned to 1.3. No-panel sessions retain the original direct refresh path with an allocation-free internal presence check.

## Active sequence

```text
T1201  foundation + deterministic internal order engine             complete
  -> T1202  independent retained panel surface + public creation    complete
  -> T1203  visibility/movement/z-order API                         complete
  -> T1204  logical composition + clipping + transparency           complete
  -> T1205  damage + incremental recomposition                      complete
  -> T1206  Unicode/wide/metadata hardening                         complete
  -> T1207  resize/lifecycle/session integration                    complete
  -> T1208  application/performance/allocation acceptance           complete
  -> T1209  public API/package/docs/regret gate                     complete
  -> T1210  RC/stable closure                                      RC qualification active
```

A green RC is followed by one stable-source `1.2.0` promotion with the same implementation/API and a fresh seven-job exact-head qualification. Merge, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.
