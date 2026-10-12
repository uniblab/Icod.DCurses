# T2506 — 2.5 color-surface contract freeze

**Candidate:** PR #38, `2.5.0-alpha.1`; pre-RC, not tagged or published.
**Environment:** Linux x64, .NET SDK/runtime 10.0.12, with `DOTNET_ROLL_FORWARD=Major`
for net8/net9 test execution; local Terminal dependency `1.29.0`. These are
automated results, not a live macOS/Windows terminal visual observation.

## Refresh cost and adversarial review

`CursesColorSurfaceRefreshTests.BoundedColorRefreshWorkloadsRemainSparse` runs
32 iterations per scenario with an 8-color synthetic capability profile. The
following Release/net10.0 observation is a single non-gating measurement;
wall-clock time and thread allocation vary by host, JIT and scheduling.

| Scenario | Iterations | Elapsed ms | Thread allocated bytes | Writes | Output bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| unchanged two-color frame | 32 | 0.605 | 0 | 0 | 0 |
| one foreground-only cell | 32 | 6.825 | 535,984 | 222 | 1,206 |
| one background-only cell | 32 | 4.116 | 537,600 | 224 | 1,216 |
| one-cell colored opaque panel move | 32 | 176.885 | 4,397,832 | not collected | 1,337 |

The baseline contract is zero cell/rendition writes for an unchanged frame;
the existing ASCII/attribute byte baselines remain in
`CursesRefreshCostBaselineTests`. There was no comparable 2.4 panel-move
measurement on this fixture, so the new colored workload is an acceptance
baseline rather than a claim of regression improvement. The durable budgets
are 0 bytes/writes on unchanged frames, at most 128 output bytes per style-only
cell change, and at most 256 output bytes per bounded panel move. Elapsed and
allocation figures are observation only, not flaky CI thresholds. The sample
surface is small; the 176 ms total for repeated session-level panel refreshes
does not establish a large-screen performance guarantee.

The new 32-cycle tiny-screen test changes both axes and repeatedly clips a
width-two glyph at the right edge, resizes and invalidates the physical frame,
then verifies style and footprint. T2501–T2504 cover clipped composition,
blank-transparent/opaque distinctions, unsupported axes, unsafe RGB, absent
default restoration, and lifecycle reassertion. Existing
`CursesRefreshDiagnosticsTests`, `CursesRefreshPublicationHardeningTests`, and
`CursesRefreshSynchronizationHardeningTests` cover cancellation and partial
output failure with conservative physical-state invalidation; the full suites
ran in this gate. No production correction or new public API was necessary.

## API, documentation, and package

`Public-API-Fingerprint-2.5.json` has the same canonical SHA-256
`87a3ffe66174634f6db4044b914b05fd31d117b93ecf5b1a151d31d59732e65e`,
109 types and 857 contract lines as immutable 2.4. The test compares the
complete exported-type list and all counts/hash; the compiled assembly also
continues to pass the published 2.4 fingerprint test on all target frameworks.
`AssemblyVersion` stays `2.0.0.0`, sole direct runtime dependency stays
Icod.Terminal 1.29.0, and there is no direct production TermInfo dependency.

The packaged README uses full repository URLs for outgoing documentation,
includes a runnable color sample command, and distinguishes requested styles
from terminal capabilities and actual host appearance. Release notes and tags
describe the measured qualification, not a new styling API. New test C# files
have GPL-3.0-or-later 2026 notices; the color sample C# and csproj carry the
same GPL notice. Library project remains LGPL-3.0-or-later.

## Local gate

- Forced solution restore with `-m:1 -p:NuGetAudit=false`: passed; this
  sandbox cannot write the default NuGet audit HTTP cache, so audit suppression
  is a local execution workaround, not a project change.
- Full Staging tests: 1,464/1,464 on each net8.0, net9.0, net10.0.
- Full Staging solution build: zero warnings and errors, including all samples.
- Full Release solution build: zero warnings and errors, including all samples.
- Full Release tests: 1,464/1,464 on each net8.0, net9.0, net10.0.
- Release `dotnet pack` with source/symbols: candidate `.nupkg` and `.snupkg`.
- `verify-release-package.sh` on Release artifacts: package metadata,
  dependency closure, XML docs, portable symbols, isolated NuGet-only consumer
  on net8/9/10, and Linux pseudo-terminal smoke passed.
- `build.sh`/`build.cmd` cannot execute in this Linux sandbox without
  PowerShell (`pwsh`/`powershell` absent); their equivalent .NET phases above
  were run directly. Windows and macOS jobs and live terminal observation
  remain for T2507 CI/maintainer evidence; they are not recorded as passes.

Any subsequent runtime/API change reopens this freeze gate. Maintainer alone
merges, tags, and publishes.
