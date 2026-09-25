# Icod.DCurses 2.2 Stable-Source Public API Baseline

**Candidate identity:** `2.2.0`\
**Status:** T2208 stable-source qualification in progress\
**AssemblyVersion:** `2.0.0.0`\
**Direct production dependency:** `Icod.Terminal 1.18.0`

## Compiled stable-source fingerprint

```text
100 exported types
783 canonical declared contract lines
sha256 7c9866abaeeacc7f64631d2a800b91333cee72ad1a53872896e8f4d8c7ccb097
```

`Public-API-Fingerprint-2.2.json` is the frozen 2.2 fingerprint. CI requires the compiled assembly to match it on .NET 8, 9 and 10. The published `Public-API-Fingerprint-2.1.json` remains immutable (96 types, 751 lines, hash `c988806ddc19834c01ea7bd75630b257f4c55026feb73bfbbf7ebfd8978bbb79`).

| Surface | Additive 2.2 delta |
|---|---|
| New type | `CursesCommandBinding` |
| Constructor | `CursesCommandBinding(CursesKeyGesture gesture, CursesCommand command)` |
| Properties | `CursesCommandBinding.Gesture`, `CursesCommandBinding.Command` |
| Router method | `CursesInteractionRouter.GetEffectiveGestureBindings()` |

T2203 adds three exported types and 27 canonical contract lines: immutable
`CursesCommandSequenceBinding` and `CursesCommandSequenceResult` values,
`CursesCommandSequenceResultKind`, five public limits, region/scope/global
sequence registration, explicit processing/cancellation state and effective
sequence discovery. Sequence snapshots are detached, read-only and ordered by
the same routing owner precedence and semantic gesture identity as processing.
Ordinary `Route` and its public signatures remain unchanged. T2207 freezes this
additive surface; any subsequent public API change requires reopening the gate
and regenerating the candidate fingerprint deliberately.
