# T2011 Stable-Source Release Gate

**Status:** `2.0.0` executable source head `d01a5287bbbda0d0bad87934d45598f5e8c9135c` qualified; final evidence-only head must also pass its own PR workflow. The PR description records the exact final head, run and artifact hashes without changing the candidate to embed its own SHA.

## Executable source qualification

[PR workflow 35825479665](https://github.com/uniblab/Icod.DCurses/actions/runs/35825479665) passed 14/14 jobs after the checkout changed to `github.event.pull_request.head.sha`: full Staging and Release runtime builds/tests on Windows, Linux and macOS x64/ARM64 plus Staging and Release package validation. Each runtime job passed 1,055 tests on each of net8.0, net9.0 and net10.0 with no failures or skips. Both package jobs verified structure, compiled assembly identity, public API/dependency boundary, XML documentation and portable symbols; fresh package-only consumers ran on all three TFMs and the live Linux pseudo-terminal refresh completed. The package build reported zero compiler warnings/errors. Package repository metadata named `d01a5287bbbda0d0bad87934d45598f5e8c9135c`, the checkout head.

| Configuration | Artifact ZIP SHA-256 | `.nupkg` SHA-256 | `.snupkg` SHA-256 |
| --- | --- | --- | --- |
| Staging (artifact `10734248567`) | `1170ab91a9cac614a566fa6e5e69ff4a0153eacce4d35d301747db9361e51c12` | `f711b9afd51b583413236c8f597fef23b15284ed6fe856eeceec2b286cc2cfb6` | `b931d446b518c1f02df760fe259eba2d58fab5cc7c6c6b8fddb9f8c017937361` |
| Release (artifact `10735290157`) | `b713f47d93614357684288ad51b49ab1befd1b6ff42c562263ffb9994315a453` | `377d337b045c02462dd69b4553365883d0a6e6dfef573c0166a84e5c5e9f7e2e` | `bf0e37e56438fc3c141ad3d64653ea7d700e37f43f33e38219d8703d11d28b70` |

Direct inspection of the Release nupkg found version `2.0.0` and only `Icod.Terminal 1.18.0` in all three dependency groups; transitive `Icod.TermInfo` is expected. The package embeds a DLL and XML documentation for each TFM, an icon, the LGPL license and README. Packaged README SHA-256 `f5501d822e74c271a75fd01b4c6e50eba9c5fe689a5945faf67b72a6b9fe9a27` and LICENSE SHA-256 `af3fecb6b14ab1b0aae2023e69bb86eac13a2631c81eae254f14baa57deb603a` matched source. The frozen API fingerprint remains 75 types / 559 contract lines / SHA-256 `1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65`; `AssemblyVersion` remains `2.0.0.0`. The [2.0 migration guide](2.0-Migration-Guide.md) covers the four public signature replacements and rebuild requirement.

## Release handoff

Once the final evidence-only PR head passes the same 14 jobs and its Release artifact points to that head, the source is ready for maintainer review and merge. The final run's package files are distinct artifacts because repository commit provenance changes with the documentation commit; use the PR description's exact final artifact hashes. After an authorized merge, run the existing main Release workflow and review its packages before choosing a tag, GitHub Release or NuGet publish action. None of those actions occurred at this gate.
