# T12A — `watch` Application-Shaped Acceptance

**Project:** `Icod.DCurses`
**DCurses development version:** `0.1.0-Alpha-17`
**Acceptance driver:** `watch`
**Status:** Focused acceptance harness implemented; validation pending

---

## 1. Purpose

T12 requires the 0.1 DCurses contract to be exercised against the real shapes
needed by `watch`, `slabtop`, and `top`.

Alpha-17 begins that pass with `watch` because its event loop is the smallest of
the three while still exercising full-screen ownership, timer/input arbitration,
resize handling, retained refresh, styling, alert output, and application-level
layout policy.

The focused harness is:

```text
samples/Icod.DCurses.Watch.Acceptance
```

It uses synthetic command snapshots. Process creation, exit-status policy, and
ANSI interpretation remain application responsibilities and are intentionally
not moved into DCurses.

## 2. Roadmap coverage

The harness maps the T12 `watch` requirements as follows:

| Requirement | Alpha-17 evidence |
|---|---|
| repeated full-screen redraw | each synthetic command snapshot is rendered through the standard screen and `RefreshAsync` |
| title/no-title | `T` toggles whether the title rows participate in the layout |
| wrapping/clipping | `W` selects `CursesWrapMode.Wrap` or `CursesWrapMode.Clip` |
| highlighted differences | lines whose semantic text changed are rendered with `Reverse` added to their style |
| interpreted child colors | already-interpreted output segments carry semantic `CursesStyle` foreground colors |
| beep on command failure | `B` controls alert-on-failure and `F` forces a simulated failing snapshot |
| repaint after resize/resume | lifecycle repaint events invalidate the retained physical screen and redraw |
| preserve last presentation | `P` freezes the current snapshot across timer ticks until resumed |
| timer plus terminal events | one timed `ReadEventAsync` call arbitrates timer expiry, input, and lifecycle activity |

The harness never writes a terminal escape sequence directly.

## 3. Difference and color policy

Difference detection in the harness compares semantic output-line text from the
previous and current command snapshots. A changed line receives
`CursesTextAttributes.Reverse` in addition to any existing semantic style.

The synthetic child-output model contains already-interpreted colored segments.
That is deliberate. Parsing arbitrary ANSI emitted by a watched child remains
`watch` policy; DCurses receives only cells/text plus semantic styles.

## 4. Event-loop policy

The harness redraws only when state is dirty.

A normal timer interval is represented by:

```text
CursesSession.ReadEventAsync(TimeSpan)
```

A timeout advances the synthetic command snapshot. Keyboard or lifecycle activity
returns immediately through the same wait. No polling loop or separate timer
thread is introduced.

Space advances immediately. When preservation is enabled, timer expirations leave
the current presentation untouched.

## 5. Manual acceptance

Run:

```text
dotnet run --project samples/Icod.DCurses.Watch.Acceptance/Icod.DCurses.Watch.Acceptance.csproj
```

Verify:

1. the synthetic output advances once per interval;
2. changed lines are visibly distinguished when the terminal supports reverse
   rendition;
3. `T` changes between title and no-title layout;
4. `W` changes the long synthetic line between wrap and clip behavior;
5. `C` enables/disables semantic child colors;
6. `B` and `F` exercise failure-alert policy;
7. `P` freezes the current presentation across timer intervals;
8. Space refreshes immediately;
9. resizing repaints with the new dimensions;
10. `Q` exits and restores the host terminal normally.

Capability degradation is acceptable when the selected terminal genuinely lacks
the relevant rendition or alert capability. The session must remain correct and
restorable.

## 6. Gate

Alpha-17 is complete when the solution builds/tests and the focused harness
behaves correctly on a live terminal.

This is not the final `watch` migration proof. Final T12 closure still requires
the migrated ProcPs `watch` application to consume DCurses without retaining a
parallel full-screen terminal implementation.
