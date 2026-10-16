# Icod.DCurses 0.9.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `0.9.0`  
**Published stable predecessor:** `0.8.0`  
**Dependency baseline:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Release theme:** Contract freeze / release candidate  
**Stable package source:** `0.9.0`  
**Assembly version:** `0.9.0.0`  
**Status:** T901–T908 complete; T909 stable-source/documentation merge gate

---

## 1. Release objective

`Icod.DCurses 0.9.0` freezes the managed contract intended to become `1.0.0`.

This is not another feature tranche. The release deliberately reviews and machine-guards the complete 0.8-derived public surface, semantic values, ownership rules, geometry, Unicode/cell behavior, exceptions/cancellation, nullable annotations, dependency boundary, package metadata, documentation, and representative application-shaped workloads.

No public breaking cleanup was accepted. Existing 0.8 source consumers therefore have no planned source migration to 0.9.

---

## 2. Frozen architecture

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

DCurses does not regain raw byte decoding, host-mode ownership, terminal capability storage, PTY ownership, terminal emulation, or application-specific policy.

The final intentional lower-layer public type set remains exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

---

## 3. Canonical public API freeze

The accepted compiled public contract is:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

`PublicApiFingerprintTests` produces the same canonical fingerprint for `net8.0`, `net9.0`, and `net10.0` and fails on unreviewed changes to public declared signatures, enum values, generic constraints, parameter/ref/default metadata, fields/constants, accessor visibility, or compiled nullability.

`PublicDependencyBoundaryTests` independently prevents additional Terminal/TermInfo type leakage.

---

## 4. Development sequence and results

```text
T901  public-surface inventory                         complete
  -> T902  machine-readable public API fingerprint   complete
  -> T903  enum/geometry/cell/text semantic freeze   complete
  -> T904  lifetime/ownership/exception freeze       complete
  -> T905  nullable/XML/dependency regret audit      complete
  -> T906  docs/migration/package-consumer freeze    complete
  -> T907  application-shaped acceptance             complete
  -> T908  complete regret/package/architecture RC   complete
  -> T909  stable source + documentation audit       merge gate
```

### T901 — inventory and regret foundation

- established `0.9.0-alpha.1` / `AssemblyVersion 0.9.0.0`;
- inventoried all exported types and public declared members;
- found no accidental hardening, synchronization, transport, resolver, physical-state, diagnostics, or test type in the public surface;
- prohibited incidental public changes for the remainder of 0.9.

Record: `docs/T901-Public-Surface-Inventory-and-Regret-Review.md`.

### T902 — machine-readable fingerprint

- added canonical compiled-assembly fingerprinting;
- froze 43 exported types / 309 canonical contract lines;
- verified one identical fingerprint across all three target frameworks;
- stored the machine baseline in `docs/Public-API-Fingerprint-0.9.json`.

### T903 — semantic freeze

Dedicated tests pin behavior which reflection cannot fully express:

- zero-based row/column coordinates;
- parent-local subwindow origins;
- rows/columns dimension semantics;
- representative range-exception parameter identities;
- half-open text-column intervals;
- no two-column glyph splitting;
- Unicode 17.0.0 width behavior;
- default narrow and explicit wide East Asian Ambiguous policy;
- semantic line identity;
- wide-cell footprint repair across overwrite/resize boundaries.

### T904 — lifetime, ownership, exceptions, cancellation

The 0.8 hardening model is accepted as the 1.x model:

- logical surfaces are single-writer unless explicitly documented otherwise;
- one Terminal-owned event consumer may coexist with serialized refresh/output;
- supplied `TerminalSession` ownership transfers only after successful curses initialization;
- caller cancellation remains cancellation;
- disposal-unblocked waits surface `ObjectDisposedException`;
- repeated disposal shares one restoration operation;
- uncertain output invalidates retained physical knowledge for safe retry;
- independent primary/restoration failures remain visible.

Record: `docs/T904-Lifetime-Ownership-Exception-and-Cancellation-Freeze.md`.

### T905 — nullable/XML/dependency regret audit

- nullable reference types remain enabled;
- compiled nullability participates in the fingerprint;
- XML documentation remains generated and required in all package TFMs;
- all five approved Terminal/TermInfo type exposures were reviewed and retained intentionally;
- no additional upstream public exposure was accepted;
- no breaking cleanup was deferred to `1.0.0`.

Record: `docs/T905-Nullable-Documentation-and-Dependency-Regret-Review.md`.

### T906 — documentation, migration, package consumer

- added `docs/0.9-Contract-Freeze-and-1.0-Migration-Guide.md`;
- retained the comprehensive generated-package smoke consumer rather than duplicating it;
- corrected the GitHub Release workflow so displayed Terminal/TermInfo dependency versions are derived from project `PackageReference` metadata instead of a stale hard-coded value;
- synchronized the README with the published 0.8 predecessor and 0.9 contract-freeze model.

### T907 — representative application acceptance

The release gate composes established deterministic coverage for:

- editor-like and pager-like editing/scrolling;
- Unicode-heavy and wide-cell boundaries;
- key/focus/paste/mouse rich input;
- resize/suspend/resume;
- cancellation/disposal and ownership cycling;
- output failure/retry;
- pads and repeated panning;
- large-screen, sparse, high-frequency, and no-op refresh;
- `top`, `slabtop`, and `watch` acceptance sample builds;
- fresh package-only consumption.

Exact pre-RC head `1011c7db06632137c4ca268d496e2f561040addb` passed Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64, and package validation.

Record: `docs/T907-Representative-Application-Acceptance-Gate.md`.

### T908 — regret/package/architecture RC gate

`0.9.0-rc.1` introduced no API or runtime behavior change.

Exact RC head:

```text
67269d0346e31c356414007dc807c82eeebe97aa
```

passed the same complete seven-job matrix with the canonical API fingerprint and dependency boundary unchanged.

Record: `docs/T908-Contract-Regret-Package-Architecture-and-RC-Gate.md`.

### T909 — stable source and documentation audit

T909 promotes the accepted RC to:

```text
Version         0.9.0
PackageVersion  0.9.0
AssemblyVersion 0.9.0.0
```

No runtime/public API behavior changes are permitted.

The final documentation audit reviews and corrects current-contract material while leaving historical release records historical. The audit covers:

- root README status and installation wording;
- this roadmap;
- the authoritative 1.0 roadmap;
- migration guidance after the no-breaking-change T905 result;
- human-readable and machine API baselines;
- T907/T908 status wording;
- package release notes;
- release-page dependency generation;
- current `ncurses` prose formatting.

Record: `docs/T909-0.9.0-Stable-Release-Closure-and-Documentation-Audit.md`.

The exact final stable-source head must pass the same Windows/Linux/macOS x64/ARM64 plus package Staging gate. Only that exact green SHA is the 0.9 merge candidate.

---

## 5. Explicit non-goals

0.9 does not add:

- native `ncurses` ABI compatibility;
- exhaustive C curses source compatibility;
- forms, menus, panels, or widget frameworks;
- declarative UI;
- terminal emulation or PTY ownership;
- SSH transport or graphics protocols;
- arbitrary child ANSI interpretation;
- bidirectional/complex-script shaping;
- transparent multi-writer thread safety;
- a public scheduler/event-loop abstraction;
- a public performance/diagnostics API without a demonstrated stable-contract need.

---

## 6. Release discipline

1. No public API change is incidental.
2. Public changes require fingerprint, regret, semantic/dependency, and migration review together.
3. Historical milestone documents remain historical.
4. Tests remain non-interactive and do not write unsolicited stdout/stderr.
5. Pull requests validate under Staging; `main`/tags validate under Release.
6. Package-only validation is mandatory.
7. `1.0.0` remains release closure over the accepted 0.9 contract, not a deferred breaking-cleanup tranche.

After the final T909 PR gate, merge, post-merge Release validation, tagging `v0.9.0`, and publication remain explicit later actions.
