# Icod.DCurses 0.6 Public API Baseline

**Release line:** `0.6.0`  
**Baseline checkpoint:** `0.6.0-alpha.2`  
**Stable predecessor:** `0.5.0`  
**Dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** T608 review in progress

## Purpose

This document records the deliberate public API additions introduced by the 0.6 rendition, drawing, and presentation release line. It is a regret-review baseline, not a replacement for XML documentation or earlier milestone baselines.

Unless listed here, the public 0.5 contract remains unchanged.

## `CursesTextAttributes`

Existing numeric values remain frozen and the new 0.6 values append as powers of two:

```text
None       = 0
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

## `CursesPresentationCapabilities`

New public readonly record struct.

Public read-only properties:

```text
int IndexedColorCount
bool SupportsDirectRgb
bool SupportsForegroundColor
bool SupportsBackgroundColor
bool SupportsDefaultColorRestoration
CursesTextAttributes SupportedAttributes
CursesTextAttributes ColorRestrictedAttributes
bool SupportsAlternateCharacterSet
bool SupportsCursorHidden
bool SupportsCursorNormal
bool SupportsCursorVeryVisible
bool SupportsColor
bool SupportsBold
bool SupportsDim
bool SupportsUnderline
bool SupportsReverse
bool SupportsStandout
bool SupportsItalic
bool SupportsBlink
bool SupportsConceal
bool SupportsStrikeout
```

The type contains semantic observations only. It exposes no terminal escape strings and no new TermInfo type.

`SupportsDefaultColorRestoration` means the selected TermInfo description provides `OriginalColorPair` (`op`). `sgr0` alone does not establish this capability.

## `CursesSession`

New read-only property:

```csharp
CursesPresentationCapabilities PresentationCapabilities { get; }
```

The existing public `TerminalDescription Terminal` property remains the approved low-level escape hatch, but ordinary presentation decisions should not require it.

## `CursesLineGlyph`

New public enum with frozen numeric values:

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

The enum represents semantic single-line box-drawing intent, not a Unicode or ACS byte value.

## `CursesCell`

New read-only properties:

```csharp
CursesLineGlyph? LineGlyph { get; }
bool IsLineGlyph { get; }
```

New factory:

```csharp
static CursesCell Line(
    CursesLineGlyph glyph,
    CursesStyle style = default
);
```

A semantic line cell has canonical one-column Unicode `Content`, but its semantic identity participates in equality. Therefore an ordinary text cell containing `─` is intentionally distinct from `CursesCell.Line(CursesLineGlyph.Horizontal)`.

No terminal escape sequence is stored in the cell.

## `CursesWindow`

New semantic convenience overloads:

```csharp
void DrawHorizontalLine(
    int row,
    int column,
    int length
);

void DrawVerticalLine(
    int row,
    int column,
    int length
);

void DrawBorder();

void DrawBorder(
    CursesStyle style
);
```

The existing 0.4 caller-supplied-cell overloads remain unchanged. The semantic overloads reuse the same geometry and wide-footprint repair implementation.

## Intentionally internal implementation types

The following 0.6 implementation concepts remain internal and are not part of the public contract:

- physical style resolver;
- physical line-glyph resolver;
- ACS source-character mapping;
- Unicode/ACS/ASCII physical representation choice;
- TermInfo color-expansion implementation details;
- `ncv` numeric bit translation.

Applications store semantic intent; terminal-specific resolution remains an implementation concern.

## Dependency boundary

0.6 adds no new Terminal or TermInfo type to public signatures.

The approved dependency-boundary allow-list remains unchanged from 0.5:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

Existing machine tests continue to enforce that boundary.

## Package and analyzer gate

The generated package must compile and execute a fresh consumer that uses:

- all new rendition flags;
- `CursesCell.Line(...)`;
- semantic line drawing;
- `CursesSession.PresentationCapabilities`.

The test project uses warning level 4 with warnings-as-errors in the normal Staging PR configuration. This is specifically intended to prevent analyzer-only failures from first appearing after merge under Release.

## Freeze decision pending

T608 remains open until:

- the exact reflection guard for this public delta is green;
- Windows/Linux/macOS Staging build/tests are green;
- package/fresh-consumer validation is green;
- README and conceptual documentation are synchronized;
- the public API regret review finds no signature that should change before RC;
- the Release analyzer behavior has no known gap.

Once those conditions pass, the same contract may be promoted to `0.6.0-rc.1` without another feature addition.
