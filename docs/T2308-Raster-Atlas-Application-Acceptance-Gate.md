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

- [ ] status reports `ATLAS`;
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

The current candidate consumes Terminal 1.25.0-alpha.5, including bounded size-aware persistent-raster transfer deadlines, kitty DA1 compatibility, immediate Unix byte input, the relative-cursor correction and coalesced Sixel screen writes. The October 3 17:20–17:22 UTC recordings show Contour movement without the earlier text flash, Windows Terminal with progressive horizontal image redraw, and WezTerm text fallback without the earlier left-margin trails. The 03:46 UTC WezTerm configuration test was inconclusive and is superseded by the restart/retest evidence below. These earlier recordings predate alpha.2 consumption.

Repeat default and `-- --raster` runs in Windows Terminal and Contour, following the [sample checklist](../samples/Icod.DCurses.RasterAtlas.Sample/README.md#live-acceptance-for-the-new-frame-path). Confirm FRAME pixels without text-map flashes, movement, camera, water collision, help text transition and image restoration, repeated resize, no stale images or repeated status bars, and Q/Escape cleanup. Run default and `-- --text` in WezTerm and confirm movement leaves no left-margin trails. `-- --text` must also work in the other two terminals. Unknown raster capability may still select TEXT; alpha.4 corrects DA1 parsing while preserving independent graphics evidence requirements.

### October 4 04:10–04:12 UTC acceptance update

The maintainer accepts performance in Windows Terminal and Contour after pulling
the Terminal 1.25.0-alpha.2 integration. Both recordings run `-- --raster`, show
`FRAME` graphics and player movement, and return cleanly to the prompt. This
accepts the reported movement/redraw performance in these two environments; it
does not claim universally flicker-free or atomic terminal presentation.

The third recording shows WezTerm running `-- --raster`, selecting `TEXT` with
`Raster unavailable; using text.`, moving, and returning cleanly to the prompt.
Earlier 03:58/04:01 UTC tests on the pre-integration checkout establish that the
three ligature settings are sufficient after restarting WezTerm; `kern=0` is not
required by that comparison. The kerning hypothesis is withdrawn.

Help open/close with frame restoration, repeated shrink/grow with status/image
cleanup, and explicit separate Q/Escape checks remain to be recorded on the current
candidate. These clips do not exercise persistent `ATLAS` ownership or establish
camera-scroll/water-collision acceptance for this exact dependency head.

### October 4 help, resize and exit follow-up

The subsequent 04:16–04:17 UTC recordings and maintainer confirmation accept
help opening/closing with FRAME restoration, resize recovery and status cleanup,
and both Q and Escape exit in Windows Terminal and Contour on alpha.2. Contour
briefly shows an old image strip during active resize; it clears when dimensions
settle. These observations supersede the pending help/resize/exit items above,
but do not establish persistent ATLAS support or artifact-free live dragging.

### WSL2/kitty alpha.5 persistent-atlas retest required

The WSL2/kitty alpha.2 text run required Enter and echoed keys. Terminal alpha.3
fixed the upstream byte transport; the maintainer accepts immediate input and
clean exit. The subsequent application log identified the DA1 parser exception
on kitty 0.32.2's empty optional attribute list. Published Terminal alpha.4 fixes
that parser. The next run reached persistent frame creation and exposed the fixed
one-second transfer deadline; published alpha.5 supplies the bounded size-aware
deadline and DCurses now consumes it. Follow the
[kitty retest](../samples/Icod.DCurses.RasterAtlas.Sample/README.md#wsl2kitty-retest-with-terminal-alpha5):
briefly check TEXT input, then explicit FRAME and default ATLAS selection. Record
the actual mode and fresh stderr. FRAME display is accepted; persistent ATLAS
ownership, sparse updates and command-driven flicker remain separate acceptance.
