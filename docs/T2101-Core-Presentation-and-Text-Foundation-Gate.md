# T2101 Core Presentation and Text Foundation Gate

**Tranche:** T2101  
**Witness status:** complete  
**Foundation status:** accepted\
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
| active implementation-equivalent head green without production/package changes | exact head `4bff3bf96f0bf5c67706df872e4d4ff7483851f1`, workflow 35911150258 | Met: 14/14 jobs green |

No file under `src/`, no package identity, no production dependency, and no published API fingerprint changes in T2101.

## Allocation-harness diagnosis and correction

[Workflow 35906986670](https://github.com/uniblab/Icod.DCurses/actions/runs/35906986670) initially left 13 jobs successful after targeted reruns. The remaining macOS ARM64 Staging job, and a Windows 10 local run supplied by the maintainer, showed that nominally allocation-free geometry and focus operations were being measured while independent test hosts and xUnit collections executed concurrently.

Starting with the .NET 9 SDK, `dotnet test` runs a multi-targeted project's TFMs in parallel by default. xUnit also runs test collections in parallel by default. Those two layers of concurrency delayed steady-state runtime/JIT stabilization and allowed runtime bookkeeping to enter the current-thread allocation samples. Raising the byte ceilings had previously reduced but not eliminated the symptom.

The correction sets `TestTfmsInParallel` to `false` in the test project and places the three affected steady-state allocation fixtures in one xUnit collection with `DisableParallelization = true`. This changes only test scheduling. It does not change any measured ceiling, iteration count, production source, package identity, public API, or dependency.

[Workflow 35911150258](https://github.com/uniblab/Icod.DCurses/actions/runs/35911150258) passed all 14 package/runtime jobs on corrective exact head [`4bff3bf96f0bf5c67706df872e4d4ff7483851f1`](https://github.com/uniblab/Icod.DCurses/commit/4bff3bf96f0bf5c67706df872e4d4ff7483851f1). T2101 is accepted and T2102 is authorized.
