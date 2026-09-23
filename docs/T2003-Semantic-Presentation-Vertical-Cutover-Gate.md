# T2003 Semantic Presentation Vertical Cutover Gate

**Date:** 2026-09-22.\
**DCurses development identity:** `2.0.0-alpha.1`.\
**Assembly identity:** `2.0.0.0`.\
**Published dependency qualified:** `Icod.Terminal 1.18.0`.\
**Status:** **accepted — T2004 may begin**.

## Gate outcome

T2003 is accepted. Capability projection, rendition normalization and transitions,
line glyph/ACS presentation, cursor movement, alerts, ordinary text, hyperlinks,
and raster placeholder cells now pass through Terminal-owned semantic profile,
planner, and output-transaction contracts on the migrated vertical path. A non-empty
refresh is prepared into one Terminal transaction, committed once, and only then
published as retained physical state. A clean refresh performs no output and no
flush.

The accepted exact executable head is
[`448313a41162293eb809ad83e1b68af1b86bf1eb`](https://github.com/uniblab/Icod.DCurses/commit/448313a41162293eb809ad83e1b68af1b86bf1eb),
qualified by [workflow 35784707093](https://github.com/uniblab/Icod.DCurses/actions/runs/35784707093).
This gate authorizes T2004 only. It does not claim complete decoupling, and it does
not authorize merge, tagging, release, or publication.

## Identity and dependency result

| Contract | Accepted result |
|---|---|
| `Version` / `PackageVersion` | `2.0.0-alpha.1` / `2.0.0-alpha.1` |
| `AssemblyVersion` | `2.0.0.0` |
| Direct Terminal reference | `Icod.Terminal 1.18.0` |
| Temporary direct TermInfo reference | `Icod.TermInfo 1.15.0` |
| Target frameworks | `net8.0`; `net9.0`; `net10.0` |
| Public API fingerprint | 75 exported types; 559 lines; SHA-256 `1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65` |

The public contract and dependency identities are unchanged from the accepted T2002
baseline. `README.md` and `docs/Public-API-Fingerprint-2.0.json` are unchanged by
T2003.

## Accepted vertical boundary

The migrated path establishes these ownership rules:

- `TerminalProfile.Screen` supplies presentation capability observations;
- explicit DCurses-to-Terminal mappings cover colors, rendition flags, line glyphs,
  alerts, and nullable cursor positions, including rejection of unknown values;
- `TerminalScreenPlanner` owns normalization, baseline/transition/reset, ACS,
  cursor, and alert operation encoding and byte counts;
- DCurses retains Unicode display-width policy, ASCII fallback, logical cells,
  damage, composition, desired-versus-physical comparison, and publication policy;
- `CursesPreparedRefresh` exposes only semantic plan/text/hyperlink/raster additions
  and commit; it exposes neither raw control strings nor the underlying transaction;
- speculative physical state is detached and becomes authoritative only after a
  successful commit; failures preserve logical damage and invalidate physical
  certainty; and
- session refresh no longer creates the legacy refresh-output adapter or owns an
  outer synchronized-output lease.

The source boundary check returned no matches for TermInfo, legacy description,
capability expansion, raw terminal writing, or the legacy output adapter in the
migrated capability, presentation, cursor, refresh-engine, and session integration
files. This is a vertical-path result, not a repository-wide TermInfo-removal claim.

## Behavioral qualification

Focused and full-suite evidence covers:

- monochrome, indexed, direct-color, invalid/default-color, restricted attribute,
  unknown-rendition, incomplete ACS, and unavailable-cursor cases;
- exact semantic ordering for plans, application text, hyperlinks, and raster cells,
  with no bytes before commit and one flush for each non-empty refresh;
- zero writes and zero flushes for repeated clean refreshes;
- captured-revision damage cleaning and detached speculative state publication;
- preparation, body, cleanup, cancellation, and flush failures retaining damage and
  forcing a complete caller-driven retry where required;
- serialized concurrent refresh, lifecycle invalidation, and mixed text/raster
  ownership; and
- four application-shaped witnesses: roguelike styled Unicode/ACS output,
  pixel-art text plus raster panels, sprite-like clipped raster movement, and an
  editor-style sparse text/cursor/rendition refresh.

The application witnesses also prove the approved T2003 ordinary-rewrite fallback
does not emit erase, character-shift, line-shift, or scroll-region controls.

## Exact-head workflow evidence

Workflow 35784707093 passed all seven required jobs:

| Job | Result |
|---|---|
| [Package candidate](https://github.com/uniblab/Icod.DCurses/actions/runs/35784707093/job/106938560755) | passed |
| [Runtime Linux x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35784707093/job/106938560802) | passed |
| [Runtime macOS x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35784707093/job/106938561029) | passed |
| [Runtime macOS ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35784707093/job/106938561141) | passed |
| [Runtime Linux ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35784707093/job/106938561143) | passed |
| [Runtime Windows ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35784707093/job/106938561156) | passed |
| [Runtime Windows x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35784707093/job/106938561178) | passed |

The Linux x64 and macOS ARM64 jobs each built with zero warnings and zero errors and
passed 960 tests on each of `net8.0`, `net9.0`, and `net10.0`, with zero failures and
zero skips. Package candidate validation passed on the same head. The current
executor had no .NET SDK, so no local build/test result is claimed; exact-head CI is
the executable qualification authority.

## Remaining migration debt

T2003 deliberately disables legacy erase, character-shift, line-shift, and
scroll-region selection in the migrated refresh path. Ordinary rewrite is the
accepted temporary behavior. T2004 restores those optimizations using opaque
Terminal plans, `ByteCount`, and `AffectedLines`, while DCurses retains semantic
eligibility and total-alternative selection.

T2005 still owns exhaustive transaction capacity/cancellation/synchronization/
publication hardening and deletion of obsolete raw-output/capability-writer shims.
The direct `Icod.TermInfo` package reference remains temporary migration debt until
T2007 proves and enforces the complete production, assembly, and NuGet boundary.
TermInfo remains a legitimate transitive dependency behind Terminal.

## Acceptance decision

Every T2003 criterion is satisfied:

- core presentation interpretation and planning use Terminal-owned contracts;
- non-empty refresh uses one semantic transaction and publishes state after commit;
- no-op, failure, concurrency, lifecycle, hyperlink, and raster behavior is covered;
- the four approved application shapes pass through the vertical path;
- migrated production files contain no forbidden legacy token;
- the reviewed public API fingerprint and package identities remain unchanged; and
- package validation plus the complete exact-head platform matrix pass.

T2004 may begin. T2005-T2011 remain ordered and pending. Any newly discovered
Terminal gap returns to the owning repository for a separately reviewed published
correction rather than reopening raw TermInfo or terminal-string access in DCurses.
