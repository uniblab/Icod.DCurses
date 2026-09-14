# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Published compatibility floor:** `1.3.0`  
**Current published package:** `1.3.0`  
**Assembly version:** `1.0.0.0`  
**Current declared runtime dependencies:** `Icod.Terminal 1.13.0`; `Icod.TermInfo 1.12.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.4.0` — deterministic interaction routing, focus, gestures, hit testing, and pointer semantics  
**Status:** T1401-T1411 complete; T1412 stable-source exact-head qualification is active; merge remains pending explicit approval

---

## Current authorities

- `Icod.DCurses-1.1.0-to-1.4.0-Development-Roadmap.md`
- `Icod.DCurses-1.4.0-Development-Roadmap.md`
- `docs/superpowers/specs/2026-09-12-icod-dcurses-1.4-interaction-routing-design.md`
- `docs/Public-API-Fingerprint-1.4.json`
- `docs/T1401-Interaction-Contract-and-Public-API-Candidate.md`
- `docs/T1409-Interaction-Acceptance-Sample.md`
- `docs/T1410-Interaction-Performance-Allocation-and-Adversarial-Hardening.md`
- `docs/T1411-Public-API-Package-Documentation-and-Regret-Gate.md`
- `docs/T1412-RC-and-Stable-1.4.0-Closure.md`

T1411 is complete. The final `1.4.0-rc.1` head passed the full seven-job matrix, and T1412 has promoted the unchanged accepted implementation/API to stable-source `1.4.0`. The stable-source exact head must pass the same matrix before explicit merge approval. The 1.0-1.3 tranche and closure documents remain historical compatibility/release authorities and are not rewritten to simulate current development state.

## Release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Historical stable baseline |
| `1.1.0` | Semantic metadata and hyperlinks | Historical stable baseline |
| `1.2.0` | Panels/layers/z-order composition | Historical stable baseline |
| `1.3.0` | Layout and resize primitives | Current published stable release |
| `1.4.0` | Interaction routing/focus/gestures/hit testing/pointer semantics | Stable-source qualification active |

The progression is intentionally cumulative:

```text
1.1  cells carry semantic meaning
1.2  retained surfaces overlap deterministically
1.3  surfaces have explicit immutable geometry and resize policy
1.4  normalized input can target logical application regions deterministically
```

## Published 1.3 API floor

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

The tagged compatibility baseline is `v1.3.0`, which resolves to commit:

```text
c10ca043a666b85225f2d3b8955a1ac2075b0d31
```

Current 1.4 interaction fingerprint:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

Version 1.4 remains additive by default. Any proposed break to the published 1.3 surface requires an explicit regret-gate finding, migration justification, and user approval before implementation.

## 1.4 release objective

`Icod.DCurses 1.4.0` adds deterministic, application-owned interaction routing over the existing semantic input, geometry, panel, and Terminal ownership foundations.

The intended flow is:

```text
Terminal-owned input decoding
        |
        v
CursesInputEvent
        |
        v
DCurses interaction router
   |            |
   |            +--> semantic key gesture -> command identity
   |
   +--> mouse coordinate -> deterministic hit-test target
                            -> region-local coordinates
                            -> pointer-shape preference

logical focus
   -> focused region
   -> forward/backward traversal
   -> deterministic repair when eligibility changes
```

The router is mechanism, not an application event loop. It does not read terminal bytes, create a second input owner, invoke arbitrary application callbacks, render widgets, retain layout rules, or perform hidden asynchronous terminal I/O.

## 1.4 architectural boundaries

The 1.4 track is governed by these rules:

- `Icod.Terminal` remains the single live terminal/input/protocol authority.
- DCurses routes already-normalized `CursesInputEvent` values; it does not parse escape sequences or terminal-family protocols.
- `CursesRectangle` remains the coordinate substrate; no second geometry model is introduced.
- Interaction regions are application interaction objects, not widgets and not rendering surfaces.
- Logical application focus is distinct from terminal/window-manager focus reports represented by `CursesFocusEvent`.
- Screen-relative and panel-associated interaction use deterministic coordinate conversion and overlap precedence.
- Panel z-order remains the authoritative precedence source for panel-associated hit targets.
- Visual blank-cell transparency does not automatically imply input transparency.
- Focus traversal in 1.4 is forward/backward deterministic traversal only; spatial focus navigation is deferred.
- Gesture matching is semantic and protocol-independent.
- Region-local and router-global command bindings produce command identities/results, not callback execution.
- Hit testing and routing remain synchronous and perform no terminal I/O.
- Pointer-shape protocol ownership stays inside `Icod.Terminal`; DCurses exposes its own curses-shaped semantic abstraction and lease.
- Public Terminal/TermInfo dependency exposure remains tightly allow-listed.
- Interaction registries, command bindings, and internal bookkeeping are bounded and deterministic.
- Existing single-writer expectations remain unless a tranche explicitly proves a safe additive concurrency contract.

## 1.4 tranche sequence

```text
T1401  interaction architecture / terminology / contract freeze                 complete
T1402  bounded interaction-region registry                                      complete
T1403  deterministic hit testing and panel precedence                           complete
T1404  logical focus and focus repair                                            complete
T1405  semantic keyboard gesture model                                           complete
T1406  command bindings and structured interaction routing                       complete
T1407  pointer-shape abstraction and Terminal-owned lease integration             complete
T1408  resize / panel / lifecycle coherence                                      complete
T1409  application acceptance sample                                             complete
T1410  hardening / performance / allocation / adversarial acceptance             complete
T1411  public API / package / docs / licensing / dependency regret gate          complete
T1412  RC and stable-source closure                                               active
```

Every implementation tranche must receive exact-head Staging qualification before being called complete. The final release retains package-only consumer validation, compiler-derived public API fingerprinting, Windows/Linux/macOS x64/ARM64 coverage, and `net8.0`/`net9.0`/`net10.0` validation.

## Deliberate 1.4 non-goals

Version 1.4 does not add:

- a widget framework;
- buttons, text boxes, menus, controls, or application navigation;
- a retained widget/event tree;
- event capture/bubbling phases;
- automatic focus-on-click policy;
- generalized pointer/mouse capture unless a later explicit requirement proves it necessary;
- drag/drop framework semantics;
- flexbox/grid/general constraint layout;
- automatic layout ownership;
- accessibility-tree ownership;
- command callback/dependency-injection machinery;
- raster placement/scene-graph ownership;
- animation;
- PTY/process hosting.

A future widget package should be able to build on the 1.4 mechanisms without bypassing Terminal ownership or reimplementing focus/hit-test/gesture routing.

## Immediate next step

T1412 is the active gate. Qualify the stable-source `1.4.0` exact head through the normal package candidate plus Windows/Linux/macOS x64/ARM64 matrix. If that exact head is green, the branch is release-ready source and PR #29 may be presented for explicit merge approval. After merge, qualify the resulting `main` Release build before tagging or publishing.
