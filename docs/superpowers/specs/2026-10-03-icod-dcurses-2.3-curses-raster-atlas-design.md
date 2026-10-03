# Icod.DCurses 2.3 `CursesRasterAtlas` Design

**Date:** 2026-10-03  
**Status:** Approved; exact public contract frozen at T2301
**Decision:** Option 1, first-class `CursesRasterAtlas`  
**Baseline:** published `Icod.DCurses 2.2.0` and `Icod.Terminal 1.24.0`  
**Release roadmap:** [2.3.0 development roadmap](../../../Icod.DCurses-2.3.0-Development-Roadmap.md)

## Intent

`CursesRasterAtlas` is a DCurses-owned presentation coordinator for a uniform grid of terminal-cell-aligned raster tiles. It converts Terminal 1.24's low-level, acknowledged resource/frame operations into a retained DCurses object that can participate in windows, pads, viewports, panels, clipping, damage and refresh.

The initial acceptance witness is an Ultima-style top-down tile viewport. The API remains general: it knows atlas rows, columns, tile pixels, changed atlas cells and retained placeholder cells; it knows nothing about maps, terrain, actors, collision, visibility, combat, save files or game time.

## Ownership boundary

| Layer | Owns |
|---|---|
| Application/game | Source art, decoded assets, map and actor models, collision, visibility, commands, event loop, scheduling and text fallback choice |
| DCurses 2.3 | Atlas geometry, retained atlas coordinates, window projection, damage-oriented tile changes, double-buffer policy, ordering with refresh, and conservative lifecycle state |
| Terminal 1.24 | Active pixel queries, exact derivation primitive, planning observations, raster uploads, opaque resources/placeholders/frames, operation evidence, acknowledgement correlation and serialized terminal output |
| TermInfo | Transitive immutable capability evidence used by Terminal; never called directly by DCurses |

DCurses must not construct raster protocol strings, expose Terminal ids, cache arbitrary source assets, decode image files, or infer terminal support from names/environment variables.

## Selected shape

The 2.3 surface is centered on these frozen public contracts. Any later public-contract change requires an explicit design and T2301 gate amendment.

```csharp
public readonly record struct CursesRasterAtlasGeometry(
    int Rows,
    int Columns,
    int TilePixelWidth,
    int TilePixelHeight
) {
    public int PixelWidth { get; }
    public int PixelHeight { get; }
}

public readonly record struct CursesRasterAtlasTileUpdate(
    int Row,
    int Column,
    TerminalRasterImage Image
);

public enum CursesRasterAtlasPresentationStatus {
    NoChanges = 0,
    Presented = 1,
    Unsupported = 2,
    Unavailable = 3,
    Failed = 4
}

public readonly record struct CursesRasterAtlasPresentationResult {
    public CursesRasterAtlasPresentationStatus Status { get; }
    public int RequestedUpdateCount { get; }
    public int CompletedUpdateCount { get; }
    public bool FrameSelected { get; }
    public string? Message { get; }
}

public sealed class CursesRasterAtlas : IAsyncDisposable {
    public const int MaximumUpdatesPerPresentation = 4096;
    public int Rows { get; }
    public int Columns { get; }
    public int TilePixelWidth { get; }
    public int TilePixelHeight { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public CursesRasterOwnershipState OwnershipState { get; }
    public bool RequiresRecreation { get; }
    public CursesRasterCell GetCell(int row, int column);
    public ValueTask<CursesRasterAtlasPresentationResult> PresentAsync(
        IReadOnlyList<CursesRasterAtlasTileUpdate> updates,
        CancellationToken cancellationToken = default
    );
    public ValueTask DisposeAsync();
}

public sealed partial class CursesSession {
    public ValueTask<CursesRasterAtlasGeometry> QueryRasterAtlasGeometryAsync(
        int rows,
        int columns,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    );

    public ValueTask<TerminalControlResult<CursesRasterAtlas>> CreateRasterAtlasAsync(
        TerminalRasterImage initialImage,
        int rows,
        int columns,
        CancellationToken cancellationToken = default
    );
}

public sealed partial class CursesWindow {
    public void WriteRasterAtlas(
        int row,
        int column,
        CursesRasterAtlas atlas,
        CursesRectangle sourceRectangle
    );
}
```

The API deliberately accepts `TerminalRasterImage`. Image storage and pixel formats already belong to Terminal; duplicating them in DCurses would create a leaky parallel graphics model. Opaque Terminal resources, placeholders, animations and frames remain internal implementation details.

## Geometry and creation

`QueryRasterAtlasGeometryAsync` validates positive rows/columns, bounded by the existing 256-cell placeholder axes and checked total pixels. It first asks Terminal for cell-pixel dimensions. If and only if that direct query times out, it may query total terminal pixels and use the current character dimensions with `TerminalPixelGeometry.TryDeriveCellDimensions`. Derivation must be exact on both axes; DCurses never rounds, guesses or caches the result. Cancellation, malformed responses, transport errors and non-divisible observations are not translated into support claims. Applications re-query after resize.

`CreateRasterAtlasAsync` requires an immutable initial image whose width divides exactly by `columns` and whose height divides exactly by `rows`. The derived tile size must be positive. Creation consults Terminal's planning snapshot as advisory early evidence, then treats actual results as authoritative. It creates, in order:

1. one raster resource from the initial image;
2. one `rows` by `columns` virtual placeholder;
3. one second full-size frame initialized from the same image.

Partial creation is rolled back in reverse order. Cleanup failures are preserved with the original failure rather than hidden. An unavailable or unsupported controlled result yields no atlas. DCurses retains no copy of `initialImage` after Terminal has accepted the resource and second frame.

## Retained projection

`GetCell` returns the existing opaque `CursesRasterCell` corresponding to one atlas coordinate. `WriteRasterAtlas` provides the safe bulk form:

- the source rectangle must lie wholly inside the atlas;
- the destination footprint must lie wholly inside the logical window;
- atlas/session ownership is validated before mutation;
- all validation completes before the first retained cell changes;
- the logical cursor is unchanged;
- existing raster-plane clipping, panels, pads, viewports, scrolling and damage semantics apply;
- visual/text cells and `CursesCellMetadata` remain unchanged.

Applications may still call `GetCell` and `SetRasterCell` directly. The bulk method is a convenience and correctness boundary, not a second retained representation.

## Presentation algorithm

Each atlas contains two known Terminal frames. DCurses tracks only which token is the selected front and which is the unselected back; it never exposes either token.

`PresentAsync` performs one serialized session activity:

1. Copy and validate the complete caller list before terminal I/O.
2. Reject more than 4096 updates, out-of-range coordinates, duplicate coordinates, non-RGB24/RGBA32 images, and images whose dimensions differ from one tile.
3. Sort the copied updates in row-major order so caller collection order cannot change terminal behavior.
4. Return `NoChanges` with no terminal I/O for an empty list.
5. Compose the complete selected front frame into the unselected back frame using replacement semantics. This makes each presentation independent of how the two physical frames alternated previously.
6. Apply each tile image to its exact back-frame pixel rectangle, awaiting every acknowledged result.
7. Select the back frame only after composition and every region update succeeds.
8. Swap the internal front/back references only after selection succeeds, then return `Presented`.

The full-frame terminal-side composition is essential. Without it, alternating buffers diverge and a later sparse update can resurrect pixels from two presentations ago.

This operation is serialized by the same `CursesSession` terminal-activity gate used by refresh, lifecycle transitions and other presentation controls. A refresh cannot interleave placeholder output between atlas composition, updates and selection. This ordering is local sequencing, not remote atomicity or a promise of gapless physical display.

## Failure model

Controlled unsupported, unavailable or failed results before a successful frame selection leave the known front frame selected. The back frame may be partially changed, but the next presentation begins by replacing it from the known front, so it can recover without source-image replay. The result reports the number of completed tile updates and does not claim presentation.

A timeout, cancellation, transport loss or other exception after output may have committed makes the remote frame or selected-frame state unknowable. The atlas sets `RequiresRecreation`, invalidates DCurses' physical-screen certainty, and rethrows. No later presentation or retained-cell retrieval that would emit stale identity is allowed. The application must dispose the atlas, recreate it from its own durable source art, replace retained atlas cells, and repaint. DCurses never retries a committed mutation blindly.

Lifecycle generation advance, missing-resource evidence, explicit resource/placeholder disposal, or session disposal also makes the atlas unusable. `OwnershipState` projects the most conservative owned component state. Disposal is idempotent, releases placeholder before resource, and does not fabricate terminal certainty when cleanup fails.

## Capability and fallback policy

Creation and presentation use Terminal results as authority. Planning snapshots and `InspectRasterOperation` are useful diagnostics and early exits, not reservations and not substitutes for an acknowledged operation. The atlas requires frame composition plus the focused region-update operation matching each tile's pixel format. Evidence for RGB24 does not prove RGBA32 and vice versa.

The application owns fallback policy. The acceptance sample attempts raster setup, but can render the same viewport with text glyphs when geometry, planning, placeholder creation, composition, updates, selection or focused evidence is unavailable. DCurses does not hide a backend ladder or silently replace raster tiles with characters.

## Limits and performance evidence

- Rows and columns: 1 through 256 each, matching the published placeholder ceiling.
- Present updates: 0 through 4096, with unique coordinates.
- Tile image: exact atlas tile dimensions; RGB24 or RGBA32 for partial replacement.
- Work: `O(update count log update count)` from deterministic sorting plus Terminal I/O; no work proportional to the application's world/map size.
- Retained storage: two frame tokens, one resource/placeholder facade and bounded copied update metadata; no decoded asset cache.

T2307 measures 1, 4, 16, 64, 121 and 256 changed-tile workloads through a package-only harness. The data decides whether a later Terminal batching proposal has real value. DCurses 2.3 does not invent batching or Indexed8 partial replacement above Terminal 1.24.

## Acceptance witness

The public-only sample presents an Ultima-style scene with:

- a fixed cell-aligned viewport backed by one atlas;
- terrain and actor tiles supplied as immutable RGB24/RGBA32 images;
- movement/camera changes converted into a bounded changed-tile list;
- DCurses panels/text over the map for status/help;
- resize-driven geometry re-query and explicit atlas recreation;
- text fallback using the same game/view model;
- clean terminal restoration.

Headless tests prove state and ordering. The manual checklist proves visible alignment, overlay behavior, resize/recreation and clean exit on representative terminals. Neither CI nor the sample claims physical rendering on every emulator.

## Non-goals

Version 2.3 does not add tile-map storage, camera policy, world/entity components, collision, visibility/fog, pathfinding, animation scheduling, image decoding, palette conversion, scaling, rotation, arbitrary physical placements, sub-cell sprites, a scene graph, a game loop, terminal emulation, PTY hosting, remote atomicity, hidden retries, or source-image replay.

Sprite-based action games such as Zelda require later evidence around independent physical placement, movement and animation. They are not smuggled into the tile-atlas contract.
