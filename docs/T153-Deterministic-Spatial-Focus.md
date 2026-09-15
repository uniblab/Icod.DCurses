# T153 — Deterministic Spatial Focus Navigation

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T153  
**Published compatibility floor:** `1.4.0`  
**Starting accepted tranche:** T152  
**Starting accepted head:** `b1e9e60df7ee6cfc3e2af10c4f3f819273c299fb`  
**Starting accepted workflow:** #839 / `34881203594`  
**Status:** implementation complete and green; documentation-complete exact-head qualification required before T154 execution

---

## Purpose

T153 extends the existing logical focus API with deterministic cardinal navigation while preserving the published 1.4 Forward/Backward behavior exactly.

The new directions are the values already frozen in T150:

```text
Forward  = 0
Backward = 1
Up       = 2
Down     = 3
Left     = 4
Right    = 5
```

No new public type or member is introduced in T153. The accepted T152 public API fingerprint therefore remains authoritative:

```text
67 exported types
515 canonical declared contract lines
sha256 30684c9670b9fb3df418648f6a7bb90b749065ae618b6b3198b86a93b5ececc5
```

## Spatial eligibility

Spatial movement requires a current logical focus region. Up/Down/Left/Right never synthesize initial focus.

Candidates must be ordinary focus-eligible regions under the current active-scope boundary. Effective geometry is computed in current screen coordinates after clipping against:

- the screen bounds;
- the associated panel bounds when present;
- current panel visibility/disposal state.

The declared rectangle is never used as if clipped cells remained spatially visible.

A candidate must lie wholly in the requested primary-axis half-plane:

```text
Up     candidate.BottomExclusive <= focused.Row
Down   candidate.Row             >= focused.BottomExclusive
Left   candidate.RightExclusive  <= focused.Column
Right  candidate.Column          >= focused.RightExclusive
```

This keeps primary-axis edge distance non-negative and avoids ambiguous overlapping-axis navigation policy.

## Deterministic ranking

Eligible candidates are compared lexicographically using integer-only arithmetic:

1. candidates whose effective rectangles overlap the focused rectangle on the perpendicular axis are preferred;
2. smaller primary-axis edge distance wins;
3. smaller perpendicular center distance wins, using doubled centers to avoid floating point;
4. smaller `TraversalOrder` wins;
5. smaller registration ordinal wins.

All intermediate geometry/ranking arithmetic which can exceed ordinary cell-coordinate addition uses `long`.

No terminal I/O, callback, clock, culture-sensitive comparison, or floating-point arithmetic participates.

## No wrapping

Forward and Backward keep their published wrapping semantics.

Spatial navigation does not wrap. When no candidate exists in the requested direction:

- `MoveFocus(...)` returns `null`;
- the existing focused region remains focused.

This is distinct from the no-focus case, where a spatial move also returns `null` but focus remains absent.

## RED evidence

RED head:

```text
c9a7735820d6aef9628ae84539b9bcb74cc0fd23
workflow #842 / 34881969744
```

The new T153 suite compiled successfully and all nine spatial tests failed for exactly the intended reason: the pre-T153 `MoveFocus` implementation rejected Up/Down/Left/Right with `ArgumentOutOfRangeException`.

On each exercised Linux TFM, the existing **755 tests passed** while the nine new tests failed.

The RED suite freezes:

- obvious Up/Down/Left/Right movement;
- no initial spatial-focus synthesis;
- no spatial wrapping and preservation of current focus;
- perpendicular-overlap priority;
- primary edge-distance ordering;
- perpendicular doubled-center ordering;
- TraversalOrder and registration-ordinal tie breaks;
- effective panel clipping;
- active-scope isolation.

## GREEN implementation evidence

Implementation head:

```text
6f440a9feda628f1948d11008402a0064bbd645b
workflow #844 / 34882549455
```

The implementation adds a private `CursesInteractionRouter.SpatialFocus` partial and minimally dispatches the four cardinal values from the existing `MoveFocus` method. Sequential focus code remains unchanged.

Workflow #844 passed:

- package candidate;
- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64.

The existing API fingerprint guard remained green, proving T153 is behavior-only at the compiled public-contract level.

## Compatibility and boundaries

T153 deliberately does not add:

- diagonal focus directions;
- spatial wrapping;
- initial focus policy;
- widget hierarchy or parent/child navigation;
- geometric weights configurable by callers;
- floating-point scoring;
- automatic layout ownership;
- automatic focus-on-pointer interaction.

Forward/Backward traversal remains the compatibility-preserving sequential path. Spatial movement uses the same active-scope and focus-eligibility rules established by T151.

## Final gate

This evidence document and the synchronized main roadmap form the T153 documentation-complete checkpoint. That exact head must pass the normal seven-job Staging matrix before T154 pointer-gesture implementation begins. The accepted exact head/workflow may be recorded in PR metadata without moving the qualified Git head.
