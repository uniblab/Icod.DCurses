# T2502 — Color Composition and Transparency Decision

## Decision

The existing pad, viewport, panel, and compositor implementation preserves complete styles. No production correction or public API addition was required.

Widgets and dedit must use `CursesPanelTransparency.Opaque` whenever a panel-owned blank carries visible background meaning. The published `BlankCellsTransparent` mode remains literal and style-insensitive: an ordinary blank remains transparent even when styled or carrying semantic metadata, while retained raster state makes that coordinate visually present.

No public-only witness requires a third transparency policy. The 2.5 contract therefore preserves the two existing enum values without amendment.

## Permanent witnesses

`CursesColorSurfaceCompositionTests` covers:

- pad viewport projection, repeated authoritative projection, and clipped wide-footprint repair;
- opaque styled-blank coverage, movement, hide/show, and resize restoration;
- styled and metadata-only blank transparency;
- raster-backed blank opacity; and
- lower wide-footprint preservation at a transparent-panel boundary.

All style comparisons use distinct indexed/RGB foregrounds, indexed/RGB backgrounds, and attributes. Wide-cell witnesses invoke `CursesCellFootprint.Validate`.

## Verification

- Focused net10.0 composition suite: 11/11 passed.
- New tests plus all `CursesPad*`, `CursesPanel*`, `CursesWindowCompositionTests`, and `CursesWindowDamageRangeTests`: 163/163 passed on each of net8.0, net9.0, and net10.0; 489 total target-specific tests.
- Production files changed: none.
- Public API changed: none.

This execution environment contains only the .NET 10 runtime. Net8.0 and net9.0 target assemblies were executed with `DOTNET_ROLL_FORWARD=Major`; repository CI remains authoritative for native target-runtime and platform coverage.
