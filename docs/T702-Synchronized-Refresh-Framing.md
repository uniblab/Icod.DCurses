# T702 — Synchronized Refresh Framing

**Release:** `0.7.0`  
**Tranche:** `T702`  
**Development version:** `0.7.0-alpha.2`  
**Assembly version:** `0.7.0.0`  
**Status:** Complete

## Accepted public policy

`CursesSessionOptions` adds one opt-in property:

```csharp
bool UseSynchronizedOutput { get; init; }
```

The default is `false`.

When enabled, one complete `CursesSession.RefreshAsync()` transaction is nested inside the synchronized-output lease owned by `Icod.Terminal`:

```text
Terminal synchronized-output begin
    DCurses retained-screen refresh payload
    final DCurses cursor placement
Terminal synchronized-output end
Terminal final-release flush
```

DCurses does not construct DEC private mode 2026 escape sequences in production code. It calls `TerminalSession.AcquireSynchronizedOutputAsync(...)` and relies on Terminal for mode ownership, nesting, write serialization, restoration, and final-release flushing.

## Why the default is off

T701 established deterministic byte baselines before this policy was introduced.

The Terminal begin/end frames are each eight protocol bytes, for a fixed unnested framing overhead of:

```text
16 bytes per refresh
```

The existing T701 one-cell ASCII refresh baseline is only ten bytes, and a clean no-op refresh emits zero bytes before the ordinary refresh flush.

Therefore synchronized output is not accepted as a universal output-size optimization. It is a visual-atomicity policy that applications may opt into when avoiding visible intermediate refresh states is worth the fixed protocol overhead.

No threshold or automatic terminal-name heuristic is used.

## Optimistic semantics

`UseSynchronizedOutput = true` requests optimistic DEC private mode 2026 framing.

It does **not** assert that the selected terminal recognizes the mode. This follows the stable `Icod.Terminal 1.0.0` contract: unsupported terminals may ignore the begin/end frames while the enclosed ordinary terminal output remains valid.

DCurses does not infer support from:

- `TERM`;
- TermInfo profile names;
- operating system;
- terminal emulator brand or lineage.

## Nesting

The DCurses lease composes with an outer Terminal synchronized-output lease.

A caller that already owns an outer Terminal lease may call an opted-in `CursesSession.RefreshAsync()` without producing duplicate physical begin/end frames. Terminal's reference-counted lease owns that behavior.

This is important because ownership transfer of a `TerminalSession` to DCurses does not create a second terminal protocol implementation.

## Failure behavior

If the refresh body fails, DCurses still attempts to dispose its synchronized-output lease.

If refresh and lease restoration both fail, the implementation reports both failures in an `AggregateException` rather than allowing disposal failure to silently replace the original refresh exception.

If lease acquisition fails or is cancelled, no DCurses refresh payload is emitted through that lease.

## Flush behavior

The ordinary refresh engine retains its existing end-of-refresh flush.

An unnested synchronized-output lease adds Terminal's required final-release flush after the end frame. T702 tests therefore observe two flushes for an opted-in refresh versus one for the default path.

Later T707 work may review whether a redundant internal flush can be safely eliminated under synchronized framing. T702 deliberately does not combine ownership changes with that optimization.

## Validation

Exact T702 implementation head `eb932c1e75d5e64432ef1db041c2b64a4eeeb368` passed pull-request run #245 on:

- Windows Staging restore/build/test;
- Linux Staging restore/build/test;
- macOS Staging restore/build/test;
- canonical package/fresh-consumer validation.

The integration tests prove:

- default-off compatibility;
- begin/payload/end ordering;
- measured 16-byte frame overhead;
- extra final-release flush;
- correct composition with an outer Terminal synchronized-output lease.

## Handoff to T703

T703 now targets actual byte reduction by choosing the least expensive safe advertised cursor-motion sequence.

The T701 ten-byte one-cell baseline remains unchanged when the synthetic terminal advertises only absolute `cup`. A terminal that also advertises a shorter exact relative or one-column motion should be able to reduce that baseline without changing logical screen semantics.