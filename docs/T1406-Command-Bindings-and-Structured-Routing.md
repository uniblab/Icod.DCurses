# T1406 — Command Bindings and Structured Routing

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1406  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** implementation/fingerprint head qualified; documentation-complete head requires its own exact-head matrix

---

## Objective

T1406 turns the T1405 semantic gesture identity into bounded application command bindings and deterministic structured input routing. DCurses resolves input to command identities or interaction targets; it does not invoke application delegates, create a hidden event loop, infer terminal protocols, or take ownership of application behavior.

The tranche implements exactly the T1401-frozen local/global binding scopes, precedence, capacity limits, routing result shapes, and event-kind behavior.

## RED checkpoint

The T1406 routing tests were committed first at:

`0567ff1f085fa057a72026f062df14bd2a37071c`

Workflow #745 / `34720941297` failed as intended across the package job and all six runtime jobs. The representative Linux ARM64 build recorded zero warnings and failed only because the frozen T1406 surface did not yet exist: `CursesCommand`, `CursesInteractionResult`, `CursesInteractionResultKind`, local/global binding APIs, and `CursesInteractionRouter.Route(...)`.

The RED suite covers:

- command-name validation, equality, and text representation;
- local and router-global gesture binding and explicit unbinding;
- rejection of default/non-bindable gestures before registry mutation;
- duplicate binding rejection without replacement;
- per-region, global, and total-router binding limits;
- focused-region command precedence over router-global commands;
- Text and Key local/global/targeted/unrouted routing;
- Paste targeted-only behavior;
- Mouse routing exclusively through hit testing;
- no focus mutation from mouse routing;
- Focus and EndOfInput remaining unrouted;
- immutable result-shape invariants for unrouted, targeted, local-command, global-command, and mouse-hit results;
- routing-result snapshot stability after later region changes or disposal;
- cleanup of region-owned bindings on region disposal;
- router lifecycle validation.

## Implementation chain

The implementation was deliberately split into small commits after the RED checkpoint:

- `3475d09b6aa4703ccbbcc5c40b17b82dcf82a341` — prepare `CursesInteractionRegion` for binding implementation;
- `b77c3ef5c3f591c6f140541e40d0b67265370b9c` — prepare `CursesInteractionRouter` for routing implementation;
- `1f5c413577d464a116fb662922e504d91a552b7c` — add `CursesCommand` identity;
- `9f3cfff7c05319404980991fe7e09efdf6897fb1` — add `CursesInteractionResultKind`;
- `40dbf4279368364d4d6127e0be3c7cc2e028352a` — add immutable `CursesInteractionResult`;
- `bf499fd3a0ddfd2a3bd165b41fae69ed4a29f26c` — implement bounded region-local gesture bindings;
- `6ae45c52b976c85c6be79374b7ea783c046ebe6b` — implement router-global bindings and structured routing.

## Command identity

`CursesCommand` is an immutable identifier only. It does not contain or invoke a delegate.

The frozen public surface is:

```csharp
public sealed record CursesCommand {
    public const int MaximumNameLength = 128;

    public CursesCommand( string name );

    public string Name { get; }

    public override string ToString();
}
```

Names are required, non-whitespace, and at most 128 UTF-16 code units. Record equality is exact ordinal value equality over `Name`, and `ToString()` returns the exact command name.

## Binding scopes and conflicts

Bindings exist in two scopes:

- region-local bindings owned by `CursesInteractionRegion`;
- router-global bindings owned by `CursesInteractionRouter`.

The public operations are:

```csharp
region.BindGesture( gesture, command );
region.UnbindGesture( gesture );

router.BindGlobalGesture( gesture, command );
router.UnbindGlobalGesture( gesture );
```

A scope may contain at most one command for a gesture. Binding a duplicate gesture in the same scope throws `InvalidOperationException` and leaves the original binding intact. Changing a binding therefore uses explicit unbind-then-bind semantics.

A default-zero or otherwise non-bindable `CursesKeyGesture` is rejected before mutation.

## Frozen capacity limits

T1406 enforces the T1401 limits:

```text
Maximum live regions per router                  4096
Maximum gesture bindings per live region          256
Maximum router-global gesture bindings            1024
Maximum total live gesture bindings per router   16384
```

The public constants remain on `CursesInteractionRouter`:

```csharp
MaximumRegions
MaximumRegionGestureBindings
MaximumGlobalGestureBindings
MaximumGestureBindings
```

Capacity checks fail closed with `InvalidOperationException`; no existing binding is evicted or replaced.

Disposed regions no longer contribute live local bindings to router capacity.

## Structured routing

`CursesInteractionRouter.Route(CursesInputEvent)` returns an immutable `CursesInteractionResult` and never invokes application callbacks.

### Text and Key

Text and Key input use the same deterministic sequence:

1. repair logical focus if needed;
2. resolve a matching binding on the eligible focused region;
3. otherwise resolve a matching router-global binding;
4. otherwise, if an eligible focused region exists, return a targeted result for that region;
5. otherwise return an unrouted result.

This makes focused-region bindings strictly higher precedence than router-global bindings while preserving global commands when there is no local match.

### Paste

Paste input is delivered as a targeted result to the current eligible focused region. If no eligible focused region exists, it is unrouted.

Version 1.4 does not add paste gestures or paste commands.

### Mouse

Mouse input is routed exclusively through the existing deterministic `HitTest(...)` contract. A successful hit returns a targeted result with the selected region and immutable hit snapshot, including local coordinates.

Mouse routing does not run key-gesture bindings and does not automatically change logical focus.

### Terminal Focus and EndOfInput

Terminal Focus input and EndOfInput remain unrouted. Terminal focus state is not application logical focus and does not mutate `FocusedRegion`.

## Result contract

T1406 adds:

```csharp
public enum CursesInteractionResultKind {
    Unrouted = 0,
    Targeted = 1,
    Command = 2
}

public sealed class CursesInteractionResult {
    public CursesInteractionResultKind Kind { get; }
    public CursesInputEvent Input { get; }
    public CursesInteractionRegion? Region { get; }
    public CursesCommand? Command { get; }
    public CursesInteractionHit? Hit { get; }
}
```

The frozen invariants are:

- `Unrouted`: `Region`, `Command`, and `Hit` are null;
- keyboard/text/paste `Targeted`: `Region` is non-null; `Command` and `Hit` are null;
- mouse `Targeted`: `Region` and `Hit` are non-null and identify the same region; `Command` is null;
- region-local `Command`: `Command` and `Region` are non-null; `Hit` is null;
- router-global `Command`: `Command` is non-null; `Region` and `Hit` are null.

Every result retains the original `CursesInputEvent`. Results are snapshots: later focus changes, region geometry changes, binding changes, or region disposal do not rewrite an already-returned result object.

## First GREEN evidence and API guard

The complete behavioral implementation head is:

`6ae45c52b976c85c6be79374b7ea783c046ebe6b`

Workflow #752 / `34721214333` built the implementation with zero warnings and zero errors. Across `net8.0`, `net9.0`, and `net10.0`, all **701 behavioral/non-fingerprint tests passed**; the only failing test was the expected current-development public API fingerprint guard.

The compiler-derived T1406 contract was identical across all target frameworks:

```text
60 exported types
482 canonical declared contract lines
sha256 fdfa06a44c4d2c3f50f52e911584e8ecd8fcd9499765be2673fe4070e2faa025
```

The new exported types over T1405 are:

- `Icod.DCurses.CursesCommand`;
- `Icod.DCurses.CursesInteractionResult`;
- `Icod.DCurses.CursesInteractionResultKind`.

No behavioral implementation correction was required after that run.

## Fingerprint qualification

`docs/Public-API-Fingerprint-1.4.json` was advanced to the compiler-derived T1406 contract at:

`e01605803c62708f28e5a5516d8fd51982a9e75a`

Workflow #753 / `34729776109` passed all seven PR jobs on that exact head:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

This jointly qualifies the T1406 implementation and the current 1.4 alpha public API fingerprint.

## Ownership and architecture audit

T1406 preserves the established ownership boundary:

- DCurses owns logical regions, focus, semantic gestures, command identifiers, binding registries, hit testing, and structured routing results;
- applications own command behavior and decide what to do with returned command identities/targets;
- `Icod.Terminal` remains authoritative for terminal input/protocol lifecycle;
- `Icod.TermInfo` remains authoritative for terminal capability/description facts already consumed by DCurses;
- no new public Terminal or TermInfo types cross the DCurses public API boundary;
- runtime dependency versions remain `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0`.

T1406 deliberately does **not** add callbacks, delegates, widgets, hidden event loops, automatic focus-on-click, pointer-shape protocol output, retained raster ownership, or raw terminal protocol parsing.

## Exit gate

T1406 is complete when this documentation-complete head passes the full PR package/runtime matrix. After that exact-head qualification, T1407 may begin pointer-shape integration with a fresh RED checkpoint.
