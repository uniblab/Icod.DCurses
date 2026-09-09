# Icod.DCurses 0.7.0 — T707 Rendition and Refresh-State Minimization

**Release line:** `0.7.0`  
**Checkpoint:** `0.7.0-alpha.7`  
**Stable predecessor:** `0.6.0`  
**Dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** T707 implemented; alpha.7 validation active

## Objective

T707 reduces redundant physical rendition transitions without weakening the reset-first correctness model frozen in 0.6.

The optimization remains entirely internal. Logical `CursesStyle` semantics, presentation degradation, session lifecycle, and the public API are unchanged.

## Accepted transition model

`CursesRefreshEngine` continues to store the last physical style that DCurses knows it successfully established.

Transitions are now divided into safe monotonic changes and changes that require normalization.

### Unknown physical rendition

Unknown state remains reset-first:

1. emit `sgr0` when advertised;
2. restore the original/default color pair with `op` when advertised;
3. apply the resolved attributes;
4. apply resolved foreground/background colors;
5. record the resulting physical style only after successful output.

No optimization assumes state across invalidation or failed output.

### Identical resolved style

When consecutive logical styles resolve to the same physical `CursesStyle`, no rendition transition is emitted.

This includes logical attributes or colors that degrade to the same supported physical representation.

### Additive attributes

When a known style changes only by adding attributes, DCurses emits only the new attribute selectors. Existing active attributes and colors are retained.

Example:

```text
Bold -> Bold | Underline
```

emits only the underline selector between the two payload runs.

### Non-default color changes

When a known non-default foreground or background changes directly to another non-default value, DCurses emits only the changed color selector.

The opposite color channel and active attributes are retained.

### Attribute removal

Removing an attribute remains reset-first. DCurses uses the established attribute-reset path, restores the default color pair, and reapplies the complete resolved desired style.

This preserves correctness on terminals where `sgr0` is the only safe way to remove one or more active attributes.

### Return to terminal-default color

A transition from a non-default color channel to terminal default uses `op`, because DCurses does not synthesize a private default-color escape sequence. The resolved desired style and any remaining non-default color channel are then re-established conservatively.

## Measured T701 comparison

The deterministic T701 style baseline was:

```text
bold cell after established default baseline
19 bytes / 4 writes / 1 flush
```

With T707, the same logical update is:

```text
13 bytes / 3 writes / 1 flush
```

This is a reduction of 6 bytes (31.6%) and one terminal write (25%) for that workload, with the same final physical style and cell content.

The other T701 reference workloads remain pinned:

- clean no-op refresh: `0 bytes / 0 writes / 1 flush`;
- one ASCII cell with absolute `cup`: `10 bytes / 2 writes / 1 flush`;
- one wide cell: `12 bytes / 2 writes / 1 flush`.

## Safety and failure behavior

The normal refresh exception boundary is unchanged. A failure during a rendition transition invalidates retained physical-screen, cursor, and rendition knowledge before the exception escapes. The next refresh therefore returns through the ordinary reset-first/unknown-state path.

T707 does not add cursor side-effect assumptions, terminal-name heuristics, or private control sequences.

ACS grouping remains unchanged: alternate-character-set entry/exit is still managed around line-glyph payload runs and is not folded into the rendition cache.

## Focused coverage

Tests now pin:

- additive attribute transitions without redundant `sgr0`/`op`;
- direct non-default color changes without redundant reset/default-color restoration;
- attribute removal retaining reset-first safety;
- return of one color channel to default while preserving/reapplying the other channel;
- distinct logical styles that resolve to the same physical style producing no redundant transition;
- the T701 19-byte bold baseline improving deterministically to 13 bytes.

## Public API result

T707 adds no public API and no new Terminal or TermInfo type leakage.

## Gate

T707 is complete when the exact `0.7.0-alpha.7` head passes Windows, Linux, macOS, and canonical package/fresh-consumer validation.

After that gate, T708 performs the benchmark, API, package, and optimization-regret review before `0.7.0-rc.1` promotion.
