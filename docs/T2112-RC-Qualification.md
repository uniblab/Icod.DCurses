# T2112 Release Candidate Qualification

**Status:** accepted, seven of seven Staging PR jobs successful\
**RC validation head:** `451005f98fe957d5754017943e8d4ce66af47cf0`\
**PR workflow:** [36046581162](https://github.com/uniblab/Icod.DCurses/actions/runs/36046581162)

The original 14-job RC run [36045945486](https://github.com/uniblab/Icod.DCurses/actions/runs/36045945486) was cancelled at user request. The replacement PR workflow runs Staging only; the existing `main` push workflow runs Release only.

`Version` and `PackageVersion` are `2.1.0-rc.1`. `AssemblyVersion` remains `2.0.0.0`, the three target frameworks remain .NET 8/9/10, and the sole direct production dependency remains `Icod.Terminal 1.18.0`. The fingerprint's release/status metadata and the package release notes agree with the RC identity; its 96 types, 751 contract lines and SHA-256 `c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79` remain the accepted T2111 public surface. The root README continues to identify 2.0.0 as the currently published version; the RC has not been published.

The PR workflow checks out the pull request head SHA explicitly. Its Staging runtime jobs build/test Windows, Linux and macOS on x64/ARM64; one Staging package job verifies package structure, direct dependencies, assembly identity, README, license, XML documentation, portable symbols and fresh package-only consumers on .NET 8/9/10, including a Linux pseudo-terminal refresh. The `main` push workflow already runs Release only.

The successful Staging package job produced artifact `10829105878`, ZIP SHA-256 `65197a270919928512f9b47200af77067a3d48695137a7cff29380eea587a738`; its `.nupkg` SHA-256 is `67824e932206608d641365ed124ee1cd182c89501d2cd670f47953e155c16c7c` and `.snupkg` SHA-256 is `2138e331718ae652ad4191f9c5615a4e564576ad1490d21f852ab71f24a9fe44`. Direct inspection of the package nuspec found version `2.1.0-rc.1`, repository commit `451005f98fe957d5754017943e8d4ce66af47cf0` and only `Icod.Terminal 1.18.0` in each target framework's dependency group. The representative Linux x64 Staging job reports 1,223 passing tests on each of .NET 8/9/10, zero failures/skips and zero build warnings. The package job also reports zero build warnings.

Once qualified, T2112 may advance only release identity/status/evidence to an unpublished `2.1.0` stable-source candidate. That candidate needs its own exact-head seven-job Staging PR matrix before maintainer handoff. The Release matrix will run after a future push to `main`; PR #33 remains open and unmerged, so that evidence is pending. No tag, GitHub Release or NuGet publication has occurred.
