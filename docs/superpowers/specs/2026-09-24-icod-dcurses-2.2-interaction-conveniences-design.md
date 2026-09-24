# Icod.DCurses 2.2 Interaction and Application Conveniences Design

**Date:** 2026-09-24\
**Status:** Proposed design for review; public APIs pending T2201\
**Baseline:** published `v2.1.0`\
**Release roadmap:** [2.2.0 development roadmap](../../../Icod.DCurses-2.2.0-Development-Roadmap.md)

## Intent and constraints

Version 2.2 is the agreed Option 3 following 2.1 core presentation/text. Its purpose is to reduce repeated command and input plumbing seen in the public-only editor and roguelike samples, while preserving their ownership of event loops, application state, command execution and drawing. It precedes a possible Widgets or editor package. Physical raster animation and tile/sprite coordination remain later tracks.

Keep the published 2.1 public API working unchanged, `AssemblyVersion 2.0.0.0`, and the sole direct runtime dependency on `Icod.Terminal` (at least `1.18.0`). Existing `CursesInteractionRouter` knows about regions, focus, scope precedence, single-key bindings, capture and clock-free pointer gestures; 2.2 must compose these rather than create a competing registry. Terminal alone performs terminal I/O and protocol work.

## Approaches considered

1. **Extend deterministic interaction mechanisms over the current router — proposed.** Select useful sequence composition and effective-binding discovery after examining both samples. Keep results as data and application loops caller-owned. This reuses the current focus/scope authority, with modest new API surface.
2. **Build a retained Widgets and command framework now.** This would simplify app code quickly but also freeze UI hierarchy, dispatch, callback, styling and ownership policy before the shared interaction mechanisms are proven. Reserve it for a separate later package.
3. **Build a session-owned event and frame loop.** This would make timer and input integration automatic but move application scheduling and command execution inside DCurses and complicate Terminal lifecycle semantics. A caller-fed, pure calculation can be evaluated later if there is evidence.

## Proposed components and data flow

1. The application obtains normalized events through the published Terminal-backed `CursesSession` path and continues to own its event loop.
2. A bounded command-sequence component associated with existing router focus/scope context classifies incoming key/text gestures: ordinary command, prefix pending, complete sequence, mismatch or cancel. It returns a result; the application decides when to present hints or execute a command.
3. A discovery query snapshots currently effective bindings, using the same precedence and eligibility as routing. Applications provide labels and draw hints or help UI themselves.
4. The optional small prompt calculator consumes normalized events and returns next state plus completion/cancel signals. It never edits a document, draws a window or invokes a callback.
5. Optional timing calculators consume caller-supplied elapsed time; no private timers or refresh loop. Terminal owns any live terminal service needed to support one.

The completed T2201 API design must name exact types/methods, document whether sequence state lives on the router or on a caller-owned object, settle precedence of prefix versus a complete binding, choose mismatch replay behavior, and fix capacity limits. Those choices are deliberately unresolved until 2.1 sample evidence and compatibility tests are collected. T2201 may defer the optional components but must record the reason and reduce later tranches accordingly.

## Invariants, failure handling and acceptance

- Existing single-key routing and scope behavior are unchanged when new facilities are unused.
- No pending command can survive an invalid focus/scope context, disposed owner, explicit cancel or end of input; no command executes implicitly.
- A mismatch cannot lose input or route the same event twice; define one explicit replay rule in T2201.
- Every registered sequence, discovery result and prompt state has a validated limit and predictable failure before partial mutation.
- Command discovery and actual route results agree under the same context; modal exclusions and disposal are reflected immediately.
- A prompt, if admitted, respects Unicode text elements, application validation and bounded memory.
- Timed facilities, if admitted, use monotonic caller-fed time with explicit reset and never introduce a background worker.
- Editor and roguelike samples exercise selected features via public API and retain their existing presentation, movement, resize and clean-exit acceptance behavior.
- Exact-head Staging PR checks, package-only consumers, API/fingerprint review and a separate main Release gate follow the established 2.1 workflow.

## Scope to be frozen at T2201

The proposed initial public scope is **bounded multi-key command composition and binding discovery**. Small prompt state and pointer/frame timing require evidence from a concrete user flow; implementation does not start merely because they appear in the roadmap. The T2201 review is the guard against convenience APIs whose only purpose is to avoid a few application lines or whose natural home is a future Widgets package.
