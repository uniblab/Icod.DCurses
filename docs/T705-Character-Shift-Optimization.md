# T705 — Physical Insert/Delete-Character Optimization

**Release:** `Icod.DCurses 0.7.0`  
**Checkpoint:** `0.7.0-alpha.5`  
**Status:** Implemented; exact checkpoint pending full PR validation

## Objective

T705 reduces refresh output for editor-like row-local shifts without changing the public logical editing contract.

`CursesWindow.InsertCells(...)` and `CursesWindow.DeleteCells(...)` continue to operate only on the logical retained screen. T705 adds an internal refresh optimization that may reproduce the resulting row with terminal insert/delete-character capabilities when the physical row is already known and an exact safe transformation can be proven.

The ordinary retained-screen renderer remains the correctness fallback.

## Candidate capabilities

The internal `CursesCharacterShiftResolver` considers only advertised TermInfo capabilities:

- `InsertCharacters` (`ich` / parameterized insert-character);
- `InsertCharacter` (`ich1` / one-character insert), repeated when that is cheaper;
- `DeleteCharacters` (`dch` / parameterized delete-character);
- `DeleteCharacter` (`dch1` / one-character delete), repeated when that is cheaper.

Parameterized and repeated one-character forms are compared by their concrete expanded terminal-byte cost. Equal-cost alternatives retain the first simpler candidate rather than adding another preference heuristic.

T705 deliberately does not synthesize a stateful `EnterInsertMode` / `ExitInsertMode` transaction. Such a path would introduce additional mode-restoration and failure-state obligations and is not justified while direct `ich`/`ich1` candidates cover the measured optimization target. T708 may revisit this only if benchmark evidence demonstrates a material gap.

## Exact transformation requirement

The resolver first requires the entire physical row to be known. It then finds the first physical/logical cell difference and tests whether the desired row is exactly obtainable by one insert or delete shift from that column.

For insertion by `n` columns:

```text
physical: prefix + source-tail + discarded-tail
desired:  prefix + n default blanks + source-tail
```

For deletion by `n` columns:

```text
physical: prefix + discarded-cells + source-tail
desired:  prefix + source-tail + n default blanks
```

The optimization is accepted only when the complete resulting row is proven equivalent. There is no speculative partial-row mutation and no heuristic inference from which logical API caused the damage.

This means the physical optimization also works when an equivalent terminal shift exists at a later first-difference column than the original logical edit origin, for example when an inserted blank is indistinguishable from an already blank physical cell.

## Safety preconditions

A candidate is rejected unless all of the following hold:

- every physical cell in the row is known;
- the current retained physical rendition is exactly the default style;
- inserted or delete-vacated cells are logical default-styled blanks;
- every cell in the shifted physical/logical range is a one-column, non-continuation cell;
- semantic line-glyph cells are absent from the shifted range;
- therefore no wide-cell leader/continuation boundary can be split or displaced ambiguously;
- an advertised direct insert/delete-character capability can be safely expanded;
- the candidate provides a strict deterministic byte win.

The default-rendition restriction is intentional. It avoids adding an unmodeled style transition before `ich`/`dch` merely to make inserted blanks correct. T707 may improve rendition-transition cost modeling, but T705 does not borrow future assumptions.

Moved cells may retain non-default styles because the terminal character-shift operation moves the existing physical cells, including their retained presentation, rather than reconstructing them from logical text.

## Cost gate

T705 uses the same `CursesOutputCostModel` and owning `TerminalSession.ApplicationEncoding` established by T701/T704.

The rewrite comparison is intentionally conservative. The resolver sums only the encoded application-text bytes of changed **nonblank** desired cells that the ordinary renderer would necessarily have to reproduce. It does not count:

- cursor movement;
- rendition transitions;
- blank-cell writes;
- erase operations;
- flush overhead.

Therefore this value is a lower bound on the ordinary row rewrite payload. A character-shift candidate must still be strictly cheaper than that lower bound.

This prevents the optimizer from claiming a win merely because it ignored a cheaper T704 erase fallback or other refresh machinery. Equal cost remains on the ordinary renderer.

## Refresh integration

Character-shift selection occurs once at the beginning of each row before ordinary span rendering.

When a plan is accepted:

1. T703 positions the cursor at the proven shift origin;
2. the selected `ich`/`ich1` or `dch`/`dch1` sequence is emitted through `TerminalCapabilityWriter`;
3. only after successful emission, the entire retained physical row is recorded as equal to the desired logical row;
4. the physical cursor remains known at the character-shift origin;
5. the ordinary dirty-row rewrite is skipped;
6. normal final requested-cursor placement and refresh flushing still occur.

If output fails, the existing refresh transaction catch path invalidates retained physical-screen, rendition, and cursor knowledge. A retry therefore cannot reuse the failed optimization assumption and falls back through an ordinary repaint from unknown physical state.

## Tests

Focused resolver tests cover:

- exact parameterized insertion;
- exact parameterized deletion;
- repeated `ich1` beating parameterized `ich`;
- strict equal-cost rejection;
- application-encoding-sensitive cost selection;
- unknown physical-row rejection;
- non-default active-rendition rejection;
- non-default inserted-blank rejection;
- wide-cell / continuation-range rejection.

Refresh integration tests use the actual public logical editing APIs and verify:

- `CursesWindow.InsertCells(2)` emits only the cheaper physical insert sequence for the selected row;
- `CursesWindow.DeleteCells(2)` emits only the cheaper physical delete sequence;
- the retained physical row is exact, proven by a following no-write refresh;
- missing character-shift capabilities fall back to ordinary rendering;
- a synthetic shift-output failure invalidates retained physical knowledge and forces an ordinary full-row repaint on retry.

## Public API and ownership

T705 adds no public API.

Ownership remains unchanged:

- `Icod.TermInfo` owns capability presence and expansion;
- `Icod.Terminal` owns live output and application-text encoding;
- DCurses owns deterministic safe selection for the retained logical/physical diff.

No terminal-name, emulator-brand, OS, or private escape-sequence heuristic is introduced.

## T705 gate

T705 is complete when the exact `0.7.0-alpha.5` source head passes:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- package/fresh-consumer validation;
- warnings-as-errors;
- the focused exact-equivalence and failure-recovery tests above.

After that gate, T706 may begin physical insert/delete-line and scroll-region optimization.
