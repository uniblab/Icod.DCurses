# Icod.DCurses 1.1.0–1.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Scope:** post-1.0 additive core development  
**Published compatibility floor:** `1.3.0`  
**Current published package:** `1.3.0`  
**Assembly version policy:** retain `1.0.0.0` through compatible additive 1.x releases  
**Current declared runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Planning status:** 1.3 is complete/published; 1.4 is the active approved development release

---

## Release sequence

```text
1.1.0  semantic cell metadata + hyperlinks                  complete/published history
1.2.0  panels/layers + z-order composition                  complete/published history
1.3.0  layout + resize primitives                           complete/published
1.4.0  interaction routing/focus/gestures/hit testing       active development
```

The sequence is cumulative: 1.1 adds meaning to retained content; 1.2 composes overlapping retained surfaces; 1.3 makes geometry manageable; 1.4 routes semantic input to logical application regions.

## Ownership boundary

DCurses consumes Terminal's live-session/input/lifecycle/semantic-output contracts rather than terminal-family protocol details and does not install private protocol writers or a second input owner.

The 1.3 layout work preserved that ownership principle internally: geometry values are immutable, `CursesLayout` is pure/stateless, and applications explicitly recompute/apply layout rather than delegating control to a background layout owner.

The 1.4 interaction work extends the same principle. DCurses may classify and route already-normalized input, but `Icod.Terminal` remains authoritative for terminal input decoding, rich-input protocol ownership, pointer-shape transport/lifetime, session lifecycle, and output serialization.

## Published 1.3 floor

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

Tagged baseline:

```text
v1.3.0 -> c10ca043a666b85225f2d3b8955a1ac2075b0d31
```

Version 1.3 contributes the reusable `CursesRectangle` coordinate substrate, screen/window/panel bounds, explicit rectangle application, retained panel resizing, and lifecycle-driven application relayout model which 1.4 will consume rather than replace.

## Release 1.4 — deterministic interaction routing

Version 1.4 turns the existing semantic input and geometry foundations into an application-facing interaction substrate without introducing widgets or a hidden event loop.

The release is planned to provide:

- bounded screen-bound interaction-region registration;
- screen-relative and panel-associated hit targets;
- deterministic overlap resolution using panel z-order plus explicit region precedence;
- region-local coordinate translation;
- logical focus independent of terminal/window-manager focus;
- explicit focus/clear plus forward/backward traversal;
- deterministic focus repair when regions become ineligible;
- immutable semantic keyboard gestures over the existing `CursesKey`, modifiers, characters, and event phases;
- region-local and router-global command bindings;
- structured routing results rather than callback invocation;
- pointer-shape preferences and a DCurses-shaped wrapper over Terminal-owned pointer leases;
- resize/panel visibility/reorder/disposal coherence;
- an application sample proving keyboard, mouse, panel, pointer, and live-resize composition;
- allocation/performance/adversarial qualification before API freeze.

The detailed authority is:

- `Icod.DCurses-1.4.0-Development-Roadmap.md`
- `docs/superpowers/specs/2026-09-12-icod-dcurses-1.4-interaction-routing-design.md`

## 1.4 key policy decisions

The approved direction deliberately keeps rendering and interaction separate.

Interaction regions are not windows, panels, widgets, event handlers, or callbacks. They identify logical application targets and routing metadata. Ordinary regions are screen-relative. Panel-associated regions derive effective screen position and eligibility from their owning panel while preserving panel z-order as the first overlap discriminator.

Logical focus is application focus and is therefore distinct from `CursesFocusEvent`, which represents actual terminal focus reports. Terminal focus loss does not erase which application region is logically focused.

Mouse routing is hit testing, not automatic application policy. A click may identify a focusable target, but the router does not automatically focus it. Visual panel transparency does not automatically create mouse click-through semantics.

Keyboard command routing is semantic rather than escape-sequence based. Focused-region bindings take precedence over router-global bindings, and duplicate gestures within one binding scope are rejected instead of silently depending on registration order.

Pointer-shape preferences are routing data. Synchronous hit testing never performs asynchronous terminal output. The application/session layer explicitly applies the desired shape through DCurses-owned semantics which delegate transport and lifetime to `Icod.Terminal`.

## 1.4 tranche sequence

```text
T1401  contract/design/public-surface candidate freeze
T1402  interaction-region registry
T1403  hit testing / clipping / panel precedence
T1404  logical focus / traversal / repair
T1405  keyboard gestures
T1406  command bindings / routing result
T1407  pointer shape integration
T1408  resize / panel / lifecycle coherence
T1409  application acceptance sample
T1410  performance / allocation / hardening
T1411  API / package / docs / licensing regret gate
T1412  RC / stable-source closure
```

## Cross-release rules

Compatible 1.x releases retain additive API by default, exact compiler-derived fingerprints, Terminal/TermInfo ownership boundaries, Unicode/wide-cell/metadata semantics, conservative lifecycle recovery, package-only consumer validation, Windows/Linux/macOS x64/ARM64 validation, and documentation/sample/package audits before stable promotion.

Historical release documents remain historical authorities and are not rewritten simply to reflect newer package/dependency state.

## Deliberate 1.4 exclusions

Version 1.4 does not add a widget/control library, retained widget hierarchy, event capture/bubbling tree, automatic focus-on-click policy, generalized drag/drop framework, flex/grid/constraint layout, automatic layout ownership, accessibility tree, raster scene graph, animation framework, PTY/process hosting, or raw terminal protocol escape hatches.

## Immediate next step

T1401 freezes the interaction contract before production implementation begins. Once that contract is accepted, T1402 becomes the first source tranche and may advance the development package identity from the published `1.3.0` floor to the 1.4 prerelease line.
