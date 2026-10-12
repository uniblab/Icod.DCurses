# T2500 — Color-Surface Baseline and Identity

## Result

Icod.DCurses development identity is `2.5.0-alpha.1`. `AssemblyVersion` remains `2.0.0.0`, the public 2.4 color model remains the foundation, and the production dependency boundary remains one direct reference to Icod.Terminal 1.29.0.

## Exact baseline

- Published baseline tag: `v2.4.0`
- Published baseline commit: `75a205d38a5a3b45fa2d5197a2a3564f139723c2`
- Planning head inherited by implementation: `8f9201ab3879b0559a8af33d9434e793426ecce5`
- 2.4 public API fingerprint: `87a3ffe66174634f6db4044b914b05fd31d117b93ecf5b1a151d31d59732e65e`
- Exported types: 109
- Contract lines: 857

The production graph is `Icod.DCurses -> Icod.Terminal 1.29.0 -> Icod.TermInfo`. Icod.DCurses has no direct production reference to Icod.TermInfo. Test-only references remain outside the production graph.

## Contract inventory

| Design invariant | Existing evidence | 2.5 closure |
| --- | --- | --- |
| Default, indexed, and RGB color values remain independent of terminal encoding | `CursesColorStyleTests`, `CursesTerminalScreenMapperTests` | T2501 and T2504 matrices |
| A cell retains complete foreground, background, and attributes | `CursesWindowTests`, `CursesVirtualScreenTests`, `CursesRasterLogicalSurfaceTests` | T2501 mutation-family matrix |
| Background and repair operations use the complete `BackgroundCell` style | `CursesEraseResolverTests`, `CursesWindowEditingTests` | T2501 background and wide-cell tests |
| Equal-cell assignment is a retained no-op while any style-axis change creates damage | `CursesVirtualScreenTests`, `CursesRefreshPhysicalStateTests` | T2501 and T2503 explicit axis tests |
| Pad, viewport, layout, copy, and overlay projection retain style | `CursesLayoutUnicodeResizeTests`, `CursesPanelRasterCompositionTests` | T2501 and T2502 projection matrices |
| Opaque panels cover lower content with styled blanks | `CursesPanelCompositionTests`, `CursesPanelUnicodeCompositionTests` | T2502 opaque styled-blank witness |
| `BlankCellsTransparent` remains style-insensitive | `CursesPanelCompositionTests`, `CursesPanelIncrementalCompositionTests` | T2502 compatibility witness |
| Foreground-only and background-only changes reach physical refresh | `CursesRefreshEngineTerminalTests`, `CursesRenditionTransitionTests` | T2503 transition matrix |
| Unsupported colors degrade to terminal defaults without quantization | `CursesPresentationResolverTests` | T2504 capability matrix |
| Reverse remains an attribute rather than a stored color swap | `CursesPresentationResolverTests`, `CursesRenditionTransitionTests` | T2504 reverse witness |
| Invalidation, resize, and lifecycle repaint restore requested logical styles | `CursesOutputFailureHardeningTests`, `CursesPanelResizeTests`, `CursesTerminalIntegrationTests` | T2503 and T2504 lifecycle matrices |
| Public-only consumers can select and display capability-appropriate palettes | No complete 2.4 sample witness | T2505 public-only sample and package consumer |

Existing tests establish useful baselines but are not treated as complete 2.5 acceptance. The named T2501–T2505 witnesses close the permanent gaps.

## Red/green evidence

The new identity test was first run against unchanged 2.4 metadata. All dependency and public-signature assertions completed before the expected collection mismatch:

```text
Expected: ["2.5.0-alpha.1", "2.5.0-alpha.1"]
Actual:   ["2.4.0", "2.4.0"]
Failed: 1, Passed: 0, Total: 1
```

The test runner in this restricted environment cannot create MSBuild worker named pipes, so local commands use `-m:1`. This changes build scheduling only; it does not change project configuration or runtime behavior.

Final verification results are recorded below after the identity change:

- Focused `PublicTwoFiveDevelopmentIdentityTests`: passed, 1/1
- Staging solution build: passed, 0 warnings and 0 errors
- Complete net10.0 test project: passed, 1,417/1,417

Two historical guards were corrected during the full-suite run. The 2.3 documentation guard now reads the versioned 2.3 roadmap instead of freezing obsolete current-roadmap status, and the 2.4 identity guard now verifies its published fingerprint plus the continuing assembly/dependency boundary instead of preventing the approved 2.5 development identity.
