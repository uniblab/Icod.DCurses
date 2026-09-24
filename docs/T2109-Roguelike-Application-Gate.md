# T2109 Roguelike Application Gate

**Status:** accepted\
**Branch:** `2.1.0-roadmap`\
**Executable head:** `0297270695ce1ffc6b8bb42a99fbb18d2ac3a219`\
**PR workflow:** [36040652864](https://github.com/uniblab/Icod.DCurses/actions/runs/36040652864), 14/14 jobs successful

## Application boundary

`Icod.DCurses.Roguelike.Sample` is a public-API executable for .NET 8/9/10. Its algorithmic terrain is generated from coordinates over a ten-million-row by 2,048-column world. The application retains its player, a four-message history, and a prepared frame bounded by the current map window. `CursesViewport` owns visible coordinates; stateless track layout partitions map, sidebar, messages and status; `WriteCells` draws the first frame and two local cells for movement within one viewport. An independent retained help panel overlays the base screen. The application uses one event reader, explicit resize recomputation and opt-in refresh diagnostics.

## Red and green evidence

- The project-contract test alone at `b879557a066b4993b0444386d6f67fb6d4492681` failed on the missing sample project in [workflow 36039760869](https://github.com/uniblab/Icod.DCurses/actions/runs/36039760869). Package jobs passed; runtime tests exposed the intended failure.
- `0297270695ce1ffc6b8bb42a99fbb18d2ac3a219` adds the sample, solution wiring, README instructions and headless application tests. Its exact-head workflow passed all 14 package/runtime jobs.

## Contract qualification

| Requirement | Permanent evidence |
|---|---|
| project configuration, solution inclusion and public-only source | `RoguelikeSampleProjectContractTests` |
| coordinate-generated large world and frame bounded to the viewport | `RoguelikeSampleStateTests.PlayerMovesAcrossAViewportWithoutAllocatingTheWorld` |
| edge clamping at large coordinates and deterministic terrain | `RoguelikeSampleStateTests.ExtremeMovementClampsAndOnlyVisibleCellsAreMaterialized` |
| disjoint map, sidebar, messages, status and overlay after resize | `RoguelikeSampleStateTests.ApplicationRegionsRecomputeAndRemainDisjoint` |
| at most four retained messages | `RoguelikeSampleStateTests.MessagesRemainBoundedAfterLongMovement` |
| two-cell local movement and retained help overlay restoration | `RoguelikeSampleStateTests.LocalMovementDamagesTwoCellsAndHelpRestoresTheBaseMap` |

The README documents arrows/WASD movement, the help toggle, quit and minimum terminal size. The CI matrix builds and tests the sample on supported TFMs and platforms. A live manual terminal run is not recorded because this execution environment has no `dotnet` installation or interactive terminal. No library public surface, direct dependency or assembly identity changed.

## Next tranche

T2110 implements the editor application against the already accepted public text and geometry APIs. PR #33 remains open; merging, tagging and publishing stay outside this implementation gate.
