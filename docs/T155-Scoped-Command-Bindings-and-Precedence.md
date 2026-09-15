# T155 — Scoped Command Bindings and Precedence

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T155  
**Published compatibility floor:** `1.4.0`  
**Starting accepted tranche:** T154  
**Starting accepted head:** `39cd424d78ef2f72f43a757081646f7625b1ab54`  
**Starting accepted workflow:** #859 / `34897754255`  
**Status:** complete

---

## Purpose

T155 inserts bounded scope-level semantic command bindings between the existing region-local and router-global binding layers. The interaction router remains synchronous, callback-free, deterministic, and terminal-I/O-free.

The frozen lookup order is:

```text
focused region local binding
    -> focused region scope
        -> parent scopes through the active modal boundary
            -> router-global binding
                -> otherwise Targeted / Unrouted as before
```

Commands remain `CursesCommand` identities only. T155 adds no handlers, delegates, execution callbacks, enabled predicates, dependency injection, or command catalog.

## Public contract

`CursesInteractionScope` gains the same public binding shape already used by regions:

```csharp
public void BindGesture( CursesKeyGesture gesture, CursesCommand command );
public bool UnbindGesture( CursesKeyGesture gesture );
```

No new exported type is introduced.

The existing frozen bounds remain authoritative:

```text
MaximumScopeGestureBindings = 256 per explicit scope
MaximumGestureBindings      = 16384 total region + scope + global bindings
```

Scope bindings therefore do not create a parallel unbounded registry.

## Capacity and mutation rules

Each scope owns a bounded dictionary of semantic gesture-to-command identities.

Binding validates, in order:

- a factory-created bindable `CursesKeyGesture`;
- a non-null `CursesCommand`;
- live scope lifetime;
- duplicate gesture absence;
- per-scope capacity;
- router-wide total-binding capacity.

Failure is mutation-atomic. Unbinding immediately returns capacity to both the per-scope and router-wide budgets.

## Modal-boundary semantics

When an explicit scope is active, command resolution never consults bindings above that active scope boundary. Router-global bindings remain globally eligible by explicit contract.

For focused regions in nested scopes, lookup starts at the region's own scope and walks parents only until the active boundary has been consulted.

When there is no focused region and an explicit scope is active, only the active scope itself is eligible before router-global lookup; inactive outer scope bindings do not leak through the modal boundary.

When no explicit scope is active, ordinary 1.4 root behavior remains unchanged: region-local bindings precede router-global bindings, and root/unscoped consumers observe no new command layer.

## RED evidence

Tests-only RED head:

```text
ecff80c025d9bf3bd0b2bac92203256d5b7ec23e
workflow #860 / 34898143633
```

The production library built successfully. Test compilation then failed across net8.0, net9.0, and net10.0 exclusively because `CursesInteractionScope.BindGesture` and `CursesInteractionScope.UnbindGesture` did not yet exist (CS1061). No unrelated production/compiler diagnostic was exposed.

The RED suite froze:

- invalid/default gesture rejection;
- null command rejection;
- duplicate failure atomicity;
- unbind/rebind behavior;
- disposed-scope rejection;
- exact per-scope capacity and returned capacity;
- shared router-wide total binding capacity;
- region-local precedence over scope/global;
- child-scope precedence over parent/global;
- parent lookup when child has no match;
- active modal-boundary isolation;
- router-global fallback;
- no-focus active-scope lookup;
- unchanged root-only region/global behavior.

## Behavioral GREEN before API promotion

Implementation head:

```text
80fe9847c20adde477721d15079a26a8b8e47b8e
workflow #863 / 34898359836
```

Package validation succeeded and every runtime lane built successfully. On representative Linux x64, all three TFMs reported:

```text
782 non-fingerprint tests passed
1 expected failure: stale public-API fingerprint
0 scoped-command behavior failures
```

The same sole fingerprint failure occurred on all runtime lanes. The compiler-derived candidate surface was identical across TFMs/OSes:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

The two additional contract lines are the two public binding members added to the already-exported `CursesInteractionScope` type.

## API promotion and final implementation qualification

The development fingerprint was promoted to:

```text
release 1.5.0-alpha.5
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

Fingerprint-promoted head:

```text
3e59e7613e214a5eaa84df13bbedba56abfefb6d
workflow #864 / 34899684038
```

Workflow #864 passed all seven required Staging jobs:

1. Package candidate;
2. Windows x64;
3. Windows ARM64;
4. Linux x64;
5. Linux ARM64;
6. macOS x64;
7. macOS ARM64.

No rerun, production correction, or API adjustment was required after fingerprint promotion.

## Result

T155 completes the fifth and final primary Family-1 capability from the approved 1.5 design. The next tranche is T156: combined coherence and adversarial hardening across scopes, capture, spatial focus, pointer gestures, commands, resize, panel lifecycle, region mutation/disposal, nested leases, capacity boundaries, deterministic replay, and router disposal.
