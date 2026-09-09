# Icod.DCurses 1.0.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Merged stable-source baseline:** `1.0.0` at main commit `686814fe3036484265513d06fe48aec0315912aa`  
**Published stable baseline:** `0.9.0`  
**Package version:** `1.0.0`  
**Assembly version:** `1.0.0.0`  
**Current runtime dependencies:** `Icod.Terminal 1.5.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Status:** T1001–T1005 complete and merged; post-closure Terminal 1.5.0 dependency refresh active on PR #24 without a DCurses version bump

---

## 1. Purpose

`Icod.DCurses 1.0.0` is the stable release closure of the contract frozen by `0.9.0`.

The feature-building and pre-1.0 breaking-cleanup window is closed. T1001–T1005 proved, documented, packaged, and merged the accepted managed contract without introducing a surprise feature family or incidental compatibility break.

The merged 0.9 `main` commit `2acf166aed9c56c330025ca923c1d7cae712f913` passed the complete repository `Release` workflow across Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64, and package/fresh-consumer validation before 1.0 candidate work began.

The documentation-complete stable 1.0 source head `5c17607194b546c6d831be5811d1865a195b970e` passed the complete seven-job PR gate and was merged into `main` as commit `686814fe3036484265513d06fe48aec0315912aa`.

PR #24 is a post-closure dependency refresh from `Icod.Terminal 1.4.0` to `1.5.0`. It deliberately leaves `Version 1.0.0`, `PackageVersion 1.0.0`, `AssemblyVersion 1.0.0.0`, and the frozen DCurses public API unchanged.

---

## 2. Architectural boundary

```text
Applications
    top / slabtop / watch / editors / pagers / TUIs
                         |
                    Icod.DCurses
       curses events / cells / windows / pads
      text-cell policy / refresh / presentation
                         |
                    Icod.Terminal
      endpoint / mode / input / lifecycle / leases
                         |
                    Icod.TermInfo
               capability authority
                         |
                 terminal / tty
```

DCurses does not regain responsibilities owned by Terminal or TermInfo. In particular it does not add a second raw input loop, private terminal-mode owner, private capability database, terminal emulator, PTY owner, or application-specific ProcPs policy.

---

## 3. Frozen public contract

The stable public contract is intentionally identical to the 0.9 freeze:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The fingerprint covers declared public signatures, enum values, generic constraints, parameter order/ref/default metadata, accessor visibility, public fields/constants, and compiled nullability.

`docs/Public-API-Fingerprint-0.9.json` remains the historical pre-1.0 freeze record. `docs/Public-API-Fingerprint-1.0.json` is the stable baseline. Tests require the two contracts to remain identical.

The intentional lower-layer public type set remains exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

The Terminal 1.5.0 dependency refresh does not add another upstream public type or otherwise change the DCurses public fingerprint.

---

## 4. Frozen semantic and lifetime contract

Behavioral compatibility which reflection alone cannot express remains guarded by the established semantic and hardening suites.

The stable contract includes:

- zero-based row/column coordinates;
- parent-local subwindow origins;
- rows/columns meaning height/width;
- half-open column-oriented text intervals;
- no splitting of two-column text elements;
- Unicode 17.0.0 display-width data;
- narrow East Asian Ambiguous characters by default and explicit wide policy;
- wide-cell leader/continuation repair;
- semantic line-glyph identity distinct from ordinary Unicode text;
- single-writer logical screen/window/pad/viewport mutation unless explicitly documented otherwise;
- one Terminal-owned event consumer allowed concurrently with serialized refresh/output activity;
- Terminal remains the live-terminal mode/input/decoder/lease owner;
- caller cancellation remains cancellation;
- disposal-unblocked public waits surface `ObjectDisposedException`;
- repeated disposal shares one restoration operation;
- uncertain output invalidates retained physical knowledge for safe retry;
- independently meaningful primary/restoration failures remain visible;
- Terminal restoration remains authoritative.

---

## 5. Completed pre-1.0 release train

| Release | Theme | Stable outcome |
|---|---|---|
| `0.2.0` | Terminal semantic input parity | Complete curses facade over stable Terminal input/event/protocol semantics |
| `0.3.0` | Unicode/cell contract | Unicode 17 width semantics, complete text elements, wide-cell invariants, column helpers |
| `0.4.0` | Editing/composition | Mature window geometry, editing, copy/overlay, drawing, and damage operations |
| `0.5.0` | Pads/large surfaces | Off-screen pads, independent viewports, panning and visible-change observation |
| `0.6.0` | Presentation | Semantic rendition, capability-aware degradation, line drawing and presentation observation |
| `0.7.0` | Refresh optimization | Synchronized output, cost-aware terminal operations, edit/scroll optimization, deterministic output gates |
| `0.8.0` | Production hardening | Explicit concurrency/lifetime/failure model, race recovery, stress and ownership acceptance |
| `0.9.0` | Contract freeze | Machine API fingerprint, semantic/lifetime/dependency/documentation/package freeze |
| `1.0.0` | Stable release closure | Frozen 0.9 contract promoted to stable assembly/package identity and merged after full RC/stable gates |

Historical tranche roadmaps and records remain authoritative for the details of those releases and are not rewritten to current package versions.

---

## 6. 1.0 closure sequence and results

```text
T1001  post-0.9 merge baseline + 1.0 RC foundation      complete
  -> T1002  1.0 public API/fingerprint carry-forward   complete
  -> T1003  documentation/package/release audit        complete
  -> T1004  exact 1.0.0-rc.1 full PR gate             complete
  -> T1005  stable 1.0.0 source + documentation gate  complete
```

### T1001 — post-0.9 baseline and RC foundation

Completed results:

- merged 0.9 source passed the complete `Release` main workflow;
- branch/PR was created from that exact merge commit;
- `Version` / `PackageVersion` became `1.0.0-rc.1`;
- `AssemblyVersion` became `1.0.0.0`;
- dependencies at this historical checkpoint were Terminal 1.4.0 / TermInfo 1.10.0;
- no runtime feature/public API change entered with the version promotion.

Record: `docs/T1001-1.0.0-Release-Closure-Foundation.md`.

### T1002 — public contract carry-forward

Completed results:

- added the explicit 1.0 machine fingerprint baseline;
- proved it matches the frozen 0.9 API contract exactly;
- retained 0.9 semantic/lifetime/dependency tests;
- preserved historical 0.9 baseline files unchanged;
- documented `AssemblyVersion 1.0.0.0` as the stable major-version identity.

Records:

- `docs/Public-API-Fingerprint-1.0.json`
- `docs/Public-API-Baseline-1.0.md`
- `docs/T1002-1.0-Public-Contract-Carry-Forward.md`.

### T1003 — documentation/package/release audit

Completed results:

- corrected README state after the 0.9 merge;
- added stable 1.0 compatibility/migration guidance;
- reviewed package metadata, XML documentation, symbols, package verifier, and fresh package consumer;
- retained Authors/Copyright/License wording and the Markdown convention of writing `ncurses` as `` `ncurses` ``;
- verified release-page dependency versions are derived from project `PackageReference` metadata;
- found no issue requiring a runtime or public-contract change.

Record: `docs/T1003-1.0-Documentation-Package-and-Release-Audit.md`.

### T1004 — exact release-candidate gate

Exact accepted RC head:

```text
1968bae18610e69e56dc8f720bffb099cb58eb24
```

Workflow run `34371426709` (#389) passed:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- package/fresh-consumer validation.

The complete `net8.0`, `net9.0`, and `net10.0` tests, API/dependency/semantic/hardening suites, representative acceptance workloads, and sample builds remained green.

Record: `docs/T1004-1.0.0-RC-Final-Gate.md`.

### T1005 — stable 1.0 source closure

Stable identity:

```text
Version         1.0.0
PackageVersion  1.0.0
AssemblyVersion 1.0.0.0
```

Exact documentation-complete stable source head:

```text
5c17607194b546c6d831be5811d1865a195b970e
```

Workflow run `34372963625` (#396) passed Windows/Linux/macOS x64/ARM64 plus package/fresh-consumer validation on that exact SHA.

PR #23 then merged the stable source into `main` as commit:

```text
686814fe3036484265513d06fe48aec0315912aa
```

Records:

- root `README.md`;
- this roadmap;
- `docs/Public-API-Fingerprint-1.0.json`;
- `docs/Public-API-Baseline-1.0.md`;
- `docs/1.0-Stable-Compatibility-and-Migration-Guide.md`;
- `docs/T1005-1.0.0-Stable-Release-Closure.md`.

The later tag/publication step remains separate from the historical T1005 branch gate.

---

## 7. Current package and release policy

The 1.0 package targets:

```text
net8.0
net9.0
net10.0
```

Current direct runtime dependencies are:

```text
Icod.Terminal 1.5.0
Icod.TermInfo 1.10.0
```

This dependency refresh does not change any DCurses version field.

PR validation uses `Staging`. Pushes to `main` and release tags use `Release`.

Package verification continues to validate assembly/package identity, XML documentation, symbols, license/readme/icon/repository metadata, exact dependency groups, and a fresh package-only consumer.

Release-page dependency wording is derived from project `PackageReference` values rather than duplicated hard-coded version strings.

The latest published GitHub release is `0.9.0`. Stable 1.0 source is not described as a published package until the `v1.0.0` tag/publication workflow succeeds.

---

## 8. Post-closure Terminal 1.5.0 dependency refresh

PR #24 updates the direct Terminal dependency only:

```text
Icod.Terminal 1.4.0 -> 1.5.0
```

Constraints:

- `Version` remains `1.0.0`;
- `PackageVersion` remains `1.0.0`;
- `AssemblyVersion` remains `1.0.0.0`;
- `Icod.TermInfo` remains `1.10.0`;
- the 1.0 public API fingerprint remains unchanged;
- the strict package verifier requires Terminal 1.5.0 in every TFM dependency group;
- current README, API baseline, migration guide, samples, and package-tool documentation describe the new dependency baseline;
- T1001–T1005 and earlier tranche records remain unchanged because they document the dependency actually validated at those historical checkpoints.

Initial PR restore/build failure is expected while NuGet.org indexing for `Icod.Terminal 1.5.0` is still propagating. Once the package resolves, the normal six-architecture plus package/fresh-consumer matrix becomes the compatibility signal.

---

## 9. Explicit 1.0 non-goals

The stable core does not require:

- native `ncurses` ABI compatibility;
- exhaustive C curses source compatibility;
- `printw`/`scanw` compatibility;
- forms, menus, or panels;
- a widget toolkit or declarative UI;
- terminal emulation;
- pseudo-terminal creation/management;
- SSH transport;
- graphics protocols;
- arbitrary child ANSI interpretation;
- bidirectional or complex-script shaping;
- wrappers for every Terminal feature;
- transparent multi-writer thread safety;
- a public diagnostics/performance API without a demonstrated stable-core requirement.

Focused compatibility/widget packages may evolve separately after the stable core contract exists.

---

## 10. Release discipline

1. No 1.0 public API change is incidental.
2. A breaking change requires an explicit compatibility decision; it is not ordinary release cleanup.
3. `<Version>` and `<PackageVersion>` remain synchronized.
4. Public/protected/internal methods validate applicable arguments at entry.
5. All `if`/`else` bodies use braces.
6. Terminal capability behavior remains TermInfo-driven when a capability models the operation.
7. Tests remain non-interactive unless explicitly manual and do not emit unsolicited stdout/stderr.
8. The Terminal/TermInfo dependency allow-list remains machine-guarded.
9. Every candidate/stable checkpoint validates the generated package and fresh consumer.
10. Historical release records remain historical.

---

## 11. Immediate status

```text
0.8.0 production hardening         published
0.9.0 contract freeze              published
1.0.0-rc.1 stable closure          accepted; seven-job gate green
1.0.0 stable source                merged into main
v1.0.0 publication                 pending
Terminal 1.5.0 dependency refresh  PR #24 active; DCurses version unchanged
```
