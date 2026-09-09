# T603-T606 — Presentation Resolution and Semantic Line Drawing

**Release line:** `Icod.DCurses 0.6.0`  
**Checkpoint:** `0.6.0-alpha.2`  
**Dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Complete

## Purpose

T603 through T606 complete the capability-aware rendition and semantic line-drawing foundation for the 0.6 release. Logical application intent remains stored in `CursesStyle` and `CursesCell`; degradation happens only at the physical terminal boundary.

## Color resolution

`CursesRefreshEngine` no longer performs private `setrgbf` / `setrgbb` probing.

Physical color resolution uses the semantic APIs supplied by `Icod.TermInfo 1.10.0`:

- `TerminalColors.GetColorSupport(...)`;
- `TerminalColors.ExpandForeground(...)`;
- `TerminalColors.ExpandBackground(...)`.

The accepted default policy is:

- terminal-default color remains terminal-default color;
- an indexed color is emitted only when it lies inside the TermInfo-advertised safe indexed range;
- an out-of-range indexed request degrades to terminal default;
- RGB is retained only when TermInfo advertises a usable direct-color model;
- unsafe direct-color values, including retained indexed-prefix collisions, degrade to terminal default;
- explicit color is not emitted unless default-color restoration is available through `op`;
- no color index is wrapped, truncated, or sent outside the advertised range.

## Attribute resolution

The physical presentation resolver preserves the requested logical `CursesStyle` in cells while resolving terminal-representable attributes.

The 0.6 vocabulary is:

```text
Bold       = 1
Dim        = 2
Underline  = 4
Reverse    = 8
Standout   = 16
Italic     = 32
Blink      = 64
Conceal    = 128
Strikeout  = 256
```

Existing 0.1-0.5 values are unchanged.

Unsupported optional attributes are omitted physically rather than causing an ordinary refresh failure. Unsupported `Standout` may degrade to `Reverse` when reverse-video is available.

The standard `ncv` (`NoColorVideo`) mask is honored when explicit foreground/background color is active. Only the physical attribute set is reduced; logical cell styles remain unchanged.

The translated `ncv` bits include:

```text
standout   1
underline  2
reverse    4
blink      8
dim        16
bold       32
invisible  64
italic     32768
```

`ColorRestrictedAttributes` is intersected with the attributes that the selected terminal actually advertises.

## Presentation capabilities view

`CursesSession.PresentationCapabilities` exposes a curses-shaped immutable observation surface without exposing terminal byte strings.

The view reports:

- safe indexed color count;
- direct RGB support;
- foreground/background selector availability;
- default-color restoration availability;
- supported rendition attributes;
- color-restricted rendition attributes;
- alternate-character-set availability;
- cursor hidden/normal/very-visible availability.

`SupportsDefaultColorRestoration` specifically means the terminal provides the TermInfo `op` (`OriginalColorPair`) path. A generic `sgr0` reset alone is not treated as equivalent color-restoration evidence.

## Semantic line cells

`CursesLineGlyph` defines the frozen single-line semantic vocabulary:

```text
Horizontal        = 0
Vertical          = 1
UpperLeftCorner   = 2
UpperRightCorner  = 3
LowerLeftCorner   = 4
LowerRightCorner  = 5
TeeUp             = 6
TeeDown           = 7
TeeLeft           = 8
TeeRight          = 9
Crossing          = 10
```

`CursesCell.Line(...)` creates a one-column logical cell that carries both:

- canonical Unicode visible content; and
- semantic line-glyph identity.

The semantic identity participates in equality. Therefore an ordinary text cell containing `─` is intentionally distinct from a semantic `Horizontal` line cell containing the same visible Unicode character.

Existing editing, copy, overlay, pad, and viewport operations preserve semantic identity because they already move complete `CursesCell` values.

## Semantic window drawing

The 0.4 caller-supplied-cell drawing APIs remain unchanged.

0.6 adds semantic convenience overloads:

- `DrawHorizontalLine(row, column, length)`;
- `DrawVerticalLine(row, column, length)`;
- `DrawBorder()`;
- `DrawBorder(CursesStyle style)`.

Styled or specialized semantic cells may also be supplied through the existing cell-based drawing overloads using `CursesCell.Line(...)`.

## Physical line presentation

Semantic line cells are resolved only during physical refresh.

Resolution order is:

1. use the terminal alternate-character-set mapping when `acsc`, `smacs`, and `rmacs` provide the requested semantic glyph;
2. otherwise use the canonical Unicode box-drawing character when the configured text-width provider reports one column;
3. otherwise use an ASCII fallback (`-`, `|`, or `+`).

Consecutive ACS-backed semantic cells are emitted inside one ACS mode run. Ordinary text that happens to contain Unicode box-drawing characters is never reinterpreted as semantic ACS content.

No escape sequence is stored in `CursesCell.Content`.

## Validation

The completed gate covers:

- Windows Staging build/tests;
- Linux Staging build/tests;
- macOS Staging build/tests;
- canonical package validation/fresh consumer;
- safe indexed and direct color degradation;
- `ncv` degradation with logical-style preservation;
- italic, blink, conceal, and strikeout output;
- semantic line cell equality/copy behavior;
- semantic horizontal/vertical/border drawing;
- ACS run coalescing;
- ordinary Unicode box text remaining ordinary text;
- Unicode fallback;
- ASCII fallback under a non-one-column box-glyph width policy.

## Remaining 0.6 work

T607 now owns integrated showcase and package acceptance. T608 remains the public API/documentation/package regret gate, including a Staging warnings-as-errors analyzer check. T609 is stable release closure.
