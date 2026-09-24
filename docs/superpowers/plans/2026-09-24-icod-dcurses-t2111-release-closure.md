# T2111 Adversarial, Package, Documentation and API Freeze Plan

**Status:** accepted at executable head `f0b69470cace0bfe29cd3db311ac156a4a50deac`, 14/14 jobs in workflow 36044565032.\
**Authority:** `Icod.DCurses-2.1.0-Development-Roadmap.md`, T2111.\
**Branch:** `2.1.0-roadmap` in open PR #33. Do not merge, tag or publish.

## Scope

Close the 2.1 public surface after the two application samples, review package and consumer evidence, and write accurate release documentation. Keep the 2.0 baseline and prior releases immutable. T2111 leaves the package at its development identity; T2112 promotes RC and stable-source candidates.

## Sequence

1. **Adversarial inventory.** Map permanent capacity, overflow, malformed Unicode, boundary geometry, cancellation, failure, resize and allocation tests to each 2.1 API. Add focused tests only where a concrete gap survives review. Examine sample-driven behavior and public API regret before freeze.
2. **API freeze.** Compare the current compiled `Public-API-Fingerprint-2.1.json` with the frozen 2.0 fingerprint, identify only the approved additive public types/members, record exported type and contract counts and hash in `Public-API-Baseline-2.1.md`. Confirm assembly identity stays `2.0.0.0`, `Icod.Terminal 1.18.0` is the sole direct production dependency, and no application policy leaked into DCurses.
3. **Documentation.** Add a 2.1 development section to the root README without implying stable publication; update the sample index and changelog, package description/release notes, API descriptions and links. Review generated XML documentation through package validation. Explain the editor's fixed-record policy and roguelike's generated world clearly.
4. **Package and consumer.** Run solution tests, package structure/license/dependency checks and fresh package-only consumers on .NET 8/9/10 in the exact-head CI matrix. Inspect any failures and correct them before a gate claim; retain artifact provenance for T2112.
5. **Gate.** Record exact executable head, workflow, 14 job results, source and package evidence, review decisions and any manual-terminal limits. Then plan T2112 RC/stable-source promotion. PR #33 remains open and unmerged.

## Acceptance

No unresolved public API regret or coverage gap; 2.1 fingerprint and baseline agree with the compiled public assembly; package-only consumer and Staging/Release platform matrix pass at the same executable head; root/package/readme/changelog descriptions accurately distinguish development from stable release.

See [the T2111 gate](../../T2111-Adversarial-Package-Documentation-and-API-Gate.md) for permanent evidence and artifact digests.
