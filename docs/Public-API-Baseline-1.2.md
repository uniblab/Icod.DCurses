# Icod.DCurses 1.2 Public API Baseline

**Release line:** `1.2.0`  
**Compatibility floor:** accepted `1.1.0` contract  
**AssemblyVersion:** `1.0.0.0`  
**Release-candidate package identity:** `1.2.0-rc.1`  
**Compiler-derived API head:** `866497c9d5015f3a149580d67eacefaf7e121aaf`  
**API qualification workflow:** #600 / `34524054785`  
**T1209 closure head:** `3728bf0e576b32747dd3a628ed5d3eca768ac67f`  
**T1209 closure workflow:** #603 / `34525966166`  

## Accepted 1.1 floor

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

## Accepted 1.2 candidate contract

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

The compiler-derived fingerprint is identical on `net8.0`, `net9.0`, and `net10.0`.

Machine-readable authority: `docs/Public-API-Fingerprint-1.2.json`.

## Intentional exported-type additions

Exactly two exported types are added over 1.1:

```text
Icod.DCurses.CursesPanel
Icod.DCurses.CursesPanelTransparency
```

No accepted 1.1 exported type is removed.

## Public panel contract

`CursesScreen.CreatePanel(int row, int column, int rows, int columns)` creates one screen-owned independent retained panel. The panel exposes its retained content through `CursesWindow ContentWindow` and exposes destination position, fixed dimensions, visibility, and transparency policy.

The public manipulation/lifetime surface consists of:

```text
Show()
Hide()
MoveTo(int row, int column)
MoveToTop()
MoveToBottom()
MoveAbove(CursesPanel sibling)
MoveBelow(CursesPanel sibling)
Dispose()
```

`CursesPanel` implements `IDisposable`. Disposal is one-way: it removes the panel from its owning screen, is idempotent, and rejects later manipulation. It does not introduce panel transfer or reattachment semantics.

`CursesPanelTransparency` contains the accepted closed values:

```text
Opaque
BlankCellsTransparent
```

Opaque is the default. Blank-cell transparency causes ordinary blank panel cells to contribute neither visual cell state nor semantic metadata to composition.

## Compatibility decisions

- Existing `CursesWindow` instances remain shared views into their owning logical screen; they do not become independent retained layers.
- `AssemblyVersion` remains `1.0.0.0` for this compatible additive release.
- Panel dimensions remain fixed in 1.2; general layout/resize belongs to 1.3.
- No public panel-stack enumeration is frozen in 1.2.
- No new `Icod.Terminal` or `Icod.TermInfo` protocol/routing type enters the public DCurses surface.
- Existing semantic metadata, Unicode width, wide-cell, pad, editing, input, lifecycle, and refresh contracts remain compatible.

## Qualification

The lifetime/API regret correction was first exercised at `ed21d00950ff3992628bf825625b62e24d957c49`, where 553 behavioral/compatibility tests passed per target framework and the only failure was the intentionally stale provisional fingerprint.

After updating the machine-readable fingerprint, exact head `866497c9d5015f3a149580d67eacefaf7e121aaf` passed workflow #600 / `34524054785` across all seven jobs.

The documentation/sample/package-complete T1209 head `3728bf0e576b32747dd3a628ed5d3eca768ac67f` then passed workflow #603 / `34525966166` across all seven jobs. The Linux ARM64 evidence leg reported 554/554 tests on each supported TFM with a zero-warning/zero-error build, and package-only panel consumption passed.

The `1.2.0-rc.1` promotion changes release identity/status documentation only. This baseline becomes the stable 1.2 public-contract authority only if T1210 preserves this fingerprint through RC and final stable-source qualification.
