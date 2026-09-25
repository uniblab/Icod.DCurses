# T2207 Adversarial, Package, Documentation and API Gate

**Date:** 2026-09-25\
**Status:** Accepted at exact executable head `e5c5917495c11edea0fc9af37c0545f297a8f47c`\
**Branch:** `2.2.0-roadmap`\
**PR workflow:** [36098389089](https://github.com/uniblab/Icod.DCurses/actions/runs/36098389089), 7/7 jobs successful

## Adversarial and allocation review

| Boundary | Permanent evidence |
| --- | --- |
| immutable sequence values, invalid input, conflicts, exact unbind and per-owner/router capacities | `CursesCommandSequenceContractTests` |
| region/scope/global precedence, shared prefixes, semantic gestures, fallback, completion and exactly-once mismatch | `CursesCommandSequenceRoutingTests` |
| focus, scope, eligibility, panel, resize, binding mutation, lazy repair and disposal invalidation | `CursesCommandSequenceContextTests` |
| deterministic, detached, read-only effective single-key and sequence snapshots | `CursesInteractionDiscoveryTests`, `CursesCommandSequenceDiscoveryTests` |
| 2.1 unused-path routing ceiling with maximum sequence registration | `CursesInteractionPerformanceHardeningTests.RegisteredCommandSequencesDoNotIncreaseOrdinaryRouteAllocationCeiling` |
| maximum shared-prefix start/cancel and capacity-sized discovery snapshots | `CursesInteractionPerformanceHardeningTests.StartingAndCancellingMaximumSharedPrefixStaysWithinBoundedAllocationCeiling`, `EffectiveBindingDiscoveryAtRegionCapacityStaysWithinSnapshotAllocationCeiling`, `EffectiveSequenceDiscoveryAtRegionCapacityStaysWithinSnapshotAllocationCeiling` |
| editor and roguelike context changes, Unicode mismatch fallback, live Control identities and raw-input policy | `EditorSampleInteractionTests`, `RoguelikeSampleInteractionTests`, `EditorSampleProjectTests`, `RoguelikeSampleProjectTests` |

The allocation additions are characterization gates for the already-implemented
T2202/T2203 contract, not a production behavior change. They prove that merely
registering the maximum region sequence set does not raise ordinary `Route`
above its existing snapshot ceiling, and that sequence processing and both
discovery APIs remain bounded by their public registration limits. No timing
assertion was added to the new paths; the existing broad representative
throughput gate remains the non-flaky wall-clock guard.

## Public API, ownership and dependency freeze

The frozen 2.2 release-candidate fingerprint is:

```text
100 exported types
783 canonical declared contract lines
sha256 7c9866abaeeacc7f64631d2a800b91333cee72ad1a53872896e8f4d8c7ccb097
```

This is additive over the immutable published 2.1 fingerprint of 96 exported
types, 751 canonical contract lines and SHA-256
`c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79`.
The delta remains limited to immutable binding discovery and bounded command
sequences. Ordinary `Route` retains its signature and behavior. Applications
continue to own labels, enabled policy, command execution, timers, document or
world state, rendering and event loops.

The regret review accepts this surface in DCurses because it composes the
published interaction router's focus/scope/command precedence without
introducing application policy. Prompt state and clock-fed timing remain
deferred: current application evidence does not justify freezing either into
core. No widget hierarchy, callback dispatcher, command execution framework,
raw terminal decoding, private clock or background pump was admitted.

`Version` and `PackageVersion` remain `2.2.0-alpha.1`; `AssemblyVersion`
remains `2.0.0.0`. The sole direct production package dependency remains
`Icod.Terminal 1.18.0`; `Icod.TermInfo` remains transitive and absent from the
DCurses project and public API boundary.

## Package and documentation review

The root README now distinguishes published stable 2.1.0 from the
2.2.0-alpha.1 source identity, explains binding discovery and command
sequences, includes a concise public API example and links the 2.2 API,
roadmap and sample documentation. The changelog records the complete additive
2.2 development delta. The sample README documents both public-only
application witnesses, controls, live Control-key identities, raw input,
context invalidation, clean screen clearing and application ownership.

The package verifier checks the `.nupkg`/`.snupkg` structure, version and
assembly identity, repository metadata, license, packaged README, XML
documentation, portable symbols and dependency closure. Fresh package-only
consumers compile and run on .NET 8, 9 and 10, including public sequence
registration, discovery, cancellation, unbinding and processing-signature
checks plus the Linux pseudo-terminal refresh.

## Exact-head qualification

[PR Staging workflow 36098389089](https://github.com/uniblab/Icod.DCurses/actions/runs/36098389089)
passed all seven jobs at exact executable head
`e5c5917495c11edea0fc9af37c0545f297a8f47c`:

- six Windows, Linux and macOS x64/ARM64 runtime jobs built successfully;
- every runtime job passed 1,285 tests on each of .NET 8, 9 and 10 with zero
  failures and zero skips;
- representative Windows x64 build output reported zero compiler warnings and
  zero errors;
- macOS x64 target-framework tests remained sequential as agreed and passed
  1,285/1,285 on every target;
- the package candidate built and validated
  `Icod.DCurses.2.2.0-alpha.1.nupkg` and `.snupkg` and ran fresh package-only
  consumers on all three target frameworks.

Staging artifact `10848192714` records the exact executable head and has ZIP
digest
`sha256:8d6be00c7c6d032118a0ce0f8c0a16dc9fb094ab971049a37adbe15bcbc1c4af`.

## Disposition

T2207 is accepted. The public API, dependency, package and documentation
contract is frozen for the 2.2 release candidate. T2208 may now perform RC and
stable-source exact-head closure; it must not add features or alter the frozen
surface without reopening T2207.

PR #34 remains draft and unmerged. No tag was created and no package was
published. PR validation used Staging only; Release validation remains
reserved for a separately authorized push to `main`.
