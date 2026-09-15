# T156 — Coherence and Adversarial Hardening

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T156  
**Theme:** combined scope/capture/focus/gesture/command lifecycle coherence  
**Starting accepted checkpoint:** `92958f5cc7583dbab64c19c40a12fe607115524a`  
**Starting qualification:** workflow #866 / `34900041904` — seven-job Staging matrix green  
**Final implementation/hardening checkpoint:** `e09324666bb6960d465f061d609a4fb53d8c5288`  
**Final qualification:** workflow #876 / `34905503151` — seven-job Staging matrix green

---

## Purpose

T156 hardens the complete 1.5 interaction state machine under geometry, scope, panel, resize, capture, gesture, command, and disposal churn. It is intentionally behavior-only: no new public API is introduced and the accepted T155 candidate fingerprint remains unchanged.

Current public-development fingerprint:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

## Defect 1 — capture resurrection across a modal-scope round trip

RED head:

```text
471285d55c8243c0c2c7c4e45d8c1bc94e6e19ab
workflow #867 / 34900606359
```

The new `CaptureCannotResurrectAcrossExcludedScopeRoundTrip` test captured a root-region pointer, activated an excluding modal scope, disposed that scope before any mouse route, then routed a move. The old implementation returned `Targeted` instead of `Unrouted`: capture had survived the temporary ineligibility and became valid again when the outer scope returned.

The remaining 783 tests passed on the inspected runtime lane, isolating the failure to capture lifetime.

Root cause: `ActivateScope` repaired focus and pointer-gesture state after establishing the new modal boundary but did not repair pointer capture.

Correction: after adding the active-scope lease, `ActivateScope` now calls `RepairPointerCaptureIfNeeded()` before continuing. Exclusion therefore terminates capture permanently at the lifecycle transition rather than depending on a later route to discover the invalidity.

GREEN checkpoint:

```text
b7e2095b17ef98fcbf2bc5aa28603f1cdbc2812f
workflow #868 / 34900788354
```

All seven Staging jobs passed.

## Defect 2 — panel hide/show could resurrect capture and click ownership

RED head:

```text
7e62c1f1ad5d0b978620d2a5301494c759f22317
workflow #869 / 34901304164
```

Two new tests exposed the same lifetime class through panel visibility:

- `CaptureCannotResurrectAcrossPanelVisibilityRoundTrip` observed `Targeted` instead of `Unrouted` after hide/show;
- `ClickOwnershipCannotResurrectAcrossPanelVisibilityRoundTrip` observed `Click` instead of `Release` after a press, hide/show, then release.

The remaining 784 tests passed on the inspected lane.

The first implementation added an internal `CursesPanel.InteractionEligibilityChanged` signal and subscribed panel-associated interaction regions. The initial region handler forwarded that signal through the general region-eligibility path. That was too broad: workflow #871 demonstrated two established 1.4 focus regressions because mouse/hit activity could now cause eager logical-focus repair where 1.4 deliberately kept focus repair lazy.

The final correction narrowed the internal panel signal to pointer ownership only:

```text
panel visibility/geometry/disposal transition
    -> associated region notification
    -> HandlePointerCaptureRegionChanged
    -> capture repair + pointer-gesture repair
```

Logical focus is not eagerly repaired by this notification. Panel-associated regions unsubscribe during region disposal, and the signal remains internal-only.

Final GREEN checkpoint:

```text
4297fd381da304c10337e9123ba051eb3901297e
workflow #872 / 34904255438
```

All seven Staging jobs passed, including the two preserved 1.4 lazy-focus tests and the two new panel round-trip tests.

## Defect 3 — screen shrink/grow could resurrect pointer ownership

RED head:

```text
7ca8813a7d9fd5153c3d83db0bcb91b5f5fa65f8
workflow #873 / 34904543625
```

The new resize tests placed a region near the lower-right edge, acquired capture or press ownership, shrank the screen until the region became fully offscreen, then restored the original screen size before another input route.

The old implementation again resurrected state:

- capture returned `Targeted` instead of `Unrouted`;
- press ownership produced `Click` instead of `Release`.

The remaining 786 tests passed on the inspected lane.

Root cause: `CursesScreen` already exposes the `Resized` lifecycle event, but `CursesInteractionRouter` did not observe it. The router therefore never saw the temporary offscreen interval if no route occurred while the screen was small.

Correction: the router now subscribes internally to `Screen.Resized`, repairs pointer capture and pointer-gesture ownership synchronously on each resize, and unsubscribes during router disposal. The resize handler intentionally does not repair logical focus, preserving the established lazy-focus contract.

GREEN checkpoint:

```text
8b6cd02e95ddcc41174b8c7faf1004291621336e
workflow #874 / 34904759849
```

All seven Staging jobs passed.

## Combined lifecycle witnesses

The hardening suite then added direct positive witnesses for the corrected ownership graph:

- region bounds offscreen/onscreen round trips cannot resurrect capture or press ownership;
- panel resize clipping/restoration cannot resurrect pointer ownership;
- panel disposal cancels capture and gesture ownership;
- nested modal-scope activation cancels pointer ownership held by an excluded outer-scope region;
- failed out-of-order scope-lease disposal is failure-atomic for active scope, capture, and command resolution;
- router disposal unsubscribes from screen resize and leaves capture/scope lease disposal idempotent.

The combined lifecycle checkpoint was `82d778990473a80deaee620a21bae0989036f848`. Its coverage is included in the later final T156 qualification below.

## Capture churn and deterministic replay

Final hardening adds two stress witnesses:

1. `CaptureChurnRecoversSingularOwnershipAfterExplicitAndAutomaticRelease`
   - 1,024 capture cycles;
   - alternates primary/secondary button ownership;
   - alternates explicit lease disposal with matching-release automatic disposal;
   - repeatedly disposes stale leases;
   - finishes by acquiring a new capture, proving the singular slot remains recoverable.

2. `CombinedInteractionLifecycleReplayIsDeterministic`
   - performs the same mixed state transition sequence twice;
   - records 256 observations per execution;
   - combines modal scope exclusion, nested scope commands, panel hide/show, capture, press/release normalization, screen shrink/grow invalidation, spatial focus, and global commands;
   - requires identical observations between independent executions.

Final head:

```text
e09324666bb6960d465f061d609a4fb53d8c5288
workflow #876 / 34905503151
```

The complete seven-job Staging matrix passed:

```text
Package candidate       success
Runtime Windows x64     success
Runtime Windows ARM64   success
Runtime Linux x64       success
Runtime Linux ARM64     success
Runtime macOS x64       success
Runtime macOS ARM64     success
```

## Capacity and failure-atomicity inheritance

T156 does not duplicate already-qualified bounded-state tests merely to increase test count. The 1.4 adversarial suite remains active and continues to cover:

- `MaximumRegions` capacity churn and slot recovery;
- router-wide total-binding exhaustion/recovery;
- deterministic panel-topology replay;
- deterministic focus-eligibility replay;
- invalid binding/focus failure atomicity.

The 1.5 tranches additionally retain:

- T151 maximum scope count/depth and scope lifecycle bounds;
- T152 singular capture ownership;
- T153 deterministic spatial-focus ranking/ties;
- T154 fixed-size per-button gesture ownership;
- T155 scope-local and router-wide binding-capacity participation.

Registration ordinals remain monotonic and are used as deterministic final tie-breakers. T156 did not add reflection or test-only production hooks merely to force the private `long` ordinal counter to numerical exhaustion; no public exhaustion mechanism exists, and the already-qualified registration-capacity/failure-atomicity behavior remains the practical boundary witness.

## Concurrency audit

T156 does not expand DCurses concurrency semantics.

The new lifecycle propagation is synchronous and remains inside the existing single-writer model:

- panel eligibility notifications execute synchronously on the mutating thread;
- `CursesScreen.Resized` handling executes synchronously on the resize thread;
- capture/gesture repair mutates only router-owned interaction state;
- no locks, background workers, tasks, callbacks into application code, or terminal I/O are introduced;
- no thread-safe or multi-writer public guarantee is added.

## T156 conclusion

T156 closes three real stale-ownership resurrection defects while preserving the 1.4 lazy logical-focus behavior and the 1.5 public API.

The accepted invariant is now stronger: once a capture or per-button gesture ownership becomes invalid because of scope exclusion, panel lifecycle/geometry, screen resize, region mutation/disposal, or router disposal, that ownership cannot become valid again merely because geometry or scope eligibility later returns.

T157 may now focus on public application acceptance and packed-package consumption rather than further lifecycle redesign.
