# Icod.DCurses 0.8 — T801–T807 Production Hardening Implementation

**Release line:** `0.8.0`  
**Initial package checkpoint:** `0.8.0-alpha.1`  
**Assembly version:** `0.8.0.0`  
**Baseline:** merged `0.7.0` source  
**Dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`

## T801 — concurrency and ownership contract

The 0.8 runtime contract deliberately avoids pretending that cell surfaces are transparently multi-writer thread safe.

- `CursesScreen`, `CursesWindow`, `CursesPad`, and `CursesPadViewport` mutation remains single-writer unless an individual API says otherwise.
- Terminal-mutating session activity continues to pass through the existing session terminal-activity gate.
- One input wait may coexist with refresh/output activity because Terminal owns and serializes the decoder.
- Competing independent event-reader loops remain unsupported ownership.
- No per-cell locks, public scheduler, or public concurrency tokens are introduced.

## T802 — session lifetime and pending waits

`CursesSession` now owns a private lifetime cancellation source separate from caller cancellation and separate from Terminal's underlying decoder lifetime.

When disposal begins:

1. new terminal activity fails with `ObjectDisposedException` through the existing activity gate;
2. the private lifetime token is cancelled;
3. pending DCurses event/lifecycle waits are unblocked;
4. disposal still waits for terminal-mutating work to unwind and performs restoration exactly once.

The wait scope links the private lifetime token with a caller token only when the caller token can actually cancel, avoiding a linked `CancellationTokenSource` allocation on the ordinary no-caller-token path.

A caller-cancelled event wait remains cancellation-compatible and retains the caller token. A disposal-cancelled DCurses wait is translated to `ObjectDisposedException`.

Crucially, this cancels the **public wait**, not Terminal's underlying decoder read. Terminal 1.4 may therefore retain a partially received UTF-8 or escape sequence for the next public wait.

## T803 — concurrent input/refresh and cancellation

Deterministic in-memory tests prove:

- a pending input wait does not block repeated refresh;
- a cancelled public wait in the middle of fragmented UTF-8 does not discard the decoder fragment; the following DCurses read completes the original character;
- cancellation during a refresh write invalidates retained physical knowledge and a later refresh returns through the safe repaint path;
- simultaneous refresh calls serialize terminal output at the session activity boundary.

No sleep-based polling is required for the synchronization points; the tests use explicit task/channel signals.

## T804 — resize and suspend/resume storms

Lifecycle stress covers:

- repeated live-size changes with refresh-driven logical-screen synchronization;
- cancellation while a suspend callback waits behind an active refresh;
- a later suspend/resume cycle after that cancellation, proving neither callback nor terminal-activity gate leaked;
- disposal while suspended, proving participant close releases the held terminal-activity lease and lets blocked refresh/disposal complete deterministically;
- repeated suspend/resume cycles with a refresh intentionally blocked during every suspended interval.

The lifecycle participant retains its existing callback gate and failure-safe release behavior; the stress suite makes those assumptions executable release gates.

## T805 — output and restoration failure recovery

Session-level failure injection covers:

- a partial-progress refresh failure followed by a complete safe retry;
- simultaneous refresh-body and synchronized-output end-frame failure, preserving both exceptions;
- rendition reset failure during session disposal while Terminal mode restoration still completes;
- end-of-input remaining a stable curses input event rather than becoming an unrelated lifetime failure.

The 0.7 physical-screen invalidation rules remain authoritative: uncertain output invalidates retained knowledge before a later refresh can optimize again.

## T806 — repeated ownership soak

The Terminal 1.4 downstream DCurses hardening soak has been mirrored as an ordinary DCurses xUnit acceptance test.

Eight complete cycles exercise:

- Terminal session entry and final mode restoration;
- a pre-existing Terminal rich-input lease using all-keys keyboard reporting, bracketed paste, focus, and button mouse;
- handoff ordering around DCurses alternate-screen ownership;
- a live DCurses refresh;
- Kitty key repeat, focus, and bracketed-paste input decoded through `CursesSession.ReadEventAsync`;
- DCurses disposal and presentation restoration;
- final rich-input cleanup;
- stale outer-lease disposal after the Curses-owned Terminal session is gone, with no duplicate control output.

This keeps Terminal authoritative for protocol negotiation and byte decoding while proving the DCurses facade survives repeated ownership churn.

## T807 — large surfaces and refresh frequency

Bounded production-scale acceptance adds:

- a `2048 x 256` pad with a `120 x 40` viewport panned in both axes for 256 iterations;
- wide-cell validation on every presented frame and an explicit far-source wide glyph check;
- a `160 x 60` live logical screen full repaint followed by 1,000 sparse one-cell refreshes;
- 512 repeated no-op refreshes, requiring the terminal write count to remain unchanged while flush semantics continue normally.

Wall-clock time and GC allocation counts are intentionally not brittle correctness thresholds. The release gates deterministic completion, final cell state, physical footprint validity, and bounded retained behavior instead.

## Public/API decision

T801–T807 justify no new public type or member. `docs/Public-API-Baseline-0.8.md` freezes a zero-public-delta release and keeps all lifetime scopes, stress transports, failure injectors, and measurements internal/test-only.
