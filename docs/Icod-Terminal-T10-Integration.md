# Icod.Terminal T10 Integration — DCurses Alpha-12

**Repository baseline:** `Icod.DCurses/main` at `39a7aa6b68ccf95e18df826c318a612abda5fd6e`
**DCurses development package:** `0.1.0-Alpha-12`
**Terminal dependency:** `Icod.Terminal 0.1.0-alpha.10`
**TermInfo dependency:** `Icod.TermInfo 1.0.0`
**Cleanup completion:** `Icod.DCurses 0.1.0-Alpha-14` on the post-Alpha-13 main line
**Current Terminal dependency after timing rebase:** `Icod.Terminal 0.1.0-alpha.11`

## Purpose

Alpha-12 is the active substrate cutover required by Icod.Terminal T10. It makes
`Icod.Terminal.TerminalSession` the single owner of live terminal state beneath
`Icod.DCurses`.

This checkpoint was intentionally staged. Alpha-12 excluded the former DCurses
live-terminal backend, native mode editor, lifecycle source, input decoder, and
session-ownership partials from compilation while the Terminal-backed
implementation under `src/Integration/` completed a full validation cycle.
Alpha-14 physically removes that retired substrate after Alpha-12 and Alpha-13
passed the supported build/test matrix.

## Responsibility boundary

After this checkpoint:

```text
Icod.TermInfo
    immutable terminal descriptions, capabilities, expansion, padding model

Icod.Terminal
    live endpoint observation
    terminal identity resolution
    input-mode capture/apply/exact restoration
    output-host setup
    live dimensions
    lifecycle and termination observation
    incremental byte/key decoding
    alternate-screen/keypad/cursor presentation leases

Icod.DCurses
    curses-shaped input/lifecycle event facade
    cells, styles, windows and logical screens
    Unicode/cell-width policy
    desired/physical screen images
    damage/diff/refresh
    rendition and color policy
    curses presentation policy
```

`Icod.CommandFramework` is no longer a runtime dependency of DCurses.

## Session ownership

`CursesSession.OpenAsync()` opens and owns a `TerminalSession` using the curses
input-mode and echo policy.

An overload accepts an already-open `TerminalSession`. Ownership transfers to
the returned `CursesSession` only after successful curses initialization. On
successful transfer, disposing `CursesSession` also disposes that Terminal
session.

The old public `TerminalBackend` injection surface left the active build in
Alpha-12. Its retained migration-reference source was physically removed in
Alpha-14 after the Terminal-backed replacement passed validation.

## Input and lifecycle

DCurses no longer decodes terminal bytes itself. `TerminalSession.ReadEventAsync`
performs incremental decoding and lifecycle wake-up. DCurses maps Terminal input
and lifecycle events into the existing `CursesInputEvent`, `CursesLifecycleEvent`,
and `CursesEvent` facade so application-facing curses semantics remain familiar.

Live dimensions use `Icod.TermInfo.TerminalSize` directly through
`TerminalSession.GetSize()`.

## Presentation ownership

Alternate-screen mode, keypad/application mode, and physical cursor visibility
are now represented by `TerminalPresentationLease` instances. DCurses decides
when those states are required; Terminal owns their reversible host transitions.

Initial alternate-screen, keypad, and cursor requests are acquired separately.
This preserves DCurses' existing graceful degradation: absence of one capability
does not prevent another supported presentation state from being entered.

## Rendition and suspend/resume

SGR rendition and color remain DCurses responsibilities because they are part of
screen composition and refresh policy rather than generic terminal-session state.

`Icod.Terminal 0.1.0-alpha.10` adds the lifecycle-participant seam needed to
coordinate that higher-layer state. DCurses registers a participant which:

1. serializes against active DCurses terminal output;
2. resets DCurses-owned rendition before Terminal releases its host state;
3. holds the DCurses terminal-activity gate across suspension;
4. invalidates physical-screen knowledge after Terminal re-enters its state;
5. releases the activity gate so the next refresh performs the required repaint.

Terminal remains the sole owner of native signal/console lifecycle registration.
DCurses does not install a second signal handler.

## Output semantics

The refresh engine retains a narrow internal output boundary for deterministic
unit tests. The live implementation delegates:

- application text to `TerminalSession.WriteTextAsync()`;
- terminfo protocol strings to `TerminalSession.WriteTerminalStringAsync()`;
- flushing to the Terminal session output service.

This means DCurses no longer owns a second terminfo protocol-byte/padding path.

## Alpha-14 cleanup completion

Alpha-14 physically removes the legacy implementation families which Alpha-12
had excluded from compilation:

- old `CursesSession` live-terminal ownership/input/lifecycle/presentation partials;
- `src/Terminal/**` backend, native mode and lifecycle machinery;
- the old DCurses incremental input decoder;
- tests tied specifically to those retired implementation contracts.

The temporary `<Compile Remove=...>` migration lists are therefore removed from
both project files. The active Terminal-backed integration tests, refresh/damage
tests, screen/window tests, and Unicode tests remain in place.

## Follow-up

The Icod.Terminal T10 responsibility reset is now closed. Development proceeds
to the existing T12 ProcPs acceptance tranche for `top`, `slabtop`, and `watch`.
Reusable gaps discovered by those acceptance consumers should be fixed in
Icod.Terminal or Icod.DCurses rather than reintroduced as application-private
terminal infrastructure.
