# Icod.DCurses.RasterAtlas.Sample

This public-API sample is an original small top-down exploration scene. It is not an
Ultima IV implementation and contains no Ultima art, maps, rules or data. Its purpose
is to exercise the presentation demands shared by cell-aligned tile games: a moving
player, a scrolling camera, sparse tile replacement, retained text/status overlays,
resize recreation and a text fallback driven by the same model.

Run it from the repository root:

```text
dotnet run --project samples/Icod.DCurses.RasterAtlas.Sample/Icod.DCurses.RasterAtlas.Sample.csproj --framework net10.0
```

Force the non-raster path even on a capable terminal:

```text
dotnet run --project samples/Icod.DCurses.RasterAtlas.Sample/Icod.DCurses.RasterAtlas.Sample.csproj --framework net10.0 -- --text
```

Arrows or WASD move, `?`/`H` toggles help, and `Q`/Escape exits. Water blocks
movement. The generated world is computed by coordinate and is never retained as a
map-sized image or cell array.

## Raster selection and ownership

The sample asks Icod.Terminal to verify `PersistentRasterGraphics`; it does not infer
support from a terminal name. On verified support it queries exact cell-pixel geometry,
creates a `CursesRasterAtlas`, projects its opaque cells into a `CursesWindow`, and
submits only changed tiles while the camera remains stable. DCurses owns retained
coordinates and serialization; Terminal owns live protocol identity and acknowledgement;
the sample owns every source pixel and gameplay decision.

If verification, exact geometry, atlas creation or presentation is unavailable, the
sample switches to ordinary retained text. `--text` skips the raster attempt entirely.
There is no hidden graphics backend ladder.

Resize is deliberately explicit. The sample disposes the old atlas, clears stale
retained cells, queries new geometry, creates a new atlas from application-owned pixels,
and projects replacement cells. An ambiguous presentation exception also abandons the
atlas and falls back to text rather than replaying a hidden image cache.

Kitty graphics-capable terminals that satisfy Terminal's complete persistent-raster
verification are expected to take the raster path. Other terminals are expected to
take the text path. The automated suite proves model behavior, bounds, protocol order
and fallback structure; it does not prove that a particular terminal emulator renders
the pixels correctly. Record that separately with the T2308/T2310 live checklist.
