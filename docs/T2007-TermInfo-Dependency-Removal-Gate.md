# T2007 TermInfo Dependency Removal Gate

**Status:** accepted on exact executable head `59ad23df36a8ec40ca66b8151770acf69d1257b5`; T2008 is next.

**Starting head:** accepted T2006 executable head `5d25874aacb3235a25087ce442b4c7ed1fe04678`. The T2006 gate and roadmap acceptance text are part of this candidate; they do not change the T2006 executable head.

## Intended production boundary

The production project declares only `Icod.Terminal 1.18.0`. It contains no direct `Icod.TermInfo` package reference, source symbol, compiled assembly reference, or metadata TypeRef. The packed nuspec contains exactly one direct Terminal dependency at the qualified minimum for each of `net8.0`, `net9.0`, and `net10.0`. Terminal remains free to restore TermInfo transitively.

## Candidate checks

- `T2005TransactionalBoundaryContractTests` rejects production TermInfo symbols and raw-output seams; T2007 exercises its real predicate with deliberately forbidden inputs.
- `T2007DependencyBoundaryTests` checks the actual project dependency list/version, rejects an injected direct TermInfo reference, scans the emitted assembly references and TypeRefs, and tests the metadata-name policy against injected TermInfo identities.
- The package verifier independently requires the Terminal-only direct dependency and `1.18.0` minimum in the project and each packed TFM group, scans each packaged assembly's metadata, and runs in-memory project/nuspec negative controls before reading artifacts.
- The package-only smoke consumer references DCurses and Terminal public types only; its own project declares DCurses alone.
- The tag-only release workflow requires the Terminal-only project reference set and describes TermInfo only as a transitive package.
- `README.md`, `docs/Public-API-Fingerprint-2.0.json`, `Version`, `PackageVersion`, and `AssemblyVersion` must remain unchanged.

## Exact-head evidence

- PR workflow [`35819157649`](https://github.com/uniblab/Icod.DCurses/actions/runs/35819157649) completed successfully on head `59ad23df36a8ec40ca66b8151770acf69d1257b5`. Its package candidate job and all six runtime jobs passed; there were no retries or corrective commits.
- Each runtime job (Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64) ran the full suite on `net8.0`, `net9.0`, and `net10.0`: **1,055 passed, 0 failed, 0 skipped per framework per job**. The seven new test cases cover the real source guard, the actual project and emitted assembly, and deliberate forbidden inputs.
- Package validation built with zero warnings and errors, ran the in-memory negative controls, verified each packed assembly's AssemblyRef and TypeRef metadata, and accepted the resulting `2.0.0-alpha.1` candidate. Its verifier rejects an injected project `Icod.TermInfo` reference with `Production project must depend directly only on Icod.Terminal.`; it rejects an extra nuspec dependency with `Package dependency set for net10.0 must contain only Icod.Terminal.` Both controls ran successfully as expected. The xUnit project guard rejects the injected direct reference with `Production must depend directly only on Icod.Terminal 1.18.0.`; its source guard rejects `using Icod.TermInfo;`, a fully qualified TermInfo type, and `WriteTerminalStringAsync` through the same predicate that scans production files.
- Inspection of the uploaded package's `Icod.DCurses.nuspec` shows exactly one direct dependency in each `net8.0`, `net9.0`, and `net10.0` group: `<dependency id="Icod.Terminal" version="1.18.0" exclude="Build,Analyzers" />`. No group declares `Icod.TermInfo`. The verifier inspected the PE metadata for each packaged framework assembly; the runtime tests independently inspected the emitted assembly. TermInfo remains a legitimate transitive dependency of Terminal and a private, direct fixture dependency in the test project.
- Fresh NuGet-only package consumers compiled and executed for all three frameworks, with no direct TermInfo source or project reference. `README.md` and `docs/Public-API-Fingerprint-2.0.json` retain identical blob identities to the starting head (`b563f6415a26e43f9136163947b4065033707ec9` and `b5d2ac38d344ee7db2a6525547d4e2f47c757b47`). `Version`, `PackageVersion`, and `AssemblyVersion` remain `2.0.0-alpha.1`, `2.0.0-alpha.1`, and `2.0.0.0`.

The local environment has no .NET SDK; the executable evidence comes from the linked GitHub Actions run. T2008 must update current consumers and documentation, then qualify those examples independently.

Test-only `Icod.TermInfo 1.15.0` remains a private fixture dependency for synthetic terminal descriptions, capability matrices and legacy cost baselines. It is not a production or package dependency. Historical 1.x documentation remains unchanged.

No merge, release tag, or publication is authorized here.
