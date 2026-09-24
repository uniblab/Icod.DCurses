# T2109 Roguelike Application Acceptance Plan

**Status:** accepted at executable head `0297270695ce1ffc6b8bb42a99fbb18d2ac3a219`; [gate](../../T2109-Roguelike-Application-Gate.md)
**Authority:** `Icod.DCurses-2.1.0-Development-Roadmap.md`, T2109; accepted T2101–T2108 public API and gates.  
**Branch:** `2.1.0-roadmap` in open PR #33. Do not merge, tag or publish.

## Goal and boundary

Build `samples/Icod.DCurses.Roguelike.Sample` as a runnable, headless-testable application. The sample owns an algorithmically generated world, player position, messages and controls. DCurses owns visible coordinates, retained cells/panels, viewport, track rectangles and refresh. No game-engine abstractions, document-sized pad, private DCurses/Terminal API, capability probing or protocol bytes belong in the sample. Preserve .NET 8/9/10, the direct `Icod.Terminal 1.18.0` production dependency and `AssemblyVersion 2.0.0.0`.

## Implementation sequence

1. **Project and red contract.** Add a project-contract test checking the new project, solution entry, framework/configuration matrix, sole project reference to DCurses and public-only source boundary. Observe it fail for the missing project. Add the executable project and solution entry without changing library public API.
2. **Pure world/view state.** Keep an application-owned player coordinate, bounded message history and deterministic terrain function in the sample. Use `CursesViewport` over a large logical extent; derive viewport origin with `EnsureVisible`/pan operations and materialize only visible rows and columns. Test movement and edge clamping, world-to-local coordinate translation, distinct cells at distant positions, and memory independent of total world area. The tests should call this state without opening a terminal.
3. **Regions and resize.** Use `CursesLayout.ArrangeRows` and `ArrangeColumns` to divide a current `CursesScreen.Bounds` into map, sidebar, status and messages, with a small-terminal fallback. Recompute rectangles on `SynchronizeDimensions()` and repaint lifecycle events. Test disjoint rectangles at ordinary, minimum and resized bounds without assuming a fixed terminal profile.
4. **Retained renderer and overlay.** Write a prepared visible map frame with `CursesWindow.WriteCells`; update a moving player and nearby terrain with bounded local cells after the baseline. Draw status/messages in their own windows. Use an independent `CursesPanel` for an optional help overlay and verify hide/show restores the underlying map. Headless tests compare the complete visible frame, a local movement patch and overlay order. Opt into T2108 diagnostics and assert the local attempt prepares less semantic work than a full-frame attempt.
5. **One event loop and manual run.** Bind arrows or WASD to movement, `?` to the help overlay and `Q`/Escape to exit. Handle end-of-input, resize, interrupt and termination; dispose the panel/session and keep one event reader. Document controls, virtual world ownership and a `dotnet run --project ... --framework net10.0` command in `samples/README.md`.
6. **Gate.** Run solution build/tests in Debug, Staging and Release where available, package validation, `git diff --check`, the public-only source check and the ordinary exact-head 14-job Windows/Linux/macOS matrix. Record a T2109 gate with source SHA, CI run, deterministic tests and manual-run limitations. Keep PR #33 open.

## Acceptance

The sample runs without a game engine, holds only visible map cells and bounded application state, updates geometry after resize, preserves panel composition, and passes deterministic headless application tests. The sample is documented and built in the solution on every supported TFM. No new DCurses public API is required.
