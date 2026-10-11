# T2406 API Gap Decision

**Decision:** No additive viewport or text-layout API is required  
**Evidence head:** `9a02d92`

The T2405 witnesses demonstrate the required editor projection through the
published DCurses surface:

- `CursesTextLayout.Create` measures caller-provided visible text;
- `GetVisualPosition`, `HitTest`, and `GetSelectionRectangles` preserve legal
  Unicode/cell geometry;
- `CursesViewport` maps bounded content and viewport coordinates; and
- `CursesWindow.PresentTextLayout` applies retained cells, continuation state,
  style, metadata, clipping, and damage.

The sole red performance witness passed a whole 100,000-column layout into the
projection API and allocated about 18 MB. The approved ownership model already
requires Widgets to own document indexing, revision caches, and bounded visible
line slices. Once expressed as a 40-character slice, the same projection stayed
within the viewport work budget. Adding a DCurses document-offset, line-index,
or cache API would duplicate Widgets policy and encourage file-sized retained
state.

Therefore T2406 closes without a production API addition. This decision does not
preclude reopening the gate if the later interaction/sample witness produces a
specific invariant that cannot be expressed through the current public surface.
