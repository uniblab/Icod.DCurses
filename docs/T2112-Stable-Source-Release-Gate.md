# T2112 Stable-Source Release Gate

**Status:** accepted, seven of seven Staging PR jobs successful\
**Executable stable-source head:** `5864231632f45410410d27c956f596c88147ca73`\
**PR workflow:** [36047805573](https://github.com/uniblab/Icod.DCurses/actions/runs/36047805573)

## Candidate identity and boundary

`Version` and `PackageVersion` are `2.1.0` on the open PR. This is an unpublished stable-source candidate, not a published package. `AssemblyVersion` remains `2.0.0.0`; the only direct production dependency remains `Icod.Terminal 1.18.0`; the targets remain .NET 8/9/10. The 2.1 public API is the T2111 accepted 96-type, 751-contract-line fingerprint with SHA-256 `c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79`. No production behavior or public API changed between the qualified RC and this source candidate.

The [RC gate](T2112-RC-Qualification.md) records seven of seven Staging PR jobs, exact-head package provenance, 1,223 tests per TFM and artifact digests. The PR workflow runs Staging on six OS/architecture targets plus one package job; the existing `main` push workflow runs Release only. Release validation is pending a separately authorized merge and push to `main`.

## Candidate verification

All six Staging runtime jobs and the Staging package job passed. Artifact `10828758372` has ZIP SHA-256 `c3fe888f43296ba12056d7b9fcacd3c3a112cc625e123d9c8e943b3a8bbb3dcf`, `.nupkg` SHA-256 `7353c68ea78632c1256f5ae8afd6584abd51e3569dd8f204b7c3e2697eafce7d`, and `.snupkg` SHA-256 `29c1c2e952902cf6c499350c98b5554b20c46891d35cea5cf3b3cacf42698d3f`. Direct nuspec inspection found version `2.1.0`, repository commit `5864231632f45410410d27c956f596c88147ca73` and only direct `Icod.Terminal 1.18.0` in all three dependency groups. Its package log reports zero build warnings and fresh consumers on each TFM plus a live pseudo-terminal refresh. The Linux x64 and macOS x64 Staging runtime jobs each report 1,223 passing tests per TFM with zero failures/skips and zero build warnings. The interactive editor and roguelike samples have headless CI tests and runnable instructions; this environment has no local `dotnet` installation or live manual terminal run.

## Maintainer handoff

Keep PR #33 open and unmerged. This evidence-only documentation commit requires its own exact-head seven-job Staging PR run; record that final head and result in the PR description. Review and merge separately if authorized; the resulting `main` push must pass its Release-only workflow before choosing a tag, GitHub Release or NuGet publication. None of those external release actions has occurred.
