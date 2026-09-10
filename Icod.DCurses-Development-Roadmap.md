# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Accepted stable compatibility floor:** `1.1.0`  
**1.1 merged baseline commit:** `99aa3a6f95d950e37f729386549dc42817f63bd1`  
**Current source package:** `1.2.0`  
**Assembly version:** `1.0.0.0`  
**Current declared runtime dependencies:** `Icod.Terminal 1.9.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.2.0` — panels, retained layers, and z-order composition  
**Status:** implementation/API/sample/test closure complete; dependency-refresh qualification pending NuGet indexing of `Icod.Terminal 1.9.0`

---

## Current authorities

- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- `Icod.DCurses-1.2.0-Development-Roadmap.md`
- `docs/T1208-Panel-Application-Performance-and-Allocation-Acceptance.md`
- `docs/T1209-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/T1210-RC-and-Stable-Closure.md`
- `docs/Public-API-Fingerprint-1.2.json`
- `docs/Public-API-Baseline-1.2.md`

Historical pre-1.2 tranche records remain historical and are not rewritten to simulate current dependency/version state.

## Release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Historical stable baseline |
| `1.1.0` | Semantic metadata and hyperlinks | Accepted compatibility floor |
| `1.2.0` | Panels/layers/z-order composition | Dependency-refresh qualification pending |
| `1.3.0` | Layout and resize primitives | Approved future release |
| `1.4.0` | Focus/interaction/gestures/hit testing/pointer semantics | Approved future release |

## API policy

Accepted 1.1 contract:

```text
45 exported types
337 canonical contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

Accepted 1.2 contract:

```text
47 exported types
356 canonical contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Exactly two exported types are added over 1.1: `CursesPanel` and `CursesPanelTransparency`. `AssemblyVersion` remains `1.0.0.0`.

## 1.2 accepted architecture

Ordinary `CursesWindow` remains a shared view. `CursesPanel` owns an independent retained surface and participates in deterministic screen-owned z-order composition.

The accepted substrate provides show/hide, movement/ordering, opaque or blank-transparent composition, clipping, damage-bounded incremental recomposition, Unicode/wide-cell/semantic-metadata coherence, live session refresh, resize/suspend/resume integration, and deterministic one-way `IDisposable` removal. Panel dimensions stay fixed in 1.2; layout/resize belongs to 1.3.

No-panel refresh retains an allocation-free internal presence check.

## Qualified 1.2 checkpoints

| Tranche | Exact head | Workflow | Result |
|---|---|---|---|
| T1207 | `c0b7fcd48c9b97eaa924299367ffd28779dfd1d0` | #593 / `34520535815` | seven jobs green |
| T1208 | `bb00707779cf3dc6c2222455a6f942469d036881` | #596 / `34522859308` | seven jobs green |
| T1209 API/lifetime | `866497c9d5015f3a149580d67eacefaf7e121aaf` | #600 / `34524054785` | seven jobs green |
| T1209 docs/sample/package | `3728bf0e576b32747dd3a628ed5d3eca768ac67f` | #603 / `34525966166` | seven jobs green |
| T1210 RC | `8c5d329fa195685c0349068ce33a100aaf9eb0a3` | #604 / `34526810086` | seven jobs green |
| T1210 stable source | `a065ef389b4e5224bd9defeb3f4de0e688e2e08a` | #605 / `34527425232` | seven jobs green |
| Final sample/test hardening | `93ef832042dac743008a065acb683d3029710f1c` | #607 / `34528856997` | seven jobs green; 560/560 per TFM |

The current branch then updates only the declared `Icod.Terminal` dependency from `1.8.1` to `1.9.0` plus current-status documentation. The accepted API remains unchanged.

## Current sequence

```text
T1201–T1210  implementation, acceptance, API/package/docs and stable closure  complete
Final hardening sample/test regression tranche                                complete
Dependency refresh Icod.Terminal 1.8.1 -> 1.9.0                              awaiting exact-head qualification
```

A temporary restore failure is expected until NuGet indexes `Icod.Terminal 1.9.0`. Do not add fallback package sources, hard-coded compatibility checks, or revert the version merely to make that propagation window green. Once NuGet indexing completes, require the exact current head to pass the normal seven-job matrix.

Merge, main Release qualification, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.
