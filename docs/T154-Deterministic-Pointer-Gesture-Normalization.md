# T154 — Deterministic Pointer-Gesture Normalization

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T154  
**Published compatibility floor:** `1.4.0`  
**Starting accepted tranche:** T153  
**Starting accepted head:** `ad04432d36fa48e29357fd78dc713d3bba7ac746`  
**Starting accepted workflow:** #846 / `34883079248`  
**Status:** complete

---

## Purpose

T154 adds deterministic clock-free pointer-gesture normalization above the normalized mouse stream already owned by DCurses. The interaction router now reports semantic press, release, move, click, drag, and wheel snapshots without introducing timers, callbacks, hidden input reads, drag/drop policy, or application execution.

The mechanism remains synchronous, bounded, deterministic, and terminal-I/O-free.

## Public contract

T154 adds the frozen public gesture vocabulary:

```csharp
public enum CursesPointerGestureKind {
    Press = 0,
    Release = 1,
    Move = 2,
    Click = 3,
    DragStart = 4,
    DragMove = 5,
    DragEnd = 6,
    WheelUp = 7,
    WheelDown = 8,
    WheelLeft = 9,
    WheelRight = 10
}
```

and the immutable gesture snapshot:

```csharp
public sealed class CursesPointerGesture {
    public CursesPointerGestureKind Kind { get; }
    public CursesMouseButton Button { get; }
    public CursesKeyModifiers Modifiers { get; }
    public CursesPointerTarget? Target { get; }
}
```

`CursesInteractionResult` gains:

```csharp
public CursesPointerGesture? PointerGesture { get; }
```

Existing result kinds, `Hit`, `PointerTarget`, command identity, and region semantics remain intact.

## State model and bounds

The router owns a fixed seven-slot pointer-press state array: exactly one slot for each concrete DCurses mouse button (`Primary`, `Middle`, `Secondary`, and `Button4` through `Button7`). No dynamically growing gesture-state registry is introduced.

Each active slot records only:

- the owning interaction region;
- the latest routed screen row and column;
- whether a drag has started.

`CursesMouseButton.None` owns no press state.

## Gesture semantics

### Direct reports

The normalized stream maps directly to:

- `Press` for a button press;
- `Release` for a release which completes neither a click nor an active drag;
- `Move` for ordinary movement;
- `WheelUp`, `WheelDown`, `WheelLeft`, and `WheelRight` for wheel reports.

Wheel reports do not mutate pending press, drag, or explicit capture ownership.

### Click

A click is emitted when:

1. a concrete-button press was targeted to a region;
2. no routed cell-coordinate movement occurred for that press ownership; and
3. the matching release is targeted to the same region.

A release over a different region remains `Release`.

### Drag

For a targeted concrete-button press:

1. same-cell movement remains `Move` and does not start a drag;
2. the first changed-cell movement to the same routed target becomes `DragStart`;
3. later matching movement becomes `DragMove`;
4. matching release to the same target becomes `DragEnd` and never also reports `Click`.

No clock or movement-time threshold participates.

### Capture-aware targeting

When T152 explicit pointer capture owns the matching button, movement and release use the captured `CursesPointerTarget`, including signed out-of-bounds local coordinates. Gesture normalization therefore preserves region ownership across pointer motion outside the captured region while leaving ordinary `CursesInteractionHit` semantics unchanged.

## Cancellation and coherence

Pending click/drag ownership is canceled when its owning relationship becomes invalid, including:

- region disablement, emptying, or disposal;
- active-scope exclusion;
- explicit pointer-capture lease release or capture invalidation;
- routing movement to a different/no target for that button.

Scope activation/deactivation repairs only states made ineligible by the resulting active-scope boundary.

A later release after cancellation is an ordinary `Release`; stale state cannot manufacture a click or drag end after ownership has been invalidated.

## RED evidence — missing public surface

RED head:

```text
3286b4232fa7246d6febc688f06436cf37d8fb68
workflow #847 / 34891901015
```

The production library built. Test compilation then failed for the intended missing T154 gesture vocabulary, beginning with absent `CursesPointerGestureKind`. No unrelated production/compiler diagnostic was exposed.

The RED suite freezes:

- exact enum numerics;
- one-to-one press/release/move/wheel mapping;
- same-target click behavior;
- different-target release behavior;
- same-cell movement behavior;
- drag start/move/end transitions;
- captured signed out-of-bounds targeting;
- region/scope/capture cancellation;
- wheel non-interference.

## Behavioral RED after contract shell

Contract-shell head:

```text
24c7955e15de0cb75747e0f2cdef67d6648a8afa
workflow #850 / 34892084874
```

With the public gesture types/result slot present but no classifier implemented, the suite compiled and reached behavior:

```text
764 existing tests passed
10 gesture behavior tests failed because PointerGesture remained null
public-API fingerprint guard failed as expected
```

This established a behavior-specific RED state before implementing the gesture state machine.

## Behavioral GREEN before fingerprint promotion

Implementation head:

```text
815a1d10cc64214b4867bd074b06cb71f3ea7a5d
workflow #854 / 34892642334
```

Representative Linux ARM64 evidence on net8.0, net9.0, and net10.0 showed:

```text
774 non-fingerprint tests passed
1 expected failure: stale public-API fingerprint
0 unrelated behavior failures
```

Package validation also succeeded. The compiler-derived public surface was stable at:

```text
69 exported types
523 canonical declared contract lines
sha256 2894e3c210f001dfed525c2d50c00d06aebfcc13c3fed32a44c264157173fd05
```

## API promotion

The 1.5 development fingerprint was promoted to:

```text
release 1.5.0-alpha.4
69 exported types
523 canonical declared contract lines
sha256 2894e3c210f001dfed525c2d50c00d06aebfcc13c3fed32a44c264157173fd05
```

The two newly exported types are:

- `Icod.DCurses.CursesPointerGesture`;
- `Icod.DCurses.CursesPointerGestureKind`.

## Final implementation/API qualification

Fingerprint-promoted head:

```text
acae6104e7f0e4e431d7f5f836978be807008a40
workflow #855 / 34892884559
```

Attempt 1 passed package validation and every runtime lane except Windows ARM64. On Windows ARM64/net9.0, the pre-existing representative mouse-routing allocation gate measured:

```text
ceiling  1,920,000 bytes / 10,000 iterations
actual   1,920,528 bytes / 10,000 iterations
```

That is an overage of 528 bytes total, or 0.0528 bytes per routed operation. The same job's net8.0 and net10.0 executions passed all 775 tests, and every other OS/architecture lane passed.

The failed Windows ARM64 job was rerun unchanged on the same source and runner family. The rerun passed the unchanged allocation ceiling and all tests. No production change and no budget relaxation were made.

Workflow #855 therefore completed successfully at run attempt 2 with all seven required Staging jobs green:

1. Package candidate;
2. Windows x64;
3. Windows ARM64;
4. Linux x64;
5. Linux ARM64;
6. macOS x64;
7. macOS ARM64.

The isolated first-attempt measurement is classified as non-reproducible runtime/JIT allocation-accounting noise, not a T154 product regression.

## Deliberate non-goals

T154 does **not** add:

- double-click or triple-click timing;
- clock ownership or time thresholds;
- hover dwell;
- inertia;
- drag/drop payloads or acceptance semantics;
- callbacks or automatic application execution;
- implicit pointer capture;
- implicit focus changes;
- terminal mouse-protocol ownership.

## Result

T154 completes the clock-free pointer-gesture mechanism required by the approved 1.5 design. The next tranche is T155: bounded scope-level command bindings and deterministic precedence from focused region through the eligible scope chain to router-global bindings.
