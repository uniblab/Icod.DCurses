# T2206 Application Interaction Acceptance Gate

**Date:** 2026-09-25\
**Status:** Accepted — automated qualification and live manual acceptance complete\
**Executable head:** `ff6e44606dedca3ec3bf95fdbe740de80bf4a169`\
**Workflow:** [36096617375](https://github.com/uniblab/Icod.DCurses/actions/runs/36096617375)

## Scope

T2206 integrates the accepted T2202 effective-binding discovery and T2203
bounded command sequences into the editor and roguelike samples. The samples
remain application-owned event loops using public DCurses APIs. DCurses routes
input to immutable command identities; each sample owns command execution,
document or world state, rendering, validation and lifecycle policy.

No T2204 prompt-state API or T2205 timing API was added. The editor retains its
bounded numeric prompt, and the roguelike retains its movement and terrain
rules. Neither sample adds a private competing command router or calls TermInfo
directly.

## Test-first evidence

The tests-only head `dfa4f9d3dc6e93d9d87e1ac153cda334c6dc277d` failed as expected in
[workflow 36088529109](https://github.com/uniblab/Icod.DCurses/actions/runs/36088529109):
the compiler reported the missing `EditorSampleInteraction.cs` and
`RoguelikeSampleInteraction.cs` sources on .NET 8, 9 and 10. This established
the application-controller contract before implementation.

Head `b78c34cac5c8d96229a4d4cd6e3bbcc3ebb73930` added the minimal controllers
and passed all seven Staging jobs in
[workflow 36088789510](https://github.com/uniblab/Icod.DCurses/actions/runs/36088789510).
Head `1af4300011a927cff6d6076368c8d327143170d6` then wired both sample event
loops through those controllers and passed all seven Staging jobs in
[workflow 36089380730](https://github.com/uniblab/Icod.DCurses/actions/runs/36089380730).

The focused headless tests prove:

- editor direct and `Ctrl+K Ctrl+G` prompt commands;
- editor prompt-scope discovery and `Ctrl+K Ctrl+C` cancellation;
- exactly-once Unicode text fallback after a sequence mismatch;
- roguelike movement plus `g h` help activation;
- map-prefix invalidation and movement suppression in the help scope;
- help-prefix invalidation and map discovery restoration on close.

## Sample integration

The editor routes each normalized input once through `ProcessCommandSequence`.
Command results preserve navigation, editing, wrapping, selection, prompt
acceptance/cancellation and clean exit. Ordinary text is inserted only from the
returned fallback event, including mismatch fallback. Its shortcut line is
derived from effective single-key and sequence discovery for the current
document or prompt context.

The roguelike uses map and help interaction contexts. Arrows/WASD, `?`, quit,
`g h` and `g m` are routed commands; opening or closing help invalidates a
pending prefix. Movement still delegates to the existing application state, so
walls, water and void remain impassable and the accepted room/corridor viewport
and sparse-update behavior is unchanged. Its visible shortcut text is likewise
derived from effective discovery.

Both samples retain orderly `screen.Clear()` plus refresh on exit.

## Live editor corrections

The first live editor run found that `Ctrl+G` and `Ctrl+K Ctrl+G` appeared to
do nothing, while `Ctrl+V` was already used by the terminal for clipboard
paste. This was not intended sample behavior. Terminal 1.18 reports traditional
C0 control bytes with an uppercase semantic character (for example, `G`) and
the Control modifier, while the sample had registered only the lowercase
modern-keyboard form. Gesture character identity is intentionally exact.

Tests-only head `1471c8d953a5d7e8ad0e4c4ea94e833211da2701` established the regression in
[workflow 36094762901](https://github.com/uniblab/Icod.DCurses/actions/runs/36094762901):
the six new live-terminal cases failed on .NET 8, 9 and 10 while package
validation remained green. The correction registers both lowercase modern and
uppercase traditional forms for each editor Control shortcut, moves selection
to `Ctrl+T`, and deliberately leaves `Ctrl+V` unclaimed for terminal clipboard
paste. It adds no public API and does not make matching generally
case-insensitive.

The follow-up live run then found that `Ctrl+K Ctrl+C` closed the program
instead of cancelling the prompt. The sequence router was not at fault: the
editor had opened its session in the default CBreak input mode, which retains
host signal processing. The host converted `Ctrl+C` into an Interrupt
lifecycle event, and the sample exited before the event could reach command
routing.

Tests-only head `1d760d9f8ec208b293f41934f349ab0f90f5534b` established the missing
application input policy in
[workflow 36096460955](https://github.com/uniblab/Icod.DCurses/actions/runs/36096460955):
all three target frameworks reported only the absent editor session-options
factory. The sample-local correction opens the editor in Raw input mode so
`Ctrl+C` is delivered to the registered command sequence. The DCurses default,
the roguelike and the public API remain unchanged; `Escape` remains the
editor's orderly-exit command.

## Exact-head automated qualification

The first package-consumer attempt at `2c4619acc614895d660bebed1ce5eb31d7daba43`
correctly exposed that `CursesInputEvent.FromText` is an internal test factory,
not public NuGet API. The correction did not widen the API: package smoke now
behaviorally verifies public sequence registration, discovery, cancellation
and unbinding, and compile-checks the public processing signature. Source-level
tests continue to cover pending, completion and mismatch processing with
synthetic events.

Initial executable head `19d0fba977e9d1272ea82f1cab705a64130fad64`
and first corrected head `df28e111ab01ff2a1c5888826681acf2ca1c1f86`
each passed all seven Staging jobs before the successive live issues were
found. Final corrected exact executable head
`ff6e44606dedca3ec3bf95fdbe740de80bf4a169` passed all seven Staging jobs in
workflow 36096617375:

- six Windows, Linux and macOS x64/ARM64 runtime jobs built successfully;
- every runtime job passed 1,281 tests on each of .NET 8, 9 and 10, with zero
  failures and zero skips;
- the macOS x64 target-framework tests remained sequential as agreed;
- the package job built and validated `Icod.DCurses.2.2.0-alpha.1.nupkg` and
  `.snupkg`;
- fresh package-only consumers compiled and ran on .NET 8, 9 and 10;
- package structure, metadata, dependency closure, assembly identity, XML
  documentation and portable symbols passed validation.

Staging artifact `10847707913` records the final corrected exact executable head and has ZIP
digest
`sha256:25196ec09c8c0448d72725227572ab584bf74192c066ae388ac89f0c47eafb68`.

## Live manual acceptance

CI cannot validate terminal appearance or the feel of an interactive event
loop. The initial local run accepted the roguelike behavior and exposed the
editor Control-key issues described above. After both corrections, the
maintainer reran both samples in a real terminal on 2026-09-25 and confirmed
that both samples were good, including the corrected editor shortcuts and
orderly terminal cleanup.

### Editor recheck

- ordinary ASCII and Unicode typing, arrows, Home/End, Page Up/Down, deletion,
  Enter, Tab, wrap and selection behave as before;
- `Ctrl+T` starts/stops selection, and `Ctrl+V` remains available for terminal
  clipboard paste;
- `Ctrl+G` and `Ctrl+K Ctrl+G` open the numeric prompt;
- Enter accepts a valid row, Escape cancels, and `Ctrl+K Ctrl+C` cancels;
- a mismatching typed digit after `Ctrl+K` is retained once in the prompt;
- resize down below the minimum and back up recovers cleanly;
- exiting clears the terminal screen.

### Roguelike (previously confirmed)

- rooms, walls, doors and corridors render with the accepted viewport wrapping;
- arrows/WASD move while walls, water and void remain impassable;
- `?` and `g h` open help, movement is suppressed there, and `?` or `g m`
  returns to the map;
- pending prefixes do not leak across help open/close transitions;
- resize down below the minimum and back up recovers cleanly;
- Q or Escape exits and clears the terminal screen.

T2206 is accepted at executable head
`ff6e44606dedca3ec3bf95fdbe740de80bf4a169`. T2207 may now freeze and
adversarially qualify the 2.2 contract; this acceptance does not authorize a
merge, tag or publication.
