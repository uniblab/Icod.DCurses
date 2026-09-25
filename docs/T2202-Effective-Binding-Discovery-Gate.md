# T2202 — Effective-Binding Discovery Gate

**Status:** Accepted on exact executable head `1d6097d2d08c73bf403fa52d9cef017aef447633`\
**Source:** PR #34 against merged `v2.1.0`

## Recorded RED results

- On tests-only head `d8532485be3726984603381a294041735171a1da`, [workflow 36074699970](https://github.com/uniblab/Icod.DCurses/actions/runs/36074699970) Linux x64 job `107883318336` failed to compile because `CursesCommandBinding` and `GetEffectiveGestureBindings` did not exist (`CS0246` and `CS1061` on .NET 8/9/10).
- The first implementation head `79f9422e4f37ae9dbe85ed42ffb5edd373d9b7d1` built; [workflow 36075207298](https://github.com/uniblab/Icod.DCurses/actions/runs/36075207298) macOS ARM64 runtime job `107884756620` passed 1,231 of 1,232 tests per TFM. Its sole failure was the deliberately provisional 2.2 public API fingerprint. CI measured 97 exported types, 756 canonical contract lines and SHA-256 `06b8e57805004a8a7edc3ce787af86ca7e3d96c7ea7beecbd1ab541e744e1f2c` across .NET 8/9/10. The package candidate job passed.
- With the compiled 2.2 fingerprint recorded, test head `be34d3ecab6c8bc0f9494c66ffecffd3fefd3395` failed `DiscoveryOrderIsIndependentOfBindingRegistrationOrder`: [workflow 36075632438](https://github.com/uniblab/Icod.DCurses/actions/runs/36075632438) Linux ARM64 job `107886080301` reported actual `[Escape, Enter]` versus required `[Enter, Escape]` on .NET 8 and 9. The package candidate passed. `Dictionary` enumeration order is not guaranteed, so order now follows semantic gesture fields within each precedence owner.

## Contract under qualification

`CursesInteractionRouter.GetEffectiveGestureBindings()` returns a detached read-only snapshot of current region, eligible scope and global bindings. First owner wins on a duplicate gesture. Each owner is ordered by semantic key, scalar, modifiers, phase and function-key number. The path is opt-in and does not change ordinary `Route` or add terminal I/O. The source and package development version is `2.2.0-alpha.1`, assembly version remains `2.0.0.0`, and the only direct production dependency remains `Icod.Terminal 1.18.0`. The published 2.1 fingerprint is unchanged.

## Accepted GREEN and package qualification

[PR Staging workflow 36076001958](https://github.com/uniblab/Icod.DCurses/actions/runs/36076001958) passed all seven jobs: six runtime OS/architecture jobs and the package candidate. The Linux x64, Windows x64/ARM64 and macOS x64 runtime logs each report 1,233 passed, zero failed and zero skipped on .NET 8, 9 and 10. The macOS x64 test matrix remained sequential as agreed. The package job built `Icod.DCurses.2.2.0-alpha.1.nupkg` and `.snupkg`, ran fresh package-only consumers for all three TFMs and executed a live Linux pseudo-terminal refresh. Staging artifact `10839514617` has ZIP digest `sha256:3e8b7f45059468d77372af22e102385dbff126d50942a61e5eca9dcc30edff41` and records this exact source head.

The [2.2 development API baseline](Public-API-Baseline-2.2.md) records the additive type/member delta and measured fingerprint. No stable 2.2 release was produced; future T2203 work extends this development branch. The final 2.2 API freeze, RC and stable-source gates remain outstanding.
