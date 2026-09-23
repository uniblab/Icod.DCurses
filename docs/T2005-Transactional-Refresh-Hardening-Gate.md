# T2005 Transactional Refresh Hardening Gate

**Date:** 2026-09-23.\
**DCurses development identity:** `2.0.0-alpha.1`.\
**Assembly identity:** `2.0.0.0`.\
**Published dependency qualified:** `Icod.Terminal 1.18.0`.\
**Status:** **accepted — T2006 may begin**.

## Gate outcome

T2005 is accepted. Ordinary, styled, hyperlink, raster, editing-plan, cursor,
alert, and rendition-reset output now share Terminal's bounded semantic screen
transaction boundary. Terminal owns serialization, synchronized framing,
hyperlink cleanup, commitment-time cancellation behavior, epochs, retained-item
and application-payload limits, and the single successful flush. DCurses owns
preparation, desired state, detached speculative physical state, damage, and
post-commit publication.

The accepted executable head is
[`262f4ff8aedabd703c03f5f5f5cdcf2bce9e0417`](https://github.com/uniblab/Icod.DCurses/commit/262f4ff8aedabd703c03f5f5f5cdcf2bce9e0417),
qualified by [workflow 35814067829](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829).
This gate authorizes T2006 only. It does not authorize merge, tagging, release,
publication, or removal of the temporary direct TermInfo package reference.

## Identity and dependency result

| Contract | Accepted result |
|---|---|
| `Version` / `PackageVersion` | `2.0.0-alpha.1` / `2.0.0-alpha.1` |
| `AssemblyVersion` | `2.0.0.0` |
| Direct Terminal reference | `Icod.Terminal 1.18.0` |
| Temporary direct TermInfo reference | `Icod.TermInfo 1.15.0` through T2007 |
| Target frameworks | `net8.0`; `net9.0`; `net10.0` |
| Public API fingerprint | 75 exported types; 559 lines; SHA-256 `1d33658358af26049d858e084a80d9f3b80abab974c1c4d3bfb36c2c2b477c65` |

No T2005 production change altered the approved public API, version identity, or
dependency floor. The direct TermInfo package reference remains explicit migration
debt because T2007 owns final package, assembly-reference, metadata, and NuGet
boundary removal.

## Accepted transaction boundary

The accepted state transition is:

1. capture the desired-screen revision and prepare one ordered
   `CursesPreparedRefresh` over one Terminal screen transaction;
2. update only a detached speculative physical-state clone while preparing;
3. let Terminal commit the complete semantic batch under its manager and output
   gates, including required cleanup and one flush;
4. after successful commitment, publish the speculative physical state and mark
   desired damage clean only through the captured revision; and
5. after any failed, stale, conflicting, cancelled-before-commit, or over-capacity
   attempt, retain logical damage and treat physical state conservatively.

The obsolete production per-write output adapter, raw capability writer, and
raster-placeholder output interface are deleted:

- `src/Integration/TerminalOutputShim.cs`;
- `src/Internal/TerminalCapabilityWriter.cs`; and
- `src/Integration/ITerminalRasterPlaceholderOutput.cs`.

A production-wide source contract rejects direct TermInfo symbols, terminal
descriptions, capability identifiers/expansion/output helpers, raw Terminal output
interfaces, legacy writer seams, and direct output flush calls. The final static
scan returned no production matches. Test-only synthetic terminal descriptions and
recording outputs remain explicit fixtures and are not production dependencies.

## Capacity and epoch evidence

`CursesPreparedRefreshHardeningTests` proves Terminal's exact boundaries remain
authoritative through the DCurses wrapper:

- exactly 65,536 retained items are accepted and the next item is rejected before
  output with Terminal's item-limit exception;
- exactly 64 MiB of UTF-8 application payload is accepted and the next byte is
  rejected before output with Terminal's payload-limit exception; and
- an intervening session write makes a prepared transaction stale, prevents body
  replay and extra flush, and permits a newly prepared transaction to succeed.

DCurses does not copy these counters, raise their limits, silently split a refresh,
or fall back to raw output.

## Cancellation, ordering, and ownership evidence

An already-cancelled refresh or direct semantic operation emits and flushes
nothing, publishes no physical state, retains logical intent, and succeeds from a
fresh attempt. A prepared transaction remains single-use even when its commit is
cancelled before entry.

Once Terminal commitment begins, later caller cancellation does not interrupt the
ordinary trailing text, hyperlink close, synchronized-output end, or required
flush. Exact-order witnesses prove that one mixed DCurses batch remains contiguous
with respect to concurrent `TerminalSession` output:

```text
synchronized begin
ordinary text
hyperlink begin / body / close
raster placeholder
trailing text
synchronized end
outside session output
```

Active synchronized-output and hyperlink leases reject conflicting DCurses
transactions before DCurses body output. Outer-lease cleanup remains Terminal-owned,
damage remains dirty, and a fresh transaction succeeds after release. Every
non-empty successful refresh flushes exactly once; every unchanged refresh emits
and flushes nothing.

## Publication and failure evidence

Deterministic blocking-output tests, without timing sleeps, prove that mutation and
explicit invalidation during commitment survive the first successful publication.
The next refresh emits later damage or performs the required full repaint, and a
third unchanged refresh is a no-op.

A blocked output failure preserves the original exception, publishes no
speculative physical state, keeps desired damage, and permits a complete retry.
Direct cursor, alert, and rendition-reset operations use the same framed semantic
commit policy. Their write failures remain observable and invalidate physical
certainty so the next screen refresh re-establishes complete content, cursor, and
rendition state rather than trusting a failed direct operation.

## Realistic workload and adversarial-overflow disposition

Five 160x60 application-shaped witnesses each combine ordinary text, alternating
styles, strict hyperlinks, retained raster placeholders, a complete repaint, sparse
updates, and unchanged no-op refreshes:

1. a roguelike text game;
2. a full-screen editor;
3. a pixel-art game with a text HUD;
4. a tile-based map; and
5. a sprite-and-HUD game.

Every non-empty workload refresh fits one bounded transaction and flushes once;
every unchanged refresh emits and flushes nothing. No realistic-workload upstream
Terminal blocker was found.

The deliberate 257x128 alternating-style-and-metadata screen exceeds Terminal's
65,536 retained-item bound during preparation. It throws the documented item-limit
exception with zero output and zero flushes while every desired cell remains dirty.
After the same desired screen is simplified to a coalescible representation, a
fresh one-transaction repaint succeeds, marks all captured damage clean, and an
unchanged retry is a no-op. Failure-atomic rejection, rather than transaction
splitting, is the accepted adversarial behavior.

## Protected-file and API evidence

The root `README.md` and `docs/Public-API-Fingerprint-2.0.json` match their remote
Task 1 blobs exactly:

| Protected file | Git blob SHA |
|---|---|
| `README.md` | `b563f6415a26e43f9136163947b4065033707ec9` |
| `docs/Public-API-Fingerprint-2.0.json` | `b5d2ac38d344ee7db2a6525547d4e2f47c757b47` |

The maintainer-restored README attribution, copyright, and license text is
unchanged. The approved 2.0 public API fingerprint is unchanged. `git diff --check`
passes, and all three deleted production shims remain absent.

## Exact-head workflow evidence

Workflow 35814067829 passed all seven required jobs:

| Job | Result |
|---|---|
| [Package candidate](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829/job/107031707685) | passed |
| [Runtime Windows ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829/job/107031707865) | passed |
| [Runtime Windows x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829/job/107031707969) | passed |
| [Runtime Linux x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829/job/107031708000) | passed |
| [Runtime Linux ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829/job/107031708029) | passed |
| [Runtime macOS x64](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829/job/107031708046) | passed |
| [Runtime macOS ARM64](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829/job/107031708091) | passed |

Every runtime job passed 1,041 tests on each target framework, with zero failures
and zero skips:

| Platform | `net8.0` | `net9.0` | `net10.0` |
|---|---:|---:|---:|
| Windows ARM64 | 1,041 | 1,041 | 1,041 |
| Windows x64 | 1,041 | 1,041 | 1,041 |
| Linux ARM64 | 1,041 | 1,041 | 1,041 |
| Linux x64 | 1,041 | 1,041 | 1,041 |
| macOS ARM64 | 1,041 | 1,041 | 1,041 |
| macOS x64 | 1,041 | 1,041 | 1,041 |

The executor has no local .NET SDK, so no local build or test result is claimed.
The exact-head package-validation and six-platform GitHub Actions matrix is the
executable qualification authority.

The incremental T2005 workflow history is:

| Slice | Accepted workflow |
|---|---|
| Source boundary and shim deletion | [35806253187](https://github.com/uniblab/Icod.DCurses/actions/runs/35806253187) |
| Capacity and stale epochs | [35810168374](https://github.com/uniblab/Icod.DCurses/actions/runs/35810168374) |
| Cancellation phases | [35810702558](https://github.com/uniblab/Icod.DCurses/actions/runs/35810702558) |
| Publication/invalidation races | [35811378614](https://github.com/uniblab/Icod.DCurses/actions/runs/35811378614) |
| Lease conflicts and serialization | [35812064278](https://github.com/uniblab/Icod.DCurses/actions/runs/35812064278) |
| Direct semantic operations | [35813337011](https://github.com/uniblab/Icod.DCurses/actions/runs/35813337011) |
| Workload and overflow capacity | [35814067829](https://github.com/uniblab/Icod.DCurses/actions/runs/35814067829) |

## Remaining migration debt

T2006 owns lifecycle, partial-write, cleanup-failure, flush-failure, disposal-race,
foreign/stale/released raster, suspend/resume, resize, and recovery qualification.
T2007 then removes the temporary direct `Icod.TermInfo` package/reference and adds
permanent production-source, assembly-reference, metadata, NuGet dependency, and
negative-control guards. Test-only TermInfo construction remains subject to that
T2007 audit.

No T2005 evidence requires additional Icod.Terminal development. Any future gap
found by T2006 or T2007 must still be corrected and published in the owning
dependency rather than bypassed inside DCurses.

## Acceptance decision

Every T2005 criterion is satisfied:

- one Terminal transaction owns each non-empty refresh and direct semantic output;
- ordered plans, text, hyperlinks, and raster placeholders remain contiguous;
- Terminal owns framing, cleanup, cancellation-after-commit, serialization, epochs,
  bounds, and flush;
- pre-commit cancellation, capacity overflow, stale epochs, and lease conflicts
  retain logical intent without DCurses transaction output;
- failures remain observable and force conservative physical recovery;
- speculative physical state and captured damage publish only after success;
- realistic workloads fit and deliberate overflow fails atomically;
- obsolete output/capability seams remain deleted and production has no direct
  TermInfo or raw-output token;
- API, version, dependency, attribution, license, and protected-file identities are
  unchanged; and
- package validation and all six runtime jobs pass on the exact executable head.

T2006 may begin. T2007-T2011 remain ordered and pending.
