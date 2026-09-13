# T1412 — RC and Stable 1.4.0 Closure

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1412  
**Published baseline:** `1.3.0`  
**Qualified T1411 head:** `570715e0764f9791fe197462a953df6eccf6105a`  
**Qualified T1411 workflow:** #797 / `34773668892` — all seven jobs green  
**RC identity:** `1.4.0-rc.1`  
**AssemblyVersion:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Status:** RC candidate qualification active

---

## Entry gate

T1411 closed on the exact candidate head:

```text
570715e0764f9791fe197462a953df6eccf6105a
```

Workflow #797 / `34773668892` passed:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

The run restored and qualified the refreshed published dependency graph:

```text
Icod.Terminal 1.13.0
Icod.TermInfo 1.12.0
```

The frozen 1.4 public API remained:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

T1411 found no public API, ownership, packaging, documentation, licensing, or dependency-boundary regret requiring an implementation/API correction before RC.

## RC promotion

The T1411-qualified implementation/API is promoted unchanged to:

```text
Version         1.4.0-rc.1
PackageVersion  1.4.0-rc.1
AssemblyVersion 1.0.0.0
```

The RC promotion may change only release identity, release-facing documentation, and closure evidence. It does not reopen the interaction contract or add features.

## RC exact-head acceptance gate

The final RC candidate head must pass the complete seven-job pull-request matrix:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

The package candidate must continue to validate package metadata, symbols, documentation, dependency groups, the frozen multi-target public API fingerprint, and fresh NuGet-only interaction consumption.

No stable-source promotion occurs until that exact RC head is green.

## Stable-source promotion

After the RC exact-head matrix is green, the same accepted implementation/API may be promoted to:

```text
Version         1.4.0
PackageVersion  1.4.0
AssemblyVersion 1.0.0.0
```

The stable-source promotion must not alter the frozen 1.4 implementation or public API. Only stable package identity, stable release notes/status documentation, and this closure record may change.

The stable-source exact head must then pass the same complete seven-job matrix again.

## Merge gate

A green stable-source branch is release-ready source, but merge remains a separate explicit approval step.

PR #29 must not be merged merely because T1412 qualification succeeds. After the stable-source exact head is fully green, the release state is reported for explicit merge approval.

After merge, the resulting `main` Release workflow must be qualified before creating/pushing the stable release tag or publishing release artifacts.

Tagging, GitHub Release creation, and NuGet publication remain separate explicit release actions.

## Invariants carried through closure

T1412 does not reopen the 1.4 architecture. Stable closure retains:

- application-owned event-loop policy;
- bounded interaction regions and gesture registries;
- deterministic panel-aware hit testing and region-local coordinates;
- logical focus distinct from terminal/window-manager focus;
- explicit focus traversal/repair and no automatic mouse focus;
- semantic gestures and command identities;
- structured results rather than callback execution;
- DCurses pointer preferences with Terminal-owned physical pointer leases;
- no hidden terminal I/O or second terminal reader;
- the published 1.3 compatibility floor;
- one frozen 1.4 fingerprint across `net8.0`, `net9.0`, and `net10.0`.

## Closure ledger

| Stage | Head | Workflow | Result |
|---|---|---|---|
| T1411 qualified | `570715e0764f9791fe197462a953df6eccf6105a` | #797 / `34773668892` | seven jobs green |
| `1.4.0-rc.1` | pending | pending | qualification active |
| stable-source `1.4.0` | pending | pending | not promoted |
| PR #29 merge | pending explicit approval | — | not merged |
| `main` Release | pending | pending | not run |
