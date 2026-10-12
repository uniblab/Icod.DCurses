# T2503 — Color Damage and Refresh

## Result

The existing damage, physical-state, mapper, and refresh implementation is sufficient. No production code, protocol output, or public API change was required.

Complete `CursesStyle` equality already participates in logical dirtying and physical differencing. Foreground-only, background-only, and attribute-only replacements become dirty; equal complete-cell rewrites remain clean. Refresh sends independent foreground/background requests through the DCurses-to-Terminal mapper and lets Terminal own transition/reset planning.

## Permanent witnesses

`CursesColorSurfaceRefreshTests` covers:

- dirty-state changes for each style axis and the equal-cell no-op;
- foreground-only and background-only rendition transitions;
- content and metadata retention across a complete color-pair change;
- zero cell output for an equal multicolor frame;
- complete style re-emission after physical invalidation;
- independent foreground/background mapper round-trip;
- default-color restoration before later default-styled text; and
- reverse retained as an attribute without logical color swapping.

## Failure classification and output deltas

The first focused run reported 11 passes and 1 failure in `InvalidatedMulticolorFrameReemitsCompleteStyles`. The assertion expected the payload substring `AB`, but the fixture correctly recorded rendition transitions between the differently styled cells. This was a test expectation error, not missing output. The permanent witness now requires both payloads plus `<fg:1>`, `<bg:2>`, `<fg:3>`, and `<bg:4>` independently.

Observed style-only deltas include the changed selector and unchanged payload:

- foreground-only update: `<fg:3>` and `A`;
- background-only update: `<bg:4>` and `A`;
- equal frame: no output text; and
- invalidated multicolor frame: both requested foreground/background pairs and both payloads.

## Verification

- Focused net10.0 refresh suite: 12/12 passed.
- New tests plus `CursesVirtualScreenChangeTrackingTests`, `CursesRefreshPhysicalStateTests`, `CursesRefreshEngineTerminalTests`, `CursesPreparedRefreshTests`, `CursesPresentationResolverTests`, and `CursesTerminalScreenMapperTests`: 76/76 passed on each of net8.0, net9.0, and net10.0; 228 total target-specific tests.
- Production files changed: none.
- Public API changed: none.

This execution environment contains only the .NET 10 runtime. Net8.0 and net9.0 target assemblies were executed with `DOTNET_ROLL_FORWARD=Major`; repository CI remains authoritative for native target-runtime and platform coverage.
