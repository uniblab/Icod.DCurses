# T2007 TermInfo Dependency Removal Gate

**Status:** implementation candidate in the local checkout; executable qualification pending. Do not advance to T2008 yet.

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

## Pending evidence

Run focused T2007 tests, package validation, and the six runtime jobs on one exact candidate head. Read the produced nuspec and metadata per target framework. Record the SHA, workflow URL, test counts, negative-control diagnostics, package direct dependency groups, and any corrective changes before accepting T2007. The local environment lacks a .NET SDK; no build or runtime success is claimed for the candidate.

Test-only `Icod.TermInfo 1.15.0` remains a private fixture dependency for synthetic terminal descriptions, capability matrices and legacy cost baselines. It is not a production or package dependency. Historical 1.x documentation remains unchanged.

No merge, release tag, or publication is authorized here.
