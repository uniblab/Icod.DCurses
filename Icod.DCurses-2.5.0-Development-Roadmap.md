# Icod.DCurses 2.5.0 Development Roadmap

**Decision:** Color-surface hardening for Icod.Widgets and dedit; approved in conversation on 2026-10-12 (America/New_York)  
**State:** Planning complete; implementation not started  
**Base:** Published Icod.DCurses 2.4.0, AssemblyVersion 2.0.0.0  
**Dependency:** Direct stable Icod.Terminal 1.29.0; no direct Icod.TermInfo production reference  
**Targets:** net8.0, net9.0, net10.0; C# 13; Windows/Linux/macOS  
**Design authority:** [2.5 color-surface design](docs/superpowers/specs/2026-10-12-icod-dcurses-2.5-color-surface-hardening-design.md)  
**Execution detail:** [2.5 implementation plan](docs/superpowers/plans/2026-10-12-icod-dcurses-2.5-color-surface-hardening.md)

## 1. Goal and release test

Certify and harden Icod.DCurses as the complete retained color surface beneath Icod.Widgets. Every applicable operation must preserve independent foreground and background colors, every style-only change must participate in damage and refresh, and colored blank surfaces must compose predictably. The work begins with the existing `CursesColor`, `CursesStyle`, `CursesCell`, presentation-capability, and Terminal-owned rendition contracts; it does not invent a second styling system.

The release test is a public-only sample and package consumer that use several simultaneous foreground/background pairs across ordinary text, blank padding, a width-two grapheme, and an opaque panel. Automated witnesses must prove the same data through logical editing, copying, pads/viewports, panels, resize, invalidation, mapping, physical differencing, and capability degradation. A clean 2.4 consumer remains source compatible.

## 2. Ownership boundary

| Layer | Owns |
|---|---|
| Application / dedit | Palette choice, preferences, document meaning, event loop, repaint decisions |
| Icod.Widgets | Theme inheritance and widget/state/part style resolution; complete widget-surface painting |
| Icod.DCurses 2.5 | Complete cell-style retention, editing/composition invariants, damage, capability projection, refresh coordination |
| Icod.Terminal 1.29 | Terminal-profile normalization, rendition transition plans, protocol encoding and serialized output |
| Icod.TermInfo | Transitive terminal-description evidence |

The direct production dependency graph remains `DCurses -> Terminal -> TermInfo`. DCurses exposes no widget palette and emits no private color escape sequence. Unsupported color requests retain the existing deterministic degradation to terminal default; DCurses does not quantize RGB or guess a nearest indexed color.

## 3. Frozen semantic choices

1. A `CursesStyle` remains the complete style for one logical cell: foreground, background, and attributes.
2. Damage/equality includes every style component. Foreground-only, background-only, and attribute-only changes are real changes.
3. Vacated and repaired cells use the complete `BackgroundCell` style of the owning window.
4. Width-two leaders and continuation cells carry identical styles; edits cannot leave a stale colored half.
5. Opaque panels preserve styled blank backgrounds. Published `BlankCellsTransparent` behavior remains unchanged: ordinary blank cells without raster state do not cover lower cells, even if styled or carrying metadata.
6. `Reverse` remains an attribute passed through capability resolution; DCurses does not mutate stored colors by swapping them.
7. Screen growth produces default cells until application repaint. Invalidation retains the logical image and discards only physical certainty.
8. No new public API is added without a failing public-consumer witness, compatibility review, and design amendment.

## 4. Testable tranches

| Tranche | Deliverable | Exit evidence |
|---|---|---|
| T2500 | 2.4 baseline inventory, permanent contract witnesses, and `2.5.0-alpha.1` development identity | Exact style/presentation/public API baseline recorded; focused witnesses fail only on demonstrated gaps; version/package advance together; AssemblyVersion and Terminal 1.29 remain fixed |
| T2501 | Logical color retention across writes, fills, clear/edit/scroll, copy, text layout, pads and viewports | Parameterized tests prove exact foreground/background/attribute retention, background synthesis, equal-cell no-op behavior, and no orphan/stale wide cells |
| T2502 | Panel composition, colored blanks, transparency, movement and resize | Opaque styled blanks cover lower content; `BlankCellsTransparent` compatibility is frozen; z-order/visibility/move/resize leave no stale color; API-gap decision recorded |
| T2503 | Damage, physical-state and rendition-transition hardening | Foreground-only/background-only changes refresh; equal frames stay quiet; mapping and Terminal-owned plans preserve supported pairs and safe reset ordering |
| T2504 | Capabilities, fallback, attributes and lifecycle | Default/indexed/RGB, independent color selectors, default restoration, no-color-video restrictions, reverse, invalidation, resize and resume have deterministic tested outcomes |
| T2505 | Public-only color-surface sample and consumer acceptance | Runnable sample shows simultaneous color pairs, opaque padding, wide text, reverse labeling, capabilities and fallback; fresh package consumer compiles/runs with Icod.DCurses as its only direct PackageReference |
| T2506 | Adversarial, performance, API, package and documentation regret gate | Style-only damage remains bounded; no unnecessary equal-frame output; API fingerprint, XML docs, package README, licenses, package dependency graph and samples are frozen |
| T2507 | RC, stable-source qualification and maintainer handoff | Exact candidate passes forced restore, all-target builds/tests, package validation, isolated consumers and PR CI; live-terminal observations and unverified platforms are recorded separately |

Each tranche receives an evidence document at `docs/T25xx-*.md` when executed. A green build alone does not accept a semantic gate. Historical 2.4 qualification records and API artifacts remain immutable release evidence; only their roadmap release-status summary has been corrected after publication.

## 5. Detailed acceptance matrix

### Logical cells and windows

- terminal-default, indexed, and RGB values retain exact semantic values;
- styled blank and continuation cells retain their full style;
- string/rune/text-element/tab/prepared-cell writes apply the requested style;
- fill, erase, clear, scrolling, insertion/deletion, copying, overlay and text-layout projection preserve or synthesize the specified complete style;
- background-only replacement makes the cell dirty even when visible content is unchanged;
- wide-character overwrite/clip/repair applies the owning background style to every vacated coordinate.

### Pads, viewports and panels

- pad storage and repeated viewport projection preserve style without document-sized redraw;
- opaque panel padding remains visibly colored through composition;
- blank-transparent panels preserve their published style-insensitive transparency rule;
- panel z-order, visibility, reposition and resize restore the correct lower or upper color without stale coordinates;
- styled blanks with semantic metadata remain transparent under the existing blank-transparent mode unless raster content makes the coordinate opaque.

### Refresh and terminal capability

- complete style participates in virtual and physical equality/damage;
- foreground-only, background-only, and attribute-only transitions reach Terminal-owned screen plans;
- supported indexed and RGB pairs remain intact through the mapper;
- unsupported/unsafe colors degrade to default without throwing or emitting private fallback bytes;
- default-color restoration and color-restricted attributes follow the reported terminal profile;
- reverse intent is preserved as an attribute, not rewritten into logical color values;
- invalidation/repaint re-emits the desired style, while unchanged refresh avoids redundant work.

### Platform and packaging

- Staging and Release warnings-as-errors builds and tests cover net8.0/net9.0/net10.0;
- CI covers the repository's Windows/Linux/macOS x64/ARM64 matrix;
- package verification confirms one DLL package, durable README, XML documentation, LGPL library and GPL sample/test notices, and direct Icod.Terminal 1.29.0 only;
- a clean package-only consumer declares Icod.DCurses as its only direct PackageReference;
- live-terminal reports identify exact OS, terminal/version, architecture, runtime, commit, requested palette, capability report, and observed output; unavailable live platforms remain unverified rather than inferred.

## 6. Public API and compatibility policy

The expected 2.5 result is stronger tests, corrected internal behavior where a witness fails, a focused sample, and durable documentation. The public surface should remain additive or unchanged. In particular, do not replace `CursesColor`, `CursesStyle`, `CursesCell`, `CursesPresentationCapabilities`, or the existing composition modes.

The most likely API question is whether callers need a third panel transparency policy that treats only default-style blanks as transparent. The default answer is no: widget surfaces with meaningful blank backgrounds use opaque panels. T2502 may reopen that answer only with an exact public-only failure and an approved design amendment before code.

## 7. Sample and documentation plan

Add `samples/Icod.DCurses.Color.Sample` as the focused acceptance witness rather than overloading the general Showcase. It chooses a conservative palette from `CursesPresentationCapabilities`, presents requested and effective capability information, repaints on resize, and remains useful on monochrome terminals. It must not identify terminals or inspect lower-layer capability strings.

The root README gains a concise color-model section and the runnable sample command. `samples/README.md` explains expected output, fallbacks, resize behavior, and manual observations. `CHANGELOG.md` changes only with implemented behavior. Planning/readiness status remains in this roadmap and T25xx evidence, not in the packaged README.

## 8. Version and release sequence

T2500 advances `Version` and `PackageVersion` together from `2.4.0` to `2.5.0-alpha.1`; `AssemblyVersion` stays `2.0.0.0`. Keep stable Icod.Terminal 1.29.0 unless a demonstrated lower-layer defect is fixed and published through its own repository first. T2506 freezes the public API/package/documentation candidate. T2507 promotes to RC and then stable source only after exact evidence. The maintainer owns merge, post-merge validation, tag, and publication.

## 9. Explicit non-goals

- Theme objects, inheritance, widget state styles, or component-part roles.
- Dedit configuration, syntax highlighting, search-result coloring, or document storage.
- Color quantization, contrast selection, alpha blending, gradients, or accessibility policy.
- Raw ANSI/OSC/DCS output, terminal-name heuristics, or direct TermInfo production use.
- Breaking changes to blank transparency.
- Hidden timers, event loops, or repaint scheduling.

## 10. Immediate next action

Review this roadmap and its implementation plan. After approval, execute T2500 natively with test-first development, record the exact 2.4 baseline, and advance only the development version. Do not implement Widgets themes until the DCurses 2.5 color-surface contract is qualified and published.
