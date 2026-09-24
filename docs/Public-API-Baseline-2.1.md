# Icod.DCurses 2.1 Public API Baseline

**Candidate package identity:** `2.1.0-rc.1` (unpublished).\
**AssemblyVersion:** `2.0.0.0`.\
**Direct production dependency:** `Icod.Terminal 1.18.0`.\
**Target frameworks:** `net8.0`, `net9.0`, `net10.0`.\
**Status:** T2111 API contract accepted; T2112 RC qualification in progress.

## Compiled fingerprint

```text
96 exported types
751 canonical declared contract lines
sha256 c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79
```

`Public-API-Fingerprint-2.1.json` is the machine-readable release-candidate authority, guarded by `PublicApiFingerprintTests`. Compared with the immutable 2.0 fingerprint (75 types, 559 lines), the 2.1 fingerprint adds 21 exported types and removes none. The full declared-member delta was reviewed under the T2111 gate; the 2.0 assembly identity and direct dependency remain unchanged.

| Surface | New exported types |
| --- | --- |
| Text layout and geometry | `CursesTextAffinity`, `CursesTextAlignment`, `CursesTextFragment`, `CursesTextHitTestResult`, `CursesTextLayout`, `CursesTextLayoutOptions`, `CursesTextOverflow`, `CursesTextPosition`, `CursesTextSelection`, `CursesTextSpan`, `CursesTextVisualLine`, `CursesTextVisualPosition`, `CursesTextWrapMode` |
| Visible-content geometry and stateless tracks | `CursesCellPosition`, `CursesViewport`, `CursesTrack`, `CursesTrackDistribution`, `CursesTrackKind` |
| Bounded opt-in diagnostics | `CursesRefreshDiagnosticsSnapshot`, `CursesRefreshOperationKinds`, `CursesRefreshOutcome` |

The 2.1 additions also extend existing window/screen/session APIs with retained layout projection, prepared bulk cell writes and diagnostic observation. Text content, document edits, world generation, navigation, prompts and event loops remain application-owned. The editor and roguelike samples exercise these APIs without adding a reusable editor or game engine to the library.

The prior 2.0 fingerprint and baseline remain historical evidence and must not be rewritten for this release.
