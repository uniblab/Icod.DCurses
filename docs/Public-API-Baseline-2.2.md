# Icod.DCurses 2.2 Development Public API Baseline

**Candidate identity:** `2.2.0-alpha.1`\
**Status:** T2202 development increment; final release API freeze pending\
**AssemblyVersion:** `2.0.0.0`\
**Direct production dependency:** `Icod.Terminal 1.18.0`

## Compiled development fingerprint

```text
97 exported types
756 canonical declared contract lines
sha256 06b8e57805004a8a7edc3ce787af86ca7e3d96c7ea7beecbd1ab541e744e1f2c
```

`Public-API-Fingerprint-2.2.json` is the mutable development fingerprint until the T2207 release API freeze. CI measured the above values from the compiled assembly on .NET 8, 9 and 10 at implementation head `79f9422e4f37ae9dbe85ed42ffb5edd373d9b7d1`; the later ordering fix changes no public API. The published `Public-API-Fingerprint-2.1.json` remains immutable (96 types, 751 lines, hash `c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79`).

| Surface | Additive 2.2 development delta |
|---|---|
| New type | `CursesCommandBinding` |
| Constructor | `CursesCommandBinding(CursesKeyGesture gesture, CursesCommand command)` |
| Properties | `CursesCommandBinding.Gesture`, `CursesCommandBinding.Command` |
| Router method | `CursesInteractionRouter.GetEffectiveGestureBindings()` |

The four added members plus the type add five canonical contract lines. The new opt-in method returns a detached read-only snapshot ordered by routing owner precedence and semantic gesture identity. Ordinary `Route` and its public signatures are unchanged. T2203 sequence composition will extend this candidate fingerprint after a separate API amendment and tests-first gate; T2207 reviews and freezes the final 2.2 contract.
