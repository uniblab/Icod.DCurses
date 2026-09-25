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
**Status:** T2202 effective-binding discovery accepted; T2203 implementation plan pending review\
**Planning snapshot:** 2026-09-24

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

### 3.1 Contextual and multi-key commands — primary candidate

Evaluate a bounded, deterministic command-sequence mechanism that consumes normalized `CursesInputEvent` key/text gestures using the existing binding ownership and scope precedence. Prefix, completion, mismatch and cancellation must be explicit. The accepted T2203 design has no timeout: applications choose when to cancel. A mismatch routes its mismatching event through ordinary single-key routing exactly once without treating it as a new sequence prefix. Maximum sequence length, total registered bindings and live pending state have explicit limits in the written specification.

Do not let a prefix hide or change an existing complete binding silently. A scope activation, focus transfer, region/scope disposal or router disposal must invalidate or re-evaluate pending context according to one written rule. The application chooses when to wait for more input and whether to execute the returned `CursesCommand`.

### 3.2 Command discovery — primary candidate

Evaluate bounded immutable snapshots of effective bindings at the current focus and scope. Results must have deterministic precedence and ordering, identify incomplete prefixes, and never expose commands hidden by a modal scope. Display labels/descriptions and enabling remain caller-owned; no command catalog, callbacks or auto-execution. Prove that discovery agrees with normal routing for the same eligible context, including scope changes and disposal.

### 3.3 Prompt input — evidence-gated candidate

The 2.1 editor owns a small numeric prompt. Assess whether a general small prompt state (text-element-safe insertion/deletion, caret movement, completion/cancel signals and a bounded length) removes repeated application code without becoming a document buffer. Its API should be usable without opening a session; applications own validation, history, completion providers, persistence, layout and styling. If its only convincing use case is the existing editor's few lines, close T2204 as deferred.

### 3.4 Timed pointer and frame calculations — evidence-gated candidate

Existing 1.5 pointer gestures intentionally require no clock. Consider opt-in elapsed-time recognition (double-click, dwell, drag threshold) only if editor/roguelike acceptance demonstrates a real case. Caller-fed monotonic timestamps, bounded state and explicit cancel/reset are required; ordinary 1.5 gesture semantics remain unchanged. Drag/drop payload ownership stays with the application. A frame pacing calculation is only admitted if it can be pure and caller-driven; DCurses will not schedule refreshes or take ownership of the event loop. Live Terminal services must be added to Terminal first, if needed, rather than smuggled into DCurses.

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
| **T2203** | Implement bounded multi-key composition | [Written specification](docs/superpowers/specs/2026-09-25-icod-dcurses-t2203-command-sequences-design.md) approved; [test-first implementation plan](docs/superpowers/plans/2026-09-25-icod-dcurses-t2203-command-sequences.md) pending review |
| **T2204** | Implement small prompt state only if T2201 accepts it; otherwise record deferral | Unicode element boundaries, length and overflow, cancellation, validation ownership, editor-shaped use case |
| **T2205** | Implement caller-fed timed pointer or pure frame calculations only if T2201 accepts them; otherwise record deferral | Clock boundary and regression tests; no background work, implicit terminal I/O or application-owned payload captured |
| **T2206** | Integrate selected mechanisms into public-only editor/roguelike samples and package-only consumers | Application-shaped tests, documented controls, live manual acceptance, no duplicated private command router, no direct TermInfo calls |
| **T2207** | Adversarial, allocation, dependency, public API, XML and documentation gate | Existing API/fingerprint parity, published package checks, platform matrix and exact-head evidence; reject APIs that belong in Widgets or Terminal |
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

Review the [T2203 test-first implementation plan](docs/superpowers/plans/2026-09-25-icod-dcurses-t2203-command-sequences.md), then execute it inline after approval. The approved specification freezes prefix conflicts, mismatches, limits and context invalidation before production code. Keep the development PR open and unmerged during the release track.
