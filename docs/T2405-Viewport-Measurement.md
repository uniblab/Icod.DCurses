# T2405 Viewport Measurement

**Status:** Accepted at `9a02d92`  
**Development identity:** `2.4.0-alpha.1`

## Public-only composition

The editor sample now centralizes visible-record projection in
`EditorSampleState.ProjectVisible`. It clears and repaints only the bounded
viewport, creates layouts only for intersecting synthetic records, and maps the
application-owned viewport origin into `CursesWindow.PresentTextLayout`. It uses
only public DCurses APIs. The document, edit dictionary, selection, cache policy,
and source-to-local coordinate policy remain sample/application responsibilities.

A 10,000,000-record synthetic document projects at most five records for a
12-row viewport. The witness bounds each pass to at most 500 source characters
and 480 retained cells whether the viewport is at the beginning or end of the
document. No file-sized pad, layout, or document cache is created.

## Long-line ruling and measurements

The initial adversarial witness intentionally passed a prebuilt 100,000-column
layout to `PresentTextLayout`. Although only 40 retained cells changed, that call
allocated 18,003,272 bytes. This is evidence that a whole document line is not a
valid viewport-local input; it is not an irreducible lower-layer gap under the
approved boundary.

The corrected composition supplies a caller-prepared 40-character visible slice
and retains its absolute starting column for tab geometry. On this Linux x64
.NET 10 Staging runner, layout plus projection allocated 30,384 bytes and took
4.078 ms. The deterministic guard allows at most 512,000 bytes and two seconds
to avoid converting a functional invariant into a machine-specific benchmark.

## Geometry and repaint evidence

Five focused tests pass. They cover:

- bounded beginning/end projection of the 10,000,000-record source;
- a 100,000-character logical line represented by a 40-character local slice;
- caret, hit-test, and selection geometry across combining text, wide cells,
  tabs, emoji sequences, and a logical newline;
- horizontal clipping through a selected wide element without a split leading
  or continuation cell;
- clearing stale reverse-video selection while preserving hyperlink metadata;
  and
- rejection of a one-million-character paste before layout or document mutation.

The existing editor sample group also passes 16/16 tests, and the editor sample
builds for net10.0 Staging with zero warnings and zero errors.
