# T159 — RC and Stable 1.5.0 Closure

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T159  
**Published baseline:** `1.4.0`  
**Qualified T158 evidence head:** `218f900aaf689029f8f5de26906f86724d029ebc`  
**Qualified T158 evidence workflow:** #893 / `34915390381` — all seven jobs green  
**Qualified RC head:** `23113b130d674da315ccbbcd384a60a0e6b47baa`  
**Qualified RC workflow:** #899 / `34993884108` — all seven jobs green  
**Stable-source identity:** `1.5.0`  
**AssemblyVersion:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Status:** stable-source identity promoted; final exact-head Staging qualification pending; merge/tag/release/publication remain explicitly unauthorized

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

That evidence-only head followed the qualified T158 performance/API checkpoint:

```text
9a8d0b2e2439bf4a936a5602d846a0c1bd20781f
workflow #888 / 34914622882
```

and the documentation-complete pre-evidence checkpoint:

```text
6336defe0fe0594301c1d20c0542ca1d8b8babd3
workflow #892 / 34915072200
```

Both also passed all seven required Staging jobs.

T158 found no production/API, ownership, packaging, documentation, licensing, or dependency-boundary regret requiring a correction before RC.

## Frozen 1.5 contract carried into closure

The accepted public API remains:

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

T159 does not reopen the implementation or public API. The accepted 1.5 mechanisms remain:

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

RC promotion changed release identity and release-facing documentation only. Production interaction code, the compiler-derived public API, target frameworks, and declared runtime dependencies remained unchanged.

The exact RC head was:

```text
23113b130d674da315ccbbcd384a60a0e6b47baa
```

Workflow #899 / `34993884108` passed the complete seven-job Staging matrix without rerun or correction:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

That qualifies the RC package identity, isolated package-only net8/net9/net10 consumer, T158 allocation/capacity gates, runtime suites, and compiler-derived API guard on the exact RC source.

## Stable-source promotion

Because the exact RC head passed all seven required jobs, the unchanged implementation/API has been promoted to:

```text
Version         1.5.0
PackageVersion  1.5.0
AssemblyVersion 1.0.0.0
```

Stable-source promotion changes stable package identity and release-facing documentation only. The seven new exported types, additive members, algorithms, capacity rules, and dependency graph are unchanged from the qualified RC.

The fingerprint metadata is promoted from the last API-changing alpha label to:

```text
release 1.5.0
status stable
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

The current stable-source exact head must pass the same complete seven-job Staging matrix before the branch may be described as release-ready source.

## Release boundary

T159 source closure does **not** authorize:

- merging PR #30;
- pushing or creating `v1.5.0`;
- creating a GitHub Release;
- publishing a NuGet package;
- performing post-merge `main` release actions.

Those remain separate explicit maintainer approval/actions after stable-source qualification.

## Closure ledger

| Stage | Head | Workflow | Result |
|---|---|---|---|
| T158 measured implementation | `9a8d0b2e2439bf4a936a5602d846a0c1bd20781f` | #888 / `34914622882` | seven jobs green |
| T158 documentation-complete | `6336defe0fe0594301c1d20c0542ca1d8b8babd3` | #892 / `34915072200` | seven jobs green |
| T158 final evidence | `218f900aaf689029f8f5de26906f86724d029ebc` | #893 / `34915390381` | seven jobs green |
| `1.5.0-rc.1` | `23113b130d674da315ccbbcd384a60a0e6b47baa` | #899 / `34993884108` | seven jobs green |
| stable-source `1.5.0` | current branch | pending | final exact-head qualification pending |
| PR #30 merge | pending explicit approval | — | not merged |
| `main` Release | pending post-merge | pending | not run |
