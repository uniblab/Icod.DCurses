# T1606 — Terminal 1.15 Placeholder Refresh Integration

**Release:** `Icod.DCurses 1.6.0-alpha.2`  
**Dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Status:** accepted

## Objective

T1606 integrates Terminal 1.15 Unicode raster-placeholder cells into the retained DCurses physical-refresh pipeline without importing Kitty/APC framing or terminal-private raster identity into DCurses.

The tranche preserves the approved ownership boundary:

- Terminal owns live raster identity, validation, encoding, acknowledgement, session serialization, and protocol output;
- DCurses owns retained logical coordinates, damage, physical-cache comparison, panel projection, cursor placement, refresh ordering, and rendition recovery.

No public API or package dependency change is introduced by T1606. The package remains `1.6.0-alpha.2`.

## Accepted implementation

### Typed Terminal-backed output boundary

DCurses now has an internal `ITerminalRasterPlaceholderOutput` seam. `TerminalSessionCursesOutput` implements it by delegating the opaque DCurses raster token to Terminal's typed `WriteRasterPlaceholderCellAsync(...)` API.

The refresh engine never constructs Kitty graphics bytes, Unicode-placeholder identity colors, raw APC frames, terminal image ids, or terminal placement ids.

### Retained physical raster state

`CursesPhysicalScreenState` now retains raster state as a third independent axis beside ordinary `CursesCell` state and sparse semantic metadata.

The physical cache therefore distinguishes:

```text
ordinary visible cell
semantic metadata
retained raster placeholder cell
```

Physical raster state is committed only after successful semantic placeholder output. Refresh failure continues to invalidate retained physical knowledge through the existing failure path.

### Refresh diffing and optimization safety

Raster identity participates in `NeedsUpdate(...)` comparison.

When either logical or retained physical raster state is present, line-shift, character-shift, and erase shortcuts are conservatively disabled. Those optimizations remain available for ordinary text-only screens and can be reconsidered separately only if proven raster-safe.

Raster coordinates render as independent one-cell semantic segments. Ordinary fallback text stored in the same logical `CursesCell` is not emitted while the raster token is present. Removing the raster token restores the retained fallback cell through the ordinary refresh path.

### Rendition recovery

Terminal placeholder encoding temporarily uses protocol-private foreground/underline identity channels. After every placeholder emission, DCurses invalidates its cached rendition certainty so following ordinary text reasserts the requested style instead of trusting stale foreground/attribute assumptions.

### Panel refresh projection

The panel-composition refresh projection now copies and compares the raster axis together with cell and metadata state. Initial projection, incremental raster addition, and incremental raster removal are all covered.

### Synchronized output

The final acceptance test proves raster semantic output remains inside the existing Terminal synchronized-output transaction. With `UseSynchronizedOutput = true`, one refresh preserves the ordering:

```text
Terminal synchronized-output begin
DCurses raster/text refresh output
Terminal synchronized-output end
```

No graphics-specific writer bypasses the canonical refresh/session serialization boundary.

## TDD evidence

### Refresh seam RED

Test-only head:

`741006a0e1d2026d764abaae97c0efe8c6f7b2ba`

Workflow **#948 / 35029916670** built successfully with zero warnings/errors and then failed exactly the three intended missing-contract assertions on every TFM:

```text
3 failed / 852 passed / 855 total
```

The missing seams were:

- internal raster-placeholder output interface;
- Terminal-backed implementation of that interface;
- retained physical raster axis.

### Refresh seam GREEN

Exact seam head:

`34cd198ce3809cef5764843353fe4a26e976a7fe`

Workflow **#954 / 35030385410** passed all seven jobs. Linux x64 reported:

```text
net8.0   855 / 855
net9.0   855 / 855
net10.0  855 / 855
```

with zero build warnings/errors.

### Raster refresh behavior RED

Test-only head:

`f660a1762681a58a9eea7951a7ee3bf181f7b3ef`

Workflow **#955 / 35030973545** built successfully and produced the intended behavior boundary on every TFM:

```text
5 failed / 856 passed / 861 total
```

The missing behavior was first placeholder emission, sparse re-emission, rendition reassertion, erase suppression, and typed-output rejection. Raster removal already followed the correct retained fallback path and therefore passed at RED.

### Core renderer GREEN

Exact head:

`7178e2ff21c8e05487431519881cb020fc191f8f`

Workflow **#956 / 35031286540** passed all seven jobs. Linux x64 reported:

```text
net8.0   861 / 861
net9.0   861 / 861
net10.0  861 / 861
```

with zero build warnings/errors.

### Panel refresh projection RED

Test-only head:

`26afbf7cea66109d885a46d7c1e348910b453c78`

Workflow **#957 / 35031542714** built successfully and failed only the three intended raster-projection witnesses on every TFM:

```text
3 failed / 861 passed / 864 total
```

### Panel refresh projection GREEN

Exact head:

`df3e975168b44c2d9bb0dcbacf6af74519be8677`

Workflow **#958 / 35031747537** passed all seven jobs. Linux x64 reported:

```text
net8.0   864 / 864
net9.0   864 / 864
net10.0  864 / 864
```

with zero build warnings/errors.

### Final synchronized-output acceptance

Accepted exact head:

`9da1991cfebd11678d5c48fe31a7db51c438487e`

Workflow **#960 / 35032145407** passed all seven jobs, including package candidate plus Windows/Linux/macOS x64/ARM64. Linux x64 reported:

```text
net8.0   865 / 865
net9.0   865 / 865
net10.0  865 / 865
```

with zero build warnings/errors.

## Accepted boundary for T1607

T1606 establishes rendering and physical-state correctness but does not add hidden resource replay or terminal-side existence inference.

T1607 now owns lifecycle hardening:

- disposal/release must invalidate retained physical media certainty;
- resume/session-generation loss must not leave stale physical raster knowledge trusted;
- logical raster references may remain retained as application intent;
- unusable Terminal ownership must never be emitted blindly;
- no source-image cache, automatic re-upload, or backend fallback is introduced.
