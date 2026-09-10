# Icod.DCurses 1.2 Public API Baseline

**Release:** `1.2.0`  
**Compatibility floor:** accepted `1.1.0` contract  
**AssemblyVersion:** `1.0.0.0`  
**Stable-source package identity:** `1.2.0`  
**API qualification head:** `866497c9d5015f3a149580d67eacefaf7e121aaf`  
**T1209 closure head:** `3728bf0e576b32747dd3a628ed5d3eca768ac67f`  
**Qualified RC head:** `8c5d329fa195685c0349068ce33a100aaf9eb0a3`  
**RC workflow:** #604 / `34526810086`  

## Accepted 1.1 floor

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

## 1.2 contract

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

The fingerprint is identical on `net8.0`, `net9.0`, and `net10.0`. Machine-readable authority: `docs/Public-API-Fingerprint-1.2.json`.

Exactly two exported types are added over 1.1:

```text
Icod.DCurses.CursesPanel
Icod.DCurses.CursesPanelTransparency
```

No accepted 1.1 exported type is removed.

## Public panel contract

`CursesScreen.CreatePanel(int row, int column, int rows, int columns)` creates one screen-owned independent retained panel. `CursesPanel.ContentWindow` exposes ordinary `CursesWindow` editing over the private retained surface.

The stable candidate provides `Show`, `Hide`, `MoveTo`, `MoveToTop`, `MoveToBottom`, `MoveAbove`, `MoveBelow`, `Transparency`, and deterministic one-way `Dispose` removal. `CursesPanel` implements `IDisposable`; disposal is idempotent and later manipulation is rejected.

`CursesPanelTransparency` contains `Opaque` and `BlankCellsTransparent`. Opaque is the default.

Existing `CursesWindow` shared-view semantics remain unchanged. Panel dimensions remain fixed in 1.2; general layout/resize belongs to 1.3. No public panel-stack enumeration is frozen. No new `Icod.Terminal` or `Icod.TermInfo` protocol/routing type enters the public surface.

## Qualification chain

- API/lifetime fingerprint: `866497c9d5015f3a149580d67eacefaf7e121aaf`, workflow #600 / `34524054785`, seven jobs green.
- Documentation/sample/package closure: `3728bf0e576b32747dd3a628ed5d3eca768ac67f`, workflow #603 / `34525966166`, seven jobs green.
- Release candidate `1.2.0-rc.1`: `8c5d329fa195685c0349068ce33a100aaf9eb0a3`, workflow #604 / `34526810086`, seven jobs green; package validation passed and Linux ARM64 reported 554/554 tests on each supported TFM with zero build warnings/errors.

The stable-source `1.2.0` promotion carries this exact implementation/API forward and requires its own full exact-head qualification before merge or publication.
