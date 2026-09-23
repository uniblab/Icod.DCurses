# T2004 Cost-Aware Editing Cutover Gate

**Date:** 2026-09-23.\
**DCurses development identity:** `2.0.0-alpha.1`.\
**Assembly identity:** `2.0.0.0`.\
**Published dependency qualified:** `Icod.Terminal 1.18.0`.\
**Status:** **accepted — T2005 may begin**.

## Gate outcome

T2004 is accepted. Erase, character-shift, line-shift, and scroll-region
optimizations now use same-session opaque `Icod.Terminal` operation plans. Terminal
owns operation encoding, padding, `ByteCount`, and `AffectedLines`; DCurses retains
exact-match eligibility, retained-screen safety, total-alternative cost, stable
candidate order, and the strict cheaper-than rewrite policy.

The accepted executable head is
[`049843eff8535718f5a7a3c9b10399b75880fd4e`](https://github.com/uniblab/Icod.DCurses/commit/049843eff8535718f5a7a3c9b10399b75880fd4e),
qualified by [workflow 35803855740](https://github.com/uniblab/Icod.DCurses/actions/runs/35803855740).
This gate authorizes T2005 only. No T2004 package has been published, and this gate
does not authorize merge, tagging, release, or publication.

## Identity and dependency result

| Contract | Accepted result |
|---|---|
| `Version` / `PackageVersion` | `2.0.0-alpha.1` / `2.0.0-alpha.1` |
| `AssemblyVersion` | `2.0.0.0` |
| Direct Terminal reference | `Icod.Terminal 1.18.0` |
| Temporary direct TermInfo reference | `Icod.TermInfo 1.15.0` through T2007 |
| Target frameworks | `net8.0`; `net9.0`; `net10.0` |
| Public API fingerprint | 75 exported types; 559 lines; SHA-256 `1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65` |

The public contract and dependency identities are unchanged from T2003. The root
`README.md` attribution and `docs/Public-API-Fingerprint-2.0.json` are unchanged by
T2004.

## Accepted editing boundary

The migrated optimization path establishes these ownership rules:

- `TerminalScreenPlanner` supplies opaque erase, character-shift, line-shift,
  scroll-region, cursor, and rendition operation plans;
- `CursesTerminalPlanSequence` preserves plan order and computes checked aggregate
  cost from Terminal-owned `ByteCount` values;
- DCurses selects an optimization only when its complete setup, operation,
  restoration, and final-position sequence is strictly cheaper than the encoded
  application-text rewrite; equal cost retains ordinary rewrite;
- a missing or zero-byte semantic editing plan, an overflowed candidate total, an
  unsafe footprint, or a non-matching retained screen falls back without output or
  speculative publication;
- `AffectedLines` remains Terminal-owned and is carried through padding-sensitive
  line and scroll planning; and
- erase and shift plans join ordinary text, hyperlinks, and raster cells in the
  existing single Terminal screen transaction and single successful flush.

The source-boundary search returned no matches for TermInfo types, capability
identifiers, expansion/output helpers, raw Terminal output, or the removed legacy
cursor resolver in the erase, character-shift, line-shift, cost, sequence, regional
safety, and refresh-engine files. This is the T2004 migrated-path boundary, not the
repository-wide package-removal gate owned by T2007.

## Cost and fallback evidence

Resolver and refresh tests prove exact operation selection and ordinary-rewrite
fallback for erase-line, erase-screen, clear-screen, character insert/delete, line
insert/delete, forward/reverse scrolling, and temporary scroll regions. The
application-shaped cost witnesses include:

- an interior editor character shift selected at 2 encoded bytes and one write
  instead of a 34-byte, three-write rewrite; and
- a pager-style line shift selected at 4 encoded bytes and three writes instead of
  a 166-byte, eleven-write rewrite.

The suite also covers repeated-single-operation plans, UTF-8 application cost,
strict equal-cost fallback, absent operations, lower-right handling, styled blanks,
wide-cell footprints, checked totals, unchanged second refreshes, and bounded/no-op
scale behavior. Planning emits no bytes; successful application commits once and
publishes physical state only after commitment.

## Regional retained-state and recovery evidence

Metadata, hyperlink ownership, raster state, and unknown physical cells block an
editing operation only when they intersect its complete source/destination safety
rectangle. Equivalent retained state immediately outside the rectangle does not
disable an otherwise safe optimization. Character shifts additionally reject
continuations, width-two cells, and line glyphs in the affected row tail; line
shifts preserve complete-row footprints and reject raster content inside the moved
region.

A temporary-region line shift records restoration debt immediately before commit.
If commitment can have emitted the temporary-region setup but not its restoration,
the next explicit refresh prefixes a full-screen `PlanScrollRegion(0, rows - 1,
rows)` recovery plan before any body output. The debt clears only after successful
restoration. Missing recovery plans fail closed, failures before any region bytes do
not invent debt, and ordinary non-region failures do not trigger region recovery.

## Application-shaped qualification

The accepted suite covers all five reviewed application shapes:

1. a roguelike text game whose message log scrolls independently of the map;
2. a full-screen editor with character insert/delete, line insert/delete, tail
   erase, temporary regions, cursor movement, and rendition;
3. a pixel-art game combining raster content with independently updated text;
4. a tile-based game combining text, Unicode, ACS/line glyphs, and raster tiles,
   including rejection of a line shift that would move raster ownership; and
5. a sprite-based game with clipped raster movement and an independently optimized
   text HUD.

These are retained-screen safety and editing-policy witnesses. They do not add a
new game engine, widget framework, animation scheduler, or physical raster-scene
API to the 2.0 migration.

## Exact-head workflow evidence

Workflow 35803855740 passed all seven required jobs:

| Job | Result |
|---|---|
| [Package candidate](https://github.com/uniblab/Icod.DCurses/actions/runs/35803855740/job/107000185649) | passed |
| [Runtime Linux ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35803855740/job/107000185413) | passed |
| [Runtime macOS ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35803855740/job/107000185535) | passed |
| [Runtime Linux x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35803855740/job/107000185546) | passed |
| [Runtime Windows x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35803855740/job/107000185608) | passed |
| [Runtime Windows ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35803855740/job/107000185614) | passed |
| [Runtime macOS x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35803855740/job/107000185688) | passed |

Linux x64 and macOS ARM64 each passed 1,014 tests on each of `net8.0`, `net9.0`,
and `net10.0`, with zero failures and zero skips. The inspected macOS ARM64 build
reported zero warnings and zero errors. Package validation built
`Icod.DCurses.2.0.0-alpha.1`, verified package structure, metadata, dependency
closure, assembly identity, XML documentation, and portable symbols, and executed
fresh package-only consumers on all three target frameworks. The current executor
has no .NET SDK, so no local build/test result is claimed; exact-head CI is the
executable qualification authority.

## Remaining migration debt

T2005 owns exhaustive transaction capacity, cancellation, synchronization, and
publication hardening plus deletion of obsolete raw-output/capability-writer shims.
T2006 owns lifecycle, partial-write, cleanup, disposal-race, and recovery
qualification. T2007 owns the final direct `Icod.TermInfo` package/reference
removal and permanent source, assembly, and NuGet dependency guards. Until T2007,
the direct `Icod.TermInfo 1.15.0` reference remains explicit temporary migration
debt; TermInfo remains a legitimate transitive dependency behind Terminal.

## Acceptance decision

Every T2004 criterion is satisfied:

- all restored editing operations use opaque Terminal plans and Terminal-owned
  costs;
- DCurses preserves exact-match, strict-win, deterministic, and regional safety
  policy;
- one transaction and post-commit publication remain intact;
- fail-closed temporary-region recovery survives uncertain commitment;
- the five approved workload shapes pass;
- migrated production files contain no forbidden legacy token;
- the reviewed API and package identities remain unchanged; and
- package validation plus the complete exact-head platform matrix pass.

T2005 may begin. T2006-T2011 remain ordered and pending. Any newly discovered
Terminal gap returns to the owning repository for a separately reviewed published
correction rather than reopening TermInfo interpretation or raw terminal strings in
DCurses.
