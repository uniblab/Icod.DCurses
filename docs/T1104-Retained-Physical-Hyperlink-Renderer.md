# T1104 — Retained Physical Hyperlink Renderer

**Project:** `Icod.DCurses`  
**Release line:** `1.1.0`  
**Tranche:** T1104  
**Development checkpoint:** `1.1.0-alpha.4`  
**Assembly version:** `1.0.0.0`  
**Runtime dependencies:** `Icod.Terminal 1.6.0`; `Icod.TermInfo 1.10.0`  
**Status:** implementation and focused acceptance staged; documentation-complete validation pending

## Purpose

T1104 connects the logical semantic-content contract from T1103 to physical retained rendering without transferring OSC 8 protocol ownership from `Icod.Terminal` into DCurses.

The renderer must distinguish:

```text
same cell / same style / same metadata
same cell / same style / changed metadata
changed cell / same metadata
unknown physical cell or semantic state
```

A semantic-only change must therefore be sufficient to produce physical refresh work even when the glyph and `CursesStyle` are unchanged.

## Terminal ownership decision

DCurses does not maintain a raw OSC 8 writer and does not hold a Terminal hyperlink lease open across arbitrary refresh output.

Each coalesced linked application-text payload is instead delegated through Terminal's bounded public semantic operation:

```csharp
TerminalSession.WriteHyperlinkAsync(
    value,
    uri,
    identifier,
    cancellationToken
);
```

This is the preferred composition for retained refresh because Terminal owns the complete:

```text
OSC 8 begin
application text
OSC 8 end / restoration
```

transaction and its serialization/failure semantics.

A long-lived hyperlink lease spanning unrelated cursor/rendition/terminal output was rejected for T1104 because it could make unrelated direct Terminal output inherit the active link. Bounded semantic writes leave the terminal hyperlink-neutral between retained linked runs.

## Internal output seam

T1104 extends the existing private refresh-output boundary with:

```text
ITerminalHyperlinkOutput.WriteHyperlinkTextAsync(...)
```

`TerminalSessionCursesOutput` implements that seam by calling `TerminalSession.WriteHyperlinkAsync(...)`.

The seam is internal. It adds no public DCurses type/member and does not create a second live-terminal ownership abstraction.

## Retained physical semantic state

`CursesPhysicalScreenState` now retains semantic metadata alongside each known physical `CursesCell`.

Properties:

- physical metadata uses the same row-sparse reference-plane strategy selected in T1102;
- unlinked physical cells do not pay per-cell metadata storage;
- physical invalidation clears both known cell state and retained semantic metadata;
- metadata can be compared independently of glyph/style equality;
- uncertain output never leaves semantic state trusted after the normal refresh exception path invalidates retained knowledge.

## Run coalescing

The refresh renderer first groups dirty physical output by:

```text
resolved CursesStyle
CursesCellMetadata equality
```

Adjacent cells with the same style and same hyperlink therefore produce one bounded hyperlink payload rather than one OSC 8 transaction per cell.

A metadata transition splits the semantic run even when the style is identical.

Alternate-character-set transitions may split one logical link run into multiple bounded Terminal hyperlink payloads because terminal protocol mode transitions must remain outside application text. This is accepted for correctness in T1104; T1107 may measure whether further optimization is worthwhile.

## Wide-cell behavior

Continuation cells reserve terminal columns but do not emit duplicate application text.

Because T1103 stores one coherent metadata value across a valid leader/continuation footprint, a linked two-column element such as `界` emits one semantic payload containing `界`, not one payload per coordinate.

## Link removal and semantic-only replacement

If a coordinate contains the same visible cell but the hyperlink changes, retained physical metadata comparison forces a repaint using the new bounded link.

If a link is removed while the visible cell remains unchanged, the same coordinates are rewritten as ordinary unlinked application text. The physical metadata state is then updated to unlinked.

This is how a terminal cell loses its previous OSC 8 association without requiring a raw DCurses OSC close operation.

## No-op behavior

After a successful refresh:

- logical cells are clean;
- physical cells match the desired logical values;
- retained physical metadata matches desired metadata.

A second refresh with no cell/style/metadata changes therefore emits no application or hyperlink payload. The ordinary refresh flush boundary remains unchanged.

## Invalidation

`CursesRefreshEngine.Invalidate()` clears retained physical cell and semantic knowledge at the next refresh boundary.

Linked content is then repainted through Terminal's bounded hyperlink operation exactly as ordinary content is repainted after physical invalidation.

## Optimization guard

T1104 does **not** assume that terminal-native erase, character-shift, line-shift, or scrolling operations preserve terminal hyperlink-cell semantics in a way that DCurses can safely model.

Therefore, while either the desired logical surface or retained physical surface contains semantic metadata, T1104 conservatively disables:

- erase-to-end/clear shortcuts;
- insert/delete-character shortcuts;
- insert/delete-line shortcuts;
- scrolling-region line-shift shortcuts.

The renderer falls back to direct retained rewriting.

This is a correctness gate rather than a permanent performance decision. T1105 defines semantic propagation for logical transforms, and T1107 may selectively re-enable physical optimizations only when their final semantic result can be proven equivalent.

## Synchronized-output composition

Terminal 1.6 documents synchronized output as a presentation-timing bracket around ordinary semantic session operations rather than a new byte buffer or long-lived exclusive output lock.

T1104 therefore permits:

```text
Terminal synchronized-output begin
DCurses cursor/rendition output
Terminal bounded OSC 8 hyperlink write
more DCurses output
Terminal synchronized-output end
```

A real `TerminalSession` + `CursesSession` integration test verifies canonical ordering and proves the composition does not self-deadlock.

## Focused coverage

`CursesHyperlinkRefreshTests` verifies:

- adjacent equivalent linked cells become one semantic payload;
- unchanged linked content produces no second payload;
- metadata-only replacement rewrites the same glyphs with the new hyperlink;
- link removal rewrites the same glyphs as ordinary text;
- linked/unlinked/linked spans remain distinct semantic runs;
- a wide linked element produces one semantic payload;
- explicit physical invalidation repaints linked content;
- semantic presence suppresses erase optimization until propagation is proven.

`CursesTerminalHyperlinkIntegrationTests` verifies through actual public Terminal and DCurses sessions:

- synchronized-output begin precedes linked output;
- Terminal emits canonical OSC 8 begin;
- linked application text follows begin;
- Terminal emits canonical OSC 8 end;
- synchronized-output release follows the bounded hyperlink transaction.

No production test helper writes raw OSC 8 on behalf of DCurses; raw bytes are inspected only as integration evidence that Terminal performed the framing.

## Failure behavior

Any exception escaping the linked semantic output path flows through the existing refresh failure boundary.

That boundary invalidates:

- physical cell knowledge;
- retained physical semantic metadata;
- current rendition knowledge;
- physical cursor knowledge.

Terminal remains responsible for its own bounded hyperlink cleanup/retry semantics. T1106 adds focused failure injection around hyperlink begin/content/end and combined restoration failures.

## Public API impact

T1104 adds **no new public API**.

The provisional public 1.1 fingerprint therefore remains:

```text
sha256:            d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
exported types:    45
contract lines:   337
```

## Exit criteria

T1104 is complete when one documentation-synchronized `1.1.0-alpha.4` head passes the complete seven-job PR gate and proves:

1. semantic-only changes are physically observable;
2. equivalent linked runs are coalesced;
3. linked output is emitted only through Terminal's typed bounded hyperlink operation;
4. physical retained state includes semantic metadata and invalidates conservatively;
5. unlinked/no-op behavior remains correct;
6. wide cells are not duplicated;
7. synchronized-output composition is non-deadlocking and correctly ordered;
8. unsafe physical edit/erase shortcuts are disabled while semantic state exists;
9. no public API delta beyond T1103 appears.

After that, T1105 may propagate semantic metadata through the complete logical editing/copy/overlay/pad/viewports model.
