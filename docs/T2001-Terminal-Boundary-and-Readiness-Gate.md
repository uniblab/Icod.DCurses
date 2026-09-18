# T2001 Terminal Boundary and Readiness Gate

**Date:** 2026-09-18.\
**DCurses target:** 2.0.0.\
**Published dependency qualified:** `Icod.Terminal 1.18.0`.\
**Status:** **accepted — T2002 may begin**.

## Gate outcome

T2001 is accepted. The direct-coupling inventory and approved public break manifest
are frozen, the original Terminal 1.17 unknown-rendition blocker was corrected in
the owning repository and published in Terminal 1.18.0, and downstream package
witnesses now qualify the complete boundary required before renderer migration.

Production DCurses remains 1.6.0 during this gate. Its production references and
assembly identity are unchanged. T2002 is authorized only for the approved 2.0
identity and public profile/dimensions cutover; this acceptance does not authorize
later renderer, transaction, dependency-removal, merge, tag, or publication work.

## Frozen inventory and break contract

The accepted inventory head is
[`01ce230289f930166a7be4cdd90a7ada4c5d94f1`](https://github.com/uniblab/Icod.DCurses/commit/01ce230289f930166a7be4cdd90a7ada4c5d94f1).

| Evidence | Accepted result |
|---|---|
| `docs/2.0-TermInfo-Coupling-Inventory.md` | 16 production, 44 test, zero sample, and three package/tool/automation paths classified |
| `docs/2.0-API-Break-Manifest.md` | Four TermInfo-bearing public API replacements plus the 2.0 assembly identity frozen |
| [Workflow 35365089670](https://github.com/uniblab/Icod.DCurses/actions/runs/35365089670) | Seven of seven jobs green on the inventory head |
| Production/package identity | DCurses 1.6.0, Terminal 1.15.0, TermInfo 1.14.0, AssemblyVersion 1.0.0.0 throughout T2001 |

The only allowed direct TermInfo use during readiness is the explicit test-fixture
bootstrap described below. Production, samples, package smoke, and current consumer
guidance receive no exception.

## Original RED witness and owning correction

The immutable RED witness is
[`269c76aa7da543998a5f48c0db32309b4484e1a6`](https://github.com/uniblab/Icod.DCurses/commit/269c76aa7da543998a5f48c0db32309b4484e1a6),
qualified by [workflow 35365496840](https://github.com/uniblab/Icod.DCurses/actions/runs/35365496840).
Against published Terminal 1.17.0, all target frameworks failed with `CS1061`
because `TerminalScreenPlanner.PlanRenditionBaseline()` did not exist.

That failure established the smallest safe upstream contract:

```csharp
public TerminalScreenOperationPlan? PlanRenditionBaseline();
```

Terminal 1.18.0 now owns that operation. It represents unknown physical rendition,
restores every reachable attribute/color axis to Terminal's normalized default,
uses Terminal-owned expansion/padding/cost semantics, remains side-effect free, and
can be emitted only through a transaction from the same session. DCurses does not
claim that unknown state is `TerminalScreenRendition.Default` and does not emit raw
reset strings.

## Published-package readiness evidence

The retained downstream witness first passed on exact head
[`460dfa2cc4ef548f5db7edfd343f49717e59d3f3`](https://github.com/uniblab/Icod.DCurses/commit/460dfa2cc4ef548f5db7edfd343f49717e59d3f3)
in [workflow 35386756114](https://github.com/uniblab/Icod.DCurses/actions/runs/35386756114).
The initial macOS ARM64 restore observed NuGet propagation lag and could see only
Terminal 1.17.1; rerunning that job after indexing completed passed without a source
change. All seven jobs then passed.

The complete readiness class passed on exact head
[`27606525ebee6149dfa18f8fd1ca0b11a03b7326`](https://github.com/uniblab/Icod.DCurses/commit/27606525ebee6149dfa18f8fd1ca0b11a03b7326)
in [workflow 35387480973](https://github.com/uniblab/Icod.DCurses/actions/runs/35387480973).

| Witness | Published behavior qualified |
|---|---|
| Unknown rendition | A real session plans and commits literal `<sgr0><op>` through a same-session transaction. |
| Item capacity | 65,536 one-byte items are retained; the 65,537th is rejected before any output. |
| Payload capacity | Exactly 64 MiB of application text is retained; the next byte is rejected before any output. |
| Stale epoch | Intervening session output leaves only `outside`; stale transaction commit throws and cannot replay `transaction`. |
| Semantic cost | A hand-costed cursor move reports five bytes and emits literal `ddrrr`, beating the seven-byte absolute candidate. |
| Framing conflict | An active synchronized-output owner rejects a framed transaction without adding bytes; disposal and a later framed transaction recover normally. |

The Linux x64 job passed 905 tests on each of `net8.0`, `net9.0`, and `net10.0`.
The package candidate and Windows/Linux/macOS x64/ARM64 runtime jobs all passed.

## DCurses 1.6 behavioral baseline

`docs/T2001-DCurses-1.6-Behavioral-Baseline.md` freezes the migration witnesses for:

- full, sparse, and no-op repaint;
- cursor placement, rendition minimization, ACS/Unicode fallback, and erase/shift plans;
- hyperlinks, raster placeholders, synchronized mixed-media framing, and failures;
- resize, suspend/resume, panels, pads/viewports, and large surfaces; and
- deterministic bytes/writes/flushes plus retained-data and allocation ceilings.

The existing suite remains the executable authority. The new baseline document does
not replace its literal assertions or introduce timing-only CI thresholds.

## Fixture exception and permanent boundary

`TerminalScreenReadinessTests` directly references `Icod.TermInfo 1.15.0` with
`PrivateAssets="all"` solely to construct synthetic `TerminalDescription` profiles.
This is a reviewed T2001 test-bootstrap exception because Terminal does not expose a
Terminal-only public profile fixture builder. The witness directly selects published
`Icod.Terminal 1.18.0`; it uses no project reference or transitive-version workaround.

T2007 must still prove that the production assembly, public API, source, samples,
package dependency groups, and fresh consumers are Terminal-only. TermInfo remains a
legitimate transitive implementation dependency through Terminal and may remain in
restore assets; the 2.0 claim is removal of DCurses's direct coupling, not removal of
TermInfo from the application graph.

## Acceptance decision

Every T2001 criterion is satisfied:

- complete inventory and approved break manifest;
- published safe unknown-rendition recovery;
- transaction item/payload limits and stale-epoch behavior;
- semantic operation cost/output parity and synchronized-owner conflict safety;
- explicit fixture-only legacy exception;
- frozen 1.6 behavioral/output/allocation baseline; and
- package validation plus the complete required platform matrix.

T2002 may begin. T2003-T2011 remain ordered and pending, and any newly discovered
Terminal gap returns to the owning repository for a separately reviewed published
correction rather than reopening raw TermInfo or terminal-string access in DCurses.
