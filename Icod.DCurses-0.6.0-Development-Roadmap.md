# Icod.DCurses 0.6.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Development line:** `0.6.0`  
**Stable published baseline:** `0.5.0`  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Release theme:** Rendition, drawing, and presentation  
**Status:** Active

---

## 1. Release Objective

`Icod.DCurses 0.6.0` SHALL complete the managed presentation vocabulary required by general TUIs while preserving the responsibility boundary established by the stable `0.2` through `0.5` releases.

The release SHALL make style and drawing requests semantic at the DCurses layer and capability-aware at the terminal boundary. Applications should be able to ask for colors, attributes, line glyphs, and cursor presentation without embedding escape sequences or manually interpreting raw terminfo capability names.

The release SHALL prefer controlled degradation over refresh-time failure when a terminal cannot represent a requested presentation exactly.

---

## 2. Architectural Decisions

### 2.1 TermInfo remains the capability authority

DCurses SHALL consume the semantic inspection and expansion APIs already provided by `Icod.TermInfo 1.10.0` rather than rebuilding capability interpretation.

In particular, color resolution SHALL use `TerminalColors.GetColorSupport(...)` and the corresponding safe expansion APIs instead of private `setrgbf` / `setrgbb` probing or a second color-capability database.

Raw terminfo types SHALL remain an implementation detail wherever an ordinary curses-shaped API is sufficient.

### 2.2 Semantic requests remain terminal-independent

`CursesColor`, `CursesStyle`, and the 0.6 semantic line-drawing vocabulary SHALL continue to represent application intent rather than terminal byte sequences.

No public presentation type may contain terminal escape sequences.

### 2.3 Degradation is explicit and deterministic

Unsupported presentation requests SHALL not fail merely because one attribute or color mode is unavailable.

The default degradation policy SHALL be deterministic and documented. The initial policy direction is:

- unsupported optional text attributes are omitted;
- `Standout` may degrade to `Reverse` when native standout is unavailable and reverse is available;
- an in-range indexed color is emitted exactly;
- an out-of-range indexed color degrades rather than being passed to an unsafe terminal selector;
- an RGB request uses direct RGB when the terminal advertises direct-color semantics through `Icod.TermInfo`;
- RGB on a non-direct-color terminal may degrade to an indexed approximation only under a documented palette policy; otherwise it degrades to terminal default;
- terminal-default foreground/background requests remain true default-color requests rather than being replaced with guessed palette entries;
- `ncv` (`NoColorVideo`) restrictions are honored when attributes and colors are combined.

Exact public policy knobs are subject to the T603/T604 regret review. The renderer SHALL NOT silently wrap indexed values or emit an index outside the terminal-advertised range.

### 2.4 Reset-first correctness is acceptable in 0.6

`0.6.0` is a presentation-correctness release, not the refresh-minimization release.

The renderer MAY retain its current reset-then-apply strategy for style changes where that gives the strongest correctness. `0.7.0` remains responsible for minimizing unnecessary resets and choosing globally cheaper output sequences.

### 2.5 Semantic line drawing is logical content

The 0.4 geometric drawing methods remain valid and caller-supplied-cell based.

`0.6.0` SHALL add semantic line-glyph intent for:

- horizontal;
- vertical;
- upper-left / upper-right / lower-left / lower-right corners;
- tee-up / tee-down / tee-left / tee-right;
- crossing.

The terminal-facing resolver SHALL prefer an appropriate one-column representation according to the accepted policy. Candidate representations are Unicode box-drawing glyphs, terminfo alternate-character-set (`acsc` + `smacs`/`rmacs`) glyphs, and ASCII fallback.

The implementation SHALL preserve the 0.3 one-column/two-column cell contract and SHALL NOT smuggle escape sequences into cell content.

### 2.6 Presentation capabilities are read-only observations

If application code otherwise has to inspect `CursesSession.Terminal` directly for ordinary TUI decisions, `0.6.0` SHOULD expose a small immutable/read-only curses presentation-capabilities view.

That view SHALL report semantic facts such as supported attributes, color depth/model, safe indexed range, semantic line-drawing availability, and cursor presentation availability without exposing raw capability strings.

---

## 3. Development Sequence

```text
T601  0.6 package/version and presentation-contract foundation
  -> T602  complete text-attribute vocabulary and capability view
  -> T603  indexed-color and default-color resolution
  -> T604  direct-RGB degradation and ncv-aware style resolution
  -> T605  semantic line-glyph cell model and resolver
  -> T606  semantic drawing overloads and ACS/Unicode/ASCII presentation
  -> T607  cursor presentation and integrated showcase/acceptance
  -> T608  public API/documentation/package regret gate
  -> T609  stable 0.6.0 closure
```

Meaningful contract checkpoints SHOULD advance the prerelease version.

---

# 4. T601 — Package and Presentation-Contract Foundation

T601 SHALL:

- set `<Version>` and `<PackageVersion>` to `0.6.0-alpha.1`;
- set `<AssemblyVersion>` to `0.6.0.0`;
- retain `Icod.Terminal 1.0.0` and `Icod.TermInfo 1.10.0`;
- publish this roadmap;
- update the top-level 1.0 roadmap to mark `0.5.0` complete and `0.6.0` active;
- establish tests proving the published `0.5.0` public contract remains intact;
- document that Release warnings-as-errors remain part of the release gate.

**Gate T601:** repository Staging build/test/package validation is green with the 0.6 foundation and no dependency-boundary change.

---

# 5. T602 — Complete Text-Attribute Vocabulary and Capability View

T602 SHALL complete the semantic attribute vocabulary required by the 1.0 presentation contract.

Candidate additions to `CursesTextAttributes`:

```text
Italic
Blink
Conceal
Strikeout
```

Existing numeric values 0 through 16 SHALL remain unchanged; new flags SHALL append as powers of two.

Required behavior:

- bold;
- dim;
- underline;
- reverse;
- standout;
- italic when advertised;
- blink when advertised;
- conceal/invisible when advertised;
- strikeout through recognized terminfo extended metadata when advertised;
- reset/exit behavior that cannot strand an attribute across style boundaries;
- unsupported optional attributes degrade without throwing during ordinary refresh.

T602 SHOULD also introduce an immutable/read-only `CursesPresentationCapabilities` view exposed by `CursesSession` if that avoids direct TermInfo inspection by normal applications.

Candidate observations include:

- maximum safely addressable indexed colors;
- direct-RGB support;
- per-attribute support;
- alternate-character-set support;
- cursor hidden/normal/very-visible support;
- color-restricted attribute mask translated into `CursesTextAttributes` where possible.

**Gate T602:** all semantic attributes can be requested and inspected without embedding terminal sequences, and unsupported attributes degrade predictably.

---

# 6. T603 — Indexed Color and Default-Color Resolution

T603 SHALL replace ad-hoc indexed-color output decisions with `Icod.TermInfo` semantic color support.

Required behavior:

- obtain terminal color support through `TerminalColors.GetColorSupport(...)`;
- use the safe indexed range reported by TermInfo;
- emit foreground/background selectors only when safely supported;
- never pass an out-of-range index to a terminal selector;
- define deterministic degradation for out-of-range indexed requests;
- preserve terminal-default foreground/background semantics;
- restore defaults through the strongest available reset/original-color capabilities;
- test monochrome, 8-color, 16-color, 256-color, and direct-color profiles;
- preserve style state correctly across default <-> colored transitions.

The historical color-pair facade remains optional and SHALL NOT become the primary color API.

**Gate T603:** indexed/default color requests are safe for every advertised terminal color range and cannot fail merely because an application requested an unavailable index.

---

# 7. T604 — Direct RGB, Degradation, and `ncv`-Aware Style Resolution

T604 SHALL complete direct-color resolution and style combination policy.

Required behavior:

- use TermInfo direct-RGB model/layout and safe expansion rather than private extended-capability names;
- define the accepted RGB-to-indexed fallback policy for non-direct-color terminals;
- define fallback to terminal default when no color representation is safe;
- honor `NoColorVideo` restrictions when non-default colors are active;
- define bold/dim interaction deterministically;
- define standout fallback semantics;
- ensure unsupported attribute/color combinations degrade before output rather than causing a partial style write followed by an exception;
- preserve logical `CursesStyle` intent in cells even when physical output is degraded;
- cover mixed foreground/background and attribute combinations exhaustively.

T604 MAY introduce a small public degradation-policy enum/options type only if concrete tests demonstrate that one built-in policy is insufficient for reasonable consumers.

**Gate T604:** a style request can be resolved to a safe physical rendition for monochrome, indexed, and direct-RGB terminals without unsafe capability expansion.

---

# 8. T605 — Semantic Line-Glyph Cell Model and Resolver

T605 SHALL introduce terminal-independent semantic line glyphs without weakening `CursesCell` invariants.

Candidate public vocabulary:

```text
CursesLineGlyph.Horizontal
CursesLineGlyph.Vertical
CursesLineGlyph.TopLeft
CursesLineGlyph.TopRight
CursesLineGlyph.BottomLeft
CursesLineGlyph.BottomRight
CursesLineGlyph.TeeUp
CursesLineGlyph.TeeDown
CursesLineGlyph.TeeLeft
CursesLineGlyph.TeeRight
CursesLineGlyph.Cross
```

Required behavior:

- semantic line cells occupy exactly one terminal column;
- cell equality/hash semantics include semantic glyph intent;
- ordinary text content and wide-cell behavior remain unchanged;
- line glyph intent survives copy/overlay/pad presentation unchanged;
- refresh selects a terminal representation without embedding escape sequences in logical content;
- a canonical Unicode representation remains available for diagnostics/inspection where useful;
- unsupported rich line drawing degrades to a safe ASCII representation.

**Gate T605:** semantic line glyphs can live in screens/windows/pads and retain all established editing/copy/damage invariants.

---

# 9. T606 — Semantic Drawing and ACS/Unicode/ASCII Presentation

T606 SHALL integrate semantic line glyphs with window drawing.

Candidate managed overloads:

```text
DrawLineGlyph(...)
DrawHorizontalLine(..., CursesStyle style = default)
DrawVerticalLine(..., CursesStyle style = default)
DrawBorder(CursesStyle style = default)
```

Existing caller-supplied-cell overloads SHALL remain valid.

Required terminal resolution order SHALL be deliberately frozen after acceptance testing. Candidate policy:

1. preferred Unicode box drawing when the logical width policy accepts the selected glyph as one column;
2. terminfo alternate-character-set mapping when complete `acsc` + enter/exit ACS semantics are available and chosen by policy;
3. ASCII fallback (`-`, `|`, `+`) otherwise.

ACS runs SHALL be entered/exited by the refresh layer, not stored as escape sequences in cells.

Required tests include:

- all semantic glyphs;
- missing/incomplete `acsc` maps;
- nested windows;
- pads/viewports;
- style changes inside/outside ACS runs;
- copy/overlay/edit through semantic glyphs;
- terminal profiles with and without ACS;
- no half-wide or continuation corruption.

**Gate T606:** applications can draw semantic boxes/tees/crossings without choosing terminal glyph bytes themselves.

---

# 10. T607 — Cursor Presentation and Integrated Acceptance

T607 SHALL review cursor presentation for any remaining meaningful curses-shaped gap beyond the existing hidden/normal/very-visible lease.

No new cursor feature SHALL be added merely for parity with one terminal family. Any addition must be supported by stable Terminal/TermInfo semantics and have a clear managed TUI use case.

T607 SHALL also extend the showcase and acceptance coverage with:

- complete style vocabulary;
- color degradation diagnostics;
- presentation-capabilities display;
- semantic borders/tees/crossings;
- Unicode/ACS/ASCII fallback demonstrations;
- pad/window composition using semantic drawing;
- lifecycle reset and session disposal after non-default rendition.

**Gate T607:** the complete 0.6 presentation vocabulary is demonstrable through public APIs and survives session lifecycle/restore paths.

---

# 11. T608 — Public API, Documentation, and Package Regret Gate

Before stable `0.6.0`:

- review every new public type/member/enum value;
- freeze appended `CursesTextAttributes` numeric values;
- freeze color degradation semantics;
- freeze `ncv` handling;
- freeze semantic line-glyph names and fallback order;
- freeze presentation-capabilities observations;
- ensure no raw escape sequence or raw capability string enters a public semantic type;
- machine-guard the accepted public surface;
- verify nullable/XML documentation;
- update README and showcase documentation;
- extend the fresh package-only consumer;
- verify no new direct dependency or Terminal/TermInfo public leakage unless deliberately accepted;
- run Staging and a dedicated Release warnings-as-errors check before RC promotion.

The dedicated Release compile check is required because the `0.5.0` publication gate demonstrated that analyzer warnings can be invisible to Staging while failing Release.

**Gate T608:** the presentation contract is intentional, documented, analyzer-clean under Release, machine-guarded, and package-consumable.

---

# 12. T609 — Stable 0.6.0 Closure

T609 is release closure only.

Required work:

- promote a green release candidate to `0.6.0`;
- retain `AssemblyVersion 0.6.0.0`;
- freeze `docs/Public-API-Baseline-0.6.md`;
- run one definitive stable-source PR matrix/package gate;
- run Release warnings-as-errors before merge where practical;
- merge only a green stable source head;
- require the exact merged `main` commit to pass the full x64/ARM64 Release matrix;
- create `v0.6.0` only after that main commit is green;
- verify NuGet.org, GitHub Packages, symbols, checksums, and GitHub Release assets.

---

## 13. Explicit 0.6 Non-Goals

`0.6.0` does not include:

- synchronized-output batching (`0.7.0`);
- insert/delete-character terminal-output optimization (`0.7.0`);
- scroll-region/line optimization (`0.7.0`);
- global escape-stream cost minimization (`0.7.0`);
- sparse/disk-backed pads;
- widgets or higher-level layout controls;
- terminal emulation;
- arbitrary font measurement or shaping;
- a replacement for TermInfo capability parsing;
- a color-pair-only primary API.

The release objective is a complete, semantic, capability-aware managed presentation contract on which the subsequent output-optimization release can operate without redefining application intent.
