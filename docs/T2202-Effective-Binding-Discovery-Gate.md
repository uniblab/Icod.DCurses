# T2202 — Effective-Binding Discovery Gate

**Status:** GREEN qualification pending\
**Source:** PR #34 against merged `v2.1.0`

## Recorded RED results

- On tests-only head `d8532485be3726984603381a294041735171a1da`, [workflow 36074699970](https://github.com/uniblab/Icod.DCurses/actions/runs/36074699970) Linux x64 job `107883318336` failed to compile because `CursesCommandBinding` and `GetEffectiveGestureBindings` did not exist (`CS0246` and `CS1061` on .NET 8/9/10).
- The first implementation head `79f9422e4f37ae9dbe85ed42ffb5edd373d9b7d1` built; [workflow 36075207298](https://github.com/uniblab/Icod.DCurses/actions/runs/36075207298) macOS ARM64 runtime job `107884756620` passed 1,231 of 1,232 tests per TFM. Its sole failure was the deliberately provisional 2.2 public API fingerprint. CI measured 97 exported types, 756 canonical contract lines and SHA-256 `06b8e57805004a8a7edc3ce787af86ca7e3d96c7ea7beecbd1ab541e744e1f2c` across .NET 8/9/10. The package candidate job passed.
- With the compiled 2.2 fingerprint recorded, test head `be34d3ecab6c8bc0f9494c66ffecffd3fefd3395` failed `DiscoveryOrderIsIndependentOfBindingRegistrationOrder`: [workflow 36075632438](https://github.com/uniblab/Icod.DCurses/actions/runs/36075632438) Linux ARM64 job `107886080301` reported actual `[Escape, Enter]` versus required `[Enter, Escape]` on .NET 8 and 9. The package candidate passed. `Dictionary` enumeration order is not guaranteed, so order now follows semantic gesture fields within each precedence owner.

## Contract under qualification

`CursesInteractionRouter.GetEffectiveGestureBindings()` returns a detached read-only snapshot of current region, eligible scope and global bindings. First owner wins on a duplicate gesture. Each owner is ordered by semantic key, scalar, modifiers, phase and function-key number. The path is opt-in and does not change ordinary `Route` or add terminal I/O. The source and package development version is `2.2.0-alpha.1`, assembly version remains `2.0.0.0`, and the only direct production dependency remains `Icod.Terminal 1.18.0`. The published 2.1 fingerprint is unchanged.

## Remaining acceptance

Wait for one exact implementation head to pass the seven PR Staging jobs (six platform/architecture runtime jobs and one package candidate) with zero test failures on .NET 8/9/10. Verify source commit and package artifact provenance, then record the run, counts and additive API delta in this gate and `docs/Public-API-Baseline-2.2.md`. Do not mark T2202 accepted from a pending or partially passed matrix.
