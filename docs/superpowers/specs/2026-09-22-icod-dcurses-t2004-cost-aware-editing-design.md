# Icod.DCurses T2004 Cost-Aware Editing Design

**Release:** `Icod.DCurses 2.0.0`  
**Tranche:** T2004 — cost-aware erase, character shift, line shift, and scrolling  
**Development identity:** `2.0.0-alpha.1`  
**Dependency baseline:** published `Icod.Terminal 1.18.0`; temporary direct `Icod.TermInfo 1.15.0` remains until T2007  
**Status:** approved conversational design awaiting written-spec review

## Goal

Restore the erase, character-shift, line-shift, and scroll-region efficiencies
temporarily disabled by T2003 without reopening raw capability access or weakening
the accepted one-transaction prepare/commit/publish boundary.

The governing rule is:

> Terminal owns operation encoding, parameter expansion, padding interpretation,
> affected-line accounting, and terminal-byte cost. DCurses owns whether an
> operation exactly reproduces the desired retained screen and is worth selecting.

T2004 is successful when ordinary text and editor-shaped workloads can again use
safe editing operations, mixed text/raster applications retain correct regional
fallback, and the migrated optimization path contains no TermInfo interpretation,
raw terminal sequence, expansion, or `TPuts` cost calculation.

## Scope

T2004 includes:

- conversion of the retained erase, character-shift, and line-shift resolvers to
  `TerminalScreenPlanner` and opaque `TerminalScreenOperationPlan` values;
- deterministic use of Terminal-owned `ByteCount` and `AffectedLines`;
- reintroduction of the accepted optimization priority in the transaction-backed
  refresh engine;
- regional safety checks for semantic metadata and raster-placeholder content;
- speculative physical-state publication for selected operations;
- successful temporary scroll-region setup and restoration inside the one refresh
  transaction;
- conservative recovery when a failed transaction may have left a restricted
  scrolling region; and
- application-shaped qualification for roguelike, editor, pixel-art, tile-based,
  and sprite-like workloads.

T2004 does not add a general optimizer graph, pixel framebuffer, raster scene graph,
sprite engine, frame scheduler, double buffering, animation clock, or new public
API. It does not remove the direct TermInfo package reference; that remains T2007.
T2005 and T2006 retain their exhaustive transaction, lifecycle, and failure-
recovery scope.

## Selected architecture

The accepted approach is a planner-backed conversion of the existing resolvers.
The current resolver boundaries and deterministic selection order remain. Their
internals stop retrieving, expanding, repeating, or costing TermInfo strings and
instead request safe plans from the session-bound Terminal planner.

The alternatives rejected for T2004 are:

- a unified optimization graph, because it would redesign candidate discovery and
  physical-state simulation rather than complete the planned dependency migration;
  and
- planner calls embedded directly throughout `CursesRefreshEngine`, because that
  would mix semantic eligibility and cost policy with transaction construction.

The dependency flow is:

```text
desired + speculative physical state
             |
             v
DCurses semantic eligibility and exact-match proof
             |
             v
TerminalScreenPlanner -> opaque operation plans and costs
             |
             v
DCurses deterministic candidate selection
             |
             v
CursesPreparedRefresh -> one Terminal transaction
             |
             v
commit succeeds -> publish speculative state and clean captured damage
```

## Resolver responsibilities

### Erase resolver

`CursesEraseResolver` considers Terminal plans for:

- erase to end of line;
- erase to end of screen; and
- clear screen.

The desired erased region must consist entirely of default-styled blank cells. Both
desired and known physical coordinates in the affected region must be free of
semantic metadata and raster cells. The resolver compares the complete candidate
plan cost against the encoded application-blank rewrite lower bound for the exact
region. It retains the stable operation order and replaces a candidate only when a
later candidate is strictly cheaper.

Erase-to-end-of-line and erase-to-end-of-screen preserve the operation cursor
position. Clear-screen leaves cursor position unknown unless Terminal's public
semantic contract later guarantees otherwise. Every selected erase leaves retained
physical cells equal to the already-proved desired default blanks.

### Character-shift resolver

`CursesCharacterShiftResolver` considers insert-character and delete-character
plans at the first differing column of one row.

The complete shifted row tail must be known, one-column, non-continuation,
non-line-glyph content. It must contain no semantic metadata or raster cells in
either desired or physical state. Inserted or vacated positions must be
default-styled blanks. These rules prevent splitting wide characters, shifting only
half of a glyph footprint, or moving retained semantic/media ownership through a
terminal operation DCurses cannot independently verify.

The operation is selected only when its complete candidate cost is strictly below
the changed nonblank application-text rewrite lower bound. Equal cost preserves
ordinary rewrite. After the operation, the speculative physical row is populated
from the desired row because the resolver's exact-match proof establishes that the
terminal operation produces that result. The cursor remains at the operation
position.

### Line-shift resolver

`CursesLineShiftResolver` considers:

- insert line;
- delete line;
- scroll forward; and
- scroll reverse.

It retains the existing complete-row matching proof. Vacated rows must be
default-styled blank rows. Every physical coordinate needed by the proof must be
known. Since whole rows move together, complete wide-cell footprints are permitted.

The affected rectangular region in desired and physical state must contain no
semantic metadata or raster cells. Metadata or media outside that region does not
disable an otherwise safe candidate. This regional rule allows, for example, a
text editor or message log to optimize while an unrelated raster viewport or HUD
remains retained elsewhere on the screen.

For an interior region, the ordered candidate bundle is:

1. set the temporary scrolling region;
2. move the cursor from unknown position to the operation row;
3. perform the line or scrolling operation;
4. restore the full-screen scrolling region; and
5. move the cursor from unknown position to the requested final position.

For an operation whose semantics do not require a temporary region, the bundle
omits steps 1 and 4 and uses the known speculative cursor where possible.

The resolver includes every non-shared plan in the checked candidate byte total.
It selects a candidate only when that total is strictly below the conservative
changed nonblank application-text rewrite lower bound. Stable candidate order wins
all ties. A selected bundle leaves the speculative affected rows equal to the
desired rows and records the requested final cursor as known.

## Rendition and cursor setup

Editing operations that create or expose blank cells require Terminal's normalized
default rendition. A candidate is unavailable when DCurses cannot establish that
rendition safely through the existing presentation resolver.

Any rendition or cursor plan required only by the candidate contributes its
Terminal-owned `ByteCount`. A setup plan required identically by both the candidate
and ordinary rewrite may be treated as a shared cost, but the implementation must
make that cancellation explicit in tests and comments. It must not silently omit a
candidate-specific cursor, rendition, scroll-region setup, restoration, or final
cursor cost.

The rewrite comparator remains deliberately conservative. Application payload
bytes form a guaranteed lower bound; omitted rewrite cursor/rendition work can only
make the real rewrite more expensive. This policy may miss a profitable operation,
but it must never choose an operation that fails the frozen strict-cost gate.

## Output cost boundary

`CursesOutputCostModel` retains only application-text cost through the configured
`Encoding.GetByteCount` operation. Terminal operation costs come exclusively from
`TerminalScreenOperationPlan.ByteCount`; affected-line observations come exclusively
from `TerminalScreenOperationPlan.AffectedLines`.

T2004 removes `TermInfoOutput.TPuts`, terminal-string padding interpretation,
capability expansion, and repeated-capability materialization from the migrated
resolvers and output cost model. `CursesLegacyCursorMotionResolver` becomes unused
and is removed rather than retained as a dead TermInfo-bearing optimization path.

All cost additions use checked arithmetic. A null Terminal plan makes that
candidate unavailable and falls back to another safe candidate or ordinary rewrite.
No caller constructs a replacement terminal sequence.

## Refresh integration

The accepted optimization priority remains:

1. one screen-level line-shift candidate before the row loop;
2. one row-local character-shift candidate for each eligible row;
3. one erase candidate at an eligible changed blank span; and
4. ordinary semantic rewrite.

The refresh engine performs selection against its detached speculative
`CursesRefreshPhysicalState`. Chosen plan bundles are added to
`CursesPreparedRefresh` in semantic order. There is no direct output, raw-control,
extra flush, or second ordinary refresh transaction.

After each selected operation, DCurses updates only the speculative cursor,
rendition, cells, metadata, and raster observations established by that operation's
proof. Successful commit publishes the speculative state and cleans only damage at
or before the captured revision. Preparation or commitment failure preserves
logical damage and invalidates retained physical certainty under the accepted T2003
contract.

No-op refresh remains zero writes and zero flushes. Every non-empty successful
refresh remains one Terminal commit and one flush, including a refresh containing
multiple editing plans.

## Temporary scroll-region recovery

A successful temporary-region candidate restores the full-screen region before its
final cursor movement, all inside the same transaction.

A transport failure can occur after region setup but before restoration. Terminal's
generic transaction cannot know that one opaque plan semantically undoes another,
so DCurses records a conservative `scrollRegionResetRequired` condition whenever a
transaction containing temporary-region setup begins commitment. The condition is
cleared only after the transaction containing restoration commits successfully.

When that condition is present, the next explicit refresh must begin its semantic
batch with:

```csharp
planner.PlanScrollRegion(
    0,
    desired.Rows - 1,
    desired.Rows
)
```

The refresh treats the cursor as unknown after this restoration plan. If Terminal
cannot provide the full-screen restoration plan, refresh fails before output with a
controlled `NotSupportedException`, retains logical damage, and keeps the recovery
condition. DCurses does not issue an out-of-band automatic retry and does not hide
the original failure.

This rule is conservative when the original failure occurred before any bytes. It
is intentionally harmless and preferable to assuming a full-screen region that may
not exist. Exhaustive partial-write, lifecycle, disposal, and compounded cleanup
qualification remains T2005-T2006 work.

## Regional semantic and raster safety

Optimization safety is determined from the coordinates an operation can affect:

| Operation | Safety region |
|---|---|
| Erase to end of line | Current row from cursor through final column |
| Erase to end of screen | Current row tail plus every following row |
| Clear screen | Entire screen |
| Character insert/delete | Operation row from first difference through final column |
| Line insert/delete | Every column from top through bottom affected row |
| Forward/reverse scroll | Every column in the active scrolling region |

Both desired and physical semantic metadata/raster planes must be empty in the
safety region. Unknown physical cells reject the candidate. Content outside the
safety region is not moved or erased by the semantic operation and therefore does
not block it.

This is an eligibility rule, not permission to move raster protocol identity.
T2004 never infers that a character or line operation can relocate a raster cell.
Any candidate whose affected range includes raster content falls back to semantic
repaint.

## Application-shaped behavior

T2004 explicitly qualifies five workload classes:

### Roguelike text game

Sparse map changes continue through ordinary sparse repaint. Default-blank tails,
message logs, menus, and purely textual scrolling regions may use editing plans.
Unicode/ACS map glyphs remain protected by footprint and glyph eligibility rules.

### Full-screen editor

Typing/deletion may use character shifts; inserted/deleted document rows may use
line shifts; viewport movement may use bounded scrolling regions; line tails may
use erase plans. Wide-character boundaries, styled blanks, and equal-cost choices
fall back deterministically.

### Pixel-art game

A Terminal-owned raster viewport and text HUD remain in one atomic refresh. Raster
coordinates use semantic repaint. An unrelated text region may still optimize when
its safety range excludes raster content. DCurses does not become a pixel
framebuffer or image decoder.

### Tile-based game

Text, Unicode, and ACS tiles receive ordinary sparse behavior and may participate
in safe textual operations. Cell-aligned raster tiles remain retained, clipped, and
repainted but are never shifted through a terminal editing operation.

### Sprite-based game

Cell-aligned raster placeholders can move through deterministic erase/repaint and
remain atomically ordered with background and HUD content. T2004 does not promise
arbitrary pixel placement, overlap composition, high-rate frame scheduling, or
double buffering.

## Test strategy

Resolver tests use real Terminal sessions over synthetic test profiles and recording
raw output. They assert semantic plan kinds, `ByteCount`, `AffectedLines`, stable
selection, no output during planning, and exact output only after transaction
commit. Test-only TermInfo profile construction remains a fixture exception until
the audited T2007 boundary; production resolvers contain no TermInfo reference.

Focused qualification covers:

- absent parameterized and repeated-operation candidates;
- strict cheaper-than and equal-cost behavior;
- application encodings with different blank/text byte counts;
- padding-sensitive operation and region costs;
- unknown physical cells;
- default and nondefault styled blanks;
- wide/continuation/glyph character-shift rejection;
- complete wide-cell line movement;
- lower-right and full-screen cursor uncertainty;
- metadata and raster content inside, adjacent to, and outside safety regions;
- successful temporary-region setup/operation/restoration/final-motion order;
- failed temporary-region commitment and required next-refresh restoration;
- one commit/flush and post-success speculative publication;
- failure retaining damage and invalidating certainty; and
- unchanged public API and dependency identities.

Application acceptance retains the T2003 roguelike, editor, pixel-art, and
sprite-like witnesses and adds a tile-based witness. Expectations change only where
T2004 deliberately selects a safe cheaper editing plan. Pixel/raster witnesses must
prove that no unsafe character or line shift crosses retained media.

The complete suite, package candidate, and Windows/Linux/macOS x64/ARM64 matrix run
on the exact candidate head. The active 2.0 API fingerprint remains 75 exported
types, 559 canonical contract lines, SHA-256
`1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65`.

## Acceptance criteria

T2004 is accepted only when all of the following are true:

- migrated erase/shift/scroll resolvers use only Terminal-owned screen plans;
- `CursesOutputCostModel` contains no TermInfo or terminal-string cost path;
- safe optimizations are restored inside the one-transaction T2003 refresh model;
- strict deterministic cost policy and ordinary rewrite fallback remain intact;
- regional metadata/raster boundaries allow unrelated text optimization but reject
  every operation that could move or erase retained semantic/media content;
- temporary scrolling regions restore on success and produce a fail-closed
  next-refresh recovery requirement after uncertain commitment;
- all five application-shaped witnesses pass;
- public API, package identity, dependency versions, root attribution, and the
  frozen 2.0 fingerprint remain unchanged; and
- the complete exact-head platform and package matrix passes.

Acceptance authorizes T2005 only. It does not authorize merge, tagging, release,
publication, or the final direct TermInfo dependency-removal claim.
