# T1311 — RC and Stable 1.3.0 Closure

**Release:** `Icod.DCurses 1.3.0`  
**Tranche:** T1311  
**Qualified T1310 head:** `c8d6a6313b9f6ca124a255c12b912f8ed89ffda7`  
**T1310 workflow:** #681 / `34694609178`  
**Qualified RC head:** `2ab949a64f63759ad9cf4e93e45a368c50ab6e49`  
**RC workflow:** #689 / `34694881296`  
**Stable-source identity:** `1.3.0`  
**AssemblyVersion:** `1.0.0.0`  
**Status:** stable-source exact-head qualification in progress  

## Entry gate

T1301–T1310 are complete. The T1310 API/package/documentation/licensing-complete source `c8d6a6313b9f6ca124a255c12b912f8ed89ffda7` passed workflow #681 / `34694609178` across all seven jobs.

That qualification includes the fresh NuGet-only package consumer for the 1.3 geometry/layout/bounds/panel-resize surface.

## Frozen public contract

T1311 carries forward unchanged:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

Exactly four exported types are added over published 1.2:

```text
CursesRectangle
CursesInsets
CursesDockEdge
CursesLayout
```

The additive existing-type members remain:

```text
CursesScreen.Bounds
CursesWindow.Bounds
CursesWindow.SetBounds(CursesRectangle)
CursesPanel.Bounds
CursesPanel.Resize(int rows, int columns)
CursesPanel.SetBounds(CursesRectangle)
```

No implementation or API change is permitted during RC/stable promotion unless a qualification failure demonstrates a release-blocking defect and that correction is explicitly requalified through the same gate.

## Release candidate qualification

The sole release candidate, `1.3.0-rc.1`, carried the T1310-qualified implementation and API unchanged.

Exact RC head:

```text
2ab949a64f63759ad9cf4e93e45a368c50ab6e49
```

Workflow:

```text
#689 / 34694881296
```

All seven jobs passed:

- package candidate;
- Windows x64 and ARM64;
- Linux x64 and ARM64;
- macOS x64 and ARM64;
- all supported target frameworks (`net8.0`, `net9.0`, `net10.0`).

The RC package also passed the fresh NuGet-only 1.3 geometry/layout/bounds/panel-resize consumer.

## Stable-source promotion

With the exact RC fully green, the branch is now promoted to source/package identity `1.3.0`.

The RC-to-stable change is limited to:

- `Version`/`PackageVersion` identity;
- stable package release-note wording;
- API fingerprint release/status metadata, without changing its hash/counts;
- current README/roadmap/API-baseline/closure status documentation.

It does not alter `src/`, test behavior, sample behavior, package-smoke behavior, runtime dependencies, or `AssemblyVersion`.

Stable-source qualification requires a second full exact-head seven-job PR matrix. The resulting stable-source SHA/workflow is repository-side completion evidence and will be reported without another self-referential source commit.

## Completion rule

T1311 is complete only when the stable-source exact head passes package validation and all six runtime jobs with the API fingerprint still exactly:

```text
a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

A green stable-source head completes repository-side 1.3 development qualification but does not itself perform a public release action.

The following remain explicit separate actions:

- merge PR #27 to `main`;
- qualify the resulting `main` Release build;
- create/push tag `v1.3.0`;
- create a GitHub Release;
- publish the NuGet package.
