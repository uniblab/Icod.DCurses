# T12B — `slabtop` Application-Shaped Acceptance

**Project:** `Icod.DCurses`
**DCurses development version:** `0.1.0-Alpha-18`
**Acceptance driver:** `slabtop`
**Status:** Focused acceptance harness implemented; validation pending

---

## 1. Purpose

T12 requires the DCurses 0.1 contract to be exercised against the real
application shapes needed by `watch`, `slabtop`, and `top`.

Alpha-18 adds the focused `slabtop` checkpoint:

```text
samples/Icod.DCurses.Slabtop.Acceptance
```

The harness uses synthetic slab-cache snapshots. Linux slab observation,
`/proc/slabinfo` parsing, aggregate calculations, and application sort policy
remain outside DCurses.

## 2. Roadmap coverage

| Requirement | Alpha-18 evidence |
|---|---|
| full-screen entry/restoration | one owned `CursesSession` wraps the entire harness |
| timer refresh | one timed `ReadEventAsync` wait drives each synthetic sample |
| repaint on resize | lifecycle repaint events redraw the current snapshot without incrementing the sample generation |
| `q` / `Q` | both ordinary text inputs terminate the loop |
| Space immediate refresh | Space samples immediately without waiting for the next timeout |
| interactive sort keys | `a`, `b`, `c`, `l`, `v`, `n`, `o`, `p`, `s`, and `u` all select semantic sort criteria |
| update only changed content | the logical frame is redrawn while DCurses retained refresh compares it to known physical state |
| suspend/resume | Terminal/DCurses lifecycle ownership restores and re-enters presentation state; resume invalidates and repaints |

No slab-specific policy is added to the DCurses library.

## 3. Sort-key coverage

The acceptance harness recognizes the same ten sort letters documented by the
migrated ProcPs `slabtop` command:

| Letter | Semantic criterion |
|---|---|
| `a` | active objects |
| `b` | objects per slab |
| `c` | total cache size |
| `l` | total slabs |
| `v` | active slabs |
| `n` | cache name |
| `o` | total objects |
| `p` | pages per slab |
| `s` | object size |
| `u` | object utilization |

Numeric criteria sort descending. Name sorts ascending.

The purpose of this mapping is to prove that DCurses delivers ordinary
application-command input immediately and without a competing terminal parser.
The application remains responsible for deciding what those letters mean.

## 4. Sampling and repaint policy

The harness keeps two operations distinct:

**Sample**
: advance the synthetic generation and rebuild the slab snapshot.

**Repaint**
: redraw the already-held snapshot because dimensions or physical-screen
  knowledge changed.

A timer timeout or Space performs both. Resize/resume performs only repaint.
This matches the requirement that a terminal resize must not force an otherwise
unnecessary slab observation.

## 5. Retained refresh

Each redraw reconstructs the desired logical frame. `CursesSession.RefreshAsync`
owns comparison with the retained physical-screen image, so unchanged cells do
not need application-specific output suppression.

This is the mechanism `slabtop` needs: application code may describe the current
report plainly while DCurses decides which terminal operations are actually
necessary.

## 6. Manual acceptance

Run:

```text
dotnet run --project samples/Icod.DCurses.Slabtop.Acceptance/Icod.DCurses.Slabtop.Acceptance.csproj
```

Verify:

1. the generation advances once per normal interval;
2. Space advances it immediately;
3. each sort-key letter changes the displayed sort criterion and table order;
4. resize repaints at the new dimensions without advancing the generation;
5. `q` and `Q` exit normally;
6. on a supported POSIX host, suspend/resume restores and re-enters the terminal
   presentation correctly;
7. the host terminal is restored after exit.

## 7. Gate

Alpha-18 is complete when the solution builds/tests and the focused harness
behaves correctly on a live terminal.

Final T12 closure still requires the migrated ProcPs `slabtop` application to
consume DCurses without retaining a parallel full-screen terminal
implementation.
