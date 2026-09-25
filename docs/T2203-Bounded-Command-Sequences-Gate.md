# T2203 — Bounded Command Sequences Gate

**Status:** Accepted on exact executable head `2cdb89f3d0ef5b6cbe5a0d72018950c579ed319d`\
**Source:** Draft PR #34 against merged `v2.1.0`

## Recorded RED results

- Tests-only head `082985eb0731b229730baf475727afb8096e122f`
  produced the intended missing-contract RED in
  [workflow 36081223791](https://github.com/uniblab/Icod.DCurses/actions/runs/36081223791).
  Linux ARM64 job `107903402756` failed on .NET 8/9/10 only for the
  absent T2203 types, limits and methods (`CS0246`, `CS0117`, `CS0103` and
  `CS1061`).
- Routing tests at `c8c32df2836e7c341662b434b95384824e813f22`
  failed the seven intended provisional behaviors in
  [workflow 36082465377](https://github.com/uniblab/Icod.DCurses/actions/runs/36082465377):
  region/scope precedence, higher single-key shadowing, shared-prefix
  completion, mismatch replay, non-keyboard mismatch and semantic matching.
- Context tests at `d278a40aded5423a384dd77a2fabd27d54204dd8`
  failed the intended eight focus, scope, eligibility, panel, resize,
  disposal and lazy-repair cases in
  [workflow 36083364014](https://github.com/uniblab/Icod.DCurses/actions/runs/36083364014).
  Binding-mutation and disposed-router guards already passed.
- Discovery head `5849f94f829ad10c268792dd3bb6ae3f491ade7f`
  passed all 1,267 functional tests per target in
  [workflow 36084187210](https://github.com/uniblab/Icod.DCurses/actions/runs/36084187210).
  Its sole failure was the deliberately old development fingerprint, which
  measured identically on .NET 8/9/10.

## Accepted contract

Regions, explicit scopes and the router global owner can register immutable
two-through-eight-gesture command sequences. Registration rejects same-owner
single-key/first-key conflicts, duplicates and proper-prefix ambiguity before
mutation. Per-owner and router-total capacities are independent of existing
single-key limits.

`ProcessCommandSequence` applies focused-region, eligible-scope and global
first-owner precedence without changing ordinary `Route`. Results explicitly
report fallback, pending, completion or mismatch; a mismatch routes its current
event exactly once and never restarts it as another sequence. Applications own
command execution and decide when to cancel a prefix. Focus, scope,
eligibility, panel, resize, binding and disposal changes invalidate pending
state without a timer or callback.

Effective sequence discovery uses the same union precedence as processing,
including higher single-key bindings that hide lower sequences. Every returned
snapshot and nested gesture list is detached, read-only and deterministically
ordered by owner and semantic gesture identity.

## Public API and compatibility

The mutable 2.2 development fingerprint is now:

```text
100 exported types
783 canonical declared contract lines
sha256 7c9866abaeeacc7f64631d2a800b91333cee72ad1a53872896e8f4d8c7ccb097
```

This is a T2203 delta of three exported types and 27 contract lines over the
accepted T2202 increment, and an overall 2.2 delta of four types and 32 lines
over published 2.1. The immutable 2.1 fingerprint remains 96 types, 751 lines
and SHA-256 `c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79`.
Version and PackageVersion remain `2.2.0-alpha.1`, AssemblyVersion remains
`2.0.0.0`, and the sole direct production dependency remains
`Icod.Terminal 1.18.0`.

## Accepted GREEN and package qualification

[PR Staging workflow 36084559207](https://github.com/uniblab/Icod.DCurses/actions/runs/36084559207)
passed all seven jobs: six runtime OS/architecture jobs and the package
candidate. Representative Linux x64, Windows x64 and intentionally sequential
macOS x64 logs each report 1,268 passed, zero failed and zero skipped on .NET
8, 9 and 10.

The package job built `Icod.DCurses.2.2.0-alpha.1.nupkg` and `.snupkg`, ran
fresh package-only consumers for all three TFMs and completed the live Linux
pseudo-terminal refresh. Staging artifact `10843144268` has ZIP digest
`sha256:6dc68baa4095dacb069e4d350c8999b1dde9ada496e5aba491e70354c55ed7e2`
and records the exact accepted source head. Direct inspection of its nuspec
confirmed only `Icod.Terminal 1.18.0` for net8.0, net9.0 and net10.0.

## Disposition

T2203 is accepted. T2204 small prompt state and T2205 caller-fed timing helpers
remain deferred: the existing samples do not provide the second concrete
application witness needed to justify either core API. T2206 may now plan
public-only editor and roguelike integration of the accepted discovery and
sequence mechanisms. T2207 still owns the final 2.2 API freeze, and T2208 owns
RC/stable-source release closure.

PR #34 remains draft and unmerged. No tag was created and no package was
published. PR validation used Staging only; Release validation remains reserved
for a separately authorized push to `main`.
