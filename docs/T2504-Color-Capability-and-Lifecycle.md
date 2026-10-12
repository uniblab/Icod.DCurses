# T2504 — Color Capability and Lifecycle

## Result

The existing DCurses capability adapter, Terminal-owned normalization, screen resize, invalidation, and lifecycle integration satisfy the approved 2.5 contract. No production code, public API, dependency, or lower-layer change was required.

## Capability and fallback decisions

- Monochrome profiles report no color and normalize color requests to terminal default.
- Indexed 8-, 16-, and 256-color profiles report their exact capacity and retain safe in-range requests.
- Direct-RGB profiles preserve safe foreground/background RGB pairs.
- Foreground-only and background-only selectors degrade only the unsupported axis; the supported axis remains intact.
- Profiles without safe default-color restoration do not claim it and degrade unsafe color requests to defaults.
- Unsafe direct RGB values degrade to `CursesColor.Default`; DCurses does not select a nearest indexed or RGB color.
- No-color-video restrictions remove only the restricted physical attribute while retaining logical application intent.
- Reverse remains an attribute and never rewrites stored foreground/background values.

These outcomes originate in the stable Icod.Terminal 1.29.0 screen planner and are projected by DCurses without duplicating Terminal policy. No Terminal defect or deferral was found.

## Resize and lifecycle decisions

- Preserved resize retains complete styles in the overlap.
- Growth creates default logical cells outside the overlap until application repaint.
- A clipped wide leader is repaired to a styled blank with no orphaned continuation.
- Logical invalidation marks cells dirty without rewriting requested content or styles.
- Suspend/resume discards physical certainty; the next refresh re-emits all requested foreground/background pairs while logical styles remain unchanged.

## Verification

- Focused net10.0 capability/lifecycle suite: 9/9 passed.
- New tests plus `CursesPresentationCapabilitiesTests`, `CursesPresentationResolverTests`, `CursesLayoutResizeIntegrationTests`, `CursesPanelResizeTests`, `CursesRefreshPublicationHardeningTests`, `CursesOutputFailureHardeningTests`, `CursesLifecycleHardeningTests`, `CursesPanelLifecycleTests`, and `CursesTerminalIntegrationTests`: 69/69 passed on each of net8.0, net9.0, and net10.0; 207 total target-specific tests.
- Production files changed: none.
- Public API changed: none.

This execution environment contains only the .NET 10 runtime. Net8.0 and net9.0 target assemblies were executed with `DOTNET_ROLL_FORWARD=Major`; repository CI remains authoritative for native target-runtime and platform coverage.
