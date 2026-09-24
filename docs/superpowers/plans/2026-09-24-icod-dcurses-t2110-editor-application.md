# T2110 Editor Application Acceptance Plan

**Status:** accepted at executable head `035f78603740fbc22a41fbf1325f5c1f58938bc9`, 14/14 jobs in workflow 36043351407; see [the T2110 gate](../../T2110-Editor-Application-Gate.md).
**Authority:** `Icod.DCurses-2.1.0-Development-Roadmap.md`, T2110; accepted T2101–T2108 public API and gates.  
**Branch:** `2.1.0-roadmap` in open PR #33. Do not merge, tag or publish.

## Goal and boundary

Build `samples/Icod.DCurses.Editor.Sample` with an application-owned synthetic document and edit policy. Use public text layout/geometry, viewport, retained presentation and track regions; do not add a reusable editor buffer, undo engine or file format to DCurses. Demonstrate Unicode source boundaries, wrap/no-wrap, selection, two-axis scrolling, status and prompt without a document-sized pad. Keep the production package identity and direct Terminal dependency unchanged.

## Implementation sequence

1. **Project and red contract.** First add a failing project-contract test for the project, solution entry, .NET 8/9/10 and Debug/Staging/Release configurations, sole DCurses project reference, and absence of internal/Terminal/TermInfo API usage. Add the executable project and solution entry.
2. **Document slice and source positions.** Generate untouched synthetic lines on demand from a large logical row count and retain only edited overrides. Keep the active line and selection anchor in application state. Call `CursesTextLayout.Create` for a bounded visible slice and use `GetPreviousPosition`, `GetNextPosition`, `GetVisualPosition`, `HitTest`, `MoveVertically` and `GetSelectionRectangles` for movement and selection. Translate slice-relative source offsets explicitly to document line/offset coordinates. Test grapheme clusters, wide glyphs, combining marks, tabs, hard breaks, insertion and deletion at valid text-element boundaries; reject half-surrogate and continuation-cell edits before mutation.
3. **Viewport and geometry.** Recompute document, status and prompt regions with `CursesLayout.ArrangeRows` on resize. Apply `CursesViewport` to visible content in wrap and no-wrap modes; `EnsureVisible` the caret vertically and horizontally without materializing hidden lines. Test a large synthetic extent, viewport edge clamping and stable source/visual mapping across scroll and resize.
4. **Rendering and input.** Present only visible lines through the public retained text/cell APIs and draw selection and caret without overlapping continuation cells. Keep one event loop for arrows, text input, Backspace/Delete, Enter, selection toggle, wrap toggle, page/scroll controls, prompt and Q/Escape. Handle end-of-input, resize, interrupt and termination; maintain ordinary DCurses/Terminal lifecycle ownership.
5. **Qualification and documentation.** Headless tests exercise editing/navigation and compare visible-slice work at small and large document extents. Check that text layout/mapping and retained mutation costs remain proportional to visible text. Document controls, ownership, limits and the runnable command in `samples/README.md`.
6. **Gate.** Verify solution builds/tests, package validation, `git diff --check`, public-only source boundary and the ordinary exact-head 14-job PR matrix. Record T2110 gate evidence, then proceed to T2111 documentation/API closure without merging PR #33.

## Acceptance

Deterministic tests cover Unicode editing and movement, selection, wrap modes, two-axis scroll and resize. The manual sample runs on supported TFMs, generates large untouched portions lazily, retains only application edits and visible layout, and requires no new DCurses public API.
