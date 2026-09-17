# T1611 — RC and Stable-Source 1.6.0 Closure

**Release:** `Icod.DCurses 1.6.0`  
**Tranche:** T1611  
**Published predecessor:** `1.5.0`  
**AssemblyVersion:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Frozen API:** 75 exported types / 559 canonical lines  
**API SHA-256:** `266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458`  
**Accepted T1610 head:** `dbb663406cfeb62ccdd4181fe34b3264a6e0462a`  
**T1610 workflow:** #999 / `35155346200`  
**Accepted RC head:** `47728ad870205c89bc1d7c0014667774ea9960d9`  
**RC workflow:** #1002 / `35158260589`  
**Stable-source identity:** `1.6.0`  
**Status:** stable-source candidate prepared; final exact-head qualification pending

---

## Objective

T1611 promotes the implementation/API accepted by T1610 unchanged through `1.6.0-rc.1` and then to stable-source `1.6.0`. No feature/API change is accepted in this tranche. Any implementation/API correction returns to T1610 for renewed regret review.

The only intended changes after T1610 are release identity and release-facing documentation/evidence.

## T1610 qualification

The final pre-RC release-regret head was:

```text
dbb663406cfeb62ccdd4181fe34b3264a6e0462a
```

Workflow #999 / `35155346200` passed all seven PR Staging jobs:

```text
Package candidate
Runtime Windows x64
Runtime Windows ARM64
Runtime Linux x64
Runtime Linux ARM64
Runtime macOS x64
Runtime macOS ARM64
```

Linux x64 built with zero warnings/errors and passed 899/899 tests on each of `net8.0`, `net9.0`, and `net10.0`.

## RC qualification

The same implementation/API was promoted to:

```text
Version         1.6.0-rc.1
PackageVersion  1.6.0-rc.1
AssemblyVersion 1.0.0.0
```

Exact RC head:

```text
47728ad870205c89bc1d7c0014667774ea9960d9
```

Workflow #1002 / `35158260589` passed all seven jobs. Linux x64 again built with zero warnings/errors and passed 899/899 tests on each target framework.

No `src/` or public-API change was made between the T1610 qualification and RC qualification.

## Stable-source promotion

After the green RC qualification, release identity was promoted to:

```text
Version         1.6.0
PackageVersion  1.6.0
AssemblyVersion 1.0.0.0
```

Publication-facing README, CHANGELOG, API fingerprint metadata, and the human-readable 1.6 API baseline were aligned to stable-source state. The exported API fingerprint remains unchanged.

The final stable-source branch qualification must pass the same seven-job Staging matrix and fresh packed-package validation before publication readiness is declared.

## Frozen stable contract

```text
75 exported types
559 canonical declared contract lines
sha256 266e23e6f3b4d5be98c81b5d5774f1de47d488d9ede7025877e46388cae6d458
```

The release remains additive over the published 1.5 contract:

```text
1.5: 69 exported types / 525 lines
1.6: 75 exported types / 559 lines
Delta: +6 exported types / +34 lines
```

## Stable package graph

```text
Icod.DCurses 1.6.0
├── Icod.Terminal 1.15.0
└── Icod.TermInfo 1.14.0
```

Target frameworks remain:

```text
net8.0
net9.0
net10.0
```

The package remains LGPL-3.0-or-later and requires license acceptance. Symbols, source-link/repository metadata, README, icon/toolchain image, XML documentation, and license are part of the established package validation contract.

## Publication boundary

T1611 branch work deliberately does **not** perform these maintainer actions:

1. merge PR #31 into `main`;
2. post-merge Release workflow acceptance on `main`;
3. create/push tag `v1.6.0`;
4. create the GitHub Release;
5. publish `Icod.DCurses 1.6.0` to NuGet.

Those actions are intentionally separate and should occur only after the final stable-source branch head is green and the maintainer explicitly chooses to proceed.

## Final acceptance record

To be completed after the final stable-source exact-head workflow finishes. The final publication-ready source must be the exact head qualified by the seven-job matrix; any subsequent source/package/document change requires requalification.
