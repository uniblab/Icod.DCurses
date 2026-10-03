# T2309 Adversarial, Package, Documentation and API Gate

## Frozen candidate

| Item | Candidate |
| --- | --- |
| package/version | `Icod.DCurses 2.3.0-alpha.1` |
| assembly version | `2.0.0.0` |
| direct runtime dependency | exactly `Icod.Terminal 1.24.0` |
| direct TermInfo dependency | none |
| exported types | 105 |
| canonical contract lines | 838 |
| API SHA-256 | `86603abe361c3a54e3b85fd2f0321e319bfb6fe11ff4abcaeb99151621827932` |

The delta over published 2.2 is additive and limited to the five atlas types and three
host methods recorded in `Public-API-Baseline-2.3.md`. No new feature family may enter
after this gate without deliberately reopening the fingerprint and roadmap decision.

## Package and consumer checks

The repository package validator must prove that the `.nupkg` contains the README,
license, icon, three TFM assets, repository/source-link metadata and the matching symbol
package. Its nuspec/deps/lock inspection must show the one direct `Icod.Terminal 1.24.0`
runtime dependency and no direct Icod.TermInfo dependency.

The fresh NuGet-only consumer compiles and executes for net8.0, net9.0 and net10.0. Its
raster smoke constructs the 2.3 geometry/update values and reflects the public geometry,
creation and projection methods from the packed assembly. The Linux package lane also
runs the existing live pseudo-terminal refresh; that validates package consumption and
terminal restoration, not live raster rendering.

## Documentation checks

- The root README defines ownership, dependency, bounds, presentation, controlled vs
  ambiguous failure, fallback, resize and recreation.
- The sample index and sample-specific README document `--text`, expected capability
  behavior, application-owned pixels, controls and live limitations.
- `Raster-Atlas-Measurement-2.3.md` records the deterministic workload and allocation
  method without a non-portable elapsed-time gate.
- XML summaries describe the limits and conservative acknowledgement semantics; the
  compiler-generated XML file remains part of all package assets.

## Gate state

Source and documentation are frozen for exact-head qualification. Acceptance requires
the package candidate and all six OS/architecture runtime jobs to pass unchanged. Live
raster and explicit fallback observations remain the separate T2308/T2310 maintainer
checklist and must not be inferred from CI.
