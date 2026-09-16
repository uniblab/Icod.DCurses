# T1607 — Raster Lifecycle, Ownership, and Disposal Hardening

**Release:** `Icod.DCurses 1.6.0-alpha.2`  
**Dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Status:** accepted

## Objective

T1607 hardens retained raster presentation across Terminal lifecycle loss, explicit ownership release, disposal, clean-screen refreshes, and concurrent disposal without introducing replay, re-upload, a reverse logical-cell index, or a second raster ownership model.

The accepted boundary remains:

- Terminal owns generation-scoped persistent raster identity, lifecycle state, protocol encoding, and terminal-side cleanup;
- DCurses may retain logical raster intent after Terminal ownership stops being current;
- DCurses must not trust retained physical raster knowledge after lifecycle/disposal invalidation;
- only `Current` raster ownership may be emitted;
- no stale/released/disposed raster identity is knowingly emitted;
- no source-image cache, hidden recreation, automatic upload, or backend fallback is introduced.

No public API or package dependency change is introduced by T1607. The package remains `1.6.0-alpha.2`.

## Accepted implementation

### Explicit facade disposal invalidates physical knowledge

`CursesRasterResource.DisposeAsync()` and `CursesRasterPlaceholder.DisposeAsync()` now invalidate the owning `CursesSession` physical refresh knowledge before delegating the winning disposal to Terminal.

Both facades continue to use `Interlocked.Exchange(...)` for ownership transfer. Therefore:

- exactly one disposer obtains the wrapped Terminal owner;
- later or concurrent disposers are no-ops;
- physical knowledge is invalidated by the winning disposal boundary;
- the facade's public ownership state becomes `Disposed / ExplicitDisposal` immediately;
- disposal never recreates Terminal ownership.

Logical retained raster references are not searched for or deleted. The accepted lazy-validation design therefore avoids an unbounded reverse index from raster owners to retained screen cells.

### Non-current retained raster intent fails closed

The refresh engine validates raster ownership at the logical refresh/use boundary before its dirty/physical-equality fast path.

A retained raster token with ownership status:

```text
Stale
Released
Disposed
```

is rejected with controlled local failure. Only `Current` proceeds to raster rendering.

The same ownership check is repeated immediately before raster-placeholder emission. This narrows the observation/use window while leaving Terminal authoritative for its own final generation/session validation.

### Clean logical screens still observe ownership loss

Ownership validation occurs before `NeedsUpdate(...)` can return false for a clean coordinate whose logical and physical raster tokens compare equal.

Therefore a screen may remain logically unchanged while the underlying Terminal placeholder transitions from `Current` to `Stale`; the next refresh still detects the ownership loss, emits no new cursor/rendition/raster output for that token, and invalidates retained physical certainty through the existing refresh failure path.

This is important because Terminal lifecycle state may change independently of DCurses logical damage.

### Suspend/resume and generation invalidation

DCurses retains its existing lifecycle behavior: resume/session-state transitions invalidate physical-screen knowledge through the established session lifecycle participant and refresh invalidation path.

Terminal 1.15 remains authoritative for generation-scoped persistent raster transitions such as `Stale / SessionStateLost`. T1607 does not add another lifecycle graph or duplicate Terminal's registry.

### Failure behavior

A lifecycle rejection does not:

- clear the application's retained logical raster reference;
- recreate a resource or placeholder;
- re-upload source pixels;
- switch raster backend;
- synthesize Sixel fallback;
- infer terminal-side existence from brand/name;
- emit raw Kitty/APC identity from DCurses.

The ordinary `CursesRefreshEngine.RefreshAsync(...)` failure path invalidates physical knowledge so a later application decision begins from uncertainty rather than a falsely trusted terminal image.

## TDD evidence

### Explicit-disposal invalidation RED

Test-only head:

`dc0dc9dd1c48993f8634291621049ec39201f805`

Workflow **#961 / 35032515418** built successfully with zero warnings/errors and failed exactly the two intended disposal-invalidation witnesses on every TFM:

```text
2 failed / 865 passed / 867 total
```

Both failures were `Expected: 1 / Actual: 0` for the refresh engine's invalidation request after resource or placeholder disposal.

### Explicit-disposal invalidation GREEN

Exact head:

`ec5c7347967db4ebb677fed4f17b2b1ee3108017`

Workflow **#965 / 35041573178** passed the full seven-job matrix after a targeted Windows ARM64 rerun. The original Windows ARM64 attempt exceeded the pre-existing interaction allocation gate by 32 bytes on net8.0 only (`1,921,056` versus `1,921,024`), while the new T1607 tests passed. No performance ceiling or production interaction code was changed; the targeted rerun passed.

Linux x64 reported:

```text
net8.0   867 / 867
net9.0   867 / 867
net10.0  867 / 867
```

with zero build warnings/errors.

### Non-current ownership RED

Test-only head:

`159a3929ccc43a44d82252253a5c2fa8701ec211`

Workflow **#966 / 35042568529** built successfully and failed exactly the three intended ownership witnesses on every TFM:

```text
3 failed / 867 passed / 870 total
```

The three missing behaviors were rejection of:

- `Stale / SessionStateLost`;
- `Released / ResourceReleased`;
- `Disposed / ExplicitDisposal`.

Each failure reported that no exception was thrown, proving non-current retained tokens were still reaching the renderer before the GREEN change.

### Ownership-preflight GREEN

Exact head:

`dd1b041456d3294537bc8eea1dc2f50ba754ab07`

Workflow **#967 / 35042961898** passed all seven jobs. Linux x64 reported:

```text
net8.0   870 / 870
net9.0   870 / 870
net10.0  870 / 870
```

with zero build warnings/errors.

The production delta from the RED head was one file, `src/Internal/CursesRefreshEngine.cs`, adding ownership preflight before the dirty/equality fast path and immediately before raster emission.

### Final clean-screen and concurrent-disposal hardening

The final test set adds:

- clean-screen `Current -> Stale` detection after a successful raster refresh;
- proof that ownership loss produces no additional raster/text output before rejection;
- concurrent placeholder disposal plus repeated disposal;
- concurrent resource disposal plus repeated disposal;
- final `Disposed / ExplicitDisposal` observation;
- retained physical invalidation after the winning disposal.

The first test-only hardening head `e0d1dc4ef2463dd5085f6db9fad4ed91fb31a8ce` exposed a test-fixture compile defect in workflow **#968 / 35044623295**: xUnit's `Assert.NotNull(object?)` overload returns `void`, so assigning it to `object` produced CS0029. No production code was implicated.

The fixture-only correction produced final accepted head:

`20f0c28f8b7da8da97020cd9dc69a7d2b8eb2e12`

Workflow **#969 / 35044829047** completed successfully across all seven jobs: package candidate plus Windows/Linux/macOS x64/ARM64.

Linux x64 reported:

```text
net8.0   873 / 873
net9.0   873 / 873
net10.0  873 / 873
```

with zero build warnings/errors.

## Accepted boundary for T1608

T1607 closes retained raster lifecycle and ownership hardening for the 1.6 track.

T1608 now owns application/package acceptance:

- add a realistic mixed-media sample using only public DCurses presentation APIs plus backend-neutral `TerminalRasterImage` creation input;
- demonstrate retained raster with scrolling/panning, clipping, panel overlay, sparse refresh, and existing interaction geometry;
- extend package-only validation over the 1.6 raster facade and its one intentional `TerminalRasterImage` dependency exposure;
- optionally demonstrate TermInfo 1.14 advisory backend planning without making `Icod.TermInfo.Inspection` a production DCurses dependency;
- preserve explicit application backend preference and never claim TermInfo 1.14 plans Unicode-placeholder semantics.
