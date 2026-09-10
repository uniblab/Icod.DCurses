# T1209 — Public API, Package, Documentation, and Regret Gate

**Release:** `Icod.DCurses 1.2.0`  
**Tranche:** T1209  
**API/lifetime qualified head:** `866497c9d5015f3a149580d67eacefaf7e121aaf`  
**API workflow:** #600 / `34524054785`  
**Documentation/sample/package closure head:** `3728bf0e576b32747dd3a628ed5d3eca768ac67f`  
**Closure workflow:** #603 / `34525966166`  
**Status:** complete  

## Purpose

T1209 freezes the public 1.2 panel contract after application/resource acceptance and audits ownership, source compatibility, nullability, dependency boundaries, package consumption, samples, and current documentation before RC promotion.

## Accepted compatibility floor

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

The test-only red head `b800fa1de8a1986f2680bab1104dbeb45058a3fb` failed because `Dispose()` and `IDisposable` did not yet exist. The implemented lifetime head `ed21d00950ff3992628bf825625b62e24d957c49` then passed 553 behavioral/compatibility tests per target framework; the sole remaining failure was the deliberately stale provisional fingerprint.

## Accepted 1.2 API

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
- `MoveTo` continues to require a fully contained destination rectangle, consistent with existing window geometry; later destination resize may clip retained panel content.
- `CursesPanelTransparency` remains the small closed policy surface `Opaque` / `BlankCellsTransparent`.
- The panel API adds no new `Icod.Terminal` or `Icod.TermInfo` public type exposure.
- `IDisposable` is a BCL lifetime interface and does not change the approved lower-layer dependency boundary.

## Package, sample, and documentation closure

The fresh NuGet-only consumer now validates panel creation, retained content, hide/show, movement, transparency, ordering, `IDisposable`, idempotent disposal, and use-after-dispose rejection.

`Icod.DCurses.Panel.Sample` demonstrates the public live-session panel path without using internal compositor APIs or raw Terminal protocol framing.

The root README, sample index, current development roadmap, 1.1–1.4 release train, dedicated 1.2 roadmap, package release notes, package-smoke documentation, and human-readable 1.2 API baseline were synchronized to the active 1.2 source and `Icod.Terminal 1.8.1` / `Icod.TermInfo 1.10.0` dependency declarations. Historical 1.0/1.1 tranche records remain unchanged as historical evidence.

Exact closure head:

```text
3728bf0e576b32747dd3a628ed5d3eca768ac67f
workflow #603 / 34525966166
```

passed package validation and Windows/Linux/macOS x64/ARM64 runtime jobs. The Linux ARM64 leg reported:

```text
Build: 0 warnings, 0 errors
net8.0:  554 passed, 0 failed
net9.0:  554 passed, 0 failed
net10.0: 554 passed, 0 failed
```

The focused panel sample built for all three TFMs and package-only panel validation executed successfully.

## Decision

T1209 is complete. The implementation/API is frozen for T1210. RC and stable-source promotion may change only release identity and release-status documentation unless a new blocker is discovered.
