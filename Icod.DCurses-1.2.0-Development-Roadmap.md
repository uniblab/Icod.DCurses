# Icod.DCurses 1.2.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.2.0`  
**Stable compatibility floor:** `1.1.0`  
**1.1 merged baseline commit:** `99aa3a6f95d950e37f729386549dc42817f63bd1`  
**Release candidate:** `1.2.0-rc.1`  
**Assembly version:** `1.0.0.0`  
**Current declared dependencies:** `Icod.Terminal 1.8.1`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** panels, independent retained layers, visibility, clipping, and deterministic z-order composition  
**Status:** T1201–T1209 complete and qualified; T1210 RC exact-head qualification active

---

## 1. Release objective

`Icod.DCurses 1.2.0` adds first-class overlapping retained surfaces without changing the stable meaning of an ordinary `CursesWindow` and without introducing widgets.

```text
CursesWindow
    -> rectangular view into one owning logical screen
    -> overlapping ordinary windows share logical cells

CursesPanel
    -> owns an independent retained cell surface
    -> belongs to one destination screen
    -> participates in deterministic bottom-to-top composition
    -> may be shown, hidden, moved, reordered, or permanently disposed
```

The accepted panel substrate supports modal help, command palettes, completion popups, context menus, temporary status/error overlays, and movable dialogs while retaining underlying content automatically.

## 2. Architectural boundary

```text
application / future widget package
              |
              v
        Icod.DCurses
 panels / layers / logical composition
 cells / metadata / retained refresh
              |
              v
        Icod.Terminal
 live session / input / lifecycle / semantic output
              |
              v
        Icod.TermInfo
 immutable capability authority
```

Version 1.2 introduces no raw OSC/CSI/DCS/APC output, no second terminal input reader, no private capability database, and no dependency on Terminal implementation details. Dependency versions are project declarations rather than package-verifier policy.

## 3. Compatibility constraints

Accepted 1.1 floor:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

Accepted 1.2 candidate:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Existing window, pad, metadata, Unicode, damage, input, lifecycle, and Terminal-ownership contracts remain compatible. `AssemblyVersion` remains `1.0.0.0`.

## 4. Frozen core design

- Ordinary windows remain shared views.
- Panels own independent retained surfaces edited through `CursesWindow`.
- One destination screen owns deterministic identity-based panel order.
- Hide/show preserves stack position.
- Composition never mutates retained producers.
- Panels are opaque by default; `BlankCellsTransparent` intentionally reveals lower content.
- Physical refresh remains existing DCurses/Terminal machinery.
- `CursesPanel : IDisposable` provides deterministic one-way removal from its screen.
- Panel dimensions remain fixed in 1.2; general layout/resize belongs to 1.3.

## 5. Qualified implementation sequence

| Tranche | Result | Exact head | Workflow |
|---|---|---|---|
| T1201 | order engine | `b4582beebe9519d5b5feaf5a4f4ada54adc21672` | #564 / `34510470939` |
| T1202 | retained panel surface | `cfb4973992c78273e0edcdff17065e8b8a029af0` | #571 / `34511809356` |
| T1203 | visibility/movement/z-order | `85d907f7cf00fa4e8839ed6173255218a9efa17f` | #575 / `34513600585` |
| T1204 | composition/clipping/transparency | `09293c42dfa2c46484542601b7c0538babe07684` | #584 / `34515384714` |
| T1205 | damage/incremental composition | `f18d3a55877115f224f9aca8ccc7cde6cfafd845` | #588 / `34517869687` |
| T1206 | Unicode/wide/metadata | `8a7ce13d25d3f911b9c17dbe6ce24af0012c2431` | #590 / `34518913849` |
| T1207 | resize/lifecycle/session | `c0b7fcd48c9b97eaa924299367ffd28779dfd1d0` | #593 / `34520535815` |
| T1208 | application/resource acceptance | `bb00707779cf3dc6c2222455a6f942469d036881` | #596 / `34522859308` |
| T1209 API/lifetime | regret-gate API | `866497c9d5015f3a149580d67eacefaf7e121aaf` | #600 / `34524054785` |
| T1209 closure | docs/sample/package | `3728bf0e576b32747dd3a628ed5d3eca768ac67f` | #603 / `34525966166` |

Every row above passed all seven PR jobs.

## 6. T1208 — application, performance, and allocation acceptance — COMPLETE

Application-shaped acceptance covers modal help, command-palette movement, transparent completion popups, context-menu reordering, hidden transient-status updates, sparse visible damage, zero-allocation no-panel presence checks, and retained-frame reuse across 1,024 settled sparse-panel compositions.

The no-panel live refresh path uses allocation-free `HasPanels` rather than allocating an empty panel snapshot.

The qualified Linux ARM64 leg reported 551/551 tests per TFM with zero warnings/errors.

Permanent record: `docs/T1208-Panel-Application-Performance-and-Allocation-Acceptance.md`.

## 7. T1209 — public API, package, documentation, and regret gate — COMPLETE

The pre-RC audit found one material API regret: hide-only lifetime semantics retained every transient panel in the owning screen. The accepted fix makes `CursesPanel` implement `IDisposable`; disposal permanently removes it, is idempotent, and rejects later manipulation. Reattachment/transfer is deliberately outside 1.2.

No other public API regret was accepted. The final candidate remains exactly two exported types above 1.1.

T1209 also added a public-only focused panel sample, runtime package-only panel validation, updated package release notes, current roadmap/README/sample documentation, T1208/T1209 records, and the 1.2 API baseline.

The documentation/sample/package-complete head `3728bf0e576b32747dd3a628ed5d3eca768ac67f` passed workflow #603 / `34525966166` across all seven jobs. Linux ARM64 reported 554/554 tests per TFM with zero build warnings/errors.

Permanent record: `docs/T1209-Public-API-Package-Documentation-and-Regret-Gate.md`.

## 8. T1210 — RC and stable 1.2.0 closure — ACTIVE

The sole release candidate is `1.2.0-rc.1`. RC promotion changes release/package identity and current status documentation only; implementation, tests, sample behavior, package-smoke behavior, dependencies, AssemblyVersion, and the API fingerprint remain unchanged.

RC qualification requires the full seven-job matrix. Only after the exact RC head is green may the same implementation/API be promoted to stable-source `1.2.0`, which must itself pass the same exact-head matrix.

RC-to-stable comparison must show no behavioral source/test/sample/package-smoke change and the API fingerprint must remain:

```text
4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Permanent record: `docs/T1210-RC-and-Stable-Closure.md`.

## 9. Explicit non-goals

Version 1.2 does not add buttons, text boxes, menus, dialogs, tables, general layout arithmetic, panel resizing, focus traversal, gesture routing, raster/image placement, animation, alpha blending, terminal pixel layout, terminal emulation, PTY/process hosting, or direct Terminal protocol output.

## 10. Current sequence

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

Merge, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.
