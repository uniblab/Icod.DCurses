# T508 — Public API, Documentation, and Package Regret Gate

**Project:** `Icod.DCurses`  
**Development line:** `0.5.0`  
**Current development version:** `0.5.0-alpha.4`  
**Stable baseline:** `0.4.0`  
**Status:** Regret review complete; final alpha gate pending

## 1. Purpose

T508 reviews the complete 0.5 pad/viewport contract before release-candidate promotion. No new feature family enters this tranche.

## 2. Public delta accepted

The 0.5 release adds exactly two public sealed types:

```text
CursesPad
CursesPadViewport
```

No public enum is added.

The exact constructors, properties, methods, and rejected surfaces are recorded in `docs/Public-API-Baseline-0.5.md` and machine-guarded by `PublicPadApiContractTests`.

## 3. Pad ownership review

`CursesPad` remains an off-screen logical surface. It does not own:

- a terminal session;
- terminal modes;
- input;
- terminal presentation leases;
- physical-screen knowledge;
- a second refresh engine.

`ContentWindow` is intentionally a normal `CursesWindow`, preserving the already-frozen 0.3/0.4 editing contract instead of creating a parallel pad-only writer/editor API.

## 4. Derived-view review

A separate `CursesSubpad` type was considered and rejected.

`ContentWindow.CreateSubwindow(...)` already provides the desired shared-storage, parent-relative, independent-cursor, lightweight-view semantics. A second hierarchy would add vocabulary without a distinct ownership model.

## 5. Viewport review

The independent `CursesPadViewport` type is retained because repeated panning otherwise requires application-private coordinate state.

Each viewport owns:

- one pad source origin;
- fixed viewport dimensions;
- fixed destination-window-local placement;
- one destination window reference;
- its own last-presented pad change snapshot.

The pad itself does not remember one global viewport position.

## 6. Presentation review

`PresentTo(...)` and `CursesPadViewport.Present()` remain destructive logical presentation operations: ordinary source blanks replace destination cells.

No transparent pad-presentation alias is added in 0.5. Applications needing overlay semantics can use the established window composition API deliberately.

Presentation preserves pad and destination cursor state and retains the stable wide-cell boundary behavior from 0.4.

## 7. Change/damage review

`HasVisiblePadChanges` is accepted as a pad-source change query, not as a destination ownership assertion.

This distinction is essential. Another logical window may overwrite the destination after a viewport presents. Therefore `Present()` always performs the authoritative logical projection even when the property is false.

Independent viewport observation uses internal per-cell logical content/damage revisions. One viewport never globally acknowledges changes on behalf of another.

No public global pad clean/acknowledge operation is accepted.

## 8. Memory-regret correction

The initial T506 implementation allocated a per-cell revision array on every `CursesVirtualScreen`. That would have imposed an 8-byte-per-cell 0.5 overhead on ordinary logical terminal screens which do not need multi-viewport change observation.

T508 corrects this before API freeze:

- per-cell revision tracking is opt-in/lazy;
- ordinary screens do not allocate revision storage;
- `CursesPad` explicitly enables tracking only on its private backing surface;
- the behavior is covered by `CursesVirtualScreenChangeTrackingTests`;
- change tracking remains internal and does not expand public API.

## 9. Resize/resource review

Pads remain fixed dense in-memory surfaces. They do not resize because the physical terminal changes size.

Viewport geometry is intentionally fixed at construction in 0.5. Each `Present()` revalidates current destination geometry. Invalid post-resize geometry fails explicitly rather than silently changing the viewport.

Very tall and very wide pads are covered by acceptance tests, while sparse/virtualized/disk-backed storage remains out of scope.

## 10. Compatibility and dependency review

The 0.5 release intentionally retains all existing 0.1/0.2/0.3/0.4 public contracts.

The dependency graph remains:

```text
Icod.DCurses
    -> Icod.Terminal 1.0.0
    -> Icod.TermInfo 1.10.0
```

No direct dependency is added and the approved public Terminal/TermInfo type allow-list is unchanged.

## 11. Package-consumer review

The fresh package-only consumer exercises the 0.5 surface from the generated `.nupkg`, including:

- `CursesPad` construction;
- derived subwindow editing;
- Unicode wide content;
- direct `PresentTo(...)`;
- `CursesPadViewport` construction through `CreateViewport(...)`;
- `HasVisiblePadChanges`;
- explicit pad touch observation;
- panning;
- repeated presentation.

## 12. Release-candidate gate

T508 may close and `0.5.0-rc.1` may be prepared when the complete alpha.4 contract plus the opt-in tracking correction passes:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package validation;
- fresh package-only consumers for all target frameworks;
- public dependency-boundary tests;
- frozen 0.5 public API contract tests;
- change-tracking opt-in tests;
- large-pad/resize/panning acceptance tests.

No additional pad feature family should enter after release-candidate promotion.
