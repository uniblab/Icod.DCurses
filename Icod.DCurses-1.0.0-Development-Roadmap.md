# Icod.DCurses 1.0.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Merged source baseline:** `0.9.0`  
**Published stable baseline:** `0.8.0` until the 0.9 tag/publication gate completes  
**Development target:** `1.0.0`  
**Current candidate:** `1.0.0-rc.1`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Status:** 1.0 stable-release closure active; T1001/T1002 foundation established

---

## 1. Purpose

`Icod.DCurses 1.0.0` is release closure over the contract frozen by `0.9.0`.

The feature-building and pre-1.0 breaking-cleanup window is closed. The purpose of 1.0 development is to prove, document, package, and publish the already-accepted managed contract without introducing a surprise feature family or an incidental compatibility break.

The exact merged 0.9 `main` commit `2acf166aed9c56c330025ca923c1d7cae712f913` passed the complete repository `Release` workflow across Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64, and package/fresh-consumer validation before the 1.0 candidate promotion began.

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

The public contract accepted for 1.0 is intentionally identical to the 0.9 freeze:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The fingerprint covers declared public signatures, enum values, generic constraints, parameter order/ref/default metadata, accessor visibility, public fields/constants, and compiled nullability.

`docs/Public-API-Fingerprint-0.9.json` remains the historical pre-1.0 freeze record. `docs/Public-API-Fingerprint-1.0.json` is the stable-target baseline. Tests require the two contracts to remain identical.

The intentional lower-layer public type set remains exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

No new Terminal/TermInfo type may enter the public signature surface during 1.0 closure.

---

## 4. Frozen semantic/lifetime contract

Behavioral compatibility which reflection alone cannot express remains guarded by the pre-1.0 semantic and hardening suites.

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

Historical tranche roadmaps and records remain authoritative for the details of those releases and are not rewritten to current package versions.

---

## 6. 1.0 closure sequence

```text
T1001  post-0.9 merge baseline + 1.0 RC foundation
  -> T1002  1.0 public API/fingerprint carry-forward
  -> T1003  documentation/package/release-workflow audit
  -> T1004  exact 1.0.0-rc.1 full PR gate
  -> T1005  stable 1.0.0 source promotion and merge gate
```

### T1001 — post-0.9 baseline and RC foundation

Required result:

- merged 0.9 source is green under the complete `Release` main workflow;
- branch/PR is created from that exact merge commit;
- `Version` / `PackageVersion` become `1.0.0-rc.1`;
- `AssemblyVersion` becomes `1.0.0.0`;
- dependencies remain Terminal 1.4.0 / TermInfo 1.10.0;
- no runtime feature/public API change enters with the version promotion.

Record: `docs/T1001-1.0.0-Release-Closure-Foundation.md`.

### T1002 — public contract carry-forward

Required result:

- add the explicit 1.0 machine fingerprint baseline;
- prove it matches the frozen 0.9 API contract exactly;
- retain 0.9 semantic/lifetime/dependency tests;
- preserve historical 0.9 baseline files unchanged;
- document `AssemblyVersion 1.0.0.0` as the stable major-version identity.

Records:

- `docs/Public-API-Fingerprint-1.0.json`
- `docs/Public-API-Baseline-1.0.md`
- `docs/T1002-1.0-Public-Contract-Carry-Forward.md`.

### T1003 — documentation/package/release audit

Review current consumer/maintainer surfaces:

- root README;
- this roadmap;
- 1.0 API baseline/fingerprint;
- migration/compatibility guidance;
- package release notes;
- XML documentation generation;
- package verifier and package-only consumer;
- samples where they state current ownership/version behavior;
- Authors/Copyright/License wording;
- release workflow and dependency-version generation;
- current prose convention writing `ncurses` as `` `ncurses` ``.

The audit must distinguish latest published package state from merged/validated source and candidate state. Stable 1.0 installation must not be advertised before publication.

Record: `docs/T1003-1.0-Documentation-Package-and-Release-Audit.md`.

### T1004 — exact RC gate

One exact `1.0.0-rc.1` head must pass:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- `net8.0`, `net9.0`, and `net10.0` tests under Staging warnings-as-errors;
- package validation;
- fresh generated-package consumer;
- the complete public API/dependency/semantic/hardening suite;
- representative editor/pager/Unicode/rich-input/lifecycle/ownership/failure/pad/high-frequency acceptance and the `top`, `slabtop`, and `watch` sample builds.

No feature/API expansion is permitted in T1004.

### T1005 — stable 1.0 source closure

After a green exact RC:

- promote unchanged runtime/public behavior to `Version 1.0.0` / `PackageVersion 1.0.0`;
- retain `AssemblyVersion 1.0.0.0`;
- freeze final README/API/package/release records;
- require the exact stable-source PR head to pass the same complete gate;
- leave merge, post-merge `Release` validation, `v1.0.0` tagging, NuGet.org/GitHub Packages publication, symbol/checksum assets, and GitHub Release creation as explicit later actions.

---

## 7. Package and release policy

The candidate and stable package continue to target:

```text
net8.0
net9.0
net10.0
```

Direct runtime dependencies remain:

```text
Icod.Terminal 1.4.0
Icod.TermInfo 1.10.0
```

PR validation uses `Staging`. Pushes to `main` and release tags use `Release`.

Package verification must continue to validate assembly/package identity, XML documentation, symbols, license/readme/icon/repository metadata, exact dependency groups, and a fresh package-only consumer.

Release-page dependency wording must be derived from the project `PackageReference` values rather than a duplicated hard-coded version string.

---

## 8. Explicit 1.0 non-goals

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

## 9. Release discipline

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

## 10. Immediate status

```text
0.8.0 production hardening         published
0.9.0 contract freeze              merged; post-merge Release gate green
1.0.0-rc.1 stable closure          active
  T1001 foundation                 established
  T1002 compatibility lock         active
  T1003 documentation/package audit next
  T1004 exact RC gate              pending
  T1005 stable source closure      pending
```
