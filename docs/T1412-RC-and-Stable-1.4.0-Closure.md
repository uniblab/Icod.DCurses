# T1412 — RC and Stable 1.4.0 Closure

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1412  
**Published baseline:** `1.3.0`  
**Qualified T1411 head:** `570715e0764f9791fe197462a953df6eccf6105a`  
**Qualified T1411 workflow:** #797 / `34773668892` — all seven jobs green  
**Qualified final RC head:** `7f6bcedf70b9cd5cd15bf2a2a53437e23dac3c2f`  
**Qualified final RC workflow:** #802 / `34774226736` — all seven jobs green  
**Qualified stable-source head:** `571af7e1904eb20233ec4fa66c6b76d86478a3b7`  
**Qualified stable-source workflow:** #808 / `34774590867` — all seven jobs green  
**Stable-source identity:** `1.4.0`  
**AssemblyVersion:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Status:** T1412 complete; release-ready source pending explicit PR #29 merge approval

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

## Stable-source promotion and qualification

The qualified final RC implementation/API was promoted unchanged to:

```text
Version         1.4.0
PackageVersion  1.4.0
AssemblyVersion 1.0.0.0
```

Stable-source promotion changed stable package identity, stable release notes/status documentation, and closure evidence only. The production interaction implementation and frozen 1.4 public API remained unchanged from the qualified RC.

The stable-source candidate was finalized at:

```text
571af7e1904eb20233ec4fa66c6b76d86478a3b7
```

Workflow #808 / `34774590867` passed the complete seven-job matrix:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

That qualification proves stable package identity and metadata, portable symbols/documentation, the refreshed dependency graph, the frozen multi-target public API fingerprint, fresh NuGet-only interaction consumption, and the Windows/Linux/macOS x64/ARM64 runtime matrix.

Subsequent closure-only documentation edits do not reopen the implementation/API decision, but the current PR head remains subject to the repository's ordinary required checks before merge.

## Merge gate

The branch is release-ready source. Merge remains a separate explicit approval step.

PR #29 must not be merged without explicit user approval. After merge, the resulting `main` Release workflow must be qualified before creating/pushing the stable release tag or publishing release artifacts.

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
| stable-source `1.4.0` | `571af7e1904eb20233ec4fa66c6b76d86478a3b7` | #808 / `34774590867` | seven jobs green |
| PR #29 merge | pending explicit approval | — | not merged |
| `main` Release | pending post-merge | pending | not run |
