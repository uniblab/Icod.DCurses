# T1105 — Editing, Composition, Pad, and Resize Semantic Propagation

**Project:** `Icod.DCurses`  
**Release:** `1.1.0`  
**Tranche:** T1105  
**Development version:** `1.1.0-alpha.5`  
**Assembly version:** `1.0.0.0`  
**Dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Status:** implementation complete; documentation-synchronized exact-head validation required

## 1. Objective

T1105 makes semantic metadata part of the same retained logical content model as the cell it describes whenever a stable structural curses operation moves or copies content.

The public `CursesCell` representation remains unchanged. Structural operations use an internal transient pair:

```text
CursesLogicalCellState
    CursesCell
    CursesCellMetadata?
```

This avoids both an unconditional public-cell metadata field and a second editing engine whose topology could diverge from cell movement.

## 2. Editing rule

For insert/delete cells and insert/delete lines:

- metadata moves with surviving source content;
- newly inserted/vacated background coordinates carry no metadata;
- content shifted beyond the window loses its metadata with that discarded content;
- blank cells carrying metadata remain meaningful content for destructive structural editing and therefore move with their metadata;
- row normalization repairs wide-cell footprints and preserves metadata only on coherent surviving leaders/continuations.

A valid two-column element is canonicalized so leader and continuation carry the leader's semantic metadata. An orphaned continuation or clipped leader is replaced with the editing background and loses semantic metadata.

## 3. Scrolling rule

`ScrollUp(...)` and `ScrollDown(...)` now move paired logical cell state rather than copying only `CursesCell`.

The iteration direction remains overlap-safe:

- upward scroll reads lower rows while writing earlier rows;
- downward scroll reads upper rows while writing later rows from bottom to top.

Newly vacated rows are filled with the window background and have no semantic metadata.

## 4. Rectangle composition rules

### CopyRectangleTo

Destructive copy transfers the complete logical source coordinate state:

```text
cell + semantic metadata
```

This includes a source blank coordinate that deliberately carries semantic metadata. Source rectangles are snapshotted before destination mutation, preserving deterministic overlapping-copy behavior for metadata as well as cells.

### OverlayRectangleTo

The existing overlay transparency rule remains authoritative: an ordinary source blank is fully transparent.

Therefore a transparent source blank:

- does not replace the destination cell;
- does not replace or clear destination semantic metadata;
- does not transfer source semantic metadata even if the source blank itself is annotated.

Nonblank source content transfers its semantic metadata with the cell.

## 5. Wide-cell composition

Composition snapshots are normalized at rectangle boundaries before destination writes.

Consequences:

- a two-column element wholly inside the copied rectangle preserves one coherent metadata value across leader/continuation coordinates;
- a rectangle beginning on a continuation cannot transfer an orphaned half-glyph;
- a leader clipped by the right rectangle boundary is converted to the boundary blank and loses semantic metadata;
- destination wide footprints are repaired before replacement;
- continuation text is never independently copied as application content.

## 6. Pad and viewport behavior

`CursesPad.PresentTo(...)` already delegates rectangular transfer to `ContentWindow.CopyRectangleTo(...)`. T1105 therefore makes ordinary pad presentation semantic-aware without adding a second pad-specific transfer path.

`CursesPadViewport.Present()` likewise inherits semantic transfer through the pad operation.

Pad change tracking already records metadata-only mutation because `CursesVirtualScreen.SetMetadata(...)` participates in the existing logical cell revision mechanism. T1105 verifies that:

- a semantic-only visible pad change makes `HasVisiblePadChanges` true;
- presenting that viewport transfers metadata and advances only that viewport's observation state;
- two independent viewports over one pad each continue to observe the change until that individual viewport presents it.

No Terminal ownership is added to pads or viewports.

## 7. Preserved screen resize

`CursesScreen.Resize(..., preserveContents: true)` now copies semantic metadata alongside each surviving overlapping cell before applying the existing wide-footprint repair pass.

If resize clips a two-column element, `CursesCellFootprint.Repair(...)` replaces the incomplete footprint through ordinary cell assignment. Because ordinary cell replacement clears semantic metadata, metadata cannot remain attached to a discarded half-glyph.

`preserveContents: false` continues to create a new blank surface with no semantic metadata.

## 8. Public API impact

T1105 adds no public type or public member.

The provisional 1.1 compiled public contract remains the T1103 baseline:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

The new `CursesLogicalCellState` is internal and transient.

## 9. Focused acceptance coverage

`CursesSemanticPropagationTests` verifies:

- insert-cell metadata movement;
- delete-cell metadata movement and vacated-tail cleanup;
- insert/delete line metadata movement;
- upward/downward scrolling;
- wide-cell edit normalization;
- destructive copy of a blank carrying metadata;
- transparent overlay preserving destination metadata;
- overlapping copy snapshot semantics;
- pad presentation;
- semantic-only pad viewport changes;
- preserved screen resize;
- clipped-wide resize metadata cleanup.

`CursesSemanticViewportIndependenceTests` separately proves that multiple viewports retain independent semantic change-observation state.

The implementation checkpoint before alpha.5 promotion passed the complete PR matrix on exact head:

```text
2c5459984ac1d5a1616ed7fea09ea69429ca872b
```

Workflow #492 (`34412687220`) passed Windows/Linux/macOS x64/ARM64 plus package validation.

## 10. Optimization boundary

T1105 establishes logical correctness but does not automatically re-enable the terminal-native line-shift, character-shift, erase, or scroll shortcuts for semantic screens.

Those physical optimizations remain conservatively disabled while desired or retained physical semantic metadata exists. T1107 may re-enable a subset only after proving that the chosen physical operation reproduces both visible cells and retained semantic intent exactly.

This separation prevents a logical propagation success from being mistaken for a proof about terminal behavior under native editing controls.

## 11. Exit gate

T1105 is complete when one documentation-synchronized `1.1.0-alpha.5` SHA passes:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- package/fresh-consumer validation.

No public API delta beyond T1103 is expected.

After that gate, T1106 may begin lifecycle, cancellation, semantic-output failure, and recovery hardening.
