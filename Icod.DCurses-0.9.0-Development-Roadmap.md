# Icod.DCurses 0.9.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.9.0`  
**Merged source baseline:** `0.8.0`  
**Published package baseline:** `0.7.0` until the 0.8 tag/publication gate completes  
**Dependency baseline:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Release theme:** Contract freeze / release candidate  
**Initial package checkpoint:** `0.9.0-alpha.1`  
**Assembly version:** `0.9.0.0`  
**Status:** T901 active

---

## 1. Release Objective

`Icod.DCurses 0.9.0` SHALL freeze the managed contract intended to become `1.0.0`.

No major feature family SHOULD enter after this release begins. The work is primarily a regret, compatibility, documentation, and acceptance pass over the complete `0.8.0` behavior.

The principal deliverable is not another feature set. It is a deliberately accepted, machine-guarded public contract whose type/member names, enum values, ownership rules, geometry semantics, Unicode/cell semantics, exceptions, cancellation behavior, nullable annotations, dependency boundary, package metadata, and documentation can be carried into `1.0.0` without surprise.

---

## 2. Frozen Architectural Direction

The existing dependency direction remains authoritative:

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

`0.9.0` SHALL NOT regain byte-stream decoding, terminal-mode ownership, terminal capability storage, PTY ownership, terminal emulation, or application-specific policy.

Any public `Icod.Terminal` or `Icod.TermInfo` type which remains visible after T905 must be explicitly justified as part of the intended 1.x contract.

---

## 3. Development Sequence

```text
T901  version + contract-freeze foundation and public-surface inventory
  -> T902  machine-readable public API fingerprint and compatibility guard
  -> T903  enum/value/geometry/cell semantic freeze
  -> T904  session/lifetime/ownership/exception/cancellation freeze
  -> T905  nullable/XML-doc/dependency-boundary regret audit
  -> T906  conceptual docs, migration guidance, and package-consumer freeze
  -> T907  representative application-shaped acceptance workloads
  -> T908  complete API/package/architecture regret and RC gate
  -> T909  stable 0.9.0 closure
```

---

# 4. T901 — Contract-Freeze Foundation and Inventory

T901 SHALL:

- set `<Version>` and `<PackageVersion>` to `0.9.0-alpha.1`;
- set `<AssemblyVersion>` to `0.9.0.0`;
- retain `Icod.Terminal 1.4.0` and `Icod.TermInfo 1.10.0`;
- publish this roadmap;
- inventory all public types, members, enums, delegates, constructors, properties, events, and methods;
- identify public surfaces inherited from earlier experimental releases which require a final keep/remove/rename decision;
- identify public signatures which expose Terminal/TermInfo types;
- prohibit new public API during T902-T909 unless a concrete 1.0 correctness or usability defect cannot be fixed compatibly.

**Gate T901:** package/version/roadmap foundation builds and packages cleanly and a complete public-surface inventory is available for T902.

---

# 5. T902 — Machine-Readable Public API Fingerprint

T902 SHALL establish a canonical public API fingerprint generated from the compiled `Icod.DCurses` assembly.

The fingerprint SHALL be deterministic across `net8.0`, `net9.0`, and `net10.0` and SHALL include, as applicable:

- public types and nested public types;
- type kind, base type, implemented public interfaces, generic arity, and public generic constraints;
- public constructors;
- public methods, including static/instance distinction, return type, parameter order/type/ref-kind/default values, generic arity, and accessor visibility where relevant;
- public properties and indexers;
- public events;
- public fields and constants;
- enum underlying type and name/value pairs;
- nullable metadata where reliably expressible through the compiled contract.

The repository SHALL retain a machine-readable baseline and a human-reviewable canonical representation or hash. CI SHALL fail when the public surface changes without an explicit baseline update.

The fingerprint is a compatibility guard, not a promise that reflection ordering is meaningful; all input SHALL be canonically sorted before comparison.

**Gate T902:** the same accepted fingerprint is produced for every target framework and accidental public changes fail deterministically.

---

# 6. T903 — Enum, Value, Geometry, Cell, and Text Semantic Freeze

T903 SHALL freeze semantic values which source compatibility alone cannot protect.

Required review and tests include:

- every public enum numeric value;
- public flags combinations and reserved/undefined behavior where applicable;
- row/column ordering and zero-based coordinate semantics;
- rows/columns versus height/width meaning;
- rectangle inclusivity/exclusivity and clipping rules;
- window/subwindow/pad/viewport coordinate spaces;
- negative/out-of-range argument behavior;
- wide-cell continuation invariants;
- Unicode normalization/malformed UTF-16 handling;
- East Asian Ambiguous-width default and opt-in behavior;
- semantic line-glyph versus ordinary text behavior;
- color/default-color and text-attribute value semantics.

Where semantics are intentionally not encoded in the API fingerprint, dedicated freeze tests SHALL pin them.

**Gate T903:** semantic-value tests cover all public enums and the principal geometry/cell/text contracts intended for 1.0.

---

# 7. T904 — Session, Lifetime, Ownership, Exception, and Cancellation Freeze

T904 SHALL deliberately accept the lifetime model hardened in `0.8.0` and freeze its externally observable behavior.

The review SHALL cover:

- session open/dispose ownership;
- presentation and rich-input lease lifetime;
- standard-screen lifetime;
- window/subwindow shared-storage lifetime;
- pad and viewport lifetime/ownership;
- suspend/resume interaction;
- one supported Terminal-owned event consumer concurrent with refresh/output activity;
- post-disposal behavior;
- caller cancellation versus disposal-induced cancellation;
- timeout/deadline behavior;
- end-of-input/disconnect behavior;
- exception types for invalid arguments, invalid geometry, unsupported/unavailable capabilities, disposed objects, cancellation, and output/restoration failures;
- aggregate-exception behavior when independent primary/restoration failures both matter.

No compatibility-sensitive exception/cancellation behavior may remain accidental after T904.

**Gate T904:** focused tests and conceptual documentation pin the accepted 1.x lifetime and failure contract.

---

# 8. T905 — Nullable, XML Documentation, and Dependency-Boundary Regret Audit

T905 SHALL perform a complete public-contract quality audit.

Required checks:

- every public type/member has deliberate nullable annotations;
- public XML documentation is present and useful for stable consumers;
- parameter/return/exception semantics are documented where non-obvious;
- no accidental public helper, implementation type, mutable collection, synchronization primitive, diagnostic object, or test seam remains;
- public constructors are all intentional;
- public setters/mutability are all intentional;
- Terminal/TermInfo public-signature exposure is reviewed member by member;
- existing dependency-boundary allow-lists are tightened to the final accepted set;
- no public API is retained merely because removing it would be inconvenient during development if it is clearly unsuitable for 1.x.

If a breaking cleanup is justified, it SHALL occur in T905 or earlier, followed by a deliberate fingerprint update and migration note. No breaking cleanup should be deferred to `1.0.0` closure.

**Gate T905:** warnings-as-errors, nullable analysis, XML documentation generation, dependency-boundary tests, and the accepted API fingerprint all pass.

---

# 9. T906 — Documentation, Migration, and Package-Consumer Freeze

T906 SHALL make the accepted contract understandable without reading implementation source.

Documentation SHALL cover:

- architectural ownership among DCurses, Terminal, and TermInfo;
- session creation/disposal and restoration;
- supported concurrency model and single-writer logical surfaces;
- input/lifecycle ownership;
- screen/window/subwindow geometry and lifetime;
- Unicode/cell-width rules;
- pads/viewports and panning;
- presentation/rendition and degradation;
- refresh/damage semantics and synchronized-output option;
- exceptions/cancellation/disposal;
- migration from the pre-1.0 releases, especially any T905 breaking cleanup;
- explicit non-goals and native-`ncurses` compatibility boundaries.

A package-only fresh consumer SHALL exercise representative stable API families from the generated `0.9` package, not repository project references.

**Gate T906:** README, samples, conceptual docs, migration guide, generated package, and fresh consumer all agree with the accepted fingerprint and dependency versions.

---

# 10. T907 — Representative Application-Shaped Acceptance

T907 SHALL run bounded deterministic workloads representative of real downstream TUIs.

Coverage SHALL include:

- editor-like cursor movement, insertion/deletion, styled text, Unicode, and scrolling;
- pager-like large text presentation, line deletion/scroll, resize, and sparse updates;
- Unicode-heavy grapheme/wide-cell boundaries;
- rich-input key/focus/paste/mouse semantics where the in-memory Terminal transport can model them deterministically;
- lifecycle resize/suspend/resume recovery;
- high-frequency sparse and no-op refresh;
- pad/viewports under repeated panning;
- cancellation and disposal during representative waits;
- output failure followed by safe retry.

The existing `top`, `slabtop`, and `watch` acceptance samples remain important compatibility indicators. T907 SHALL avoid importing ProcPs policy into DCurses.

**Gate T907:** representative workloads pass for `net8.0`, `net9.0`, and `net10.0` without API changes or retained-state corruption.

---

# 11. T908 — Complete Regret, Package, Architecture, and RC Gate

T908 is the final pre-stable regret pass.

It SHALL verify:

- the accepted API fingerprint and enum/semantic freeze tests;
- zero unreviewed public API changes after T905;
- Windows x64 and ARM64;
- Linux x64 and ARM64;
- macOS x64 and ARM64;
- `net8.0`, `net9.0`, and `net10.0`;
- Staging warnings-as-errors;
- package identity, license, README, symbols, repository metadata, and exact dependency groups;
- package-only fresh consumer;
- conceptual documentation and migration guide;
- architecture/dependency boundaries;
- representative T907 workloads;
- public API regret decision suitable for direct promotion to 1.0 after the subsequent stable closure.

The accepted release-candidate version SHOULD be `0.9.0-rc.1` unless defects require another RC.

**Gate T908:** one exact RC head passes the full six-architecture runtime matrix plus package validation.

---

# 12. T909 — Stable 0.9.0 Closure

T909 is release closure only.

It SHALL:

- promote the accepted RC to `Version 0.9.0` / `PackageVersion 0.9.0` while retaining `AssemblyVersion 0.9.0.0`;
- freeze the final pre-1.0 public API fingerprint and semantic contracts;
- synchronize README, package release notes, API baseline, migration/conceptual docs, 0.9 roadmap, and 1.0 roadmap;
- require the exact stable-source PR head to pass the complete T908 gate;
- leave merge, post-merge Release validation, tagging, and publication as explicit later actions.

No new feature family or public API may enter T909.

---

## 13. Explicit 0.9 Non-Goals

`0.9.0` does not add:

- native `ncurses` ABI compatibility;
- exhaustive source-level C curses compatibility;
- forms, menus, panels, or widget/toolkit frameworks;
- declarative UI;
- terminal emulation or PTY ownership;
- SSH transport;
- graphics protocols;
- arbitrary child ANSI interpretation;
- bidirectional or complex-script shaping;
- transparent multi-writer thread safety;
- a public scheduler/event-loop abstraction;
- a public performance/diagnostics API without a demonstrated 1.0 requirement.

---

## 14. Release Discipline

Throughout 0.9:

1. No public API change is considered incidental.
2. Any accepted public API change requires fingerprint and migration-review updates in the same tranche.
3. Historical milestone documents remain historical.
4. Tests remain non-interactive and do not write unsolicited stdout/stderr.
5. All `if`/`else` bodies use braces.
6. Public/protected/internal methods validate applicable arguments at entry.
7. Pull requests validate under Staging; `main`/tags validate under Release.
8. Package-only validation is mandatory before RC or stable promotion.
9. `1.0.0` remains release closure, not a place to introduce deferred breaking cleanup.
