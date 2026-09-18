# Icod.DCurses 2.0 T2003 Vertical Cutover Design

**Date:** 2026-09-18.\
**Status:** approved architecture; implementation not yet started.\
**Applies to:** T2003, with explicit staging consequences for T2004 and T2005.\
**Prerequisites:** accepted T2001 readiness gate and accepted T2002 public cutover gate.\
**Published dependency:** `Icod.Terminal 1.18.0`

## 1. Decision

T2003 uses a vertical cutover. It introduces the final transaction-backed output
direction while migrating presentation capabilities, rendition, line glyphs, ACS,
cursor movement, alerts, and rendition recovery to Terminal-owned semantic APIs.
It does not create a temporary second renderer and does not keep a mixed raw/semantic
output path.

Every refresh prepared by the migrated engine is represented as one ordered
`TerminalScreenOutputTransaction`. The transaction contains opaque Terminal plans,
application text, typed hyperlinks, and typed raster-placeholder cells. DCurses
retains logical composition, damage, operation eligibility, desired-versus-physical
comparison, and speculative physical-state policy.

T2003 temporarily falls back to ordinary rewriting wherever erase, character-shift,
line-shift, or scrolling-region optimization would require a still-unmigrated raw
TermInfo capability. T2004 restores those optimizations with Terminal plans and
Terminal-reported costs. T2005 remains a separate acceptance gate for exhaustive
transaction, capacity, cancellation, synchronization, uncertainty, and cleanup
hardening and for deleting the obsolete output shims.

No intermediate T2003 package is a supported 2.0 preview or publication candidate.

## 2. Why this staging is required

`TerminalScreenOperationPlan` is intentionally opaque and session-bound. Its encoded
control data can be emitted only by adding it to a transaction created by the same
`TerminalSession`. DCurses cannot extract a plan's terminal string and cannot safely
interleave a plan transaction with the existing borrowed/raw output path.

Consequently, a planner-only T2003 would require one of three undesirable states:

1. an unused semantic renderer beside the production renderer;
2. raw and transaction output interleaved without one atomic ordering authority; or
3. repeated single-operation transactions that flush and publish independently.

The vertical cutover avoids all three. It moves the renderer onto one transaction
boundary first, temporarily chooses rewrite over unmigrated optimizations, and then
restores those optimizations in T2004 without changing output ownership again.

## 3. Ownership and invariants

### Terminal owns

- immutable `TerminalProfile` and `TerminalScreenCapabilities`;
- rendition normalization and safe baseline/transition/reset planning;
- ACS glyph resolution and ACS mode plans;
- cursor and alert plans;
- opaque operation encoding, padding, byte cost, and affected-line count;
- transaction item/payload limits, session/epoch validation, output serialization,
  synchronized framing, typed hyperlink/raster output, flush, and protocol cleanup.

### DCurses owns

- public curses-shaped capability observations;
- conversion between curses semantic values and Terminal semantic values;
- retained cells, styles, metadata, raster coordinates, Unicode width, and fallback;
- windows, pads, panels, clipping, composition, damage, and desired screen state;
- whether a safe operation reproduces the desired screen and is worth selecting;
- speculative cursor/rendition/screen state and post-commit publication;
- retaining damage and invalidating physical certainty after failure.

### Permanent invariants

- Production code does not inspect TermInfo capability identifiers in migrated paths.
- Production code does not extract, reconstruct, or log opaque plan control strings.
- One refresh creates and commits at most one screen transaction.
- No output occurs while a refresh is being prepared.
- Logical intent is not rolled back merely because terminal commitment fails.
- Physical state is published only after successful commitment.
- A failed or uncertain commitment retains damage and makes physical state unknown.
- A null essential plan is a controlled unsupported failure, never permission to
  synthesize an escape sequence.
- No new public API or additional 1.6-to-2.0 break is introduced by T2003.

## 4. Semantic mapping layer

T2003 adds explicit internal conversions. Numeric enum equivalence is never assumed.

### Presentation capabilities

`CursesPresentationCapabilities.Create` consumes
`TerminalScreenCapabilities`, obtained from `CursesSession.Profile.Screen`. It maps:

- indexed/direct color and foreground/background/default-restoration observations;
- each `TerminalTextAttributes` flag to the corresponding
  `CursesTextAttributes` flag;
- color-restricted attributes through the same explicit flag map;
- ACS and cursor-visibility observations directly.

All existing public properties and values remain unchanged for equivalent profiles.
The production type no longer imports or accepts `Icod.TermInfo`.

### Colors, attributes, and rendition

An internal rendition adapter converts:

- `CursesColor.Default` to `TerminalScreenColor.Default`;
- non-negative indexed colors to `TerminalScreenColor.Indexed`;
- RGB colors to `TerminalScreenColor.Rgb`;
- every known curses attribute through an explicit Terminal flag map.

`TerminalScreenPlanner.NormalizeRendition` becomes the sole production authority for
supported colors, reversible attributes, standout fallback, and no-color-video
restrictions. The normalized Terminal result is converted back to `CursesStyle` for
DCurses speculative physical-state comparison. Invalid internal enum/color states
continue to fail deterministically rather than being silently reinterpreted.

Known-state transitions use `PlanRenditionTransition` or `PlanRenditionReset`.
Unknown physical rendition uses `PlanRenditionBaseline`; DCurses never substitutes
`TerminalScreenRendition.Default` for unknown state. A missing required baseline or
transition plan fails closed and leaves the screen damaged.

### Line glyphs and ACS

`CursesLinePresentationResolver` maps each `CursesLineGlyph` explicitly to
`TerminalLineGlyph` and calls `ResolveLineGlyph`.

When Terminal supplies a representation, DCurses retains its content and ACS-mode
requirement. When Terminal returns no representation, DCurses retains its existing
policy: use the canonical Unicode glyph if the configured width provider reports one
column; otherwise use the established ASCII fallback. ACS entry and exit are emitted
only through `PlanAlternateCharacterSet`.

### Cursor movement and alerts

`CursesCursorMotionResolver` becomes a thin DCurses validation/selection adapter over
`PlanCursorMove`. It supplies a nullable known current position and a required target
position, and returns the opaque plan and its Terminal-owned `ByteCount`. It does not
expand capabilities or calculate terminal-control byte costs.

Public `SetCursorPositionAsync` preserves its current contract that absolute cursor
addressing must be advertised. Refresh-internal movement may use any safe candidate
selected by Terminal from the known current position.

`AlertAsync` maps `CursesAlertKind` explicitly to `TerminalAlertKind` and uses
`PlanAlert`, preserving preferred/fallback behavior. A null plan returns `false`.

## 5. Transaction-backed prepared refresh

T2003 introduces an internal prepared-refresh component owned by one
`CursesRefreshEngine` operation. It wraps one
`TerminalScreenOutputTransaction` and a local speculative physical-state snapshot.

The component exposes only semantic additions required by the renderer:

- add an opaque `TerminalScreenOperationPlan`;
- append application text;
- append a typed hyperlink and its text;
- append one or a batch of typed raster-placeholder cells; and
- commit exactly once.

It does not expose raw strings as terminal controls. Application text remains text;
only Terminal plans may represent controls.

The engine receives the owning `TerminalSession`, `TerminalProfile`, and
`TerminalScreenPlanner` instead of a `TerminalDescription` plus raw output writer.
Test seams record semantic additions and commit outcomes rather than implementing
TermInfo expansion. Synthetic TermInfo profiles may remain confined to the accepted
test-bootstrap exception used to create real Terminal sessions.

### Refresh sequence

1. Acquire existing DCurses activity/refresh coordination.
2. Complete dimensions, lifecycle, and presentation-lease work that may itself emit
   Terminal output.
3. Capture desired content, damage, and the current physical-state certainty.
4. Create one transaction, thereby capturing the Terminal output epoch.
5. Prepare an ordered batch using only Terminal plans and typed/text transaction
   operations while updating a local speculative physical state.
6. Validate capacity and all required plans before commitment.
7. Commit once.
8. On success, publish the speculative physical state and clear only the captured
   damage that is still current.
9. On failure, retain logical content/damage, invalidate physical certainty, and
   propagate the original Terminal exception without an automatic retry.

Synchronized refresh uses `TerminalScreenOutputTransactionOptions.UseSynchronizedOutput`.
The existing outer synchronized-output lease is not used around a transaction.

Application text runs are coalesced where adjacent output has identical semantic
requirements. Raster-placeholder cells use the transaction's batch operation where
ordering and retained composition permit it. These rules avoid consuming one bounded
transaction item per ordinary screen cell.

## 6. Temporary T2003 optimization policy

The existing erase, character-shift, line-shift, and scrolling-region resolvers still
depend on TermInfo expansion and raw control output. T2003 does not carry their raw
results into the transaction and does not split output around them.

Instead, the refresh engine rejects those optimization candidates and uses its safe
ordinary rewrite path. This is a temporary output-volume/performance difference, not
a visible-screen semantic difference. Eligibility logic and tests are retained so
T2004 can replace only the operation-planning/cost side with:

- `PlanErase`;
- `PlanCharacterShift`;
- `PlanLineShift`;
- `PlanScrollRegion`; and
- plan `ByteCount` / `AffectedLines`.

No T2003 package is published. T2004 must restore accepted optimization behavior and
regret evidence before the migration can advance to T2005.

## 7. Direct presentation operations

Operations outside a normal refresh still use Terminal transactions and the same
physical-state rules:

- `AlertAsync` commits one alert plan and does not alter screen certainty.
- `SetCursorPositionAsync` updates logical cursor intent, commits one cursor plan,
  publishes the physical cursor only on success, and invalidates it on failure.
- `ResetRenditionAsync` selects a known-state reset or unknown-state baseline,
  commits one plan, and publishes default rendition only on success.

These operations retain existing activity coordination. They do not borrow raw
output and do not silently retry a stale transaction.

## 8. Failure, cancellation, and lifecycle behavior

Preparation failures, including null required plans, invalid media ownership, item
capacity, and payload capacity, emit zero bytes. Damage and logical content remain.

Cancellation before `CommitAsync` emits nothing. Once commitment begins, Terminal's
commitment and cleanup policy is authoritative. DCurses does not catch cancellation
or output failures merely to mark content clean.

Any commitment exception makes cursor, rendition, and physical-screen knowledge
conservatively unknown. The exception is preserved. T2005 adds exhaustive witnesses
for partial writes, flush failures, cleanup conflicts, stale epochs, disposal races,
and primary-plus-cleanup failure reporting; T2003 establishes the same fail-closed
state transition without claiming that the adversarial matrix is complete.

Resize, suspend, resume, output invalidation, and raster ownership loss continue to
preserve logical intent and invalidate physical certainty. A later refresh must use
Terminal's rendition baseline before relying on default physical state.

## 9. Tests and acceptance witnesses

T2003 follows a retained RED/GREEN sequence. Existing tests are adapted, not deleted.

### Mapping and planner tests

- every capability and attribute flag maps explicitly;
- indexed/RGB/default colors normalize identically to Terminal;
- unsupported and non-reversible requests normalize safely;
- known transition, reset, and unknown baseline plans are selected correctly;
- every line glyph maps to Terminal ACS, Unicode, or ASCII fallback correctly;
- cursor plans preserve known/unknown current-position behavior and plan cost;
- alert preference/fallback and unsupported results are preserved.

### Transaction and state tests

- one refresh creates one transaction and commits once;
- plans, text, hyperlinks, and raster cells retain required order;
- synchronized refresh is configured on the transaction rather than an outer lease;
- no production raw-output method is invoked;
- successful commit publishes speculative physical state;
- preparation or commit failure retains damage and invalidates physical certainty;
- an invalidation/new damage event during commitment survives publication;
- large ordinary text runs are coalesced within item/payload limits.

### Application-shaped witnesses

- **Roguelike:** sparse styled cell changes, Unicode/ACS borders, cursor movement, and
  scrolling intent produce a correct atomic frame; optimization is allowed to fall
  back to rewrite until T2004.
- **Pixel-art terminal game:** text, panels, and Terminal-owned raster placeholders
  commit in one ordered frame and remain damaged on failure.
- **Sprite-like terminal workload:** cell-aligned retained raster placeholders move,
  clip, and compose deterministically. This is not a general sprite-engine claim.
- **Screen editor:** sparse text edits, cursor placement, rendition changes, and line
  rewrite remain correct; T2004 separately restores insert/delete/scroll efficiency.

The complete `net8.0`, `net9.0`, and `net10.0` suite, package candidate, and
Windows/Linux/macOS x64/ARM64 matrix must pass on the exact T2003 acceptance head.

## 10. Tranche consequences

### T2003 acceptance

T2003 accepts the semantic mapping and transaction-backed vertical cutover. All
visible presentation behavior must pass except for the explicitly temporary choice
to rewrite instead of using erase/shift/scroll optimizations. No public API fingerprint
change is permitted.

### T2004 acceptance

T2004 restores erase, character-shift, line-shift, and scroll-region optimizations
using opaque Terminal plans and Terminal-owned costs. It removes the temporary rewrite
difference and reruns the editor/roguelike performance and exact-operation witnesses.

### T2005 acceptance

T2005 no longer means the first transaction use. It is the exhaustive transaction
qualification and cleanup gate: full capacity/coalescing, synchronized framing,
hyperlink/raster failure paths, stale epochs, publication races, cleanup failures,
and removal of the raw output/capability-writer shims.

The 2.0 roadmap and tranche plans must be amended to reflect these responsibilities
before implementation claims acceptance.

## 11. Non-goals

T2003 does not:

- remove the direct TermInfo package reference; T2007 owns that final proof;
- eliminate the accepted test-only synthetic-profile bootstrap;
- add public API or change the approved 2.0 fingerprint;
- publish an alpha package;
- add widgets, a game loop, animation scheduling, collision detection, image decoding,
  arbitrary graphical transforms, or a general sprite/raster scene graph;
- restore erase/shift/scroll optimizations before T2004; or
- merge, tag, release, or publish the branch.

## 12. Completion criteria

The design is implemented only when all of the following hold:

1. migrated presentation/core-screen production paths contain no TermInfo capability
   inspection, expansion, padding, raw control construction, or borrowed output;
2. one refresh uses one Terminal screen transaction with ordered semantic content;
3. basic prepare/commit/publish failure behavior is fail-closed;
4. rewrite fallback replaces every still-unmigrated optimization without semantic
   screen corruption;
5. direct cursor/alert/reset operations use Terminal plans and transactions;
6. the public API fingerprint remains the accepted T2002 fingerprint;
7. retained unit, integration, mixed-media, application-shaped, package, and platform
   evidence passes on one exact head; and
8. the gate records the temporary T2004 optimization debt and does not claim final
   direct-dependency removal or release readiness.
