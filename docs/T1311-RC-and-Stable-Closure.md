# T1311 — RC and Stable 1.3.0 Closure

**Release:** `Icod.DCurses 1.3.0`  
**Tranche:** T1311  
**Qualified T1310 head:** `c8d6a6313b9f6ca124a255c12b912f8ed89ffda7`  
**T1310 workflow:** #681 / `34694609178`  
**Qualified RC head:** `2ab949a64f63759ad9cf4e93e45a368c50ab6e49`  
**RC workflow:** #689 / `34694881296`  
**Qualified stable-source head:** `b90b444556291434bddb9f56d031f09ac1aabfbf`  
**Stable-source workflow:** #719 / `34708925529`  
**Merged main commit:** `18255d59136922b9e246f4113dc6fdb6a9ea24a3`  
**Main Release workflow:** #17 / `34709142076`  
**Stable-source identity:** `1.3.0`  
**AssemblyVersion:** `1.0.0.0`  
**Stable-source runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** repository-side closure complete; public tag/release/NuGet publication pending  

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

The dependency refresh described below does not alter this DCurses public API fingerprint.

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

All seven jobs passed: package candidate, Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64, and all supported target frameworks (`net8.0`, `net9.0`, `net10.0`).

The RC package also passed the fresh NuGet-only 1.3 geometry/layout/bounds/panel-resize consumer. That RC evidence used the then-declared `Icod.Terminal 1.9.0` and `Icod.TermInfo 1.10.0` dependency graph.

## Stable-source promotion and dependency refresh

With the exact RC fully green, the branch was promoted to source/package identity `1.3.0`.

During final stable-source closure, the declared dependencies were refreshed to `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0`. `AssemblyVersion` remained `1.0.0.0`.

This dependency refresh was intentionally narrow:

- `Version` and `PackageVersion` remain `1.3.0`;
- `AssemblyVersion` remains `1.0.0.0`;
- the frozen DCurses public API remains unchanged;
- `src/` implementation behavior was not changed merely for the dependency bump;
- tests and samples remained behaviorally unchanged;
- package-smoke source remained the same consumer contract;
- historical 1.2-and-earlier records were not rewritten.

## Dependency-refreshed stable-source qualification

Final stable-source exact head:

```text
b90b444556291434bddb9f56d031f09ac1aabfbf
```

Workflow:

```text
#719 / 34708925529
```

All seven jobs passed:

- package candidate, including structural dependency metadata verification;
- fresh NuGet-only package consumer restore/build/run against `Icod.Terminal 1.11.1` and `Icod.TermInfo 1.11.0`;
- Windows x64 and ARM64;
- Linux x64 and ARM64;
- macOS x64 and ARM64;
- all supported target frameworks (`net8.0`, `net9.0`, `net10.0`).

The API fingerprint remained exactly:

```text
a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

This completed repository-side T1311 qualification.

## Merge and main Release qualification

PR #27 was merged to `main` as:

```text
18255d59136922b9e246f4113dc6fdb6a9ea24a3
```

The resulting `main` Release workflow also passed:

```text
#17 / 34709142076
```

Thus the exact dependency-refreshed 1.3 source is merged and Release-qualified on `main`.

## Remaining public release actions

Repository-side 1.3 development and main Release qualification are complete. These remain separate explicit actions:

- create/push tag `v1.3.0`;
- create a GitHub Release;
- publish the NuGet package.

No checked-in document should describe T1311, PR #27 merge, or main Release qualification as still pending.
