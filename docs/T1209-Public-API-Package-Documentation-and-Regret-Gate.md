# T1209 — Public API, Package, Documentation, and Regret Gate

**Release:** `Icod.DCurses 1.2.0`  
**Tranche:** T1209  
**API/lifetime qualified head:** `866497c9d5015f3a149580d67eacefaf7e121aaf`  
**Workflow:** #600 / `34524054785`  
**Status:** documentation/sample/package closure in progress  

## Purpose

T1209 freezes the public 1.2 panel contract only after application/resource acceptance, then audits ownership, source compatibility, nullability, dependency boundaries, package consumption, samples, and current documentation before RC promotion.

## Accepted compatibility floor

The accepted 1.1 contract remains the compatibility floor:

```text
45 exported types
337 canonical declared contract lines
sha256 21dff2e57d8bbc9b2f0e40aa4ee4dfd575dd765d02d0f670424c93b2bdc1c039
```

## Lifetime regret found and corrected

The pre-freeze audit found one material defect in the provisional design: `Hide()` removed a panel from composition but left it permanently retained by the owning screen's panel order. A long-lived screen repeatedly creating one-shot dialogs, completion popups, or transient notifications could therefore retain every panel until screen lifetime ended.

The accepted correction is:

```csharp
public sealed class CursesPanel : IDisposable
```

`Dispose()` permanently removes the panel from its owning screen's order. It is idempotent, sets the panel non-visible, and later panel manipulation throws `ObjectDisposedException`. Other panels cannot order themselves relative to a disposed panel. Reattachment and transfer are intentionally outside the 1.2 contract.

A test-only red head `b800fa1de8a1986f2680bab1104dbeb45058a3fb` failed because `Dispose()` and `IDisposable` did not yet exist, giving the intended TDD signal.

The implemented lifetime head `ed21d00950ff3992628bf825625b62e24d957c49` built cleanly and passed 553 behavioral/compatibility tests on each target framework; its sole test failure was the deliberately stale provisional API fingerprint.

## Final 1.2 candidate API

The compiler-derived replacement fingerprint is identical on net8.0, net9.0, and net10.0:

```text
47 exported types
356 canonical declared contract lines
sha256 4810ebb088764acedbb94aca84b231677886b9c1a1f920d9a30f960cbe1dfce7
```

Exactly two exported types are added over 1.1:

- `CursesPanel`;
- `CursesPanelTransparency`.

The fingerprint-complete API/lifetime head `866497c9d5015f3a149580d67eacefaf7e121aaf` passed workflow #600 / `34524054785` across all seven jobs.

## Other regret decisions

No further public API change was accepted.

- Ordinary `CursesWindow` shared-view behavior remains unchanged.
- Panel dimensions remain fixed in 1.2; general layout/resize primitives belong to 1.3.
- Public panel-stack enumeration is not added without a demonstrated consumer.
- `MoveTo` continues to require a fully contained destination rectangle, consistent with existing window geometry. Destination resize may subsequently clip a retained panel without mutating its content.
- `CursesPanelTransparency` remains the small closed policy surface `Opaque` / `BlankCellsTransparent`.
- The panel API adds no new `Icod.Terminal` or `Icod.TermInfo` public type exposure.
- `IDisposable` is a BCL lifetime interface and does not change the approved lower-layer dependency boundary.

## Package and sample closure

T1209 extends the fresh NuGet-only consumer with runtime panel-surface validation covering creation, retained content, hide/show, movement, transparency, top/bottom manipulation, `IDisposable`, idempotent disposal, and use-after-dispose rejection.

A focused `Icod.DCurses.Panel.Sample` demonstrates the public live-session path: retained base content remains available while a panel is hidden, shown/moved, switched to blank-cell transparency, and finally disposed. The sample uses no internal compositor API and emits no raw Terminal protocol framing.

The root README, sample index, current development roadmap, approved 1.1–1.4 release train, dedicated 1.2 roadmap, package release notes, and human-readable 1.2 API baseline are synchronized with the current 1.2 source identity and `Icod.Terminal 1.8.1` / `Icod.TermInfo 1.10.0` declarations.

Historical 1.0/1.1 tranche records remain historical and are not rewritten to pretend their original dependency/version checkpoints were different.

## Closure rule

This documentation/sample/package closure produces a new exact head. T1209 is complete only when that exact head passes the full seven-job PR matrix, including package validation and Windows/Linux/macOS x64/ARM64 runtime validation.

After that qualification, T1210 may promote the unchanged source through a release candidate. Merge, tag, GitHub Release creation, and NuGet publication remain explicit later actions.
