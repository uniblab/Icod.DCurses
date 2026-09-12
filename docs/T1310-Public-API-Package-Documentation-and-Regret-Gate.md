# T1310 Public API, Package, Documentation, and Regret Gate

## Scope

T1310 freezes the `Icod.DCurses 1.3` development contract before release-candidate promotion. It adds no new runtime feature. Its purpose is to find mistakes while correction is still cheaper than carrying an avoidable public contract through stable 1.x compatibility.

The gate covers:

- public API naming, mutability, overload shape, and planned 1.4 reuse;
- compiler-derived API fingerprint consistency;
- fresh NuGet-only package consumption of the complete 1.3 surface;
- README and roadmap accuracy;
- source/test/sample licensing headers;
- package metadata and declared dependency consistency.

## Baseline

Published 1.2 contract:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Reviewed 1.3 candidate contract:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

Machine-readable authority: `docs/Public-API-Fingerprint-1.3.json`.

Human-readable authority: `docs/Public-API-Baseline-1.3.md`.

## Public API regret audit

### New exported types

The four additions over 1.2 are:

```text
CursesRectangle
CursesInsets
CursesDockEdge
CursesLayout
```

The audit found no reason to rename or replace them.

`CursesRectangle` and `CursesInsets` are immutable value types, which matches their role as terminal-independent geometry rather than retained layout ownership. Rows/columns terminology is consistent with the rest of DCurses and avoids accidental pixel semantics.

`CursesDockEdge` uses the expected four edge names and is validated by `CursesLayout.Dock` before use.

`CursesLayout` accurately describes a pure layout utility rather than an object graph. It is static, has no instance state, and has no mutable static state.

### Existing-type additions

The six additions to published types are:

```text
CursesScreen.Bounds
CursesWindow.Bounds
CursesWindow.SetBounds(CursesRectangle)
CursesPanel.Bounds
CursesPanel.Resize(int rows, int columns)
CursesPanel.SetBounds(CursesRectangle)
```

The audit accepts these names and semantics:

- `Bounds` is an ordinary immutable snapshot and does not expose mutable geometry ownership.
- `SetBounds` is explicit application of a final rectangle, complementing existing move/resize operations without creating automatic layout behavior.
- `CursesWindow.SetBounds` does not make the standard window independently resizable.
- nested-window bounds remain relative to the immediate parent, preserving the established window coordinate model.
- panel bounds remain screen-relative because panels are screen-owned retained surfaces.
- panel `Resize` expresses the already-planned retained-surface operation directly and preserves 1.2 ownership/z-order semantics.

No new overload creates source ambiguity with the published 1.2 surface.

### Future 1.4 reuse

The geometry substrate is intentionally reusable by the planned 1.4 focus/interaction work. Focus regions, hit-test rectangles, and interaction bounds can consume `CursesRectangle` without requiring a second coordinate abstraction or granting 1.4 a retained layout owner.

No 1.4-specific focus, gesture, pointer, or hit-test policy is prematurely frozen in 1.3.

## Package-only consumer gate

The fresh package-smoke project remains outside `Icod.DCurses.sln` and contains no repository project reference. It restores the packed `Icod.DCurses` artifact and therefore validates the package surface rather than source-tree visibility.

T1310 adds `tools/package-smoke/LayoutSmoke.cs`. Its module initializer compiles and executes representative use of:

- rectangle construction, containment-related derived geometry, and insets;
- fixed split allocation;
- docking;
- proportional allocation;
- clipping;
- `CursesScreen.Bounds`;
- `CursesWindow.Bounds` and `SetBounds`;
- retained `CursesPanel.Resize` with width-two content preservation;
- `CursesPanel.Bounds` and `SetBounds` with retained content preservation.

The package-smoke documentation also corrected a stale dependency statement from `Icod.Terminal 1.8.1` to the actual declared `Icod.Terminal 1.9.0`. `Icod.TermInfo` remains `1.10.0`.

## Fingerprint gate

The T1310 fingerprint is the same compiler-derived contract already reached at T1306 and carried unchanged through T1307-T1309:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

T1310 changes the fingerprint metadata to `development-baseline-t1310`; it does not alter the API hash, type count, or contract-line count.

The published 1.2 fingerprint/baseline remain historical authorities and are not rewritten.

## Licensing/header audit

New 1.3 library source files use the LGPL-3.0-or-later header, including:

```text
src/CursesRectangle.cs
src/CursesInsets.cs
src/CursesLayout.cs
src/CursesDockEdge.cs
src/CursesWindow.Bounds.cs
```

Modified existing library source retains the LGPL header.

New 1.3 tests use the GPL-3.0-or-later test-suite header. The interactive layout sample uses the GPL header. The newly added package-only executable smoke source also uses the GPL header.

`Icod.DCurses.csproj` still begins exactly with:

```xml
<?xml version="1.0" encoding="utf-8"?>
```

No license-family correction was required during the audit.

## Architecture/non-goal audit

The implementation still contains no retained layout tree, automatic screen-layout owner, widget framework, focus router, hit-test engine, pointer policy, animation system, raster placement layer, or general constraint solver.

Applications remain responsible for explicit recomputation:

```text
current Screen.Bounds
    -> pure CursesLayout calculations
    -> explicit window/panel SetBounds or panel Resize
    -> ordinary retained RefreshAsync
```

This boundary is preserved by the T1307 integration/sample work and the T1309 state/allocation acceptance tests.

## Performance evidence entering T1310

T1309 qualified exact head `c209780cf4a0afbf71991e34a1d8912373426922` in workflow #671 / `34625614322`, all seven jobs green.

The pure geometry acceptance still requires an exactly zero-byte stabilized sample. Because the .NET test runner can inject a tiny sporadic fixed allocation into an individual measurement window, the test takes eight independent post-warmup samples and requires the minimum to be exactly zero rather than relaxing the allocation ceiling.

Steady-state explicit layout application/composition retains its separate bounded allocation ceiling and retained composed-frame identity requirement.

## Documentation closure

T1310 updates the root README and active roadmaps to describe 1.3 as the current development candidate rather than leaving the repository documented as 1.2/pending-1.3 planning.

Historical 1.0-1.2 tranche documents remain historical and are not rewritten to simulate current release state.

## Qualification rule

T1310 is not complete merely because this audit document exists. The exact head containing all package-smoke, fingerprint, baseline, README, roadmap, and package-metadata corrections must pass:

- package candidate validation;
- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- all supported TFMs (`net8.0`, `net9.0`, `net10.0`).

Only that fresh exact-head evidence closes T1310 and permits T1311 RC promotion.
