# Complete-frame raster fallback implementation plan

> Execute inline, continuing the user's approved Terminal/DCurses fallback work.

**Goal:** Let the atlas sample use verified ordinary Kitty or Sixel output when persistent Kitty resources are unavailable.

**Architecture:** Terminal owns verification, backend choice, encoding, and the output transaction. DCurses adds an explicit complete-frame refresh; the application owns image generation and fallback. Persistent atlas ownership stays unchanged.

**Tech stack:** C#, .NET 8/9/10, Icod.Terminal 1.25.0-alpha; no native calls or terminal-name branching.

## Contract

- Add `CursesSession.RefreshRasterAsync(TerminalRasterImage image, int row, int column, CursesRasterAtlasGeometry geometry, CancellationToken cancellationToken = default)`.
- Image dimensions must exactly match caller-supplied geometry. Placement must fit the current screen and leave its final row unused to avoid graphics scrolling. Geometry must be re-observed after resize/font changes.
- One explicit frame per call; no retained source image, persistent identity, automatic replay, or backend selection in DCurses.
- Reject overlapping visible panels and retained raster cells before output. Applications can temporarily show a text view for overlapping help.
- Clear the screen and repaint text, then position/write the image and restore the cursor from unknown position, inside one Terminal transaction. Clear/repaint on the first ordinary refresh after a frame, including recovery from uncertain output. No retry after partial output.
- Terminal must have current verified ordinary raster evidence; transaction preflight remains the zero-byte rejection boundary.

## Tasks

- [x] Add scripted tests for transaction order, final cursor, repeated frames, text transition, unsupported evidence, invalid geometry/bounds, panel overlap, cancellation, and failed output recovery.
- [x] Add the refresh API, prepared output adapter, conservative physical-state handling, and diagnostics.
- [x] Update sample selection: atlas → complete frame → text. Add `--raster` to exercise ordinary raster without persistent negotiation; keep `--text`. Avoid retransmitting on idle resize polls. Use text while help overlaps; rebuild exact geometry on resize.
- [x] Update runtime/test dependency and package verification gates to 1.25.0-alpha, API fingerprint, sample documentation, release notes, and both roadmaps.
- [ ] Run CI on PR35; inspect failures and fix them. Keep live terminal acceptance pending until the user tests the new sample.

## Review focus

Resize between image generation and refresh must reject before output. Images must not scroll the status bar. A failed commit must never publish physical certainty. Ordinary text refresh must remove previous raster damage. Idle polling must not retransmit full frames.

## Execution evidence

Local starting tree `dba507acd58c2c8f2ee244f8dab5298066072120` matches remote PR35 head `fadd99547b01887b231d354b5a3e41aceeb2dc0a`. No local .NET SDK; automated execution is delegated to existing CI, not claimed locally. The continuing user approval covers implementation and PR35 updates; no new design approval round is required.

Implementation heads: `161c154112a09acfc2bc5d36e06dc3215b7e14a4` and `6d70fa04ff8f705ff828d8e424867f0f88d237f3`. Initial CI compiled all six lanes and passed packaging; it exposed the expected two contract updates and a Sixel fixture incorrectly recognizing Kitty. The fixture now sends only primary DA with Sixel support when Kitty is absent. CI observed 105 exported types / 839 contract lines and fingerprint `b86ba658fd90d041eeccf5345ecc71a978bea785f0b2bbb9b8274091ccc4afc1`.

Final review performed inline. A sample resize discovered by refresh itself could otherwise be missed by the following dimension poll; the sample now compares against the last arranged dimensions and explicitly rebuilds geometry after a preflight resize rejection. Complete-frame panel overlap is deliberately rejected, with text help in the sample; live physical clearing/placement remains an acceptance requirement. No source image is retained by the refresh engine.
