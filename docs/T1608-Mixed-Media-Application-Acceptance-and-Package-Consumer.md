# T1608 — Mixed-Media Application Acceptance and Package Consumer

## Status

**Accepted.** Functional/package acceptance completed during T1608 and the sample was expanded during T1611 release audit to include retained hyperlink metadata beside text/raster presentation.

T1608 validates the Icod.DCurses 1.6 retained mixed-media public surface from two independent consumer perspectives:

1. an application-shaped repository sample built only from public DCurses/Terminal APIs; and
2. an isolated package-only consumer restored from the generated Icod.DCurses 1.6 artifact on `net8.0`, `net9.0`, and `net10.0`.

T1608 adds no production-library API and does not change the 1.6 package dependency graph.

## Accepted architectural boundary

The application sample uses:

- `TerminalRasterImage` as the backend-neutral source-image input;
- `CursesSession.CreateRasterResourceAsync(...)` for session-owned live raster ownership;
- `CursesRasterResource.CreatePlaceholderAsync(...)` and `CursesRasterPlaceholder.GetCell(...)` for placeholder semantics;
- `CursesWindow.WriteRasterCell(...)` for retained logical raster placement;
- `CursesWindow.WriteWithMetadata(...)` with `CursesHyperlink` for terminal-independent semantic metadata in the same retained pad;
- `CursesPad` and `CursesPadViewport` for off-screen retained media plus panning/projection;
- `CursesPanel` with blank-cell transparency for independent retained overlay composition;
- `CursesInteractionRouter` and regions over the same presentation geometry; and
- ordinary `CursesSession.RefreshAsync()` for the existing serialized physical refresh path.

The sample does **not** emit raw Kitty Graphics or Sixel commands, expose Terminal-private raster identities, create a hidden source-image cache, replay uploads after loss, rank backends, or silently switch protocols.

Terminal remains authoritative for live raster identity, acknowledgement, encoding, lifecycle, and protocol output. DCurses remains authoritative for logical cells, semantic metadata, retained raster coordinates, pads/viewports, panels, clipping, damage, composition, and refresh.

## RED evidence

The original T1608 repository-acceptance RED was committed at:

`bdb87e385ac26e4d596ece65017a67cbd3d9caf5`

Workflow **#971 / run `35045179997`** built successfully and failed at test execution because the required mixed-media sample did not yet exist. On Linux ARM64, each target framework reported:

```text
Failed: 1
Passed: 873
Total:  874
```

The sole failure was `CursesMixedMediaApplicationAcceptanceTests.RepositoryContainsMixedMediaSampleAndPackageOnlyRasterWitnesses`, with the explicit message that T1608 required the mixed-media sample project. This established the intended missing-acceptance-artifact RED rather than a compiler or unrelated regression failure.

## Sample implementation and compiler correction

T1608 added:

- `samples/Icod.DCurses.MixedMedia.Sample/Icod.DCurses.MixedMedia.Sample.csproj`
- `samples/Icod.DCurses.MixedMedia.Sample/Program.cs`
- solution registration for the sample across Debug/Staging/Release
- `tools/package-smoke/RasterSmoke.cs`
- package-consumer acceptance coverage in `CursesMixedMediaApplicationAcceptanceTests`

The first sample implementation used a nonexistent public `CursesWindow.SetCell(...)` helper in its label writer. CI correctly rejected that source. The helper was changed to the established public cursor/write path (`Move(...)` followed by `Write(...)`) at commit:

`efe84f2a3f71698a5adb61e456824a81256005b6`

No production API was added to accommodate the sample.

## Package-smoke regression and root cause

An intermediate T1608 attempt moved the established package-smoke top-level source from `tools/package-smoke/Program.cs` to another filename and replaced `Program.cs` with a manifest. That shape was incorrect for two existing contracts:

- `InteractionSampleProjectContractTests` intentionally reads the canonical `tools/package-smoke/Program.cs` and expects the accepted 1.5 interaction witnesses there; and
- both `.github/scripts/verify-release-package.sh` and `.github/scripts/verify-release-package.cmd` stage `Program.cs` as the isolated consumer entry point.

The resulting failures were therefore not raster-library failures: the existing interaction contract could not find its markers, and the isolated package consumer reported `CS5001` because its staged `Program.cs` had no top-level entry point.

The correction restored the original canonical `Program.cs`, removed the temporary duplicate, retained `RasterSmoke.cs` as a separate module-initializer witness, and changed both Unix and Windows package validators to stage `RasterSmoke.cs` beside `Program.cs`.

The corrected exact head is:

`9b1448739871751428d52881dd3c5ec1ed678c9a`

## GREEN qualification

Workflow **#979 / run `35141097323`** qualified exact head `9b1448739871751428d52881dd3c5ec1ed678c9a` across all seven jobs:

- Package candidate — success
- Runtime Windows x64 — success
- Runtime Windows ARM64 — success
- Runtime Linux x64 — success
- Runtime Linux ARM64 — success
- Runtime macOS x64 — success
- Runtime macOS ARM64 — success

Linux x64 built with:

```text
0 Warning(s)
0 Error(s)
```

and passed:

```text
net8.0   874 / 874
net9.0   874 / 874
net10.0  874 / 874
```

The package candidate independently verified package structure, metadata, dependency closure, assembly identity, XML documentation, and portable symbols. It then restored the generated package into a fresh isolated consumer and successfully compiled/executed that consumer on all three target frameworks.

`RasterSmoke.cs` executes inside that isolated consumer via a module initializer, so these runs also prove the packaged 1.6 raster facade and intentional `TerminalRasterImage` exposure compile and execute without repository project internals.

## Release-audit sample expansion

During T1611 publication audit, the sample was compared against the approved T1608 goal of a realistic **text + hyperlink + panel + raster-placeholder** composition. The accepted sample already covered text, raster, pads/viewports, panels, clipping, and interaction geometry, but did not yet include semantic hyperlink metadata.

The acceptance test was strengthened first to require both `WriteWithMetadata` and `CursesHyperlink`. Exact RED head `e386665af39a19c98671c3d97dac5a1c3028a124` built cleanly and workflow #1010 failed only because those sample witnesses were absent while package validation remained green.

The GREEN sample then added a retained project hyperlink inside the same pannable pad and a visible fallback message when raster resource/placeholder ownership is unavailable. The sample therefore demonstrates all three retained presentation axes together without changing production code or API:

```text
ordinary text/cell state
semantic hyperlink metadata
session-bound raster placeholder state
```

The sample continues rendering text/metadata/panel content when raster ownership is unavailable; it does not infer a terminal backend or silently switch protocols.

## TermInfo planning scope

The original T1608 plan allowed an optional `Icod.TermInfo.Inspection` raster-backend planning demonstration. During T1608 review, the maintainer chose to revisit the larger question of whether DCurses should communicate with TermInfo directly or route terminal-facing decisions entirely through Icod.Terminal in the **1.7 development track**.

Accordingly, the optional TermInfo planner demonstration is deferred rather than freezing another 1.6 example around a layering decision that is explicitly scheduled for reconsideration. This does **not** change 1.6 production dependencies: `Icod.DCurses 1.6.0` continues to declare `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0`.

## T1608 outcome

T1608 and the final release-audit sample expansion prove that the retained mixed-media API can be consumed in an application-shaped composition and from the packed artifact while preserving the established package-consumer and interaction contracts. The final sample shows retained text, semantic hyperlink metadata, raster placeholders, pad/viewports, panel composition, interaction geometry, and graceful raster-unavailable behavior without introducing a new production API, protocol identity, backend-specific command surface, replay/cache behavior, or hidden backend policy.
