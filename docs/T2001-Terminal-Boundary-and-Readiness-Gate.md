# T2001 Terminal Boundary and Readiness Gate

**Date:** 2026-09-18.  
**DCurses target:** 2.0.0.  
**Published dependency under test:** `Icod.Terminal 1.17.0`.  
**Status:** **blocked — do not begin T2002**.  
**Owning correction:** Icod.Terminal, proposed additive minor release 1.18.0.

## Gate outcome

T2001 froze the complete direct-coupling inventory and approved public break manifest, then stopped at its first Terminal readiness witness. Terminal 1.17.0 has no public operation that can unconditionally establish Terminal's normalized default rendition when DCurses no longer knows the physical rendition.

This is required after startup uncertainty, explicit invalidation, resume and output failure. Supplying `TerminalScreenRendition.Default` to `PlanRenditionReset(current)` would falsely claim the current state is known and can produce a zero-byte default-to-default plan. Emitting `ExitAttributeMode`/`OriginalColorPair` through `WriteTerminalStringAsync`, borrowed output or transitive TermInfo access would violate the approved 2.0 boundary.

Therefore T2001 is blocked. T2002-T2011 remain pending and production code/package references/version metadata remain unchanged.

## Accepted inventory evidence

The accepted inventory head is [`01ce230289f930166a7be4cdd90a7ada4c5d94f1`](https://github.com/uniblab/Icod.DCurses/commit/01ce230289f930166a7be4cdd90a7ada4c5d94f1).

| Evidence | Result |
|---|---|
| `docs/2.0-TermInfo-Coupling-Inventory.md` | 16 production, 44 test, zero sample and three package/tool/automation paths classified |
| `docs/2.0-API-Break-Manifest.md` | four TermInfo-bearing public API replacements plus the 2.0 assembly identity frozen |
| [PR workflow run 35365089670](https://github.com/uniblab/Icod.DCurses/actions/runs/35365089670) | seven of seven jobs green: package candidate and Windows/Linux/macOS x64/ARM64 runtime jobs |
| Production/package identity | unchanged at DCurses 1.6.0, Terminal 1.15.0 and TermInfo 1.14.0 during T2001 |

A preceding plan-only run had one pre-existing allocation measurement exceed its Windows ARM64/net9 ceiling by 32 bytes while every other job/TFM passed. The exact inventory head reran the same Windows ARM64 job successfully, so no unrelated performance threshold was changed during this readiness work.

## Minimal failing package witness

The witness commit is [`269c76aa7da543998a5f48c0db32309b4484e1a6`](https://github.com/uniblab/Icod.DCurses/commit/269c76aa7da543998a5f48c0db32309b4484e1a6). It made two test-only changes:

1. selected published `Icod.Terminal 1.17.0` directly in the DCurses test project; and
2. created a real in-memory `TerminalSession` whose synthetic profile exposes `ExitAttributeMode = "<sgr0>"` and `OriginalColorPair = "<op>"`, then requested and committed:

```csharp
TerminalScreenOperationPlan plan = session.Screen.PlanRenditionBaseline()
	?? throw new InvalidOperationException(
		"The selected profile cannot establish a rendition baseline."
	);
```

The witness intentionally names the required semantic operation. It does not substitute a known default rendition, inspect raw capabilities for production behavior, or bypass the session transaction.

### RED result

| Item | Evidence |
|---|---|
| Workflow | [run 35365496840](https://github.com/uniblab/Icod.DCurses/actions/runs/35365496840) |
| Representative job | [Runtime Linux x64, job 105666835959](https://github.com/uniblab/Icod.DCurses/actions/runs/35365496840/job/105666835959) |
| Command | `dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true` after successful restore |
| Diagnostic | `CS1061: 'TerminalScreenPlanner' does not contain a definition for 'PlanRenditionBaseline'` |
| Target frameworks | the same sole diagnostic on `net8.0`, `net9.0` and `net10.0` |
| Package validation | failed at the same compile-time witness, not at restore, helper construction or an unrelated test |

The intentionally uncompilable test and its test-only package reference are removed from the active PR by a normal follow-up commit. The immutable witness commit and workflow retain the RED evidence. The test should be restored unchanged, except for selecting the published fixing version, when upstream work is available.

## Required Terminal contract

The smallest owning-repository correction is an additive method on `TerminalScreenPlanner`:

```csharp
public TerminalScreenOperationPlan? PlanRenditionBaseline();
```

Required semantics:

- represent an **unknown current physical rendition**, not a transition from a caller-supplied known state;
- unconditionally restore every rendition axis that Terminal's selected profile and planner can change to Terminal's normalized default;
- use Terminal-owned capability interpretation, expansion, padding and encoded-byte costing;
- include attribute reset and original-color restoration in safe order when both are required and available;
- return `null` when the profile exposes rendition state Terminal can enter but cannot safely restore from unknown state;
- return a valid zero-byte plan only when the selected profile exposes no rendition state the planner could have changed;
- remain side-effect free and session-owned like every other `TerminalScreenOperationPlan`; and
- permit output only by adding the plan to a transaction created by the same `TerminalSession`.

Minimum upstream tests should cover an `sgr0` + `op` profile with literal `<sgr0><op>` output, attribute-only and color-only profiles, an empty profile, unsafe non-restorable profiles, exact `ByteCount`/`AffectedLines`, side-effect-free planning, same-session transaction ownership and foreign-session rejection.

Adding this public method is a backward-compatible API addition but a SemVer minor change. The proposed minimum release is `Icod.Terminal 1.18.0`; it must not be hidden in a 1.17.x patch. DCurses must consume a published package, not an unpublished project reference or transitive implementation detail.

## Deferred T2001 work

The stop-at-first-blocker rule deliberately leaves these T2001 qualifications incomplete:

- retained rendition-baseline witness turning green against the published correction;
- transaction 65,536-item and 64 MiB payload boundaries;
- stale output-epoch rejection;
- semantic operation `ByteCount` and cursor/rendition cost parity;
- cleanup/synchronized-output conflicts;
- 1.6 behavioral/output/allocation baseline capture; and
- complete suite/package/platform acceptance on one exact T2001 head.

Source inspection of Terminal 1.17 is useful context but is not accepted package behavior for those deferred checks.

## Fixture exception and permanent boundary

The RED witness necessarily used `TerminalDescriptionBuilder` and TermInfo capability enums to construct a synthetic upstream profile because Terminal 1.17 has no Terminal-only public profile fixture builder. This is a reviewed test-bootstrap exception only. It does not permit TermInfo in production, samples, package smoke or current consumer guidance.

T2007 still must prove that the production assembly/public API/source/NuGet dependency groups are Terminal-only while allowing TermInfo's legitimate transitive presence through Terminal and any narrowly enumerated test fixture bootstrap.

## Resume criteria

Resume T2001 only after separately authorized Terminal work has:

1. accepted the exact contract and recovery semantics above (or a reviewed equivalent that does not require DCurses to claim a known state);
2. published the additive API in a minor Terminal release;
3. turned the retained DCurses package witness green with exact bytes through a same-session transaction; and
4. left no raw-output or direct TermInfo workaround in DCurses.

Then run the deferred readiness and behavioral witnesses. T2001 may be marked accepted only when all of them and the complete exact-head CI/package matrix pass together.
