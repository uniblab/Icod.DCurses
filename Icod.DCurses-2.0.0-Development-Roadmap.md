# Icod.DCurses 2.0.0 Development Roadmap

**Theme:** Terminal-only integration; remove direct TermInfo coupling.\
**Status:** T2001-T2004 accepted; T2005 is the next implementation tranche.

**Planning date:** 2026-09-18.\
**Behavioral baseline:** published `Icod.DCurses 1.6.0`.\
**Dependency baseline:** published and T2001-qualified `Icod.Terminal 1.18.0`; raise the minimum only if a documented upstream readiness blocker requires a later published release.\
**Targets:** `net8.0`; `net9.0`; `net10.0`.\
**Configurations:** `Debug`; `Staging`; `Release`.\
**Technology:** C# 13, .NET, PowerShell 5.1-compatible automation, cmd/sh. No Python.\
**Current development identity:** `Version` and `PackageVersion` both `2.0.0-alpha.1`; `AssemblyVersion` `2.0.0.0`, established and qualified in T2002.

## 1. Goal and scope

Make `Icod.Terminal` the sole terminal-facing dependency of DCurses. Version 2.0 changes the dependency boundary and the public signatures that currently expose TermInfo. It preserves the retained presentation and interaction behavior of 1.6 except for explicitly reviewed migration differences.

The dependency direction is `Icod.DCurses -> Icod.Terminal -> Icod.TermInfo`.

This means:

- no direct TermInfo package reference in the production project or DCurses NuGet dependency groups;
- no TermInfo types in DCurses public signatures, implementation type references, or direct assembly references;
- no DCurses capability database, raw capability identifiers, parameter expansion, padding interpreter, or duplicated terminal command encoder;
- no production use of Terminal's legacy TermInfo-bearing APIs or borrowed/raw output escape hatches;
- TermInfo remains in the transitive dependency graph through Terminal. Removing it from deployment is neither required nor desirable.

Version 1.6 is the endpoint for new 1.x features. Necessary 1.6.x maintenance may continue independently. New features belong to 2.1+ after this migration is accepted. Do not mix widgets, new input protocols, animation scheduling, physical raster scenes, or application-framework work into 2.0.

This PR now carries the ordered 2.0 migration. T2002 established the development identity, selected Terminal 1.18.0, and completed the approved public profile/dimensions cutover. T2003 moved the core presentation path and ordinary rewrite refresh through Terminal-owned planners and one semantic output transaction. T2004 restored erase, character-shift, line-shift, and scroll-region optimization through opaque Terminal plans and Terminal-owned costs. The direct TermInfo dependency and T2005-T2007 hardening/removal work remain migration debt, and no checkpoint authorizes publication.

## 2. Reference snapshot and authorities

The planning review used:

| Authority | Snapshot |
|---|---|
| DCurses source | [`c6c365fecb1257b2fed931a57f00e8d516af277f`](https://github.com/uniblab/Icod.DCurses/tree/c6c365fecb1257b2fed931a57f00e8d516af277f) |
| DCurses published baseline | [`v1.6.0`](https://github.com/uniblab/Icod.DCurses/releases/tag/v1.6.0), published 2026-09-17 |
| Terminal source | [`ce2d76dda3f7d455a891d4268f453c99112cae8e`](https://github.com/uniblab/Icod.Terminal/tree/ce2d76dda3f7d455a891d4268f453c99112cae8e) |
| Terminal published prerequisite | [`v1.18.0`](https://github.com/uniblab/Icod.Terminal/releases/tag/v1.18.0), published 2026-09-18 |
| Terminal boundary design | [1.17 screen-output design](https://github.com/uniblab/Icod.Terminal/blob/v1.17.0/docs/superpowers/specs/2026-09-17-1.17.0-terminal-screen-output-design.md) |
| Terminal API authority | [1.18 public baseline](https://github.com/uniblab/Icod.Terminal/blob/v1.18.0/docs/Public-API-Baseline-1.18.md) |
| DCurses public baseline | [1.6 API baseline](docs/Public-API-Baseline-1.6.md) and [fingerprint](docs/Public-API-Fingerprint-1.6.json) |
| Active release policy | [main roadmap](Icod.DCurses-Development-Roadmap.md) |

The 1.6 fingerprint is 75 exported types, 559 canonical declared contract lines, SHA-256 `266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458`. It remains historical evidence, not a fingerprint to overwrite with 2.0 signatures.

Terminal 1.18 includes the 1.17 screen boundary plus safe unknown-rendition recovery. T2001's package-only downstream witnesses prove the required public contracts are usable; they do not mean DCurses's renderer has already been migrated.

## 3. Responsibility boundary

| Owner | Responsibilities |
|---|---|
| TermInfo, behind Terminal | Immutable descriptions, capability lookup, parameter expansion, padding semantics, and capability/color data |
| Terminal | Live session/input/lifecycle, semantic profile and dimensions, safe operation encoding and cost, reversible modes, hyperlinks/raster ownership, serialized output commitment and protocol cleanup |
| DCurses | Cells/styles/metadata/raster coordinates, Unicode display width, windows/pads/panels, layout/clipping/composition, damage, desired-versus-physical comparison, optimization selection, physical-state certainty and repaint policy |
| Application | Event loop, model, commands, source-image durability, widget semantics, navigation and high-level policy |

An opaque Terminal plan says how to perform a safe operation and what its encoded byte cost is. DCurses decides whether that operation reproduces the desired screen and is cheaper than rewriting. Do not move the retained renderer into Terminal or build a second live-session backend inside DCurses.

## 4. Public API migration

The proposed break set is deliberately small:

| 1.6 API | 2.0 contract |
|---|---|
| `CursesSession.Terminal : Icod.TermInfo.TerminalDescription` | Remove; expose `CursesSession.Profile : Icod.Terminal.TerminalProfile` |
| `CursesSession.GetDimensions() : TerminalControlResult<Icod.TermInfo.TerminalSize>` | Same method name, return `TerminalControlResult<TerminalDimensions>` |
| `CursesSession.SynchronizeDimensions() : TerminalControlResult<Icod.TermInfo.TerminalSize>` | Same method name, return `TerminalControlResult<TerminalDimensions>` |
| `CursesLifecycleEvent.Dimensions : Icod.TermInfo.TerminalSize?` | `Icod.Terminal.TerminalDimensions?` |
| Assembly identity `1.0.0.0` | `2.0.0.0`; consumers rebuild against 2.0 |

`CursesSession.OpenAsync(TerminalSession, ...)`, Terminal result/endpoint/raster-image types, DCurses logical geometry, and the public `CursesPresentationCapabilities` shape remain. Reimplement the latter as a projection of `Profile.Screen`, with explicit enum mappings rather than assumptions about numeric enum equivalence.

Do not retain a TermInfo-returning obsolete shim or type-forwarder. Consumers requiring the old API remain on 1.6. Advanced terminal-description construction remains an application/Terminal configuration concern; DCurses receives the configured Terminal session and does not inspect its legacy description.

T2001 freezes the complete break manifest after inspecting all signatures, including generic arguments, nullable values, fields, constructors, constraints and attributes. Additional breaking changes require an explicit rationale and roadmap update. Preserve existing status/message/native-error behavior when projecting dimensions.

## 5. Source migration map

Paths below are relative to the DCurses repository. Existing test families should be adapted with their coverage retained, not deleted merely because their old fake output boundary is inconvenient.

| Area / current files | Replacement and retained policy | Principal qualification |
|---|---|---|
| `Icod.DCurses.csproj` | Upgrade Terminal; eventually remove direct TermInfo; change both version properties together | Restore/package dependency groups and assembly metadata |
| `src/Integration/CursesSession.Terminal.cs`, `CursesSession.Screen.Terminal.cs`, `CursesSession.Lifecycle.Terminal.cs`, `CursesLifecycleEvent.Terminal.cs` | `Profile`, Terminal `GetDimensions()`, lifecycle `Dimensions` | Session ownership, resize/resume, unavailable/failed results |
| `src/CursesPresentationCapabilities.cs`, `src/Integration/CursesSession.PresentationCapabilities.cs` | Project `TerminalScreenCapabilities`; keep curses-shaped public observations | Capability projection and unsupported profiles |
| `src/Internal/CursesPresentationResolver.cs` | Map curses style/color/attributes to `TerminalScreenRendition`; use `NormalizeRendition` and rendition plans | Indexed/direct color, reversible attributes, default restoration |
| `src/Internal/CursesLinePresentationResolver.cs` | `ResolveLineGlyph` and `PlanAlternateCharacterSet`; retain DCurses Unicode-width/ASCII fallback | ACS/non-ACS runs, one-column fallback, width-two coherence |
| `src/Internal/CursesCursorMotionResolver.cs`, `src/Integration/CursesSession.Presentation.Terminal.cs` | `PlanCursorMove`, `PlanAlert`, rendition plans; retain Terminal presentation leases | Cursor uncertainty, alerts, unsupported operations, lease restoration |
| `src/Internal/CursesEraseResolver.cs`, `CursesCharacterShiftResolver.cs`, `CursesLineShiftResolver.cs` | `PlanErase`, `PlanCharacterShift`, `PlanLineShift`, `PlanScrollRegion` | DCurses semantic eligibility, wide cells, media/metadata and affected-line safety |
| `src/Internal/CursesOutputCostModel.cs` | Consume plan `ByteCount`; retain application-text encoding cost only | Padding excluded from bytes, deterministic ties, total alternative cost |
| `src/Internal/CursesRefreshEngine.cs`, `CursesPhysicalScreenState.cs`, `src/Integration/CursesSession.Refresh.Terminal.cs` | Prepare a complete semantic batch, commit once, publish staged physical state afterward | Sparse/full repaint, failures, invalidation races, clean-state timing |
| `src/Internal/TerminalCapabilityWriter.cs`, `src/Integration/TerminalOutputShim.cs` | Remove raw capability writer; replace any useful test seam with a semantic transaction adapter | No raw terminal strings or borrowed output in production |
| `src/Integration/CursesSession.Presentation.Terminal.cs`, `CursesSession.Lifecycle.Terminal.cs`, raster integration | Keep existing ownership leases; coordinate them outside screen transactions | Suspend/resume/dispose, lock ordering, resource certainty |
| `tests/Icod.DCurses.Tests/src/PublicDependencyBoundaryTests.cs`, `PublicApiFingerprintTests.cs`, `PublicOneSixApiBaselineTests.cs`, `GlobalUsings.cs` | New 2.0 boundary/baseline guards; retire the active 1.2 fingerprint alias without rewriting historical snapshots | No leaked TermInfo types or silent API drift |
| `samples/`, `tools/package-smoke/`, `tools/package-verifier/Program.cs`, `packaging/`, `.github/` | Terminal-only usage, fresh consumer, independent expected dependency/API policy | Packed artifacts across all target frameworks |

The inventory is a starting map, not an allowlist of files permitted to retain coupling. T2001 searches all production source, tests, samples, tools, project files, and current documentation, including fully qualified names and aliases.

## 6. Dependency readiness and shortcomings

No new TermInfo feature is assumed necessary. Capability parsing/expansion/padding/color defects belong in TermInfo and must reach DCurses through a published Terminal dependency update. Do not reopen TermInfo's public `TerminalSize` ownership just to complete this migration: Terminal-owned dimensions already exist.

The following Terminal integration risks must be closed by evidence, not by assuming the prerequisite release covers every DCurses scenario:

| Issue | Evidence / significance | Required disposition |
|---|---|---|
| Unknown physical rendition recovery | Published Terminal 1.18 `PlanRenditionBaseline()` safely restores every reachable rendition axis from unknown state; T2001 verifies literal `<sgr0><op>` output through a same-session transaction. | Use the baseline plan after startup uncertainty, invalidation, resume, and failed output. Never substitute `Default` for unknown. |
| Whole-refresh capacity | Terminal 1.18 bounds a transaction to 65,536 retained items and 64 MiB application payload; T2001 verifies both boundaries reject the next addition before output. Raster batches count individual cells. | Coalesce runs; reject an over-limit refresh before output without marking it clean. No silent multi-transaction splitting. If required workloads regress, obtain an upstream bounded-batch solution before acceptance. |
| Rendition transition efficiency | Terminal normalizes and safely plans transitions, but its reset/reapply choices need not match DCurses 1.6 incremental rendition minimization. | Compare exact operation costs and output volume on identical workloads; upstream any material regression rather than reintroduce an encoder. Record intentional changes with evidence. |
| Test profile injection and legacy providers | Existing tests build TermInfo descriptions and alias `TerminalSize`; Terminal retains legacy TermInfo-bearing configuration/provider contracts for 1.x compatibility. | Separate synthetic upstream profile setup from renderer assertions. Prefer Terminal-only setup. Any unavoidable test-only bootstrap exception must be enumerated and reviewed in T2001; never allow it into production, samples, or package consumers. If zero test references require a new Terminal test seam, record that as upstream work. |
| Output epoch and ownership scopes | Transactions reject intervening session-owned output; hyperlink/synchronized scopes can conflict. Borrowed output is outside the guarantee. | Complete dimensions/lease work before transaction creation; remove outer refresh synchronized leases; exercise external session activity and scope conflicts. Do not access borrowed output or silently retry a stale committed transaction. |

Static profile facts are not live capability verification. Null plans mean unavailable operations, not permission to synthesize escape sequences. Optimization can fall back to a safe rewrite; an unavailable essential cursor/rendition operation must produce a controlled failure, not a falsely successful refresh.

## 7. Refresh commitment and recovery contract

The renderer follows this sequence:

1. Acquire the existing DCurses activity/refresh coordination and observe dimensions/lifecycle state. Complete required Terminal lease operations before capturing the transaction epoch.
2. Compose the desired screen and validate media ownership. Create the Terminal transaction before computing output decisions that depend on the captured physical state. Do not perform intervening session output while preparing it.
3. Plan against a local speculative cursor/rendition/screen state. Add same-session opaque plans, application text, typed hyperlink runs and same-session raster cells in output order. Keep current DCurses semantic safety checks for erase/shift/scroll selection.
4. Validate/coalesce the entire batch. A planning or capacity failure emits nothing and leaves damage available for a later refresh. Use the transaction's `UseSynchronizedOutput` option instead of owning a separate refresh framing lease.
5. Call `CommitAsync` once. Terminal owns the output gate, framing, cancellation commitment point, flush, and protocol cleanup. This is serialization and cleanup, not terminal rollback or proof that pixels were displayed.
6. Publish the speculative physical state and mark only the captured damage clean after successful commitment. An invalidation or newer change during preparation/commit must survive publication; never erase a pending repaint signal. Preserve the existing caller-coordination policy for logical screen mutation rather than inventing thread safety.
7. On failure, retain logical content/damage and conservatively invalidate physical cursor/rendition/screen certainty. Preserve primary and cleanup failures. Do not retry or replay automatically. A later explicit refresh must establish a safe Terminal-owned baseline and current media ownership; if that cannot be proved, fail closed and require session recovery/disposal.

Cancellation before commitment emits nothing. Once Terminal commits, ordinary caller cancellation does not intentionally truncate the batch or required cleanup. Document this whole-refresh boundary as a change from the former sequence of individual writes. No DCurses catch block may discard Terminal cleanup failures.

The first implementation uses one bounded transaction per refresh. Multi-transaction streaming is not an accidental fallback; it would require a separately reviewed partial-commit, synchronization and recovery design.

## 8. Ordered implementation tranches

Every tranche starts with focused failing tests, records the expected red result, makes the smallest coherent change, runs focused and relevant regression tests, and commits a reviewable checkpoint. Use the established inline development process. Write a detailed tranche implementation plan before changing code; do not treat this release-level roadmap as permission to invent missing recovery semantics.

Create tranche evidence under `docs/T2001-...md` through `docs/T2011-...md` as work is completed. Record exact commit SHA, commands/results, CI run links, API/dependency differences and remaining blockers. No acceptance entry may claim results from another head.

### T2001 — Inventory, contract freeze and Terminal readiness

**Depends on:** the published prerequisite and roadmap review.\
**Files:** source/test/tool areas in section 5; create `docs/T2001-Terminal-Boundary-and-Readiness-Gate.md` and `docs/2.0-API-Break-Manifest.md`.

**Status:** accepted against published Terminal 1.18.0; see `docs/T2001-Terminal-Boundary-and-Readiness-Gate.md`.

- [x] Enumerate every direct/public/implementation TermInfo use and every raw-output path; map each to a concrete Terminal API or retained DCurses policy.
- [x] Freeze the four public API replacements and assembly identity; enumerate any additional breaks explicitly.
- [x] Capture 1.6 behavioral and output/allocation witnesses before changing the renderer.
- [x] Run package-based Terminal-only witnesses for unknown-rendition recovery, stale epochs, limits, cleanup, and rendition cost. The original 1.17 blocker was corrected in the owning repository and published as Terminal 1.18.0.
- [x] Classify fixture-only legacy dependencies explicitly; define the source and metadata checks that will enforce the production boundary.

**Acceptance:** complete inventory and approved break manifest; each readiness issue has passing evidence or an explicit blocker. Blocked dependent tranches cannot advance; planning documentation is not proof of readiness.

### T2002 — Development identity and public profile/dimensions cutover

**Depends on:** T2001 API decisions.\
**Files:** `Icod.DCurses.csproj`, session/screen/lifecycle integration, current API tests and test-project baseline includes; create the initial `docs/Public-API-Baseline-2.0.md` and `docs/Public-API-Fingerprint-2.0.json`.

**Status:** accepted on exact head `f030d1b1b7e18f4566c171d4fc8f5a4765a3f8cc`; see `docs/T2002-Public-Terminal-Cutover-Gate.md`.

- [x] Establish `2.0.0-alpha.1` in both version properties and `2.0.0.0` assembly identity; upgrade Terminal to the qualified published minimum.
- [x] Add `Profile`, remove `Terminal`, and migrate both dimension methods plus lifecycle dimensions. Temporarily retained internal TermInfo paths are tracked debt, not a completed decoupling claim.
- [x] Verify positive dimensions, unavailable/unsupported/failed results, messages/native errors, resize/resume behavior, and unchanged ownership transfer on session initialization failure.
- [x] Select the new 2.0 baseline explicitly in active fingerprint tooling. Preserve historical 1.x artifacts; replace unconditional binary-compatibility assertions with reviewed break-manifest checks.

**Acceptance:** builds/tests pass on all target frameworks, public TermInfo signature leaks are gone, and the API diff contains only approved changes.

### T2003 — Semantic presentation and core screen operations

**Depends on:** T2002 and T2001 recovery readiness.\
**Files:** presentation capabilities/resolvers, line/cursor resolvers, presentation integration; their existing test families.

**Status:** accepted on exact head `448313a41162293eb809ad83e1b68af1b86bf1eb`; see `docs/T2003-Semantic-Presentation-Vertical-Cutover-Gate.md`.

The approved staging for the T2003 vertical cutover is:

```text
T2003: transaction-backed vertical cutover for profile/rendition/ACS/cursor/alert and ordinary rewrite refresh
T2004: restore erase/character-shift/line-shift/scroll optimizations with Terminal plans and costs
T2005: exhaustive transaction/capacity/cancellation/synchronization/publication hardening and legacy-shim deletion
```

No T2003 package was published. T2004 closes the temporary ordinary-rewrite difference for accepted erase and shift candidates.

- [x] Map DCurses colors/attributes/glyphs into Terminal-owned values; delegate normalization, safe transitions, reset, ACS and cursor/alert planning.
- [x] Preserve Unicode-width/ASCII fallback, unsupported-profile behavior, alert preference/fallback, and existing presentation lease ownership.
- [x] Cover monochrome, indexed/direct colors, invalid/default colors, restricted/non-reversible attributes, unknown physical state, incomplete ACS and unavailable cursor movement.

**Acceptance:** migrated helpers contain no TermInfo interpretation or raw command construction; renderer-level behavior is preserved or has an explicitly approved difference.

### T2004 — Cost-aware erase, shift and scroll planning

**Depends on:** T2003.\
**Files:** erase/character/line resolvers, output cost model, corresponding tests and refresh optimization fixtures.

**Status:** accepted on exact executable head `049843eff8535718f5a7a3c9b10399b75880fd4e`; see `docs/T2004-Cost-Aware-Editing-Cutover-Gate.md`.

- [x] Replace expanded capability strings with `TerminalScreenOperationPlan` values and use their `ByteCount`/`AffectedLines`.
- [x] Keep eligibility and total-alternative selection in DCurses, including setup/restoration cursor, rendition and scroll-region costs. Retain deterministic tie rules and encoding-aware application-text cost.
- [x] Cover wide-cell footprints, styled blanks, metadata/media boundaries, lower-right behavior, absent operations, region restoration, and padding-sensitive affected-line counts.

**Acceptance:** each chosen optimization reproduces the desired screen and strictly beats rewrite according to the frozen policy; equal cost retains rewrite, and no `TPuts`, expansion or raw capability cost path remains in these helpers.

### T2005 — Transactional refresh and exhaustive hardening

**Depends on:** T2004 and all T2001 transaction/recovery blockers closed.\
**Files:** refresh engine, physical-state tracking, refresh/session integration, output shim and refresh/hyperlink/raster integration tests. Split preparation/commit helpers into focused internal files if needed, without adding a public backend framework. T2005 owns exhaustive transaction/capacity/cancellation/synchronization/publication hardening and deletion of obsolete raw-output/capability-writer shims.

**Detailed plan:** `docs/superpowers/plans/2026-09-23-icod-dcurses-t2005-transaction-hardening.md`. The first source-boundary slice is complete on PR head `5465b1bba685eafb078e4a7123835525b5c9f426`; exhaustive hardening and the acceptance gate remain pending.

- [ ] Implement section 7's prepare/commit/publish state transition with a single Terminal screen transaction.
- [ ] Compose ordinary text, strict hyperlinks, opaque raster cells and operation plans without parallel direct writes.
- [ ] Transfer synchronized framing/flush/semantic cleanup to Terminal; remove the outer refresh lease and per-write output route as they become unused.
- [ ] Test whole-batch output ordering, one commit/flush, no interleaving, no clean-state publication before success, and invalidation surviving commitment.
- [ ] Complete exhaustive capacity/cancellation/synchronization hardening, including pre-commit and post-commit cancellation, item/payload limits, failure cleanup, stale epochs, and lease conflicts; delete obsolete raw-output/capability-writer shims.

**Acceptance:** normal sparse/full/mixed-media refresh and direct cursor/alert/reset operations use the semantic boundary; all failure paths retain correct logical content and physical uncertainty.

### T2006 — Lifecycle, failure, output uncertainty and recovery qualification

**Depends on:** T2005.\
**Files:** lifecycle/session disposal/refresh/raster integration, session lifetime and mixed-media hardening tests.

- [ ] Exercise partial writes, flush failure, primary plus cleanup failure, foreign/stale/released raster tokens and disposal races.
- [ ] Verify suspend/resume and resize preserve logical intent, invalidate physical knowledge, and never replay stale raster identity or silently recreate resources.
- [ ] Prove no deadlock from activity/refresh/lifecycle coordination and Terminal manager/output gate ordering.

**Acceptance:** recovery and failure evidence closes every readiness condition; required cleanup is attempted, failures stay observable, and unsafe recovery fails closed.

### T2007 — Dependency removal and permanent enforcement

**Depends on:** T2006.\
**Files:** project references, remaining dependency leaks, `PublicDependencyBoundaryTests.cs`, new source/assembly boundary tests, package verifier, and audited fixture setup.

- [ ] Remove the production `Icod.TermInfo` PackageReference and final direct dependency/reference leaks; remove unused dependencies from samples/tools. T2005 owns deletion of obsolete raw capability-writer/output shims.
- [ ] Enforce no production TermInfo symbols, legacy `TerminalSession.Terminal`/`GetSize()`/lifecycle `Size` use, `WriteTerminalStringAsync`, or borrowed output writes/flushes. Keep explicit exceptions limited to reviewed test bootstrap and historical documentation.
- [ ] Inspect emitted DCurses assembly references and metadata TypeRefs as well as recursively examined public signatures; a package-only compile alone cannot catch transitive coupling.
- [ ] Assert the DCurses NuGet direct dependency set independently as exactly `Icod.Terminal` with the qualified minimum in each TFM group. Do not merely compare package dependencies against an equally wrong project file.
- [ ] Demonstrate negative controls: reintroducing a production TermInfo type, a direct package reference, or a forbidden raw-output call causes its respective guard to fail.

**Acceptance:** no direct production/runtime API/assembly coupling; TermInfo's legitimate transitive restore presence does not fail the guard; test bootstrap exceptions, if any, are narrow and documented.

### T2008 — Consumer migration, samples and documentation

**Depends on:** T2007.\
**Files:** `samples/`, `tools/package-smoke/`, package verification scripts, `README.md`, `CHANGELOG.md`; create `docs/2.0-Migration-Guide.md`.

- [ ] Update all samples and current guidance to `Profile`/`TerminalDimensions`, preserving retained mixed-media and interaction demonstrations.
- [ ] Write before/after examples for every API break, rebuild/version guidance, transaction cancellation/limits, physical invalidation and advanced Terminal configuration boundaries.
- [ ] Restore a freshly packed DCurses candidate into an isolated NuGet-only consumer with no project references or direct TermInfo package/source usage; run across every supported TFM.
- [ ] Exercise dimensions/profile, plain/styled/line-drawing refresh, hyperlink text, media lifecycle and panel/pad composition through the published surface.

**Acceptance:** documented migration examples compile; consumers use only DCurses and, where explicitly needed, Terminal types. Current documentation no longer suggests 1.7 decoupling or claims 2.0 is additive over 1.x.

### T2009 — Behavioral parity and performance qualification

**Depends on:** T2008.\
**Files:** existing conformance/refresh/optimization/panel/pad/Unicode/interaction and allocation tests; sample/package acceptance fixtures.

- [ ] Compare against the T2001 baseline for plain/styled/ACS/Unicode/hyperlink/raster content, sparse/full redraw, erase/shift/scroll, large pads, composition and interaction behavior.
- [ ] Measure output volume, transaction allocation/retained memory and refresh work on the same workloads; record differences and reject unexplained material regressions. No timing-only flaky CI thresholds.
- [ ] Run all tests on net8.0/net9.0/net10.0 and the Windows/Linux/macOS x64/ARM64 runtime matrix plus package-candidate validation.

**Acceptance:** parity or explicitly approved migration differences, bounded memory/capacity behavior and exact-head cross-platform evidence. Historical tests are not dropped to make the branch green.

### T2010 — API, package and documentation freeze

**Depends on:** T2009.\
**Files:** 2.0 API artifacts/break manifest/migration guide, package verifier and metadata, XML documentation, licensing/README/changelog and this roadmap.

- [ ] Freeze matching 2.0 public API fingerprints across all TFMs; review all new/removed/changed signatures against the break manifest.
- [ ] Verify XML docs, package/nuspec dependency groups, assembly identity, license payloads, isolated consumer and source/metadata boundary guards.
- [ ] Synchronize permanent docs and release notes, documenting any raised Terminal floor or test-only bootstrap exception.

**Acceptance:** no accidental feature additions or unreviewed breaks; package/docs accurately describe direct versus transitive dependencies. No new feature family after this gate.

### T2011 — RC and stable-source release closure

**Depends on:** T2010.\
**Files:** version/release metadata, acceptance evidence and roadmap status only unless qualification finds a defect.

- [ ] Advance `Version` and `PackageVersion` together through RC to `2.0.0`, keeping `AssemblyVersion` `2.0.0.0`.
- [ ] Qualify the exact final source head and package artifacts; rerun qualification after any evidence/metadata change that changes the candidate head.
- [ ] Record accepted SHA, CI runs, artifact hashes, dependency floor and migration guide before recommending maintainer merge.

**Acceptance:** exact-head runtime/package/API/boundary/consumer gates green. Merge, post-merge Release validation, tagging, GitHub Release and NuGet publication are separate explicit maintainer actions.

## 9. Verification commands and completion criteria

From the complete repository checkout:

```sh
dotnet restore Icod.DCurses.sln
dotnet build Icod.DCurses.sln -c Staging --no-restore -p:ContinuousIntegrationBuild=true
dotnet test Icod.DCurses.sln -c Staging --no-build --no-restore --logger trx
```

Run the established package pipeline as well; `validate` builds/packs/verifies but does not replace the runtime test command:

```powershell
./packaging/Invoke-Build.ps1 -Section validate -Configuration Staging
```

Repeat final qualification in Release and use the existing six-platform runtime matrix. Targeted tests precede the full suite in every red/green cycle. Any newly added automation must use the approved technologies and preserve PowerShell 5.1 compatibility where applicable.

Completion requires all of the following, not just successful compilation:

- [ ] Direct dependency, production source, public signature, assembly metadata and NuGet gates prove Terminal-only coupling.
- [ ] All four TermInfo-bearing public API sites are migrated and documented; 1.x artifacts remain intact.
- [ ] No raw control encoding/output bypass remains in production, including alert/reset/lifecycle paths.
- [ ] Ordinary, styled, Unicode, ACS, hyperlink and raster refresh pass the prepare/commit/publish and recovery requirements.
- [ ] Required large-screen workloads fit bounded transactions or have a reviewed upstream solution; no silent loss of damage or partial-commit workaround.
- [ ] Samples, isolated package consumers, API/XML/license validation and all runtime platforms pass for the exact candidate head.
- [ ] The dependency floor, migration breaks, capacity/cancellation rules and any fixture-only exception are explicit.

## 10. Immediate next checkpoint

Write and review the detailed T2005 implementation plan. Then complete exhaustive transaction capacity, cancellation, synchronization, and publication hardening and delete obsolete raw-output/capability-writer shims without expanding the public backend surface. T2004 is accepted on executable head `049843eff8535718f5a7a3c9b10399b75880fd4e`; see `docs/T2004-Cost-Aware-Editing-Cutover-Gate.md`. Keep the direct TermInfo reference only for still-unmigrated paths; its final package/reference removal remains T2007. No merge, release tag, or publication is authorized by this checkpoint.
