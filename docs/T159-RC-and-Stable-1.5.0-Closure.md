# T159 — RC and Stable 1.5.0 Closure

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T159  
**Published baseline:** `1.4.0`  
**Qualified T158 evidence head:** `218f900aaf689029f8f5de26906f86724d029ebc`  
**Qualified T158 evidence workflow:** #893 / `34915390381` — all seven jobs green  
**Qualified RC head:** `23113b130d674da315ccbbcd384a60a0e6b47baa`  
**Qualified RC workflow:** #899 / `34993884108` — all seven jobs green  
**Initially qualified stable-source head:** `af30a6c2df842d76b8cb9e9b7855aeb3a16e9ff9`  
**Initially qualified stable-source workflow:** #905 / `34994761777` — all seven jobs green  
**Stable-source identity:** `1.5.0`  
**AssemblyVersion:** `1.0.0.0`  
**Final runtime dependencies:** `Icod.Terminal 1.15.0`; direct `Icod.TermInfo 1.14.0`  
**Status:** implementation/API closure complete; final dependency-refresh exact-head qualification is required before PR #30 merge approval; merge/tag/release/publication remain explicitly unauthorized

---

## Entry gate

T158 closed on exact evidence head:

```text
218f900aaf689029f8f5de26906f86724d029ebc
```

Workflow #893 / `34915390381` passed the complete Staging matrix:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

That evidence-only head followed the qualified T158 performance/API checkpoint `9a8d0b2e2439bf4a936a5602d846a0c1bd20781f` (#888 / `34914622882`) and documentation-complete checkpoint `6336defe0fe0594301c1d20c0542ca1d8b8babd3` (#892 / `34915072200`), both also seven-job green.

T158 found no production/API, ownership, packaging, documentation, licensing, or dependency-boundary regret requiring a correction before RC at that time.

## Frozen 1.5 contract carried through closure

The final public API is:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

Published 1.4 remains the compatibility floor:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

T159 did not reopen the interaction implementation or public API. The accepted 1.5 mechanisms remain:

- bounded interaction scopes with immutable parentage and explicit LIFO activation leases;
- explicit singular pointer capture without implicit logical focus;
- signed captured pointer-target snapshots distinct from ordinary in-bounds hits;
- deterministic integer-only Up/Down/Left/Right logical focus;
- clock-free pointer gesture normalization for press/release/move/click/drag phases/wheel;
- scope command bindings with region -> scope chain -> router-global precedence;
- bounded capacity, deterministic repair, stale-ownership invalidation, and callback-free results;
- no widget framework, hidden event loop, drag/drop policy, multi-click timing policy, raster scene ownership, or terminal protocol ownership.

## RC promotion and qualification

The T158-qualified implementation/API was promoted unchanged to:

```text
Version         1.5.0-rc.1
PackageVersion  1.5.0-rc.1
AssemblyVersion 1.0.0.0
```

Exact RC head:

```text
23113b130d674da315ccbbcd384a60a0e6b47baa
```

Workflow #899 / `34993884108` passed the complete seven-job Staging matrix without rerun or correction. RC promotion changed release identity and release-facing documentation only; production interaction code, the compiler-derived public API, target frameworks, and then-declared runtime dependencies remained unchanged.

## Stable-source promotion and qualification

The qualified RC implementation/API was then promoted unchanged to:

```text
Version         1.5.0
PackageVersion  1.5.0
AssemblyVersion 1.0.0.0
```

The fingerprint metadata was promoted from the last API-changing alpha label to final stable identity while retaining the identical contract:

```text
release 1.5.0
status stable
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

Exact initially qualified stable-source candidate head:

```text
af30a6c2df842d76b8cb9e9b7855aeb3a16e9ff9
```

Workflow #905 / `34994761777` passed the complete seven-job Staging matrix:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

The RC-to-stable compare contained only release-facing files: `Icod.DCurses.csproj`, README, the two active roadmaps, `docs/Public-API-Fingerprint-1.5.json`, and this closure record. No `src/`, tests, samples, workflows, target frameworks, or dependency declarations changed between the qualified RC and that stable-source candidate.

## Final dependency refresh

Publication preparation after the initial stable-source qualification deliberately advanced the final package dependency graph to:

```text
Icod.Terminal 1.15.0
Icod.TermInfo  1.14.0   direct
```

`Icod.Terminal 1.15.0` itself directly depends on `Icod.TermInfo 1.14.0`, so TermInfo also appears transitively through Terminal. DCurses nevertheless retains its own direct TermInfo reference because production DCurses source directly consumes TermInfo namespaces/types in its capability, presentation, refresh, cursor-motion, erase, lifecycle, and Terminal-integration layers. TermInfo is therefore a direct DCurses dependency, not merely a transitive dependency inherited from Terminal.

This refresh changes no DCurses public API, target framework, assembly version, interaction behavior, or ownership contract. It does change the package dependency graph, so the earlier #905 stable-source qualification is no longer sufficient as the final merge gate. The refreshed exact head must pass the complete package/runtime matrix again.

Package validation for the refreshed head must prove:

- `.nupkg` and `.snupkg` identity remains `1.5.0`;
- net8.0, net9.0, and net10.0 dependency groups declare direct `Icod.Terminal 1.15.0` and direct `Icod.TermInfo 1.14.0`;
- the isolated package-only consumer still restores, compiles, and executes on all three TFMs;
- the compiler-derived public API fingerprint remains 69 types / 525 lines / the frozen SHA-256;
- Windows/Linux/macOS x64/ARM64 runtime suites remain green.

## Final dependency-refresh gate

The dependency refresh and synchronized release-facing documentation are the final pre-merge changes. Once the refreshed exact head passes the normal seven-job Staging matrix and the packed artifact is inspected, no further source change is planned before explicit PR #30 merge approval.

That dependency qualification does not reopen the implementation/API decision.

## Release boundary

T159 source closure does **not** authorize:

- merging PR #30;
- pushing or creating `v1.5.0`;
- creating a GitHub Release;
- publishing a NuGet package;
- performing post-merge `main` release actions.

Those remain separate explicit maintainer approval/actions.

After explicit PR merge approval and merge, the resulting `main` Release workflow must be inspected and accepted before any stable tag or publication action.

## Closure ledger

| Stage | Head | Workflow | Result |
|---|---|---|---|
| T158 measured implementation | `9a8d0b2e2439bf4a936a5602d846a0c1bd20781f` | #888 / `34914622882` | seven jobs green |
| T158 documentation-complete | `6336defe0fe0594301c1d20c0542ca1d8b8babd3` | #892 / `34915072200` | seven jobs green |
| T158 final evidence | `218f900aaf689029f8f5de26906f86724d029ebc` | #893 / `34915390381` | seven jobs green |
| `1.5.0-rc.1` | `23113b130d674da315ccbbcd384a60a0e6b47baa` | #899 / `34993884108` | seven jobs green |
| initial stable-source `1.5.0` | `af30a6c2df842d76b8cb9e9b7855aeb3a16e9ff9` | #905 / `34994761777` | seven jobs green |
| dependency-refresh `1.5.0` | current branch | pending | exact-head package/runtime qualification required |
| PR #30 merge | pending explicit approval | — | not merged |
| `main` Release | pending post-merge | pending | not run |
