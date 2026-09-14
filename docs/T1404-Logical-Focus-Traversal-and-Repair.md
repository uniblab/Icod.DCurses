# T1404 — Logical Focus, Traversal, and Repair

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1404  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** implementation/fingerprint head qualified; documentation-complete head requires its own exact-head matrix

---

## Objective

T1404 adds router-owned application logical focus which remains completely distinct from terminal/window-manager focus reports. It implements explicit focus, deterministic forward/backward traversal, wrapping, and repair when the focused interaction region becomes ineligible.

The tranche does not add input routing, command dispatch, callbacks, mouse focus policy, terminal protocol work, or terminal I/O.

## RED checkpoint

The T1404 focus tests were committed first at:

`3785c26610cfc25bb9b2de485e0b9670e3a2ebd9`

Workflow #737 / `34718226058` failed as intended with zero warnings because the frozen T1404 public surface did not yet exist:

- `CursesFocusDirection`;
- `CursesInteractionRouter.FocusedRegion`;
- `CursesInteractionRouter.Focus(...)`;
- `CursesInteractionRouter.ClearFocus()`;
- `CursesInteractionRouter.MoveFocus(...)`.

No unrelated production regression was present in the RED run.

The RED suite covers:

- explicit focus and idempotent clear;
- null/foreign/disposed region validation;
- ineligible focus attempts preserving existing focus;
- traversal ordering by `TraversalOrder` then registration ordinal;
- forward and backward wrap;
- no-current-focus direction semantics;
- no-eligible-region behavior;
- undefined direction validation;
- immediate repair when the current region becomes disabled, non-focusable, fully clipped by a region-owned bounds change, or disposed;
- repair from a disposed region's former traversal slot;
- lazy repair after screen clipping;
- lazy repair after panel hide/resize changes;
- proof that pure `HitTest(...)` does not repair logical focus;
- disposed-router behavior for all focus operations.

## Public surface

T1404 adds exactly one exported type and four public router members:

```csharp
public enum CursesFocusDirection {
    Forward = 0,
    Backward = 1
}

public sealed partial-contract CursesInteractionRouter {
    public CursesInteractionRegion? FocusedRegion { get; }

    public bool Focus( CursesInteractionRegion region );
    public void ClearFocus();
    public CursesInteractionRegion? MoveFocus( CursesFocusDirection direction );
}
```

The implementation landed at:

`5ef19026b3842eb71fbee95a1bba3509d1f39338`

## Logical-focus ownership

Logical focus is state owned by `CursesInteractionRouter` only. It does not represent terminal focus and is not driven by `CursesFocusEvent`.

A terminal `Focused` or `Unfocused` input event therefore cannot implicitly replace or clear the router's logical focused region.

The router does not subscribe to hidden screen/panel lifecycle callbacks merely to maintain focus. This preserves the T1401 application-owned/single-writer architecture.

## Explicit focus

`Focus(region)`:

1. validates null, router ownership, router disposal, and region disposal;
2. repairs any stale existing focus before applying the request;
3. returns `false` without changing the repaired current focus if the requested region is not currently focus eligible;
4. otherwise stores the supplied region and returns `true`.

`ClearFocus()` is idempotent and performs no traversal.

Reading `FocusedRegion` is a focus-sensitive operation and therefore performs lazy repair before returning the current value.

## Eligibility

T1404 uses the T1401 frozen focus-eligibility contract. A region is focus eligible only when all of the following hold:

- it is not disposed;
- `IsEnabled` is true;
- `IsFocusable` is true;
- its effective clipped geometry is non-empty;
- when panel-associated, the current panel is visible and not disposed.

Ordinary region eligibility is clipped against the current screen. Panel-associated eligibility is clipped against current region bounds, panel bounds, and screen bounds.

No duplicate geometry cache or background observer graph is introduced.

## Traversal

Eligible regions are ordered by the exact frozen tuple:

```text
(ascending TraversalOrder, ascending registration ordinal)
```

The implementation uses repeated bounded scans of the router's existing region registry rather than sorting or allocating a temporary traversal collection.

`MoveFocus(Forward)` always selects the next eligible traversal slot and wraps to the first eligible region.

`MoveFocus(Backward)` always selects the previous eligible traversal slot and wraps to the last eligible region.

With no current focus:

- `Forward` chooses the first eligible region;
- `Backward` chooses the last eligible region.

With no eligible regions, traversal stores and returns `null`.

Undefined `CursesFocusDirection` values fail with `ArgumentOutOfRangeException` before focus mutation.

## Repair

Repair direction is always forward, independent of the user's most recent traversal direction.

When the current region becomes ineligible, repair selects the first eligible region after the former traversal slot and wraps to the first eligible region when needed. If no eligible region remains, focus becomes `null`.

### Immediate repair

Region-owned changes can notify the owning router directly and therefore repair immediately when they invalidate the current focused region:

- `IsEnabled = false`;
- `IsFocusable = false`;
- `SetBounds(...)` producing no effective visible area;
- region disposal.

Region disposal captures the former traversal tuple before removing the region, then repairs from that former slot. Registration ordinals remain monotonic and unreused.

### Lazy repair

External geometry/state changes are not subscribed through a background graph. They repair at focus-sensitive boundaries instead:

- reading `FocusedRegion`;
- `Focus(...)`;
- `MoveFocus(...)`.

This covers current screen size and current panel visibility, size, position, and disposal state.

Later T1406 routing will add the other T1401-frozen focus-sensitive boundary: routing text/key/paste input.

Pure `HitTest(...)` intentionally remains focus-neutral and does not repair or mutate focus.

## Implementation properties

The focus/traversal core:

- owns no background work;
- performs no terminal input or output;
- invokes no application callbacks;
- uses the existing bounded region registry;
- creates no unbounded focus history;
- performs traversal/repair with nonallocating scans in the normal path;
- leaves mouse focus policy entirely application-owned.

Router disposal clears focus before disposing/removing owned regions so teardown does not perform unnecessary repair churn.

## First GREEN evidence and API guard

Workflow #738 / `34718456495` built the complete solution cleanly with zero warnings.

Across `net8.0`, `net9.0`, and `net10.0`, all **655 behavioral tests passed**. The only failure was the expected current-development public API fingerprint guard.

The compiler-derived T1404 contract was identical on all three TFMs:

```text
56 exported types
443 canonical declared contract lines
sha256 2fa4edb0c303c3cfb88c9c5aa0c3dd2386dc1022e27aa506d369fddc622ae3a6
```

No behavioral implementation correction was required after this run.

## Fingerprint qualification

`docs/Public-API-Fingerprint-1.4.json` was advanced to the compiler-derived contract at:

`d380fa0033f7d3c6d0932167f1c31cc7594be5af`

Workflow #739 / `34718551272` then passed all seven PR jobs on that exact head:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

This qualifies the T1404 implementation and the current alpha API fingerprint together.

## Scope audit

T1404 deliberately does **not** add:

- automatic focus on mouse hit/click;
- terminal focus coupling;
- semantic key gestures;
- command identities/bindings;
- structured input routing;
- pointer-shape behavior;
- event bubbling/capture;
- callback dispatch;
- terminal protocol parsing or I/O.

Those remain assigned to later tranches exactly as frozen by T1401.

## Exit gate

T1404 is complete when this documentation-complete head passes the full PR package/runtime matrix. After that exact-head qualification, T1405 may begin the semantic keyboard gesture model with a fresh RED checkpoint.
