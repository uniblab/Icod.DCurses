# T701 — Refresh-Cost Foundation and Baseline

**Release:** `0.7.0`  
**Tranche:** `T701`  
**Development version:** `0.7.0-alpha.1`  
**Assembly version:** `0.7.0.0`  
**Status:** Complete  
**Validation:** Windows, Linux, macOS, and canonical package/fresh-consumer validation green

## 1. Objective

T701 establishes a deterministic measurement foundation before `0.7.0` changes physical terminal-operation selection.

The purpose is not to expose performance counters as a public application contract. It is to make output-cost decisions machine-testable and to preserve a known pre-optimization baseline against which T702-T707 can demonstrate real savings.

## 2. Cost model

`CursesOutputCostModel` is intentionally internal.

Application text is counted through the configured application encoding because `TerminalSession.WriteTextAsync(...)` uses the session application-text encoding.

Already-resolved TermInfo terminal strings are counted through `TermInfoOutput.TPuts(...)` semantics rather than through application UTF-8. This matters because Terminal emits those strings byte-exact through Latin-1 and TermInfo interprets/removes padding directives such as `$<...>` before transport output.

The model therefore distinguishes:

```text
application text     -> configured application encoding
terminal capability  -> TermInfo terminal-string semantics
```

It does not estimate cost from source string length.

## 3. Frozen pre-optimization baselines

The deterministic baseline terminal uses:

```text
CursorAddress = <cup:%p1%d,%p2%d>
```

After one initial refresh establishes retained physical-screen knowledge, the following baseline expectations are machine-guarded:

| Scenario | Bytes | Writes | Flushes |
|---|---:|---:|---:|
| Clean no-op refresh | 0 | 0 | 1 |
| One changed ASCII cell at `(0,0)` | 10 | 2 | 1 |
| One changed wide `界` cell at `(0,0)` | 12 | 2 | 1 |
| One changed bold ASCII cell with `<sgr0>` / `<b>` | 19 | 4 | 1 |

The one-cell ASCII case is deliberately simple: nine cursor-address bytes plus one application-text byte. Later cursor-motion work can improve that number only by selecting a shorter safe advertised operation.

The wide-cell case proves measurement counts encoded application bytes rather than terminal columns.

The bold case records the reset-first rendition cost inherited from the stable `0.6.0` presentation contract; T707 may improve it only while preserving equivalent physical rendition.

## 4. Padding behavior

The cost tests explicitly prove that source padding notation is not counted as terminal output bytes.

For example:

```text
ESC [ H $<25*> X
```

has a measured terminal-string byte cost of four bytes when padding delay is ignored by the deterministic cost calculation.

This keeps later candidate comparisons focused on emitted bytes rather than the textual representation of a TermInfo capability.

## 5. Public API decision

T701 adds no public type, property, event, or diagnostics object.

A public refresh-statistics API remains rejected by default. T708 may revisit that decision only if concrete consumer evidence demonstrates a need that cannot be satisfied by internal tests, benchmark tooling, or external transport measurement.

## 6. T701 gate result

Exact head `bd3514118b9120ac71602e6dceb8ca98c3669c86` passed pull-request run #241 on:

- Windows Staging restore/build/test;
- Linux Staging restore/build/test;
- macOS Staging restore/build/test;
- canonical Staging package validation and fresh package consumption.

Both library and tests retained warning level 4 with warnings-as-errors under Staging.

## 7. Handoff to T702

T702 may now evaluate synchronized-output framing against measured refresh costs.

The Terminal-owned DEC mode 2026 begin/end frames impose fixed output overhead. Therefore synchronized output SHALL be treated as a presentation/atomicity policy, not assumed to be a universal byte optimization.

Any accepted DCurses policy must:

- compose through `TerminalSession.AcquireSynchronizedOutputAsync(...)`;
- preserve Terminal nesting semantics;
- avoid terminal-name/emulator heuristics;
- leave ordinary refresh semantics intact when disabled;
- measure frame overhead explicitly;
- retain `RefreshAsync()` as the transaction boundary.