# T704 — Cost-Aware Erase Selection

**Release:** `Icod.DCurses 0.7.0-alpha.4`  
**Tranche:** T704  
**Status:** Implemented; exact checkpoint subject to PR matrix/package validation

## Objective

T704 replaces the former fixed-width `ClearToEndOfLine` heuristic with deterministic physical erase selection at the retained-screen refresh boundary.

The selector considers only advertised TermInfo capabilities and chooses an erase only when the capability is strictly cheaper in encoded output bytes than the best narrower fallback that DCurses can safely use for the same logical result.

## Candidate operations

The internal `CursesEraseResolver` considers:

- literal application-text blanks;
- `ClearToEndOfLine` (`el`);
- `ClearToEndOfScreen` (`ed`);
- `ClearScreen` (`clear`).

Selection is deliberately hierarchical:

1. `el` must beat literal blanks for the current row tail;
2. `ed` must beat the best combination of literal blanks / `el` over the complete screen tail;
3. `clear` must beat the best already-selected screen-tail plan and is considered only when the complete logical screen is default-styled blank.

Equal-cost candidates do not replace the narrower fallback.

## Safety preconditions

An erase candidate is constructed only when every physical cell it would erase is logically:

- blank;
- default-styled;
- inside the retained screen dimensions.

This prevents a shorter `ed` or `clear` sequence from erasing preserved logical content merely because the escape itself is inexpensive.

Styled blanks are not widened into an erase region. This is important because terminal erase semantics are not a substitute for rendering arbitrary semantic rendition.

## Encoding-aware cost

Literal blank cost uses the owning `TerminalSession.ApplicationEncoding` in production. This keeps the cost model aligned with the actual byte representation used for application text rather than assuming UTF-8.

TermInfo protocol strings continue to use `CursesOutputCostModel.GetTerminalStringByteCount(...)`, including padding-directive semantics.

## Affected-line semantics

`TerminalCapabilityWriter` now has an affected-line-aware overload. Multi-line erase operations pass the number of lines they affect to the Terminal output seam, preserving TermInfo padding semantics for capabilities whose padding depends on affected line count.

- `el`: 1 affected line;
- `ed`: remaining screen rows beginning with the current row;
- `clear`: complete screen row count.

## Retained physical state

After successful output, DCurses updates retained physical knowledge for exactly the cells the erase operation made logically correct:

- `el`: current row from the erase origin to end of line;
- `ed`: current row tail plus every following row;
- `clear`: complete screen.

`el` and `ed` retain the cursor at the erase origin.

`clear` conservatively marks cursor position unknown because a terminal's `clear` capability may include cursor movement. Final requested cursor placement therefore goes through the ordinary T703 safe cursor-motion resolver from unknown state.

Any output failure still enters the existing refresh failure path and invalidates retained physical/cursor/rendition knowledge.

## Tests

T704 adds focused coverage for:

- literal blanks beating a longer `el`;
- `el` beating literal blanks;
- `ed` beating repeated narrower row operations;
- `clear` beating the best screen-tail plan when the whole logical screen is blank;
- non-default styled blanks blocking broader erase;
- application encoding changing the literal/erase cost decision;
- refresh-engine emission of literal, `ed`, and `clear` paths;
- affected-line propagation;
- retained-screen equivalence demonstrated by a no-write second refresh after successful broad erase.

The pre-existing refresh test for trailing blanks is updated to require an actual strict byte win before expecting `el`.

## Public API and dependency result

T704 adds no public DCurses API.

Dependency versions remain:

- `Icod.Terminal 1.0.0`;
- `Icod.TermInfo 1.10.0`.

`AssemblyVersion` remains `0.7.0.0`.

## Gate

T704 is complete when the exact `0.7.0-alpha.4` implementation/documentation head passes:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- package validation/fresh-consumer checks.

After that checkpoint, T705 may begin physical insert/delete-character optimization.
