# T2308 Raster-Atlas Application Acceptance Gate

## Automated application evidence

`Icod.DCurses.RasterAtlas.Sample` is an original, public-API top-down exploration
sample. One deterministic coordinate-generated model drives both raster and text
presentation. The automated suite proves:

- stable-camera movement produces only the old and new tile updates;
- a camera shift produces exactly one bounded update per visible cell;
- resize rejects old geometry and reconstructs only the new viewport image;
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

The automated implementation and exact-source-head matrix are complete. Source head
`751890de112be2e8d0be1757e1fdc6fd47b14c8d` passed
[pull-request workflow 1296](https://github.com/uniblab/Icod.DCurses/actions/runs/37106805634) across the package candidate and all six
runtime jobs.

T2308 remains pending only on the two live checklists above. CI cannot prove that a
particular emulator renders raster pixels correctly, so the live boxes must not be
pre-checked from scripted transport evidence.
