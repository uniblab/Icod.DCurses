# T2101 Core Presentation and Text Foundation Gate

**Tranche:** T2101  
**Witness status:** complete  
**Foundation status:** blocked; one pre-existing allocation-noise matrix job remains red\
**Branch:** `2.1.0-roadmap`

## Intentional RED witness

The accepted first missing 2.1 contract is:

```csharp
public readonly record struct CursesTextPosition {
	public CursesTextPosition( int offset );
	public int Offset { get; }
}

public sealed record CursesTextSpan {
	public CursesTextSpan(
		CursesTextPosition start,
		int length,
		CursesStyle style,
		CursesCellMetadata? metadata = null
	);

	public CursesTextPosition Start { get; }
	public int Length { get; }
	public CursesTextPosition End { get; }
	public CursesStyle Style { get; }
	public CursesCellMetadata? Metadata { get; }
}
```

Commit [`ecc4c6b1808d656d48c78395957f0c5e14cd61b2`](https://github.com/uniblab/Icod.DCurses/commit/ecc4c6b1808d656d48c78395957f0c5e14cd61b2) added only `T2101CorePresentationRedWitnessTests.cs`. It constructed those two types and asserted their accepted initial values. It added no production source, stub, package change, or dependency.

[Workflow 35905155613](https://github.com/uniblab/Icod.DCurses/actions/runs/35905155613) completed with all 14 matrix/package jobs failing at compilation as intended. Restore succeeded. The representative [Staging Linux x64 job 107331166216](https://github.com/uniblab/Icod.DCurses/actions/runs/35905155613/job/107331166216) reported the same diagnostics on `net8.0`, `net9.0`, and `net10.0`:

```text
T2101CorePresentationRedWitnessTests.cs(8,3): error CS0246: The type or namespace name 'CursesTextPosition' could not be found
T2101CorePresentationRedWitnessTests.cs(9,3): error CS0246: The type or namespace name 'CursesTextSpan' could not be found
```

Inspection of the Windows Staging and both package-validation logs found the same two missing symbols and no different compiler error. This demonstrates that the first T2102 public contract is absent from 2.0 rather than already implemented.

## Reversion

Commit [`e24c93f1cdf0452dfd14aaf1f386d6ae0321ac6e`](https://github.com/uniblab/Icod.DCurses/commit/e24c93f1cdf0452dfd14aaf1f386d6ae0321ac6e) normally reverted the witness. The active branch therefore no longer contains the uncompilable test. The failed exact-head workflow remains durable evidence, while T2102 owns the production implementation and permanent behavioral tests.

## Foundation evidence

The T2101 decision is based on these artifacts:

- [`T2101-2.0-Core-Presentation-Baseline.md`](T2101-2.0-Core-Presentation-Baseline.md) for published identity, dependency, behavior, and workload evidence;
- [`2.1-Core-Presentation-and-Text-API-Design.md`](2.1-Core-Presentation-and-Text-API-Design.md) for the accepted T2102-T2108 signatures and semantics;
- [`2026-09-23-icod-dcurses-2.1-core-presentation-text.md`](superpowers/plans/2026-09-23-icod-dcurses-2.1-core-presentation-text.md) for the T2102-T2108 test-first implementation sequence;
- an ordinary exact-head PR workflow after the witness reversion and final roadmap updates.

## Acceptance evaluation

| Criterion | Evidence | Result |
|---|---|---|
| published 2.0 identity and dependency boundary frozen | baseline document, compiled identity/dependency tests, immutable public fingerprint | Met |
| editor, roguelike, pad, text-width, and layout-helper baselines recorded | T2101 workload tests and baseline tables | Met |
| exact public signatures and edge semantics reviewed | accepted 2.1 API design | Met |
| Terminal 1.18.0 sufficient | design boundary review; no new live-terminal contract | Met |
| first missing contract proved RED and reverted | commits `ecc4c6b` and `e24c93f`; workflow 35905155613 | Met |
| T2102-T2108 implementation sequence reviewed | accepted implementation plan | Met |
| active post-roadmap head green without production/package changes | exact head `3b8213142ddee9abdbadc4abb37b2c5170cbe3ae`, workflow 35906986670 | Blocked: 13/14 jobs green |

No file under `src/`, no package identity, no production dependency, and no published API fingerprint changes in T2101.

## Blocking evidence and corrective action

[Workflow 35906986670](https://github.com/uniblab/Icod.DCurses/actions/runs/35906986670) leaves 13 jobs successful after targeted reruns. The remaining [macOS ARM64 Staging job](https://github.com/uniblab/Icod.DCurses/actions/runs/35906986670/job/107344831063) fails only these previously documented allocation-noise tests:

- `CursesInteractionPerformanceHardeningTests.FocusTraversalStaysWithinMeasurementNoiseFloor`, with observed values 3,328 and 3,472 against 0-1,024;
- `CursesInteraction15PerformanceTests.SpatialFocusIsAllocationFreeApartFromMeasurementNoise`, with observed value 1,488 against 0-1,024.

The new T2101 tests, package jobs, and the other runtime jobs are green. Repeated isolated reruns on the unchanged SHA have produced the same class of noise-only failure; no T2101 file is named in the diagnostics.

The smallest corrective action is either a successful unchanged rerun of that one job or an explicit maintainer exception for these known noise-floor tests. The user chose to keep the tests unchanged, so this gate does not alter their thresholds. Until one of those actions occurs, T2101 is blocked and T2102 is not authorized.
