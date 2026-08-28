# T12C — `top` Application-Shaped Acceptance

**Project:** `Icod.DCurses`
**DCurses development version:** `0.1.0-Alpha-19`
**Acceptance driver:** `top`
**Status:** Focused acceptance harness implemented; validation pending

---

## 1. Purpose

T12 requires the DCurses 0.1 contract to be exercised against the real
application shapes needed by `watch`, `slabtop`, and `top`.

Alpha-19 adds the largest focused checkpoint:

```text
samples/Icod.DCurses.Top.Acceptance
```

Synthetic task snapshots keep process observation, CPU accounting, process
control, filtering, and other ProcPs policy outside DCurses.

## 2. Roadmap coverage

| Requirement | Alpha-19 evidence |
|---|---|
| summary and task areas | separate logical summary and task windows |
| multiple logical regions | root summary/task/status windows plus task header/body subwindows |
| rapid repaint | a short timed wait updates the logical frame; retained refresh owns physical differences |
| Enter/Space immediate refresh | both bypass the current refresh delay |
| navigation keys | arrows, Home/End, Page Up/Page Down, Left/Right, Tab and Shift+Tab are consumed semantically |
| control keys | Ctrl+L invalidates retained physical-screen knowledge and repaints |
| vertical/horizontal application policy | selected task, visible page, and horizontal field offset remain application state |
| help/alternate logical view | `h` or `?` replaces the monitor frame with a help view; Escape returns |
| prompts | `d` or `s` opens an in-screen editable delay prompt using text, Backspace, Enter, and Escape |
| styling | bold, reverse, underline, standout, indexed foreground, and indexed background are requested semantically |
| resize relayout | every render recomputes regions from current logical dimensions |
| suspend/resume | lifecycle ownership remains in Terminal/DCurses; repaint events invalidate and redraw |
| cursor policy | prompt mode shows and positions the physical cursor; normal/help modes hide it |

The harness never writes a terminal escape sequence directly.

## 3. Multiple-region contract

A normal frame is reconstructed from current dimensions using:

- a summary root window;
- a task root window;
- a one-row task-header subwindow;
- a task-body subwindow;
- a status root window.

The windows are views over the same `CursesScreen`. They are recreated after a
resize so application layout follows the new dimensions without owning physical
terminal mechanics.

## 4. Input contract

The harness consumes the same semantic event stream exposed to applications.

Normal monitor mode accepts:

```text
Up / Down
Page Up / Page Down
Home / End
Left / Right
Tab / Shift+Tab
Ctrl+L
Enter / Space
P / M / N / T
c
d / s
h / ?
=
q / Q
Escape
```

The application owns what those commands mean. DCurses owns decoding and
delivery.

## 5. Prompt and cursor contract

`d` or `s` opens a delay editor inside the retained screen. While the prompt is
active:

- ordinary text edits the prompt buffer;
- Backspace removes input;
- Enter validates and commits the delay;
- Escape cancels without damaging the monitor frame;
- the physical cursor is requested visible and positioned at the edit point.

When prompt mode ends, the cursor is hidden again and the normal frame is
redrawn.

## 6. Styling contract

The harness intentionally requests all style families required by the T12 top
gate:

- bold summary/title text;
- reverse task headers;
- underline summary labels;
- standout selected-task presentation;
- indexed foreground colors for selected task classes;
- indexed background color for the status region.

Capability degradation is acceptable where the terminal cannot render a
particular style. The semantic frame and restoration contract must remain valid.

## 7. Manual acceptance

Run:

```text
dotnet run --project samples/Icod.DCurses.Top.Acceptance/Icod.DCurses.Top.Acceptance.csproj
```

Verify:

1. the process-like values update rapidly without visible whole-screen flashing;
2. arrows, paging, Home/End, and horizontal scrolling update selection/viewport;
3. Tab and Shift+Tab move the displayed logical focus in opposite directions;
4. Ctrl+L forces a correct repaint;
5. `P`, `M`, `N`, and `T` change semantic task ordering;
6. `c` changes the command presentation;
7. `h` or `?` opens help and Escape returns to the monitor;
8. `d` or `s` opens the delay prompt, shows the cursor, accepts editing, and
   restores the hidden cursor after Enter/Escape;
9. resizing completely relays out the regions;
10. on supported POSIX hosts, suspend/resume restores and re-enters presentation
    state and repaints;
11. `q` or `Q` exits with the host terminal restored normally.

## 8. Gate

Alpha-19 is complete when the solution builds/tests and the focused harness
behaves correctly on a live terminal.

At that point the focused `watch`, `slabtop`, and `top` T12 harness set is
complete. T12 itself remains open until the migrated ProcPs applications are
reviewed against these contracts and any remaining application-local generic
terminal machinery is removed.
