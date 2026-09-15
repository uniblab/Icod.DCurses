# T1604 — Raster Editing, Copy, Pad, and Viewport Propagation

**Release:** `Icod.DCurses 1.6.0`  
**Tranche:** T1604  
**Development identity:** `1.6.0-alpha.2`  
**AssemblyVersion:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Status:** implementation qualification pending

## Objective

T1604 carries retained raster state through the existing logical editing and rectangular-transfer machinery rather than creating raster-specific editing algorithms.

The tranche covers:

- insert/delete cells;
- insert/delete lines;
- explicit scroll up/down;
- subwindow projection;
- destructive rectangular copy;
- blank-cell overlay semantics;
- overlapping copy snapshots;
- pad presentation;
- viewport pan/re-presentation and change tracking;
- clipping;
- same-session transfer; and
- pre-mutation rejection of a known foreign-session raster reference when the destination is bound to a live `CursesSession`.

## RED evidence

The first test-only head exposed a harness compile mistake and was not accepted as RED.

The corrected test-only RED head is:

```text
79152c0d6108a31ba38484f09bf25eadb6d94dd6
```

Workflow **#937 / 35016106603** built successfully with zero warnings/errors on Linux x64, then failed at test execution on the intended T1604 gaps. Linux x64 reported:

```text
11 failed
830 passed
841 total
```

The failures were retained-raster loss across editing/copy/pad paths plus the intentionally absent destination-session ownership seam. The package candidate passed.

## GREEN candidate

The minimal production candidate is:

```text
27cd4bd5f1be4ed1285e4524af0f8cf4fe8e5504
```

It:

- includes the raster token in `CursesLogicalCellState` snapshots;
- restores retained raster after ordinary-cell/metadata commit;
- preserves per-coordinate raster on normalized wide-cell continuation state;
- treats a blank+raster source coordinate as present during overlay while metadata-only blank coordinates remain transparent;
- validates the complete source snapshot against a session-bound destination before the first destination mutation;
- binds session-created logical screens to the canonical `CursesSession` raster owner;
- preserves retained raster state in the overlapping upper-left region during `CursesScreen.Resize(..., preserveContents: true)`; and
- lets pad and viewport presentation inherit the corrected rectangular-copy behavior.

No public API was added, so the T1603 `1.6.0-alpha.2` API fingerprint remains the expected public baseline.

The exact post-documentation head must pass the ordinary seven-job Staging matrix before T1604 is accepted.
