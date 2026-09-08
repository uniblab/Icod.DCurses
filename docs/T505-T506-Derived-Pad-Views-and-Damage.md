# T505-T506 — Derived Pad Views and Independent Viewport Damage

**Project:** `Icod.DCurses`  
**Development line:** `0.5.0`  
**Checkpoint:** `0.5.0-alpha.3`  
**Stable baseline:** `0.4.0`  
**Status:** Implementation complete; validation pending

## 1. Purpose

T505 and T506 freeze how one large pad is subdivided for editing and how multiple viewport consumers observe pad-local changes independently.

The design deliberately reuses the already-frozen window and logical-screen machinery instead of creating parallel pad-only editing or damage systems.

## 2. T505 — derived views

No `CursesSubpad` public type is introduced.

The official derived-pad-view contract is:

```text
CursesPad.ContentWindow.CreateSubwindow(...)
```

This already provides the required semantics:

- views share the same underlying pad cell storage;
- edits through one view are immediately visible through the parent and overlapping sibling views;
- origins are relative to the immediate parent view;
- cursor state belongs to each view independently;
- Unicode and wide-cell semantics are identical to ordinary windows;
- views are lightweight and non-disposable.

Adding a second pad-only view hierarchy would duplicate established `CursesWindow` behavior without adding ownership value.

## 3. T506 — damage ownership

The pad does not expose a global "mark clean" operation.

One viewport acknowledging a presentation must never hide changes from another viewport over the same pad.

The backing logical surface therefore maintains internal per-cell content/damage revisions. Revisions advance for:

- actual cell-value changes;
- wide-footprint repair changes;
- explicit `Touch`, `TouchLine`, and `TouchRegion` damage;
- logical invalidation.

Physical-screen dirty state remains separate. Calling the ordinary refresh-layer clean operation does not erase these pad-observation revisions.

## 4. Per-viewport observation

`CursesPadViewport` now exposes:

```text
bool HasVisiblePadChanges
```

The property is true when:

- the viewport has never been presented;
- its pad source origin differs from its last presented origin;
- at least one currently visible pad cell has a newer content/damage revision than this viewport last presented.

Mutations outside the current pad source rectangle do not make the property true.

Each viewport stores its own presented revision snapshot. Presenting one viewport updates only that viewport's observation state.

## 5. Authoritative presentation remains required

`HasVisiblePadChanges` describes pad-source changes only. It is not a destination ownership contract.

`Present()` always performs the logical projection even when `HasVisiblePadChanges` is false. This is intentional because another window may have overwritten the destination region since the prior presentation.

The destination logical store already avoids dirtying equal replacement cells, so correctness does not require skipping the full viewport walk at the source layer.

## 6. Damage propagation

Pad-local explicit touch/invalidation must remain meaningful even when cell values are unchanged.

At presentation time:

- a changed viewport source origin marks the complete destination rectangle dirty;
- otherwise, changed/touched source cells are propagated to the destination in contiguous row spans;
- the normal rectangle copy remains authoritative for destination contents;
- the destination refresh engine continues to expand dirty spans across wide-cell continuation boundaries.

This means panning invalidates newly exposed destination cells even when their values happen to compare equal to the previous projection.

## 7. Multi-viewport result

The model supports multiple simultaneous viewports over one pad without global acknowledgement:

```text
pad
 ├─ viewport A -> destination A
 └─ viewport B -> destination B
```

A pad edit visible to both makes both report changes. After A presents, A is clean relative to its own source snapshot while B still reports the same pending pad change until B presents.

## 8. Contract boundaries

T505/T506 do not add:

- a separate `CursesSubpad` hierarchy;
- pad-owned terminal refresh state;
- pad-owned physical-screen knowledge;
- a public global pad clean/acknowledge operation;
- destination ownership assumptions;
- terminal scrolling optimization.

The next tranche, T507, hardens this contract under destination resize/reposition, edge clipping, large dimensions, repeated panning, and representative editor/table/log workloads.
