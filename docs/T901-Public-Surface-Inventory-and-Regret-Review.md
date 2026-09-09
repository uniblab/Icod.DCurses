# T901 — Public Surface Inventory and Initial Regret Review

**Release line:** `0.9.0`  
**Checkpoint:** `0.9.0-alpha.1`  
**Merged source baseline:** `0.8.0`  
**Dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Status:** initial inventory accepted; T902 fingerprint guard established

## Inventory result

The compiled `Icod.DCurses` assembly exports **43 public types**. The initial canonical fingerprint contains **309 declared contract lines** and has SHA-256:

```text
274b87ec28a253e4891f7f72dea847eaf7d57f45e7b6dd2ae4b464e783046639
```

The same SHA was produced by `net8.0`, `net9.0`, and `net10.0` in the first T902 Linux validation run.

The machine-readable baseline is `docs/Public-API-Fingerprint-0.9.json`; `PublicApiFingerprintTests` regenerates the canonical contract from the compiled assembly and fails when it changes without a deliberate baseline update.

## Public type families

### Session and ownership

- `CursesSession`
- `CursesSessionOptions`
- `CursesScreenResizedEventArgs`

### Logical surfaces and geometry

- `CursesScreen`
- `CursesVirtualScreen`
- `CursesWindow`
- `CursesPad`
- `CursesPadViewport`
- `CursesWrapMode`

### Cells, text, color, rendition, and drawing

- `CursesCell`
- `CursesColor`
- `CursesColorKind`
- `CursesStyle`
- `CursesText`
- `CursesTextAttributes`
- `CursesLineGlyph`
- `CursesPresentationCapabilities`
- `CursesAmbiguousWidthPolicy`
- `ICursesTextWidthProvider`
- `UnicodeCursesTextWidthProvider`
- `CursesCursorVisibility`
- `CursesAlertKind`

### Input, lifecycle, and protocol facade

- `CursesEvent`
- `CursesEventKind`
- `CursesInputEvent`
- `CursesInputEventKind`
- `CursesKey`
- `CursesKeyEventPhase`
- `CursesKeyModifiers`
- `CursesInputMode`
- `CursesInputProtocolLease`
- `CursesInputProtocolOptions`
- `CursesKeyboardReportingMode`
- `CursesLifecycleEvent`
- `CursesLifecycleEventKind`
- `CursesFocusEvent`
- `CursesFocusState`
- `CursesMouseAction`
- `CursesMouseButton`
- `CursesMouseEvent`
- `CursesMouseTrackingMode`
- `CursesPasteEvent`
- `CursesPastePhase`

## Initial regret decision

No exported type is an obvious hardening helper, test seam, output-cost implementation detail, synchronization primitive, transport abstraction, native backend, or private Terminal/TermInfo replacement.

The type families correspond to public API introduced and reviewed deliberately in the 0.1-0.8 milestone baselines. Therefore T901 does **not** remove or rename a public type merely to create churn before 1.0.

This is not the end of the regret audit. T902-T905 review members, semantic values, exceptions, nullable annotations, XML documentation, mutability, constructors, and upstream type exposure. A breaking cleanup remains permitted through T905 when a concrete 1.x problem is identified; any such cleanup must update the fingerprint and migration record in the same tranche.

## Upstream dependency boundary

The existing `PublicDependencyBoundaryTests` allow exactly these upstream public type definitions:

```text
Icod.Terminal.TerminalControlResult<T>
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalSession
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

T905 will make the final keep/remove decision for each exposure. No additional Terminal/TermInfo type may enter the public signature surface accidentally.

## Public-change rule from this point

After this inventory, every intentional public API change must include:

1. the code change;
2. an explicit regret rationale;
3. an updated `Public-API-Fingerprint-0.9.json`;
4. affected semantic/dependency tests;
5. migration documentation when source or behavior compatibility changes.

`1.0.0` is not a deferred breaking-cleanup tranche.
