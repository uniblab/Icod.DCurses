# T2308 Raster-Atlas Application Acceptance Gate

## Automated application evidence

`Icod.DCurses.RasterAtlas.Sample` is an original, public-API top-down exploration
sample. One deterministic coordinate-generated model drives both raster and text
presentation. The automated suite proves:

- stable-camera movement produces only the old and new tile updates;
- a camera shift produces exactly one bounded update per visible cell;
- resize rejects old geometry and reconstructs only the new viewport image;
- lifecycle notifications and timed public-API dimension synchronization drive the
  same recreation path without operating-system or terminal-name detection;
- the sample is built for .NET 8, 9 and 10 as part of the solution;
- `--text`, capability verification, exact geometry, atlas creation, retained
  projection and presentation are all visible in application source;
- the generated world is never stored as a world-sized image or cell plane.

The sample uses a retained `CursesPanel` for help and ordinary retained text for the
status area. Raster setup or controlled presentation failure switches to text. An
ambiguous presentation exception abandons the atlas; it is never silently retried.
Resize explicitly disposes the old atlas, clears its retained cells, obtains new exact
geometry, creates a replacement and projects new cells.

## Manual checklist

Run the following on a representative raster-capable terminal:

```text
dotnet run --project samples/Icod.DCurses.RasterAtlas.Sample/Icod.DCurses.RasterAtlas.Sample.csproj --framework net10.0
```

- [ ] status reports `RASTER`;
- [ ] movement updates the player without tearing;
- [ ] camera movement replaces the complete visible viewport coherently;
- [ ] help appears above the raster map and closes cleanly;
- [ ] resize recreates the atlas and leaves no stale cells;
- [ ] Q/Escape restores the terminal.

Run the explicit fallback on any representative terminal:

```text
dotnet run --project samples/Icod.DCurses.RasterAtlas.Sample/Icod.DCurses.RasterAtlas.Sample.csproj --framework net10.0 -- --text
```

- [ ] status reports `TEXT`;
- [ ] the same movement, collision and camera rules apply;
- [ ] help, resize and clean exit work without raster output.

## Gate state

The automated implementation and exact-source-head matrix are complete. Implementation
head `6995e5c3f809412a290a345512c294d35c011b55` passed
[pull-request workflow 1302](https://github.com/uniblab/Icod.DCurses/actions/runs/37111392715) across the package candidate and all six
runtime jobs. That matrix includes the portable polling regression on .NET 8, 9 and 10.

A Windows Terminal text-fallback observation exposed the missing repaint when no
lifecycle resize notification was delivered. The portable polling fix is automated and
green.

On 2026-10-03, the Terminal 1.24.1 dependency-refresh candidate was exercised in
Windows Terminal, Contour 0.7.0.8982 and WezTerm on Windows. All three reported `TEXT` and
`Persistent raster unavailable; using text.` rather than throwing the earlier malformed
persistent-identity exception. Windows Terminal and Contour additionally confirmed that
movement worked, water blocked movement, the camera scrolled and Q restored the terminal
cleanly. These observations accept the controlled fallback, and the first two accept the
shared-model movement/collision/camera behavior and clean Q exit in those environments.
They do not accept raster rendering: none supplied the nonzero terminal-assigned
persistent image identity required by the atlas contract. The WezTerm capture alone does
not distinguish which capability-evidence branch selected the fallback.

T2308 therefore remains pending on the representative raster-capable checklist. Help,
resize and Escape also remain unchecked for this exact Windows-host retest.
CI cannot prove that a particular emulator renders raster pixels correctly, so the live
raster boxes must not be pre-checked from scripted transport evidence.

## Complete-frame fallback acceptance (pending)

The current candidate consumes Terminal 1.25.0-alpha.2, including the relative-cursor correction and coalesced Sixel screen writes. The October 3 17:20–17:22 UTC recordings show Contour movement without the earlier text flash, Windows Terminal with progressive horizontal image redraw, and WezTerm text fallback without the earlier left-margin trails. The October 4 03:46 UTC WezTerm recording still shows ground dots shifting beside the player after ligatures were disabled; a kerning-disabled comparison is pending. These recordings predate alpha.2 consumption and do not close acceptance.

Repeat default and `-- --raster` runs in Windows Terminal and Contour, following the [sample checklist](../samples/Icod.DCurses.RasterAtlas.Sample/README.md#live-acceptance-for-the-new-frame-path). Confirm FRAME pixels without text-map flashes, movement, camera, water collision, help text transition and image restoration, repeated resize, no stale images or repeated status bars, and Q/Escape cleanup. Run default and `-- --text` in WezTerm and confirm movement leaves no left-margin trails. `-- --text` must also work in the other two terminals. Unknown raster capability may still select TEXT; this dependency update does not change graphics verification.
