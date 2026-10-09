# Icod.DCurses 2.3.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release:** `2.3.0`  
**Theme:** First-class `CursesRasterAtlas` coordination (approved Option 1)  
**Baseline:** published and tagged `v2.2.0`  
**Current source and package version:** `2.3.0`  
**Assembly version:** `2.0.0.0`  
**Direct runtime dependency:** stable `Icod.Terminal 1.28.0`; no direct `Icod.TermInfo` reference
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Status:** Stable 2.3.0 source/package candidate qualified. T2301–T2309 are implemented and the documented ATLAS/FRAME/TEXT live checks are accepted in their tested environments. Stable implementation/package head `8c528a5ec8925635f7c704091fda3fe187d9ffe7` passed all seven jobs in [PR Staging run 1374](https://github.com/uniblab/Icod.DCurses/actions/runs/37970187901). Documentation closure is complete; merge, post-merge Release validation, tagging, and publication remain maintainer actions.
**Planning snapshot:** 2026-10-04

**Design authority:** [`CursesRasterAtlas` design](docs/superpowers/specs/2026-10-03-icod-dcurses-2.3-curses-raster-atlas-design.md)  
**Implementation plan:** [2.3 atlas implementation plan](docs/superpowers/plans/2026-10-03-icod-dcurses-2.3-curses-raster-atlas.md)

---

## 1. Release decision and goal

The approved choice is Option 1: add a generalized, first-class `CursesRasterAtlas` to DCurses 2.3. Terminal 1.24 now provides the missing mechanics—pixel geometry, planning observations, intrinsic resource geometry, focused operation evidence, acknowledged frame composition, RGB24/RGBA32 partial replacement and frame selection. DCurses can therefore own the presentation policy that belongs above those mechanics.

The release target is a cell-aligned tile renderer suitable for an Ultima-style application while preserving functional breaks:

```text
game/application model
    source art, maps, actors, rules, loop, fallback
                    |
                    v
DCurses 2.3 CursesRasterAtlas
    tile geometry, retained coordinates, damage list,
    front/back sequencing, panels/clipping/refresh ordering
                    |
                    v
Terminal 1.24
    active geometry, opaque resources/frames/placeholders,
    acknowledged mutations, selection and lifecycle evidence
```

This is not a game engine and not a generic raster scene graph.

## 2. Compatibility and architecture requirements

- Preserve every published 2.2 public member and enum value. The 2.3 surface is additive; `AssemblyVersion` remains `2.0.0.0`.
- Keep the production graph `Icod.DCurses -> Icod.Terminal -> Icod.TermInfo`. No raw terminal string, direct TermInfo access or protocol-private identity enters DCurses.
- Reuse the existing `CursesRasterCell`, windows, pads, viewports, panels, clipping, editing, composition, damage and refresh machinery. Do not add a second retained raster plane.
- Keep application assets caller-owned. DCurses must not retain arbitrary image data merely to replay after lifecycle loss.
- Serialize atlas mutations through the existing session terminal-activity gate so they cannot interleave with refresh or lifecycle output.
- Validate and copy a complete presentation request before first output. Never retain caller collections.
- Treat Terminal planning/inspection as advisory. Actual controlled results and acknowledgement remain authoritative.
- Mark ambiguous committed failure as requiring recreation; never blind-retry or pretend a remote transaction is atomic.
- Keep all limits explicit and checked before allocation/output.
- The Ultima-style sample is an acceptance witness, not an API excuse for game-specific names or types.

## 3. Planned public capability

### 3.1 Atlas geometry

Add a bounded cell-pixel geometry query for an atlas grid. Use the direct Terminal cell-pixel query first. Only its timeout may select exact terminal-pixel/character-grid derivation. Reject fractional geometry, overflow and stale assumptions; applications re-query after resize.

### 3.2 Atlas ownership and creation

Create one session-owned facade around one Terminal raster resource, one virtual placeholder grid and two known full-size frames. The initial immutable image must divide exactly into the requested rows and columns. Expose immutable atlas/tile pixel geometry, retained `GetCell`, conservative ownership and recreation-required state—never Terminal ids or frames.

### 3.3 Retained projection

Add a validate-before-mutation `CursesWindow.WriteRasterAtlas` bulk helper for an atlas source rectangle. It projects existing `CursesRasterCell` values into the current retained plane, preserves text/metadata, does not move the cursor, and inherits window/pad/panel/viewport clipping and damage.

### 3.4 Double-buffered presentation

Add bounded tile updates with deterministic row-major ordering. Each nonempty presentation copies current front to back with replacement composition, applies acknowledged tile regions, selects the back only after success, then swaps internal front/back references. Empty work performs no terminal I/O.

Controlled failures return explicit status and completed-update counts without selecting a partial back frame. Ambiguous committed failures poison the atlas for further use and require application-driven recreation from durable source art.

## 4. Tranche plan and gates

| Tranche | Deliverable | Acceptance gate |
|---|---|---|
| **T2300** | Record Option 1; add design, roadmap and implementation plan; advance to `2.3.0-alpha.1`; update the sole direct dependency to Terminal 1.24.0 | Documents agree on ownership, sequencing, failure and non-goals; project identity/reference are exact; no atlas implementation is claimed |
| **T2301** | API-regret review and permanent failing contract tests | Exact names, enum values, defaults, validation, limits, result semantics and XML intent frozen; RED failures are only missing 2.3 API |
| **T2302** | Pixel geometry and planning integration | Direct query, timeout-only exact fallback, resize/re-query, overflow, malformed/cancel/transport behavior and advisory capacity tests pass |
| **T2303** | Atlas creation, immutable geometry, ownership and rollback | Resource/placeholder/two-frame order is correct; all partial failures clean up in reverse; no source image is retained; disposal is idempotent |
| **T2304** | Window/pad/viewport atlas projection | Bulk projection is failure-atomic and cursor-neutral; clipping, panel transparency/z-order, scrolling, copying, resize and damage retain published behavior |
| **T2305** | Front/back presentation coordinator | Front-to-back replace precedes row-major updates; selection is last; swap occurs only after selection; empty/duplicate/oversized/format/dimension cases are exact |
| **T2306** | Refresh serialization and lifecycle/failure hardening | Atlas work cannot interleave with refresh/lifecycle output; definite failures recover through the next front copy; ambiguous failures and generation loss require recreation |
| **T2307** | Adversarial, capacity, allocation and workload measurement | Bounds fail before output; 1/4/16/64/121/256-tile package workloads recorded; no map-sized storage or hidden image cache; later Terminal work is evidence-gated |
| **T2308** | Public-only Ultima-style sample and package-only consumer | Raster and explicit text fallback share one model; movement, overlays, viewport, resize/recreate and clean exit pass automated/manual acceptance |
| **T2309** | Public API, package, dependency, XML and documentation freeze | Stable 105-type / 839-line contract and `Icod.Terminal 1.28.0` package boundary qualified at implementation/package head `8c528a5e` in seven-job PR Staging run 1374 |
| **T2310** | RC, scoped live-terminal evidence and stable-source qualification | Stable source/package head green across the PR matrix; ATLAS/FRAME/TEXT evidence and limitations recorded for the tested environments; merge, post-merge validation, tag and publication remain maintainer actions |

Every tranche records an exact commit and evidence document. Green CI is necessary but not sufficient: semantic, package and manual gates still apply.

## 5. Test strategy

### Pure contract tests

- geometry construction, checked multiplication and exact divisibility;
- tile-coordinate to pixel-coordinate mapping;
- update-list copying, duplicate rejection and deterministic row-major order;
- empty presentation and capacity boundaries;
- window source/destination validation and failure atomicity.

### Terminal integration tests

- direct cell query success and timeout-only exact fallback;
- advisory planning snapshot checked before allocation without treating it as a reservation;
- resource, placeholder, second-frame creation and every partial rollback point;
- focused operation evidence per composition/RGB24/RGBA32 operation;
- exact command order: front copy, updates, select, swap;
- controlled unsupported/unavailable/failed results at each step;
- timeout, cancellation, malformed reply, transport loss and resource-missing invalidation.

### Retained-presentation tests

- windows, subwindows, pads and viewports;
- panel transparency and z-order;
- scrolling, copy/overlay, resize and clipping;
- sparse damage and unchanged text/metadata;
- stale/released/disposed atlas cells rejected before output;
- atlas operation and refresh/lifecycle mutual exclusion.

### Package and live evidence

- fresh NuGet-only consumers for `net8.0`, `net9.0` and `net10.0`;
- Windows/Linux/macOS x64/ARM64 Staging matrix;
- representative Kitty-capable live terminal for tile alignment, overlays, resize/recreation and clean exit;
- text fallback on a terminal where the raster path is unavailable.

## 6. Bounds and failure policy

- Atlas rows/columns: 1–256 each.
- Updates per presentation: 0–4096, unique by atlas coordinate.
- Update images: exact tile dimensions and RGB24 or RGBA32.
- All row/column/pixel products use checked or widened arithmetic before narrowing.
- Creation and presentation requests are copied/validated before first mutation or output.
- A controlled failure before selection never presents the partial back frame. The next call resynchronizes it from the known front.
- An exception after possible output commitment sets `RequiresRecreation`; subsequent present calls fail before output.
- Lifecycle loss is not repaired from hidden data. The application recreates from its own source art and replaces retained cells.
- No claim of remote atomicity, gapless display or terminal memory reservation is made.

## 7. Documentation and sample requirements

Before T2309, update the README with the atlas ownership boundary, minimum Terminal version, fallback guidance, failure/recreation rule and a compact public usage example. The sample README must explain which terminals are expected to support the raster path, how to force text fallback, how resize recreation works, and what the sample does not prove.

The Ultima-style sample must remain clearly original and minimal. It demonstrates a top-down tile viewport and status/help overlays; it must not copy copyrighted Ultima art, maps, names or data.

## 8. Non-goals

No tile-map container, camera policy, entity/component system, collision, visibility/fog, pathfinding, AI, combat, inventory, persistence, game loop, asset file decoder, palette conversion, texture scaling, arbitrary transforms, physical sprite scene, sub-cell motion, animation scheduler, widget library, terminal emulator or PTY host enters 2.3.

No hidden fallback ladder, source-image replay, automatic retry, raster batching, Indexed8 partial updates, remote-capacity query or protocol extension is invented in DCurses. Any later Terminal integration remains subject to the T2307 package-evidence boundary.

## 9. Release policy

T2309 is the API/package regret deadline. After it, only fixes required by acceptance evidence enter the release. T2310 records the exact stable source/package qualification. Merge, the post-merge Release workflow, tag, GitHub Release and NuGet publication remain separate maintainer actions.

## 10. Immediate next step

Merge the reviewed stable candidate after its documentation-only PR matrix is green.
Then require the merge commit to pass the `main` Release workflow before creating and
pushing `v2.3.0`. GitHub Release and NuGet publication remain explicit maintainer
actions. No further feature development or protocol workaround enters this release.

## Historical complete-frame fallback extension (2026-10-03)

The approved fallback work reopened T2308 and T2309 for Terminal 1.25.0-alpha. DCurses
added explicit `RefreshRasterAsync`: one application-owned frame, exact geometry, the
same serialized transaction as text, conservative damage cleanup, and no persistent
identity emulation. Terminal owns Kitty/Sixel selection and encoding. The sample prefers
atlas, then complete frame, then text; `--raster` directly exercises the second path.

Automated integration and the additive API fingerprint were subsequently requalified.
Windows Terminal and Contour supplied the requested FRAME observations, WezTerm supplied
a controlled fallback observation, and the later Terminal 1.28.0 / Kitty 0.49.2 retest
supplied the accepted ATLAS witness recorded in the stable-release section below.

### October 3 recording follow-up

The supplied Windows Terminal and Contour recordings show graphics in `FRAME` mode,
but the fallback text map flashes between images. Complete-frame refresh now omits
covered logical text and the sample requests synchronized output. Wide text may not
straddle an image edge. The regression-only run reproduced all four new cases with
1,359 existing tests passing per framework ([run 37136861256](https://github.com/uniblab/Icod.DCurses/actions/runs/37136861256)).

The WezTerm recording shows text fallback with left-margin player trails, reopening
its earlier text acceptance. Terminal [PR #73](https://github.com/uniblab/Icod.Terminal/pull/73)
published 1.25.0-alpha.1 to reject newline-dependent relative cursor plans. The initial DCurses
integration consumed that package in production and tests, with matching package and
dependency-boundary gates. Repeat default, `-- --raster`, and `-- --text` tests.
No terminal rendering or
stable-release acceptance is claimed by CI alone.

### 17:20–17:22 UTC retest

The next recordings show Contour complete-frame movement without the earlier text
flash. Windows Terminal replaces the text flash with visible clearing and partial
horizontal image bands; immediate-frame acceptance remains open. Terminal
[PR #74](https://github.com/uniblab/Icod.Terminal/pull/74) coalesces prepared Sixel
fragments into one bounded image write, with unchanged bytes and failure rules.
At this retest, DCurses remained on alpha.1 while alpha.2 publication was pending.

WezTerm remains TEXT. The player no longer leaves left-edge trails in the supplied
recording, but terrain glyphs appear joined or uneven. Compare with font ligatures
disabled before attributing this remaining symptom to cursor planning. See the
sample README for the isolated check. Resize/help acceptance is still outstanding;
these movement recordings do not qualify every checklist item.

### Published alpha.2 integration (2026-10-04 UTC)

Terminal 1.25.0-alpha.2 is now published. Production and test references, package
verification, dependency guards, and current documentation now require alpha.2.
DCurses remains 2.3.0-alpha.1 with AssemblyVersion 2.0.0.0 and an unchanged public
API. Dependency head `84175cf817b8b092e0d7dbe1198c82f11e635740` passed all six
runtime lanes and the package/fresh-consumer gate in
[workflow 1323](https://github.com/uniblab/Icod.DCurses/actions/runs/37175860045).

The 03:46 UTC WezTerm recording after disabling ligatures shows no clear improvement.
Ground dots beside the moving player shift roughly 1–2 pixels; kerning remains an
unconfirmed explanation at that point, superseded by the later retests below. Alpha.2
does not address text shaping or change graphics capability verification.

Repeat default and forced-frame runs in Windows Terminal and Contour to assess
Sixel redraw, then help/resize/status cleanup and Q/Escape. A single Sixel write
cannot prove atomic host rendering. Persistent-atlas acceptance and T2310 remain
open; this dependency bump does not promote the DCurses release.

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

### Published alpha.3 Unix input integration (2026-10-04 UTC)

WSL2/Ubuntu 24.04 with kitty 0.32.2 rendered text but waited for Enter and echoed
keys into the map. Terminal [PR #75](https://github.com/uniblab/Icod.Terminal/pull/75)
corrected the process-standard-input transport for Linux and macOS. Its real
pseudo-terminal regression passed CBreak/Raw immediate input, UTF-8, response
routing, no echo, cancellation, restoration and reopening on .NET 8/9/10 in
[run 37180001042](https://github.com/uniblab/Icod.Terminal/actions/runs/37180001042).

At this checkpoint DCurses consumed published 1.25.0-alpha.3 in production, tests, package checks
and the release dependency guard. DCurses remains 2.3.0-alpha.1; its public API
and assembly identity are unchanged. This dependency update requires its own
CI qualification, recorded in PR #35. Follow the sample
[WSL2/kitty checklist](samples/Icod.DCurses.RasterAtlas.Sample/README.md#wsl2kitty-acceptance-with-terminal-1280)
after pulling. Earlier Kitty graphics parser errors are not independently claimed
fixed, and persistent ATLAS acceptance remains open. No native I/O or protocol
workaround is added to DCurses.

### Published alpha.4 DA1 compatibility integration (2026-10-04 UTC)

The maintainer accepts immediate input and clean exit in WSL2/kitty on alpha.3.
The captured startup exception identified Terminal's strict DA1 parser rejecting
kitty 0.32.2's `CSI ?62;c` empty optional attribute list. Terminal
[PR #76](https://github.com/uniblab/Icod.Terminal/pull/76) corrects that parser and
retains independent graphics evidence requirements. Alpha.4 is now published.

DCurses consumes 1.25.0-alpha.4 in production, tests, package checks and the release
dependency guard. Package version 2.3.0-alpha.1, assembly identity and public API
remain unchanged. Dependency CI is recorded in PR #35. The next live acceptance
is forced FRAME followed by default ATLAS selection; report the actual mode and
capture fresh stderr. Persistent ATLAS acceptance and the earlier payload-size
report remain open until retested. No protocol workaround is added to DCurses.

### Published alpha.5 persistent-transfer deadline integration (2026-10-04 UTC)

The alpha.4 live retest verified correct FRAME input and display, then timed out
while Terminal added the second full-size frame to the persistent atlas. The root
resource and placeholder had already succeeded. Terminal
[PR #77](https://github.com/uniblab/Icod.Terminal/pull/77) replaces the fixed
one-second deadline for payload-bearing persistent uploads with a bounded
size-aware deadline while retaining one-second control-command deadlines and the
existing acknowledgement, cancellation and conservative failure semantics.

DCurses now consumes published `Icod.Terminal 1.25.0-alpha.5` in production,
tests, package checks and the release dependency guard. Package version
`2.3.0-alpha.1`, assembly identity and public API remain unchanged. The dependency
contract first failed against the alpha.4 production reference with three exact
identity failures and 1,361 other tests passing per framework in
[run 37221648749](https://github.com/uniblab/Icod.DCurses/actions/runs/37221648749).
The next live acceptance is default ATLAS startup followed by movement, collision,
camera scrolling, help restoration, resize cleanup and independent Q/Escape exit.
No timeout or protocol workaround is added to DCurses.

### Confirmed Kitty upload ACK defect; deliberately unresolved (2026-10-04)

The independent [two-pixel Bash reproducer](tools/kitty-frame-ack.sh) received
root and single-chunk `OK` replies in Kitty 0.32.2, no reply for the documented
`a=f,m=0` final continuation, and `Gi=1,r=4;OK` when the final continuation
repeated `i=1`. Frame 4 proves the silent upload created frame 3. Source inspection
agrees: the final response is constructed from continuation controls lacking an
image identifier. This establishes an upstream upload-acknowledgement defect,
not a rejected frame or an Icod response-matcher failure.

Alpha.5's live default run still times out on the first appended atlas frame and
falls back to FRAME. Forced TEXT works; forced/default fallback FRAME has correct
input and display but command-driven flicker. The size-aware deadline bounds the
wait; it does not cure the missing ACK. The [complete bug report](tools/kitty-frame-ack-bug-report.md)
records the captured output, source evidence, untested clean-config/new-version
cases, and submission status. Filing with `kovidgoyal/kitty` was attempted but
returned HTTP 403, `Resource not accessible by integration`; there is no upstream
issue number to cite.

The maintainer directs that this trouble remain unresolved after documentation
and reporting. No production workaround, alpha.6 dependency bump, further probe
expansion, or ATLAS acceptance is claimed. Animation-control response assumptions
and FRAME flicker are separate open concerns, outside the confirmed upstream
upload bug report. T2308/T2309 and T2310 remain open.

### Feature development hold and release boundary (2026-10-04)

The maintainer directs release preparation for the existing feature set, with no
workaround for the Kitty ACK defect. This decision supersedes earlier next steps
requiring further Kitty diagnosis or persistent-ATLAS live acceptance before RC.
It does not turn fallback FRAME observations into ATLAS acceptance. Retain the
current public surface and dependency; defer additional atlas/backend work.

The [release readiness record](docs/2.3-Release-Readiness.md) distinguishes automated
contract evidence, live rendering observations, deferred acceptance and outstanding
version/dependency and RC/stable decisions. Correct the stale alpha.4 release guard
to match the alpha.5 project reference. No renderer behavior changes are included.


## Stable 2.3.0 release preparation (2026-10-09)

Published stable `Icod.Terminal 1.28.0` is the sole direct production
dependency for the stable DCurses candidate. Dependency policy remains
version-agnostic: project, test, package-verifier, and release gates require the
single direct package identity `Icod.Terminal` and a declared package version;
successful restore, compilation, and tests establish API compatibility.

The candidate retains `AssemblyVersion 2.0.0.0`, the frozen 105-type /
839-line additive API contract, complete-frame fallback, recoverable
presentation-timeout fallback, and full-atlas update coalescing. The public
sample uses the supplied water, grass, forest, road, and player artwork and
exposes ATLAS, FRAME, and TEXT explicitly.

Maintainer acceptance covers immediate input, movement, water collision, camera
scrolling, help restoration, resize cleanup, and independent Q/Escape exit in
the recorded environments. This is not a claim of universally atomic or
flicker-free host presentation. Historical Kitty 0.32.2 acknowledgement
evidence remains preserved rather than generalized to current Kitty releases.

Release closure is procedural: implementation head `a6c580c` passed every Staging
runtime and package job in run 1373. The documentation-only closure head must pass
the same PR checks; the reviewed PR must be merged separately; the merge commit
must pass the `main` Release workflow; only then may `v2.3.0` be created and
pushed. This preparation does not merge, tag, or publish.
