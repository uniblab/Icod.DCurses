# Icod.DCurses Public API Baseline — 0.9

**Release line:** `0.9.0`  
**Stable predecessor:** `0.8.0`  
**Dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Assembly version:** `0.9.0.0`  
**Status:** final pre-1.0 public-contract baseline

## Purpose

`0.9.0` is the contract-freeze release immediately before `1.0.0` stable closure. It introduces no new feature family and accepts no public API change relative to the 0.8 public surface.

The machine-readable authoritative fingerprint is stored in `docs/Public-API-Fingerprint-0.9.json` and guarded by `PublicApiFingerprintTests`.

## Canonical fingerprint

```text
sha256:            274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
exported types:    43
contract lines:   309
```

The same canonical fingerprint is produced for `net8.0`, `net9.0`, and `net10.0`.

The fingerprint covers declared public types and members, enum underlying types and numeric values, constructors, fields/constants, properties/indexers, events, methods/operators, generic constraints, parameter order/type/ref-kind/default values, accessor visibility, and compiled nullable metadata.

## Public type families

The 43 exported types remain grouped into the established contract families:

- session/ownership: `CursesSession`, `CursesSessionOptions`, `CursesScreenResizedEventArgs`;
- logical surfaces/geometry: `CursesScreen`, `CursesVirtualScreen`, `CursesWindow`, `CursesPad`, `CursesPadViewport`, `CursesWrapMode`;
- cells/text/presentation: `CursesCell`, `CursesColor`, `CursesColorKind`, `CursesStyle`, `CursesText`, `CursesTextAttributes`, `CursesLineGlyph`, `CursesPresentationCapabilities`, `CursesAmbiguousWidthPolicy`, `ICursesTextWidthProvider`, `UnicodeCursesTextWidthProvider`, `CursesCursorVisibility`, `CursesAlertKind`;
- input/lifecycle/protocol facade: the established curses event, input, key, modifier, focus, mouse, paste, lifecycle, input-mode, keyboard-reporting, protocol-options, and protocol-lease types.

No hardening helper, lock/scheduler, refresh-cost resolver, retained physical-state object, transport abstraction, diagnostic record, test seam, or lower-level mode/parser implementation is exported.

## Semantic freeze

Dedicated tests freeze compatibility-sensitive behavior not fully represented by reflection:

- coordinates are zero-based and row/column ordered;
- subwindow origins are parent-local;
- rows/columns mean height/width;
- invalid geometry uses the established managed exception categories and parameter identities;
- column-oriented slices are half-open and never split a two-column element;
- Unicode width data remains version 17.0.0;
- East Asian Ambiguous characters are narrow by default and wide only through the explicit wide-Ambiguous provider;
- wide-cell leader/continuation footprints are repaired across overwrite and resize boundaries;
- semantic line glyph identity remains distinct from ordinary Unicode box-drawing text.

## Lifetime, ownership, cancellation, and failure freeze

The 0.8 hardening model is accepted as the 1.x model:

- logical screen/window/pad/viewport mutation is single-writer unless explicitly documented otherwise;
- one Terminal-owned event consumer may wait concurrently with serialized refresh/output activity;
- supplied `TerminalSession` ownership transfers only after successful DCurses initialization;
- caller cancellation remains cancellation and preserves the caller token where DCurses creates the cancellation exception;
- disposal-unblocked public waits surface `ObjectDisposedException`;
- repeated disposal shares one restoration operation;
- uncertain/partial output invalidates retained physical knowledge for safe retry;
- independently meaningful primary/restoration failures are preserved;
- Terminal remains the authoritative live-session/mode/input/lifecycle owner.

## Approved Terminal/TermInfo public boundary

The final lower-layer type allow-list is exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

These exposures are intentional advanced integration/identity/result seams. No additional Terminal or TermInfo type is accepted into the 1.x DCurses public surface.

## Nullable and documentation contract

Nullable reference types remain enabled. Compiled public nullability participates in the canonical fingerprint.

XML documentation generation remains enabled and package validation requires XML documentation beside the `net8.0`, `net9.0`, and `net10.0` library assemblies. Conceptual ownership, geometry, Unicode, presentation, refresh, lifetime, cancellation, package, and migration guidance is maintained in the README and the 0.9 migration/contract documents.

## Migration result from 0.8

No public breaking cleanup was accepted during T901–T905. Existing 0.8 source consumers are therefore expected to compile against the 0.9 public contract without a source migration.

The principal 0.9 change is contract enforcement: accidental public-signature, enum-value, nullability, dependency-boundary, and selected semantic changes now fail machine gates.

## 1.0 rule

`1.0.0` is release closure over this baseline. Any deliberate breaking change after stable `0.9.0` requires a new compatibility decision rather than being treated as routine 1.0 cleanup.
