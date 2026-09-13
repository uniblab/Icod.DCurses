# T1412 — RC and Stable 1.4.0 Closure

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1412  
**Published baseline:** `1.3.0`  
**Qualified T1411 head:** `570715e0764f9791fe197462a953df6eccf6105a`  
**Qualified T1411 workflow:** #797 / `34773668892` — all seven jobs green  
**Qualified final RC head:** `7f6bcedf70b9cd5cd15bf2a2a53437e23dac3c2f`  
**Qualified final RC workflow:** #802 / `34774226736` — all seven jobs green  
**Stable-source identity:** `1.4.0`  
**AssemblyVersion:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Status:** stable-source exact-head qualification active; merge remains pending explicit approval

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

## RC promotion and qualification

The T1411-qualified implementation/API was promoted unchanged to:

```text
Version         1.4.0-rc.1
PackageVersion  1.4.0-rc.1
AssemblyVersion 1.0.0.0
```

The RC promotion changed release identity and release-facing documentation only. It did not reopen the interaction contract or add features.

### Windows ARM64 timeout investigation

The first RC exact-head workflow, #800 / `34773915278`, passed the package candidate plus Windows x64, both Linux architectures, and both macOS architectures. Its initial Windows ARM64 attempt failed one pre-existing acceptance test on `net10.0`:

```text
Icod.DCurses.Tests.CursesRichInputAcceptanceTests
    .RichInputFamiliesAndModifiedKeysUseSingleCursesEventStream

System.OperationCanceledException
```

The test used one five-second `CancellationTokenSource` across a sequence of seven independent `ReadEventAsync(...)` operations. On the failing Windows ARM64/net10 run, the shared absolute budget expired partway through the scripted rich-input sequence.

Evidence that this was a test-timeout defect rather than an RC implementation regression:

- the exact T1411 implementation had already passed Windows ARM64 in workflow #797;
- RC promotion changed package identity/documentation, not production interaction/input code;
- the same RC Windows ARM64 job was rerun unchanged and the full test step passed;
- the other five runtime architectures and package candidate passed the original RC attempt.

The acceptance test was hardened without changing production code or public API: each expected semantic input event now receives its own five-second bounded read timeout instead of sharing one absolute deadline across the entire multi-event sequence.

Test-only hardening commit:

```text
9e6a17f65079d6052db7183a493b98d523387b31
```

This preserves hang detection while removing dependence on cumulative runner scheduling time.

### Final RC gate

The final documented RC head was:

```text
7f6bcedf70b9cd5cd15bf2a2a53437e23dac3c2f
```

Workflow #802 / `34774226736` passed the complete seven-job matrix:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

The hardened Windows ARM64 acceptance suite passed under the same final RC exact head. No production implementation or public API correction was required.

## Stable-source promotion

The qualified final RC implementation/API is now promoted unchanged to:

```text
Version         1.4.0
PackageVersion  1.4.0
AssemblyVersion 1.0.0.0
```

Stable-source promotion changes stable package identity, stable release notes/status documentation, and closure evidence only. The production interaction implementation and frozen 1.4 public API remain unchanged from the qualified RC.

## Stable-source exact-head acceptance gate

The stable-source exact head must pass the complete seven-job pull-request matrix again:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

The package candidate must continue to validate package metadata, symbols, documentation, dependency groups, the frozen multi-target public API fingerprint, and fresh NuGet-only interaction consumption.

Stable-source is release-ready only after that exact head is green.

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
| initial `1.4.0-rc.1` | `f21110e7d1e75bbe89152ef44c2aa51fd6fac63f` | #800 / `34773915278` | Windows ARM64 timed out once; unchanged rerun passed |
| RC test hardening | `9e6a17f65079d6052db7183a493b98d523387b31` | — | per-read timeout; production/API unchanged |
| final `1.4.0-rc.1` | `7f6bcedf70b9cd5cd15bf2a2a53437e23dac3c2f` | #802 / `34774226736` | seven jobs green |
| stable-source `1.4.0` | pending exact final head | pending | qualification active |
| PR #29 merge | pending explicit approval | — | not merged |
| `main` Release | pending | pending | not run |
