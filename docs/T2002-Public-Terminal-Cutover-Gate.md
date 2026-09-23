# T2002 Public Terminal Cutover Gate

**Date:** 2026-09-18.\
**DCurses development identity:** `2.0.0-alpha.1`.\
**Assembly identity:** `2.0.0.0`.\
**Published dependency qualified:** `Icod.Terminal 1.18.0`.\
**Status:** **accepted — T2003 may begin**.

## Gate outcome

T2002 is accepted. The development identity is established, all four approved
TermInfo-bearing public contracts now use Terminal-owned profile/dimension types,
status and error metadata are preserved, and the resulting 2.0 public API is frozen
without changing the historical 1.x artifacts.

The accepted exact executable head is
[`f030d1b1b7e18f4566c171d4fc8f5a4765a3f8cc`](https://github.com/uniblab/Icod.DCurses/commit/f030d1b1b7e18f4566c171d4fc8f5a4765a3f8cc),
qualified by [workflow 35390788950](https://github.com/uniblab/Icod.DCurses/actions/runs/35390788950).
This gate authorizes T2003 only. It does not claim that the renderer or package is
fully decoupled, and it does not authorize merge, tagging, release, or publication.

## Identity and dependency result

| Contract | Accepted result |
|---|---|
| `Version` / `PackageVersion` | `2.0.0-alpha.1` / `2.0.0-alpha.1` |
| `AssemblyVersion` | `2.0.0.0` |
| Direct Terminal reference | `Icod.Terminal 1.18.0` |
| Temporary direct TermInfo reference | `Icod.TermInfo 1.15.0` |
| Target frameworks | `net8.0`; `net9.0`; `net10.0` |
| Public terminal dependency set | Approved `Icod.Terminal` types only; no `Icod.TermInfo` type |

The TermInfo 1.15.0 floor is temporary and required because Terminal 1.18.0 depends
on TermInfo 1.15.0. The direct DCurses reference remains only while the 1.6 renderer
is migrated in T2003-T2006; T2007 removes it and proves the production/package
boundary. TermInfo remains a legitimate transitive dependency behind Terminal.

## Approved public API delta

The compiled 2.0 development fingerprint is:

```text
75 exported types
559 canonical declared contract lines
sha256 1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65
```

The 75 exported type names are unchanged from 1.6. Reflection and recursive public
dependency guards accepted only these member/identity changes:

```text
- CursesSession.Terminal : Icod.TermInfo.TerminalDescription
+ CursesSession.Profile : Icod.Terminal.TerminalProfile
~ CursesSession.GetDimensions() generic result value -> TerminalDimensions
~ CursesSession.SynchronizeDimensions() generic result value -> TerminalDimensions
~ CursesLifecycleEvent.Dimensions -> TerminalDimensions?
~ AssemblyVersion 1.0.0.0 -> 2.0.0.0
```

No exported type, enum value, unrelated signature, nullability annotation, parameter
default, generic constraint, or ownership contract changed. The immutable 1.6
fingerprint remains 75 types, 559 lines, SHA-256
`266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458`.

## Behavioral qualification

Focused integration coverage verifies:

- `Profile` exposes the underlying Terminal-owned profile;
- positive dimensions are returned without conversion through TermInfo;
- unavailable, unsupported, and failed results preserve status, message, and native error;
- synchronization resizes/invalidates the logical screen only for an available result;
- failed synchronization leaves the existing logical screen dimensions unchanged;
- resize/resume lifecycle events carry nullable Terminal-owned dimensions; and
- existing session initialization ownership-transfer behavior remains covered.

The pre-baseline hardening head
[`3c745196d1f7698841d3e0ce3277011424f5968a`](https://github.com/uniblab/Icod.DCurses/commit/3c745196d1f7698841d3e0ce3277011424f5968a)
ran in [workflow 35389886165](https://github.com/uniblab/Icod.DCurses/actions/runs/35389886165).
Production built and package validation passed; every runtime lane reported only the
intentionally stale 1.6 active-fingerprint expectation. Each target framework had
910 passing tests and that single expected fingerprint failure. The observed hash
and counts were identical on every platform and became the reviewed 2.0 artifact.

## Exact-head workflow evidence

Workflow 35390788950 passed all seven required jobs:

| Job | Result |
|---|---|
| [Package candidate](https://github.com/uniblab/Icod.DCurses/actions/runs/35390788950/job/105748408267) | passed |
| [Runtime macOS ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35390788950/job/105748408544) | passed |
| [Runtime Windows x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35390788950/job/105748408575) | passed |
| [Runtime Linux x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35390788950/job/105748408607) | passed |
| [Runtime Linux ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35390788950/job/105748408726) | passed |
| [Runtime Windows ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35390788950/job/105748408731) | passed |
| [Runtime macOS x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35390788950/job/105748408837) | passed |

The Linux x64 job passed 912 tests on each of `net8.0`, `net9.0`, and `net10.0`
with zero failures and zero skips. The macOS ARM64 log independently reported the
same per-framework totals. Package candidate validation passed on the same head.

## Remaining migration debt

T2002 deliberately did not migrate the retained renderer. Production still has a
direct TermInfo 1.15.0 package reference and an internal `TerminalDescription` seam
used by capability interpretation, expansion, raw output, and cost selection.
T2003-T2006 replace those paths with Terminal-owned semantic screen operations and
transactions. T2007 removes the direct reference/shims and proves source, assembly,
NuGet metadata, sample, and package-consumer boundaries.

## Acceptance decision

Every T2002 criterion is satisfied:

- exact development/package/assembly identity;
- published Terminal 1.18.0 selected with the required temporary TermInfo floor;
- four approved public replacements and no additional API break;
- status/error and resize/lifecycle parity coverage;
- explicit 2.0 API fingerprint with immutable 1.x history;
- no public TermInfo dependency; and
- package validation plus the complete exact-head platform matrix.

T2003 may begin. T2004-T2011 remain ordered and pending. Any newly discovered
Terminal gap returns to the owning repository for a separately reviewed published
correction rather than reopening raw TermInfo or terminal-string access in DCurses.
