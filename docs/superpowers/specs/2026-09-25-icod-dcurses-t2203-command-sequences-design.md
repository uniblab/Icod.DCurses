# Icod.DCurses T2203 Bounded Command Sequences Design

**Date:** 2026-09-25\
**Status:** Approved conversational design; written specification pending review\
**Release:** Icod.DCurses 2.2.0\
**Baseline:** accepted T2202 effective-binding discovery at executable head `1d6097d`

## Intent

T2203 adds opt-in, bounded multi-key command composition to the existing
`CursesInteractionRouter`. Applications continue to own their event loops,
command execution, labels and drawing. DCurses classifies normalized input and
returns immutable data; it never invokes application callbacks, waits for input,
starts a timer or performs terminal I/O.

The published `Route(CursesInputEvent)` behavior remains unchanged. Applications
that do not register or process command sequences pay no sequence-matching cost
during ordinary routing.

## Chosen architecture

Pending sequence state belongs to `CursesInteractionRouter`. Sequence
registrations belong to the same region, scope and global owners as single-key
bindings. This preserves one focus/scope authority and lets the router invalidate
pending input when its routing context changes.

A caller-owned matcher was rejected because it would need to duplicate owner
precedence and observe every focus, scope, eligibility and disposal mutation.
Adding implicit sequence waits to `Route` was rejected because it would change
the published single-key contract and introduce hidden timing policy.

## Public API

Add these limits to `CursesInteractionRouter`:

```csharp
public const int MaximumCommandSequenceLength = 8;
public const int MaximumRegionCommandSequenceBindings = 128;
public const int MaximumScopeCommandSequenceBindings = 128;
public const int MaximumGlobalCommandSequenceBindings = 512;
public const int MaximumCommandSequenceBindings = 4096;
```

Add an immutable binding value:

```csharp
public sealed class CursesCommandSequenceBinding
{
    public CursesCommandSequenceBinding(
        IReadOnlyList<CursesKeyGesture> gestures,
        CursesCommand command);

    public IReadOnlyList<CursesKeyGesture> Gestures { get; }
    public CursesCommand Command { get; }
}
```

The constructor copies `gestures` into a detached read-only array. It rejects a
null list, null command, a list shorter than two or longer than
`MaximumCommandSequenceLength`, and any gesture not created by a
`CursesKeyGesture` factory.

Add result data:

```csharp
public enum CursesCommandSequenceResultKind
{
    Fallback = 0,
    Pending = 1,
    Completed = 2,
    Mismatch = 3
}

public sealed class CursesCommandSequenceResult
{
    public CursesCommandSequenceResultKind Kind { get; }
    public CursesInputEvent Input { get; }
    public IReadOnlyList<CursesKeyGesture> MatchedGestures { get; }
    public CursesCommand? Command { get; }
    public CursesInteractionResult? Fallback { get; }
}
```

Result construction is library-owned. `Input` is the event supplied to the
current processing call. `MatchedGestures` is:

- empty for `Fallback`;
- the current prefix for `Pending`;
- the complete sequence for `Completed`;
- the abandoned prefix, excluding the mismatching event, for `Mismatch`.

`Command` is non-null only for `Completed`. `Fallback` is non-null only for
`Fallback` and `Mismatch`.

Add sequence registration to existing owners:

```csharp
// CursesInteractionRegion and CursesInteractionScope
public void BindGestureSequence(
    IReadOnlyList<CursesKeyGesture> gestures,
    CursesCommand command);
public bool UnbindGestureSequence(
    IReadOnlyList<CursesKeyGesture> gestures);

// CursesInteractionRouter
public void BindGlobalGestureSequence(
    IReadOnlyList<CursesKeyGesture> gestures,
    CursesCommand command);
public bool UnbindGlobalGestureSequence(
    IReadOnlyList<CursesKeyGesture> gestures);
```

Add opt-in processing, state and discovery to the router:

```csharp
public bool HasPendingCommandSequence { get; }

public CursesCommandSequenceResult ProcessCommandSequence(
    CursesInputEvent input);

public bool CancelPendingCommandSequence();

public IReadOnlyList<CursesCommandSequenceBinding>
    GetEffectiveGestureSequenceBindings();
```

All new router members throw `ObjectDisposedException` after router disposal.
Region and scope registration members retain their existing ownership and
disposal conventions.

## Registration rules

A sequence contains two through eight bindable gestures. The owner copies the
list before storing it, so later caller mutation cannot affect registration.

Within one owner:

- the same sequence cannot be registered twice;
- a complete sequence cannot be a proper prefix of another sequence;
- multiple sequences may share a nonterminal prefix, such as `g g` and `g d`;
- a sequence first gesture cannot also have a single-key binding;
- adding a single-key binding whose gesture starts a sequence is likewise
  rejected.

These checks are independent of registration order. Violations throw
`InvalidOperationException` without changing existing bindings. Invalid
arguments and capacity failures are also detected before mutation.

Each region and scope can own at most 128 live sequences, the router can own at
most 512 global sequences, and one router can own at most 4,096 sequences in
total. Sequence counts are separate from the existing single-key gesture limits.

## First-gesture precedence

`ProcessCommandSequence` repairs focus using the existing router rule and then
examines owners in this order:

1. focused region, when present;
2. that region's scope ancestors through and including the active scope, or only
   the active scope when no region is focused;
3. router-global bindings.

The first owner containing either a matching single-key binding or one or more
sequences beginning with the input owns that gesture. A same-owner single/sequence
conflict cannot exist because registration rejects it.

If the winning owner has a single-key binding, processing returns `Fallback`
containing the result of exactly one `Route(input)` call. If it has sequence
candidates, processing returns `Pending` and retains only that owner's matching
candidates. Lower-precedence owners are ignored for the lifetime of that pending
sequence.

Consequently, an opt-in sequence at a focused region can take precedence over a
lower global single-key binding, while a higher single-key binding prevents a
lower sequence from starting. Ordinary direct calls to `Route` remain exactly
as published in 2.1.

## Pending, completion and mismatch

While a sequence is pending, only the retained owner's candidates are examined:

- when one or more candidates still share the extended prefix, return
  `Pending`;
- when the prefix exactly equals a registered sequence, clear pending state and
  return `Completed` with its command;
- when no candidate matches, clear pending state and return `Mismatch`.

A mismatch discards the earlier prefix. The mismatching event is passed through
ordinary `Route(input)` exactly once, and that immutable result is returned in
`Fallback`. The event is not reconsidered as the start of another sequence.
This prevents input loss and prevents accidental double routing.

Non-keyboard input can never start a sequence. With no pending sequence it
returns `Fallback`. While pending it follows the same mismatch-and-fallback
rule. No prefix has a timeout; the application decides when to call
`CancelPendingCommandSequence()`, which returns `true` only when it clears
live pending state.

## Context invalidation

Pending state is cleared immediately by every mutation that can change effective
keyboard routing, including:

- a focus assignment, move, clear or lazy focus repair that changes the focused
  region;
- scope activation or deactivation;
- region eligibility or panel-eligibility changes;
- screen resize;
- single-key or sequence bind/unbind;
- disposal of the pending owner, a relevant region or scope, or the router.

After automatic invalidation, `HasPendingCommandSequence` is `false`, and the
next input is processed from a fresh context. Automatic invalidation does not
synthesize a result or retain discarded input. Applications that change context
while showing prefix UI can observe the property or explicitly cancel before the
mutation.

## Effective sequence discovery

`GetEffectiveGestureSequenceBindings()` returns a new detached read-only
snapshot. It uses the same first-owner precedence as
`ProcessCommandSequence`, including higher single-key bindings that prevent a
lower sequence from starting. Within each owner, sequences are sorted
lexicographically by the existing semantic gesture order and then by length.
Owner groups remain in routing-precedence order.

The existing `GetEffectiveGestureBindings()` remains a snapshot of ordinary
single-key `Route` behavior and is not changed. The new query describes only
sequences that can start through `ProcessCommandSequence`; neither query
executes application code or retains an owner in its returned values.

## State and allocation bounds

Pending state retains the owning region/scope identity when applicable, a
copied prefix of at most eight gestures and at most one owner's registered
candidate set. It never grows with input duration or application content.

No sequence matching, candidate enumeration or result allocation is added to
ordinary `Route`. Existing single-key bind methods perform only the bounded
same-owner first-gesture conflict check needed to keep registration order
independent.

## Verification

Implementation is test-first and must demonstrate:

- a compile-failing RED checkpoint for the frozen public API;
- region, nested scope, modal and global owner precedence;
- shared prefixes, completion, explicit cancellation and mismatch fallback;
- exactly-once mismatch fallback and no restart from the mismatching event;
- conflict rejection in both registration orders without partial mutation;
- Unicode text, Space equivalence, modifiers, phases and function keys;
- all length, per-owner and router-total capacity boundaries;
- focus, scope, eligibility, resize, unbind and disposal invalidation;
- detached immutable result and discovery snapshots with deterministic ordering;
- unchanged direct `Route` results and published 2.1 API compatibility;
- a measured additive 2.2 public API fingerprint;
- Staging CI on .NET 8, 9 and 10 across the existing platform matrix;
- fresh package-only consumers and the established Linux pseudo-terminal check.

The workspace has no local .NET SDK. RED and GREEN build/test evidence therefore
comes from exact-head PR Staging workflows. PR work remains Staging-only; Release
validation is reserved for an authorized push to `main`.

## Exclusions

T2203 does not add timers, timeout results, callbacks, handlers, command
execution, labels, enabling predicates, a widget tree, an application event loop
or terminal protocol work. Prompt editing and timed pointer/frame facilities
remain separately evidence-gated.
