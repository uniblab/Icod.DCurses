# Icod.DCurses 1.2.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.2.0`  
**Stable compatibility floor:** `1.1.0`  
**1.1 merged baseline commit:** `99aa3a6f95d950e37f729386549dc42817f63bd1`  
**Development package:** `1.2.0-alpha.1`  
**Assembly version:** `1.0.0.0`  
**Current declared dependencies:** `Icod.Terminal 1.8.1`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Theme:** panels, independent retained layers, visibility, clipping, and deterministic z-order composition  
**Status:** T1201–T1208 qualified; T1209 API/lifetime qualified and documentation/sample/package closure active

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

Existing applications using overlapping windows therefore do not silently acquire retained-layer behavior after upgrading.

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

Version 1.2 introduces no raw OSC/CSI/DCS/APC output, no second terminal input reader, no private capability database, and no dependency on Terminal implementation details.

Dependency versions are project declarations rather than package-verifier policy. Restore/build/test establish compatibility; package verification establishes package integrity.

## 3. Compatibility constraints

Accepted 1.1 floor:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

Current 1.2 candidate after the lifetime regret gate:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Required invariants remain:

1. `AssemblyVersion` remains `1.0.0.0`.
2. Existing `CursesWindow` shared-cell semantics remain unchanged.
3. Existing screen/window/pad/viewport/metadata/Unicode/damage/input/lifecycle behavior remains compatible.
4. Public panel APIs expose no Terminal protocol/routing types.
5. Ordinary no-panel use pays no panel-related per-cell storage cost.
6. Semantic metadata composes with the cell state it annotates.
7. Wide leader/continuation footprints remain valid after clipping/occlusion.
8. Hidden or occluded changes do not cause unnecessary composed-frame churn.
9. Terminal remains the sole physical terminal owner.

## 4. Frozen core design

### 4.1 Ordinary windows remain views

`CursesWindow` continues to project directly into one owning logical screen.

### 4.2 Panels own independent retained surfaces

Panel content is independent of the screen beneath it and is edited through the existing `CursesWindow` API, reusing Unicode, style, drawing, editing, metadata, and scrolling behavior.

### 4.3 Deterministic screen-owned stack

Panels belong to exactly one `CursesScreen`. Identity, not value equality, determines membership and ordering. Hidden panels retain stack position.

### 4.4 Composition does not mutate retained producers

Moving, hiding, reordering, clipping, occluding, or composing a panel never rewrites its retained content.

### 4.5 Opaque by default; explicit blank transparency

`CursesPanelTransparency.Opaque` is the default. `BlankCellsTransparent` makes ordinary blank panel cells contribute neither cell nor metadata, revealing the lower retained state.

### 4.6 Physical refresh remains existing DCurses/Terminal machinery

The panel compositor produces a retained logical frame. The existing refresh engine remains the physical renderer and Terminal remains the serialized protocol/session owner.

### 4.7 Lifetime is deterministic

`CursesPanel` implements `IDisposable`. Disposal permanently removes the panel from the owning screen's composition order and rejects later manipulation. This prevents long-lived screens from retaining unbounded one-shot transient panels. Reattachment/transfer is deliberately outside 1.2.

## 5. T1201 — foundation and deterministic order engine — COMPLETE

Introduced the internal identity-based order engine and froze the 1.2 branch/version policy.

Qualified exact head:

```text
b4582beebe9519d5b5feaf5a4f4ada54adc21672
workflow #564 / 34510470939
```

All seven jobs passed.

## 6. T1202 — independent retained panel surface — COMPLETE

Added `CursesPanel` creation and independent retained content without composition side effects. Panel backing uses the destination text-width policy and existing window editing semantics.

Qualified exact head:

```text
cfb4973992c78273e0edcdff17065e8b8a029af0
workflow #571 / 34511809356
```

All seven jobs passed.

## 7. T1203 — visibility, movement, and z-order operations — COMPLETE

Added `Show`, `Hide`, `MoveTo`, `MoveToTop`, `MoveToBottom`, `MoveAbove`, and `MoveBelow`. Hide/show preserves stack membership; cross-screen relative ordering is rejected.

Qualified exact head:

```text
85d907f7cf00fa4e8839ed6173255218a9efa17f
workflow #575 / 34513600585
```

All seven jobs passed.

## 8. T1204 — composition, clipping, and transparency — COMPLETE

Added terminal-independent logical composition from the base screen plus visible panels bottom-to-top, with screen-edge clipping, opaque blank occlusion, explicit blank transparency, cell/metadata pairing, and final footprint repair.

Qualified exact head:

```text
09293c42dfa2c46484542601b7c0538babe07684
workflow #584 / 34515384714
```

All seven jobs passed.

## 9. T1205 — damage and incremental recomposition — COMPLETE

Retains one lazy composed frame per screen and observes producer revisions without clearing producer dirtiness. Routine changes re-resolve affected cells/rectangles rather than rebuilding the whole frame.

Explicit `TouchCell` invalidation propagates when the touched producer actually wins composition at the coordinate; occluded touches remain suppressed.

Qualified exact head:

```text
f18d3a55877115f224f9aca8ccc7cde6cfafd845
workflow #588 / 34517869687
```

All seven jobs passed. Linux ARM64 reported 529/529 tests per TFM.

## 10. T1206 — Unicode, wide cells, line glyphs, and metadata — COMPLETE

Qualified width-two leader/continuation composition, partial occlusion and reveal, screen-edge clipping, semantic line glyphs, styled blanks, hyperlink metadata, semantic-only metadata replacement, transparent blanks with source metadata, and `CursesCellFootprint` invariants.

Qualified exact head:

```text
8a7ce13d25d3f911b9c17dbe6ce24af0012c2431
workflow #590 / 34518913849
```

All seven jobs passed. The only pre-qualification failure was a test-harness namespace import; no production compositor change was required.

## 11. T1207 — resize, lifecycle, and live-session integration — COMPLETE

Live `CursesSession.RefreshAsync()` now renders panel composition. Destination resize clips without resizing/discarding panel retained content; restoring size reveals it again. Suspend/resume invalidates physical knowledge and repaints the retained composition.

No-panel sessions remain on the original direct refresh path. Sessions with panels use an internal retained projection bridge so the existing refresh engine remains the sole physical renderer.

Qualified exact head:

```text
c0b7fcd48c9b97eaa924299367ffd28779dfd1d0
workflow #593 / 34520535815
```

All seven jobs passed. Linux ARM64 reported 543/543 tests per TFM with zero warnings/errors.

## 12. T1208 — application, performance, and allocation acceptance — COMPLETE

Application-shaped acceptance covers:

- modal help overlay and underlying restoration;
- command-palette movement without content reconstruction;
- completion-popup blank transparency;
- context-menu z-order changes;
- hidden transient-status updates;
- sparse visible damage;
- no-panel presence checks with zero measured thread allocation;
- 1,024 settled sparse-panel compositions reusing the retained frame within a bounded allocation ceiling.

The no-panel live refresh path uses an allocation-free `HasPanels` check rather than allocating an empty order snapshot.

Qualified exact head:

```text
bb00707779cf3dc6c2222455a6f942469d036881
workflow #596 / 34522859308
```

All seven jobs passed. Linux ARM64 reported 551/551 tests per TFM with zero warnings/errors.

Permanent record: `docs/T1208-Panel-Application-Performance-and-Allocation-Acceptance.md`.

## 13. T1209 — public API, package, documentation, and regret gate — CLOSURE ACTIVE

The pre-RC audit covered overload ambiguity, nullability, dependency boundaries, panel geometry, panel enumeration, lifetime/ownership, package consumption, sample coverage, and current documentation.

### Lifetime regret correction

The audit found one material problem: a hidden transient panel remained strongly retained by `CursesScreen.panelOrder` indefinitely. Repeated one-shot dialogs/popups could therefore accumulate for the lifetime of a long-lived screen.

The accepted fix is `CursesPanel : IDisposable` with one-way removal from its owning screen. Disposal is idempotent; later panel manipulation throws `ObjectDisposedException`; another active panel cannot order itself relative to a disposed panel.

Test-only red head:

```text
b800fa1de8a1986f2680bab1104dbeb45058a3fb
workflow #597 / 34523435656
```

failed only because the lifetime API did not yet exist.

The implemented lifetime head `ed21d00950ff3992628bf825625b62e24d957c49` passed 553 behavioral/compatibility tests per TFM; the sole failure was the intentionally stale provisional fingerprint. Compiler-derived replacement:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Fingerprint-complete API head:

```text
866497c9d5015f3a149580d67eacefaf7e121aaf
workflow #600 / 34524054785
```

passed all seven jobs.

No other public API regret was accepted:

- panel size remains fixed because general layout/resize belongs to 1.3;
- panel enumeration remains internal because no public consumer currently requires it;
- the fully-contained `MoveTo` rule remains consistent with existing window geometry; clipping is a destination-resize concern;
- `IDisposable` adds only a BCL interface and no new Terminal/TermInfo public dependency;
- the 1.2 surface remains exactly two exported types above 1.1.

The remaining T1209 work is package-smoke, focused sample, README/current-roadmap/package metadata synchronization, and exact-head qualification of that documentation-complete tree.

Permanent record: `docs/T1209-Public-API-Package-Documentation-and-Regret-Gate.md`.

## 14. T1210 — RC and stable 1.2.0 closure — NEXT

After the documentation-complete T1209 head is green, promote through one release candidate. Stable closure requires clean restore/build/test/package validation, the six runtime legs, fresh NuGet-only consumer validation on all TFMs, exact public API baseline, synchronized docs/samples/package metadata, and no known composition/lifecycle/Unicode/metadata/damage blocker.

Merge, tag, GitHub Release creation, and NuGet publication remain explicit separate actions.

## 15. Explicit non-goals

Version 1.2 does not add buttons, text boxes, menus, dialogs, tables, general layout arithmetic, panel resizing, focus traversal, gesture routing, raster/image placement, animation, alpha blending, terminal pixel layout, terminal emulation, PTY/process hosting, or direct Terminal protocol output.

## 16. Current sequence

```text
T1201  foundation + deterministic internal order engine             complete
  -> T1202  independent retained panel surface + public creation    complete
  -> T1203  visibility/movement/z-order API                         complete
  -> T1204  logical composition + clipping + transparency           complete
  -> T1205  damage + incremental recomposition                      complete
  -> T1206  Unicode/wide/metadata hardening                         complete
  -> T1207  resize/lifecycle/session integration                    complete
  -> T1208  application/performance/allocation acceptance           complete
  -> T1209  public API/package/docs/regret gate                     closure active
  -> T1210  RC/stable closure                                      next
```
