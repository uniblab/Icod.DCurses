# T2505 — Public-Only Color-Surface Acceptance

**Candidate:** `2.5.0-alpha.1` on PR #38. This is a development qualification, not a stable release or a live-terminal observation.

The new `Icod.DCurses.Color.Sample` is a standalone GPL-licensed executable targeting .NET 8, 9 and 10. It references only the DCurses project and uses only its public API. The sample chooses a conservative terminal-default, indexed or RGB request palette from `CursesPresentationCapabilities`; paints independent foreground/background requests, reverse as an attribute, a width-two grapheme and an opaque styled-blank panel; synchronizes dimensions and invalidates physical certainty on repaint events; and hides the panel before its final clear. It does not inspect terminal identity, emit raw escapes or add a lower-layer dependency. The sample index and sample-specific README give the exact command, controls, fallback cautions and a manual observation template.

The public-only contract guard was observed red while the sample project was absent, then passed after the project was added. A second cleanup guard was observed red before the panel-hide exit fix, then passed 2/2. The candidate sample built across all target frameworks with zero warnings/errors in the Staging solution build. A live interactive sample run has not been claimed; live appearance must be recorded per terminal and host separately.

The isolated package consumer initially failed compilation because `CursesScreen.ComposePanels()` is internal. The correction uses public pad viewport projection, public cell inspection and an opaque panel's public content window. The T2502 internal composition suite is the separate witness for final overlay composition. The corrected consumer ran against the packed `Icod.DCurses.2.5.0-alpha.1.nupkg` with only Icod.DCurses as its direct PackageReference on net8.0, net9.0 and net10.0; package verification also checked dependency closure, symbols and XML documentation. The Linux pseudo-terminal smoke path completed. No new public API or production dependency was introduced.

## Reproduction

From the repository root:

```text
dotnet run --project samples/Icod.DCurses.Color.Sample/Icod.DCurses.Color.Sample.csproj -c Release -f net10.0
dotnet test tests/Icod.DCurses.Tests/Icod.DCurses.Tests.csproj -c Staging -m:1 --no-build --no-restore
dotnet pack Icod.DCurses.csproj -c Staging --include-symbols --include-source -o <artifact-directory>
bash .github/scripts/verify-release-package.sh <artifact-directory> Staging
```

The local execution sandbox had a read-only `/root/.local/share/NuGet/http-cache` during restore. Local solution restore/build/test therefore supplied `-p:NuGetAudit=false` to avoid an environmental NU1900 warning, then ran with zero build warnings/errors. The package-only smoke project already disables audit for its isolated restore. Normal CI should independently run its repository package and runtime matrix without treating this sandbox workaround as a released project setting.

The T2505 completion gate is the public-only sample, solution build, all-target test run, package-only consumers and exact PR branch push. T2506 remains responsible for adversarial/performance measurement, public API fingerprint and durable packaged README; T2507 remains the RC/stable-source gate. Neither merge, tag nor publication is performed here.
