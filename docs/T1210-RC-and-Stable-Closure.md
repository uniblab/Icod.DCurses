# T1210 — RC and Stable 1.2.0 Closure

**Release:** `Icod.DCurses 1.2.0`  
**Tranche:** T1210  
**Qualified T1209 closure head:** `3728bf0e576b32747dd3a628ed5d3eca768ac67f`  
**T1209 workflow:** #603 / `34525966166`  
**RC identity:** `1.2.0-rc.1`  
**AssemblyVersion:** `1.0.0.0`  
**Status:** RC exact-head qualification active  

## Entry gate

T1201–T1209 are complete. The documentation/sample/package-complete T1209 source at `3728bf0e576b32747dd3a628ed5d3eca768ac67f` passed workflow #603 / `34525966166` across all seven jobs.

That gate includes package verification, fresh package-only panel consumption, the focused panel sample, Windows/Linux/macOS x64/ARM64 runtime validation, and 554/554 tests on each of net8.0, net9.0, and net10.0 on the Linux ARM64 evidence leg with zero build warnings/errors.

## Frozen public contract

T1210 carries forward unchanged:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Exactly two exported types are added over the accepted 1.1 contract: `CursesPanel` and `CursesPanelTransparency`.

No source implementation, test behavior, sample behavior, package-smoke logic, Terminal/TermInfo dependency declaration, or AssemblyVersion change is permitted during ordinary RC/stable promotion.

## RC promotion

The sole release candidate is `1.2.0-rc.1`.

The RC promotion changes only release/package identity and current release-status documentation from the qualified T1209 tree. It does not alter the panel implementation or accepted API.

RC qualification requires the full PR matrix:

- package candidate validation;
- Linux x64 and ARM64;
- macOS x64 and ARM64;
- Windows x64 and ARM64;
- the existing net8.0/net9.0/net10.0 test and package-consumer gates.

The exact RC SHA/workflow will be recorded in PR #26 after the run completes. The branch is not moved merely to self-record its own SHA.

## Stable-source promotion

Only after the RC exact head is fully green may the same implementation/API be promoted to stable-source `1.2.0`.

Stable promotion changes only version/package/release-status documentation. The RC-to-stable compare must show no source/test/sample/package-smoke behavioral change and the API fingerprint must remain exactly unchanged.

The resulting stable-source exact head must itself pass the full seven-job PR matrix. That exact SHA/workflow is then recorded in PR #26 without another source commit.

## Release boundary

A green stable-source head completes repository-side 1.2 development qualification but does not itself perform any release action.

The following remain explicit separate actions:

- merging PR #26 to `main`;
- qualifying the resulting `main` Release build;
- creating/pushing a `v1.2.0` tag;
- creating a GitHub Release;
- publishing the NuGet package.

None are performed by T1210 without explicit maintainer direction.
