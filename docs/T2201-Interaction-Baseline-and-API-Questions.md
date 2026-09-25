# T2201 — 2.1 Interaction Baseline and API Questions

**Date:** 2026-09-24\
**Status:** Discovery baseline and RED witness accepted; sequence API amendment remains T2203\
**Source:** merged `v2.1.0` plus PR #34 planning branch

## Published baseline

The immutable `docs/Public-API-Fingerprint-2.1.json` and `docs/Public-API-Baseline-2.1.md` record 96 exported types, 751 canonical declared contract lines and SHA-256 `c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79`. The package targets .NET 8/9/10, retains `AssemblyVersion 2.0.0.0` and directly references only `Icod.Terminal 1.18.0`. T2202 must compare new APIs against this snapshot, never rewrite it.

`CursesInteractionRouter.Route(CursesInputEvent)` already handles text/key, paste, mouse, focus and end-of-input. For keyboard commands, the winning owner is the focused region, then its scope ancestors through the active scope boundary, then the router-global binding. If there is no focus, only the active scope and globals participate. Bindings have a 16,384 router-wide ceiling; region and scope have 256 each; global has 1,024. Gesture matching uses `CursesKeyGesture.Matches(input)`, which normalizes modifiers and distinguishes text events from key phases. A new binding system cannot safely replace this method with dictionary-key equality against arbitrary input.

The 2.1 editor sample owns a numeric go-to-record prompt: it accepts at most eight ASCII digits, handles Escape/Backspace/Enter and validates the result itself. The roguelike sample owns map movement, help visibility and its shortcut switch. Both own `ReadEventAsync`, lifecycle and resize handling, refresh and clean screen exit. Neither currently uses the interaction router; T2206 must demonstrate that integrating the new mechanisms removes more code than it adds without changing typing, movement or terminal cleanup.

## T2201 public-design decisions to freeze

1. **Single-key preservation:** Existing `Route` never waits for a sequence and remains behaviorally identical. A new opt-in path returns explicit pending/completed/fallback outcomes and never invokes a command.
2. **Binding ownership:** Sequence registrations use the current region/scope/global ownership and active-scope precedence. Avoid an independent command registry that can disagree with focus, disposal or modal scopes.
3. **Prefix ambiguity:** Decide whether a complete single-key binding wins over an overlapping multi-key prefix, or reject such conflicting registrations; write a test for the chosen rule. A call on unmatched continuation must return the current event through ordinary routing exactly once; the consumed prefix has no automatic text insertion.
4. **Invalidation:** A focus move, scope activation/deactivation, binding change, region/scope disposal, panel eligibility change, resize or explicit cancel cannot allow a stale prefix to complete. The application can abandon a prefix without executing it.
5. **Discovery parity:** Effective bindings and their owner precedence must match normal route results. The snapshot is owned/immutable and bounded. Within each owner, order by the documented semantic gesture fields; [Dictionary enumeration order](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-10.0) is unspecified. Prompt labels/localization remain application-owned.
6. **Deferred candidates:** There is one narrow prompt witness in the editor and no timed pointer/frame-loop witness in either sample. T2201 should defer these unless a separate concrete acceptance flow establishes reusable benefit. Do not add a private timer, widget or terminal service merely to complete the option menu.

## Observed RED and exit gate

The [2.2 public API design](2.2-Interaction-API-Design.md) freezes the first additive discovery type and method, snapshots and limits. The [T2201–T2202 implementation plan](superpowers/plans/2026-09-24-icod-dcurses-t2201-t2202-discovery.md) follows the two sample loops. Tests-only executable head `d8532485be3726984603381a294041735171a1da` failed in [PR workflow 36074699970](https://github.com/uniblab/Icod.DCurses/actions/runs/36074699970): Linux x64 job `107883318336` reported `CS0246` for missing `CursesCommandBinding` and `CS1061` for missing `GetEffectiveGestureBindings` on .NET 8/9/10, exactly the intended RED. There were no production/API/identity changes on that head. This accepts the T2201 discovery foundation and permits T2202 implementation; T2203 sequences require their separate API amendment and RED test.
