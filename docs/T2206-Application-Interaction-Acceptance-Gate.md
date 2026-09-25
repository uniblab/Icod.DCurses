# T2206 Application Interaction Acceptance Gate

**Date:** 2026-09-25  
**Status:** Automated qualification complete; live manual acceptance pending  
**Executable head:** `19d0fba977e9d1272ea82f1cab705a64130fad64`  
**Workflow:** [36090193035](https://github.com/uniblab/Icod.DCurses/actions/runs/36090193035)

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

## Exact-head automated qualification

The first package-consumer attempt at `2c4619acc614895d660bebed1ce5eb31d7daba43`
correctly exposed that `CursesInputEvent.FromText` is an internal test factory,
not public NuGet API. The correction did not widen the API: package smoke now
behaviorally verifies public sequence registration, discovery, cancellation
and unbinding, and compile-checks the public processing signature. Source-level
tests continue to cover pending, completion and mismatch processing with
synthetic events.

Exact executable head `19d0fba977e9d1272ea82f1cab705a64130fad64`
passed all seven Staging jobs in workflow 36090193035:

- six Windows, Linux and macOS x64/ARM64 runtime jobs built successfully;
- every runtime job passed 1,274 tests on each of .NET 8, 9 and 10, with zero
  failures and zero skips;
- the macOS x64 target-framework tests remained sequential as agreed;
- the package job built and validated `Icod.DCurses.2.2.0-alpha.1.nupkg` and
  `.snupkg`;
- fresh package-only consumers compiled and ran on .NET 8, 9 and 10;
- package structure, metadata, dependency closure, assembly identity, XML
  documentation and portable symbols passed validation.

Staging artifact `10844849549` records the exact executable head and has ZIP
digest
`sha256:9a50b9d829b416a70a6f25439d9ce9ac7e34c28ca8542a5dc33634d6edb07111`.

## Required live manual acceptance

CI cannot validate terminal appearance or the feel of an interactive event
loop. Before accepting T2206, run both samples in a real terminal and confirm:

### Editor

- ordinary ASCII and Unicode typing, arrows, Home/End, Page Up/Down, deletion,
  Enter, Tab, wrap and selection behave as before;
- `Ctrl+G` and `Ctrl+K Ctrl+G` open the numeric prompt;
- Enter accepts a valid row, Escape cancels, and `Ctrl+K Ctrl+C` cancels;
- a mismatching typed digit after `Ctrl+K` is retained once in the prompt;
- resize down below the minimum and back up recovers cleanly;
- exiting clears the terminal screen.

### Roguelike

- rooms, walls, doors and corridors render with the accepted viewport wrapping;
- arrows/WASD move while walls, water and void remain impassable;
- `?` and `g h` open help, movement is suppressed there, and `?` or `g m`
  returns to the map;
- pending prefixes do not leak across help open/close transitions;
- resize down below the minimum and back up recovers cleanly;
- Q or Escape exits and clears the terminal screen.

T2206 remains pending until this checklist is confirmed. T2207 must not treat
green CI alone as live application acceptance.
