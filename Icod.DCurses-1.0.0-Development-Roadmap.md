# Icod.DCurses 1.0.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Published stable baseline:** `0.8.0`  
**Validated stable-source target:** `0.9.0`  
**Development destination:** `1.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Status:** 0.9 contract freeze complete through T908; stable-source T909 merge gate active; `1.0.0` release closure next

---

## 1. Purpose

This roadmap carries `Icod.DCurses` to a stable `1.0.0` managed curses contract.

The feature-building portion of the pre-1.0 train is complete. `0.9.0` freezes the public and semantic contract intended to become 1.0; `1.0.0` is therefore release closure, not another feature or breaking-cleanup tranche.

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

DCurses does not regain responsibilities that belong to Terminal or TermInfo. In particular it does not add a second raw input loop, private terminal-mode owner, private capability database, terminal emulator, PTY owner, or ProcPs/application policy.

---

## 3. Release train

| Release | Theme | Outcome |
|---|---|---|
| `0.2.0` | Terminal semantic input parity | Stable curses facade for complete Terminal key/event/protocol semantics |
| `0.3.0` | Unicode and terminal-cell contract | Unicode 17 width data, complete text elements, wide-cell invariants, column-safe helpers |
| `0.4.0` | Window editing and composition | Geometry, editing, copy/overlay, drawing, region/damage operations |
| `0.5.0` | Pads and large surfaces | Off-screen pads, independent viewports, panning and visible-change observation |
| `0.6.0` | Rendition, drawing, presentation | Complete semantic style/presentation vocabulary and capability-aware degradation |
| `0.7.0` | Refresh/output optimization | Cost-aware terminal operations, synchronized output, structural edit/scroll optimizations |
| `0.8.0` | Production hardening | Explicit concurrency/lifetime/failure model, ownership/race recovery, stress acceptance |
| `0.9.0` | Contract freeze / release candidate | API fingerprint, semantic/lifetime/dependency/documentation/package freeze |
| `1.0.0` | Stable release closure | Publish the accepted 0.9 contract without a surprise feature family |

---

## 4. Completed contract milestones

### 0.2 — semantic input

The curses facade preserves the stable Terminal semantic keyboard/input model, including modern key vocabulary, press/repeat/release phases, modifiers, associated text, focus, paste, mouse, lifecycle, protocol acquisition, and controlled availability. Terminal remains the byte-stream decoder and lease owner.

### 0.3 — Unicode/cells

The frozen text model includes malformed UTF-16 normalization, complete Unicode text-element processing, zero/one/two-column semantics, continuation-cell invariants, combining/variation-selector/keycap/flag/emoji/ZWJ behavior, Unicode 17.0.0 width data, explicit narrow/wide East Asian Ambiguous policy, and public measure/truncate/slice helpers expressed in terminal columns.

Bidirectional layout, Arabic/Indic shaping, and general font shaping remain outside the core contract.

### 0.4 — windows/editing/composition

The window model provides shared logical views with zero-based parent-local geometry, repositioning/resizing, inspection, fill/erase, insert/delete cells and lines, copy/overlay, drawing, and damage operations while preserving wide-cell correctness.

### 0.5 — pads/large surfaces

`CursesPad` is an off-screen logical surface reusing the window editing contract. `CursesPadViewport` adds independent projection/panning/change-observation state without owning terminal modes, input, or physical refresh state.

### 0.6 — presentation

The semantic presentation contract includes indexed/direct color policy, default color restoration, bold/dim/italic/underline/reverse/standout/blink/conceal/strikeout, `ncv`-aware degradation, presentation-capability observation, semantic line cells, and ACS/Unicode/ASCII fallback.

### 0.7 — refresh/output optimization

The retained logical/physical model remains authoritative while safe, strictly cheaper advertised terminal operations may be selected. The release includes synchronized-output framing, cost-aware cursor movement/erase, exact insert/delete-character and insert/delete-line/scroll optimization, temporary scroll-region restoration, differential rendition transitions, deterministic byte/write gates, and safe fallback after output uncertainty.

### 0.8 — production hardening

`0.8.0` is complete and published.

The accepted 1.x hardening model is:

- logical screen/window/pad/viewport mutation is single-writer unless explicitly documented otherwise;
- one Terminal-owned event consumer may wait concurrently with refresh/output activity;
- terminal-mutating session work is internally serialized;
- caller cancellation remains distinguishable from disposal;
- disposal unblocks pending DCurses event/lifecycle waits without discarding Terminal decoder state;
- repeated disposal shares one restoration operation;
- resize/suspend/resume and rich-input ownership survive repeated stress cycles;
- output uncertainty invalidates retained physical knowledge for safe retry;
- Terminal restoration remains authoritative after DCurses presentation failures;
- no public hardening/scheduler/lock/diagnostics API is added.

The runtime/package gate covers Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64 and `net8.0`/`net9.0`/`net10.0`.

---

## 5. Version 0.9.0 — final contract freeze

`0.9.0` is the contract intended to become 1.0.

The canonical compiled public API baseline is:

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

It is machine-generated identically on `net8.0`, `net9.0`, and `net10.0` and covers declared public signatures, enum values, generic constraints, parameter/ref/default metadata, accessor visibility, fields/constants, and compiled nullability.

Dedicated semantic tests additionally freeze zero-based row/column and parent-local geometry, rows/columns dimension semantics, representative range/argument exception categories and parameter identities, half-open column slicing, Unicode 17.0.0/Ambiguous-width policy, semantic line identity, wide-footprint repair, ownership transfer, cancellation/disposal, EOF, output-failure/retry, dual-failure, and restoration rules.

The final accepted lower-layer public type definitions are exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

No public breaking cleanup was accepted relative to 0.8. Existing 0.8 source consumers therefore have no planned migration to the 0.9 public contract.

The package/release review corrected GitHub Release dependency-note generation so Terminal/TermInfo versions come from the project package references rather than a hard-coded string.

Representative acceptance reuses the established editor/pager, Unicode, rich-input, lifecycle, ownership, failure-recovery, pad, large-screen, sparse/high-frequency/no-op, `top`, `slabtop`, `watch`, and package-only consumer gates.

Exact `0.9.0-rc.1` head:

```text
67269d0346e31c356414007dc807c82eeebe97aa
```

passed Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64, and package/fresh-consumer validation.

T909 promotes the unchanged contract to stable `0.9.0`, adds the final human-readable API baseline, and performs a documentation audit/correction pass before the exact stable-source merge gate.

Detailed records:

- `Icod.DCurses-0.9.0-Development-Roadmap.md`
- `docs/Public-API-Fingerprint-0.9.json`
- `docs/Public-API-Baseline-0.9.md`
- `docs/T901-Public-Surface-Inventory-and-Regret-Review.md`
- `docs/T904-Lifetime-Ownership-Exception-and-Cancellation-Freeze.md`
- `docs/T905-Nullable-Documentation-and-Dependency-Regret-Review.md`
- `docs/0.9-Contract-Freeze-and-1.0-Migration-Guide.md`
- `docs/T907-Representative-Application-Acceptance-Gate.md`
- `docs/T908-Contract-Regret-Package-Architecture-and-RC-Gate.md`
- `docs/T909-0.9.0-Stable-Release-Closure-and-Documentation-Audit.md`

---

## 6. Version 1.0.0 — stable closure

`1.0.0` SHALL be release closure over the accepted 0.9 contract.

Before publication:

- the complete 0.9 fingerprint SHALL remain intentionally accepted;
- no unreviewed public signature, enum value, nullability, dependency-boundary, or semantic change may enter;
- all `net8.0`, `net9.0`, and `net10.0` targets SHALL build/test/package cleanly;
- Windows/Linux/macOS x64/ARM64 validation SHALL pass;
- package-only consumers SHALL pass from the generated artifact;
- `top`, `slabtop`, and `watch` acceptance builds SHALL remain valid downstream indicators;
- Unicode/cell behavior and geometry semantics SHALL remain documented/tested;
- terminal restoration SHALL remain demonstrated for normal exit, exceptions, cancellation, resize, suspend/resume, and output failure;
- README, migration guide, conceptual docs, XML docs, package metadata, and release notes SHALL agree;
- NuGet.org/GitHub Packages publication SHALL use the validated tag artifact.

If 1.0 closure discovers a defect that can be fixed within the frozen contract, it should be fixed compatibly. A breaking change requires a deliberate compatibility decision rather than being treated as ordinary release cleanup.

---

## 7. Explicit 1.0 non-goals

The stable core does not require native `ncurses` ABI compatibility, exhaustive C curses source compatibility, `printw`/`scanw` compatibility, forms/menus/panels, a widget toolkit, declarative UI, terminal emulation, PTY management, SSH transport, graphics protocols, arbitrary child ANSI interpretation, bidirectional/complex-script shaping, wrappers for every Terminal feature, transparent multi-writer thread safety, or a public diagnostics/performance API without a stable-core requirement.

---

## 8. Cross-cutting release rules

1. `<Version>` and `<PackageVersion>` remain synchronized.
2. Debug is local development; PRs use Staging; `main`/tags use Release.
3. `net8.0`, `net9.0`, and `net10.0` remain first-class targets.
4. Public/protected/internal methods validate applicable arguments at entry.
5. All `if`/`else` bodies use braces.
6. Terminal capability behavior remains TermInfo-driven where a capability models the operation.
7. Tests remain non-interactive unless explicitly manual and do not emit unsolicited stdout/stderr.
8. Public API changes are deliberate compatibility decisions.
9. The Terminal/TermInfo dependency allow-list prevents accidental lower-layer leakage.
10. Every release validates the generated package and fresh consumer.
11. Historical milestone documents remain historical rather than being rewritten to current dependency versions.

---

## 9. Immediate sequence

```text
0.2.0 semantic input parity                    complete
  -> 0.3.0 Unicode / terminal-cell contract   complete
  -> 0.4.0 editing/composition                complete
  -> 0.5.0 pads/large surfaces                complete
  -> 0.6.0 presentation                       complete and published
  -> 0.7.0 refresh/output optimization        complete and published
  -> 0.8.0 production hardening               complete and published
  -> 0.9.0 contract freeze / RC               stable-source merge gate
  -> 1.0.0 stable release closure             next
```
