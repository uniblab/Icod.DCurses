# T1210 — RC and Stable 1.2.0 Closure

**Release:** `Icod.DCurses 1.2.0`  
**Tranche:** T1210  
**Qualified T1209 closure head:** `3728bf0e576b32747dd3a628ed5d3eca768ac67f`  
**T1209 workflow:** #603 / `34525966166`  
**Qualified RC head:** `8c5d329fa195685c0349068ce33a100aaf9eb0a3`  
**RC workflow:** #604 / `34526810086`  
**Stable-source identity:** `1.2.0`  
**AssemblyVersion:** `1.0.0.0`  
**Status:** stable-source exact-head qualification active  

## Entry gate

T1201–T1209 are complete. The T1209 documentation/sample/package-complete source `3728bf0e576b32747dd3a628ed5d3eca768ac67f` passed workflow #603 / `34525966166` across all seven jobs.

## Frozen public contract

T1210 carries forward unchanged:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Exactly two exported types are added over 1.1: `CursesPanel` and `CursesPanelTransparency`.

## Release candidate qualification

The sole release candidate `1.2.0-rc.1` was promoted with release/package/status documentation changes only.

Exact RC head:

```text
8c5d329fa195685c0349068ce33a100aaf9eb0a3
workflow #604 / 34526810086
```

passed all seven PR jobs: package candidate plus Windows/Linux/macOS x64/ARM64 runtime validation. The Linux ARM64 evidence leg built with zero warnings/errors and passed 554/554 tests on each of net8.0, net9.0, and net10.0. The API fingerprint remained unchanged.

## Stable-source promotion

The implementation/API from the qualified RC has now been promoted to source identity `1.2.0`.

The RC-to-stable change is limited to version/package identity and current release-status documentation. It must not alter `src/`, tests, sample behavior, package-smoke behavior, dependencies, or `AssemblyVersion`.

Stable-source qualification requires the same full seven-job PR matrix. The resulting exact stable-source SHA/workflow will be recorded in PR #26 without making another source commit merely to record its own SHA.

## Completion rule

T1210 is complete only when the stable-source exact head passes package validation and all six runtime jobs with the API fingerprint still exactly:

```text
4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

A green stable-source head completes repository-side 1.2 development qualification but does not itself perform a release action.

The following remain explicit separate actions:

- merge PR #26 to `main`;
- qualify the resulting `main` Release build;
- create/push tag `v1.2.0`;
- create a GitHub Release;
- publish the NuGet package.
