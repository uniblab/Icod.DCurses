# Package Validation Scripts

`verify-release-package.sh` and `verify-release-package.cmd` are equivalent host
wrappers for the T13 package-validation contract.

Both wrappers require:

```text
<artifact-directory> <Debug|Staging|Release>
```

For local Debug validation:

```text
.github\scripts\verify-release-package.cmd artifacts Debug
bash .github/scripts/verify-release-package.sh artifacts Debug
```

For pull-request/Staging validation:

```text
.github\scripts\verify-release-package.cmd artifacts Staging
bash .github/scripts/verify-release-package.sh artifacts Staging
```

For release validation:

```text
.github\scripts\verify-release-package.cmd artifacts Release
bash .github/scripts/verify-release-package.sh artifacts Release
```

The wrappers:

- read `PackageVersion` from `Icod.DCurses.csproj`;
- require the matching `.nupkg` and `.snupkg`;
- run `tools/package-verifier` against package structure, metadata, dependencies,
  assembly identity, XML documentation, and symbol payloads;
- copy `tools/package-smoke` into a temporary directory;
- use an isolated NuGet cache and a temporary NuGet configuration;
- restore the exact packed DCurses version from the artifact directory;
- resolve runtime dependencies from NuGet;
- build and execute the package-only smoke consumer under `net8.0`, `net9.0`,
  and `net10.0` without any repository-local project reference.

The package verifier and smoke consumer intentionally remain outside
`Icod.DCurses.sln`; they validate the packed artifact after the repository
solution itself has already built and tested.
