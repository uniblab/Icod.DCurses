# T703 — Cost-Aware Cursor-Motion Selection

## Status

Complete for `0.7.0-alpha.3`.

## Objective

T703 replaces unconditional use of absolute cursor addressing with deterministic selection among safe advertised TermInfo cursor-motion capabilities.

The optimization remains internal to the physical refresh engine. Application-visible cursor requests and logical screen state are unchanged.

## Selection contract

`CursesCursorMotionResolver` evaluates only capabilities advertised by the active `TerminalDescription` and compares their concrete expanded terminal-string byte counts.

Candidate forms include:

- absolute cursor addressing;
- row-address plus column-address composition;
- row-only or column-only absolute positioning when one axis is already correct;
- cursor home for `(0, 0)`;
- carriage return when the target is column zero on the known current row;
- parameterized relative up/down/left/right motion;
- repeated one-step relative up/down/left/right motion.

Equal-cost ties retain the established absolute cursor-address path.

## Safety rules

Relative candidates are considered only when both current cursor coordinates are known.

The refresh engine deliberately clears physical cursor knowledge after output that reaches the final screen column because line-wrap behavior can make the resulting hardware position ambiguous. After such an event, T703 therefore selects only operations that are safe from unknown position, such as absolute addressing, row/column-address composition, or home where applicable.

Physical cursor coordinates are updated only after the selected terminal sequence is written successfully. Existing refresh failure handling invalidates retained physical state when output fails.

No terminal-name, emulator-brand, operating-system, or private escape-sequence heuristic is used.

## Engine integration

`CursesRefreshEngine.MoveCursorAsync()` is the single physical cursor-movement seam. It now delegates candidate construction and selection to `CursesCursorMotionResolver` and emits the selected sequence through `TerminalCapabilityWriter`.

This means the same optimization applies consistently to:

- refresh span placement;
- erase-operation placement;
- final requested cursor placement;
- direct serialized cursor positioning through `SetCursorPositionAsync()`.

## Tests

Focused resolver tests cover:

- one-column relative motion beating absolute address;
- parameterized motion beating repeated single-step motion;
- row/column address composition from unknown state;
- carriage return selection;
- home selection;
- equal-cost absolute tie-breaking;
- controlled failure when no safe candidate exists.

Refresh-engine integration tests additionally prove that:

- a known physical cursor uses cheaper relative motion;
- absolute `cup` remains the fallback when no cheaper safe capability exists;
- equal-cost motion continues to select absolute addressing at the physical output seam.

## Release impact

T703 does not add public API. `AssemblyVersion` remains `0.7.0.0`; package/version metadata advances to `0.7.0-alpha.3`.

The next development tranche is T704: cost-aware erase-to-line, erase-to-screen, and whole-screen selection.
