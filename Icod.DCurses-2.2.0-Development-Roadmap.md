# Icod.DCurses 2.2.0 Development Roadmap

**Project:** `Icod.DCurses`\
**Release:** `2.2.0`\
**Theme:** Interaction and application conveniences (the selected Option 3)\
**Baseline:** merged and tagged `v2.1.0`, PR #33\
**Current source and package version:** `2.2.0-alpha.1`\
**Assembly version:** `2.0.0.0`\
**Direct runtime dependency:** `Icod.Terminal 1.18.0` minimum; no direct `Icod.TermInfo` reference\
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`\
**Configurations:** `Debug`; `Staging`; `Release`\
**Status:** T2207 accepted; T2208 RC and stable-source closure pending\
**Planning snapshot:** 2026-09-25

**Design proposal:** [2.2 interaction and application conveniences](docs/superpowers/specs/2026-09-24-icod-dcurses-2.2-interaction-conveniences-design.md). The published [2.1 roadmap](Icod.DCurses-2.1.0-Development-Roadmap.md), [interaction sample](samples/Icod.DCurses.Interaction.Sample/Program.cs), [editor](samples/Icod.DCurses.Editor.Sample/Program.cs) and [roguelike](samples/Icod.DCurses.Roguelike.Sample/Program.cs) are the baseline evidence.

---

## 1. Goal and release decision

The agreed order is core presentation and text (2.1), **interaction and application conveniences (2.2)**, then a possible higher-level Widgets or editor package. This release makes the already-published focus, scope, command, pointer and input mechanisms easier for application authors to compose. It does not turn DCurses into an application framework.

The editor should be able to present a bounded prompt and discover currently relevant commands without duplicating infrastructure. The roguelike should be able to express context-sensitive input and an overlay without a parallel command router. Both remain executable, public-API-only examples; they own their document/world state, application commands, game rules, rendering loop and event loop.

The [T2201 baseline and API questions](docs/T2201-Interaction-Baseline-and-API-Questions.md) and [2.2 interaction API design](docs/2.2-Interaction-API-Design.md) place effective-binding discovery before command sequences: discovery first proves precedence without changing `Route`. The candidate features below define what to evaluate, not a commitment to ship every candidate. Each optional tranche either passes its stated gate or closes with evidence explaining why it is deferred. The release must ship useful interaction work even if optional timing facilities are excluded.

## 2. Architecture and compatibility requirements

- Preserve the 2.1 source contract and `AssemblyVersion 2.0.0.0`; a breaking change requires a separate explicit decision.
- Keep `Icod.DCurses -> Icod.Terminal -> Icod.TermInfo`. Terminal owns live input decoding, modes, timers or queries requiring terminal I/O, output and protocol decisions. DCurses never parses escape sequences or calls TermInfo directly.
- Build atop `CursesInteractionRouter` and its existing precedence: focused region, active scope ancestry, then router-global bindings. Existing single-key calls and routing results retain their behavior.
- Route commands as immutable identities and data. Application code handles execution, names/descriptions, localization, enabled policy, async tasks and cancellation.
- Keep explicit, bounded input state. A cancelled sequence, scope change, disposed region or completed prompt cannot leave a stale pending command.
- Accept caller-supplied time where timing is justified; no private clock, background pump or hidden sleep. Single-threaded application ownership remains the default.
- Do not require a live terminal for pure command, prompt, timing or discovery calculations. No widget tree, retained editor document or game engine enters core.
- Do not add dependency/version churn while the design is being reviewed. T2202 advances `Version` and `PackageVersion` together to `2.2.0-alpha.1` only after T2201 accepts the public surface.

## 3. Candidate interaction facilities

### 3.1 Contextual and multi-key commands — accepted

Evaluate a bounded, deterministic command-sequence mechanism that consumes normalized `CursesInputEvent` key/text gestures using the existing binding ownership and scope precedence. Prefix, completion, mismatch and cancellation must be explicit. The accepted T2203 design has no timeout: applications choose when to cancel. A mismatch routes its mismatching event through ordinary single-key routing exactly once without treating it as a new sequence prefix. Maximum sequence length, total registered bindings and live pending state have explicit limits in the written specification.

Do not let a prefix hide or change an existing complete binding silently. A scope activation, focus transfer, region/scope disposal or router disposal must invalidate or re-evaluate pending context according to one written rule. The application chooses when to wait for more input and whether to execute the returned `CursesCommand`.

### 3.2 Command discovery — accepted

Evaluate bounded immutable snapshots of effective bindings at the current focus and scope. Results must have deterministic precedence and ordering, identify incomplete prefixes, and never expose commands hidden by a modal scope. Display labels/descriptions and enabling remain caller-owned; no command catalog, callbacks or auto-execution. Prove that discovery agrees with normal routing for the same eligible context, including scope changes and disposal.

### 3.3 Prompt input — deferred

The 2.1 editor owns a small numeric prompt, but no second application-shaped
witness justifies freezing a general prompt state into DCurses core. T2204 is
deferred. Applications retain validation, history, completion, persistence,
layout and styling; new evidence may reopen a separate proposal later.

### 3.4 Timed pointer and frame calculations — deferred

Existing 1.5 pointer gestures intentionally require no clock, and the current
editor/roguelike flows do not justify timed pointer or frame APIs. T2205 is
deferred. Any future proposal still requires caller-fed monotonic time, bounded
state, explicit reset and no background scheduling or hidden terminal I/O.

## 4. Application acceptance

### Editor

Qualify at least one contextual command sequence with a prompt or help overlay, preserving ordinary typing, Unicode caret/selection semantics, editing, resize and clean terminal exit. If prompt state is accepted, exercise Unicode grapheme deletion, empty input, cancellation and bounded-length failure. Show effective shortcuts in application-owned UI using discovery rather than duplicating binding precedence.

### Roguelike

Qualify context-dependent commands between map and help/overlay states, with unchanged movement rules (walls, water and void impassable), viewport wrap/room presentation, deterministic sparse updates, resize and clean exit. A pending sequence must not execute after changing overlays or scopes. Pointer or frame helpers need their own application-shaped witness if accepted.

Both samples continue to call `screen.Clear()` and refresh during orderly exit. Keep headless deterministic state tests alongside a manual run checklist; do not pretend CI can validate human terminal appearance.

## 5. Tranche plan and acceptance gates

| Tranche | Deliverable | Gate |
|---|---|---|
| **T2201** | Freeze baseline, inspect repeated input paths, compare API approaches, decide optional candidates and write public API/implementation plan | Discovery foundation accepted: tests-only head `d853248` observed expected missing-API RED in workflow 36074699970; sequence contract remains a separate T2203 amendment |
| **T2202** | Move `Version`/`PackageVersion` to `2.2.0-alpha.1`; implement effective-binding discovery | Accepted at exact head `1d6097d`; [workflow 36076001958](https://github.com/uniblab/Icod.DCurses/actions/runs/36076001958) green 7/7; see [T2202 gate](docs/T2202-Effective-Binding-Discovery-Gate.md) |
| **T2203** | Implement bounded multi-key composition | Accepted at exact head `2cdb89f`; [workflow 36084559207](https://github.com/uniblab/Icod.DCurses/actions/runs/36084559207) green 7/7; see [T2203 gate](docs/T2203-Bounded-Command-Sequences-Gate.md) |
| **T2204** | Implement small prompt state only if justified | Deferred: only the editor's narrow numeric prompt is evidenced |
| **T2205** | Implement caller-fed timed pointer or pure frame calculations only if justified | Deferred: no current application-shaped timing witness |
| **T2206** | Integrate selected mechanisms into public-only editor/roguelike samples and package-only consumers | Accepted at executable head `ff6e446`; [workflow 36096617375](https://github.com/uniblab/Icod.DCurses/actions/runs/36096617375) green 7/7; [both live samples confirmed](docs/T2206-Application-Interaction-Acceptance-Gate.md) |
| **T2207** | Adversarial, allocation, dependency, public API, XML and documentation gate | Accepted at exact executable head `e5c5917`; [workflow 36098389089](https://github.com/uniblab/Icod.DCurses/actions/runs/36098389089) green 7/7; see [T2207 gate](docs/T2207-Adversarial-Package-Documentation-and-API-Gate.md) |
| **T2208** | RC followed by stable-source qualification | PR runs Staging jobs only; exact source/package provenance and fresh consumers; Release validation only after a separately authorized merge to `main` |

Do not advance a tranche solely because CI is green: its stated semantic and package gates must also hold. Work on this PR may advance tranche by tranche. Record an accepted or deferred result and exact commit for every gate in this roadmap or a linked gate document.

## 6. Testing and resource bounds

- Write focused tests that fail for the new behavior before production code, and preserve all published 2.1 behavioral tests.
- Test context switches: focus, nested scope entry/exit, modal scope, region disposal, panel visibility, screen resize, cancellation, end of input and router disposal.
- Test ambiguous and conflicting sequences, Unicode text events, modifier keys, mismatch/replay, sequence-capacity boundaries, oversized input and invalid timestamps where timing is selected.
- Set explicit ceilings in T2201 before new public types are frozen. Allocations on an unused path must stay at the 2.1 baseline; retained pending input is bounded by registered sequences, never proportional to application content.
- Use C#, PowerShell 5.1-compatible PowerShell, cmd and sh; no Python. Qualify `net8.0`, `net9.0` and `net10.0`, and the established Windows/Linux/macOS x64/ARM64 Staging PR matrix.
- Validate the built `.nupkg` in fresh package-only consumers; assert the direct Terminal dependency, absence of direct TermInfo and public API additive delta.

## 7. Non-goals and future sequence

No callback dispatch tree, retained widget hierarchy, automatic focus policy, command handler/execution framework, editor document/undo/search engine, game entity/AI/pathfinding engine, terminal emulator, PTY host, raster scene graph, image decoding, tile atlas or sprite animation belongs in 2.2. A future `Icod.DCurses.Widgets` package may use the proven interactions; graphics work requires separate physical-placement and lifecycle design.

## 8. Immediate next step

Execute T2208's RC and stable-source exact-head closure without adding features
or changing the frozen T2207 public surface. Keep the development PR open and
unmerged; Release validation remains reserved for a separately authorized push
to `main`.
