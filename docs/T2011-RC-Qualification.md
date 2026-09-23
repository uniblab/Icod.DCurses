# T2011 Release Candidate Qualification

**Status:** accepted on exact RC head `591a26532ec2e0fe4d637732e3f826cebd4385b0` with `Version`/`PackageVersion` `2.0.0-rc.1`, `AssemblyVersion` `2.0.0.0` and direct `Icod.Terminal 1.18.0` only.

[PR workflow 35824700019](https://github.com/uniblab/Icod.DCurses/actions/runs/35824700019) passed all 14 jobs: Staging and Release runtime builds and full tests on Windows, Linux and macOS, each x64 and ARM64, and both package validation configurations. The runtime suite passed 1,055 tests per target framework (net8.0, net9.0 and net10.0) with no failures or skips. Both package jobs built with zero warnings/errors, verified dependency/assembly/API/XML/symbol metadata, ran fresh package-only consumers for all three frameworks and completed a live Linux pseudo-terminal refresh.

GitHub artifact ZIP SHA-256 digests: Staging `1c7acb0e1a50942241840349e665c8a9a2f84cbc604ee31208d8c63d83691bae` (artifact `10735041290`) and Release `d02925a8d0356fc25cb7a8bd0b6d5395a9bc99b267f7f5620335762b69d8d7c7` (artifact `10734373557`). The reviewed 2.0 public contract remains 75 exported types / 559 lines / SHA-256 `1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65`.

The RC is unpublished. The next candidate advances to `2.0.0`, keeps the exact public API and Terminal minimum, and must receive its own exact-head Staging/Release qualification. Merge, tag, release and NuGet publication remain maintainer steps.
