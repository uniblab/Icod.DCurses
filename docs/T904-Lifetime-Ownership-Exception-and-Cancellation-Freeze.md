# T904 — Lifetime, Ownership, Exception, and Cancellation Freeze

**Release line:** `0.9.0`  
**Foundation:** merged `0.8.0` production-hardening behavior  
**Status:** accepted for the 1.x contract

## Session ownership

The ordinary `CursesSession.OpenAsync(...)` overload opens the underlying `TerminalSession` and owns it for the lifetime of the curses session.

The advanced overload accepting an already-open `TerminalSession` remains intentional. Ownership transfers only after successful curses initialization. After successful initialization, disposing the `CursesSession` also disposes the supplied Terminal session. If initialization fails before transfer, the caller retains responsibility for that Terminal session.

This advanced bridge is retained because it permits applications/frameworks which already have authoritative Terminal ownership to layer curses presentation without creating a second live-terminal owner.

## Logical-surface ownership

The 0.8 single-writer contract is retained:

- `CursesScreen`, `CursesWindow`, `CursesPad`, and `CursesPadViewport` mutation is single-writer unless a specific API explicitly states otherwise;
- windows and subwindows are shared views, not independent lifetime-owned compositing layers;
- a `CursesPadViewport` observes/projects its pad and destination; it does not own terminal state;
- the standard screen belongs to its `CursesSession` and is not usable as an independent terminal owner after session disposal;
- one Terminal-owned event consumer may wait concurrently with refresh/output work;
- competing independent raw/event reader loops are not a supported ownership model.

No per-cell lock, public scheduler, or public ownership token is introduced.

## Cancellation freeze

Caller cancellation is distinct from disposal:

- a caller-cancelled event wait throws an `OperationCanceledException`-compatible exception and retains the caller token when DCurses creates the cancellation exception;
- cancelling one public wait does not cancel/discard Terminal's underlying decoder read or fragmented input state;
- cancellation of refresh/output activity leaves retained physical state either unchanged or conservatively invalidated according to whether output certainty was lost;
- timeout/deadline overloads continue to return curses timeout events rather than converting ordinary timeout into disposal.

These rules are machine-covered by `CursesSessionLifetimeHardeningTests` and `CursesConcurrentInputRefreshHardeningTests`.

## Disposal freeze

Once disposal begins:

- new terminal-mutating work is rejected deterministically;
- pending DCurses input/lifecycle waits are unblocked;
- disposal-induced cancellation of those waits is surfaced as `ObjectDisposedException`;
- repeated `DisposeAsync()` calls share one restoration operation;
- disposal waits for terminal-mutating activity to leave the serialized activity boundary;
- curses-owned rendition/presentation state is restored before or alongside authoritative Terminal restoration;
- Terminal mode/session restoration is attempted even when a curses rendition/presentation cleanup step fails.

These rules are covered by `CursesSessionLifetimeHardeningTests`, `CursesLifecycleHardeningTests`, and `CursesOutputFailureHardeningTests`.

## Input end/disconnect freeze

A clean terminal end-of-input remains a semantic curses input event (`CursesInputEventKind.EndOfInput`) rather than being manufactured into a generic exception.

Terminal remains responsible for byte-stream decoding and for distinguishing the lower-level causes it owns. DCurses maps the stable semantic event contract; it does not install a second reader or parser.

## Output and restoration failure freeze

When output certainty is lost:

- retained physical-screen knowledge is invalidated conservatively;
- a subsequent refresh may repaint through the safe renderer rather than relying on uncertain retained state;
- independently meaningful primary-operation and restoration failures are both preserved;
- the established dual-failure path uses `AggregateException` rather than silently discarding either failure;
- synchronized-output end/restoration is attempted even when the body operation failed;
- terminal mode restoration remains authoritative during disposal even when rendition reset fails.

`CursesOutputFailureHardeningTests` pins these behaviors.

## Argument and invalid-state exception policy

Public entry points continue to use standard managed exception categories:

- `ArgumentNullException` for required null references;
- `ArgumentException` for structurally invalid values such as control-bearing printable cell/text content;
- `ArgumentOutOfRangeException` for invalid enum values, negative/zero dimensions where prohibited, and coordinates/ranges outside the declared geometry contract;
- `InvalidOperationException` for operations incompatible with the current logical/session state where there is no dedicated availability-result contract;
- `ObjectDisposedException` after the owning session has begun disposal;
- `OperationCanceledException`-compatible cancellation for caller-requested cancellation;
- underlying I/O/restoration exceptions, or `AggregateException` for independent dual failures, when terminal operations fail.

T903 pins representative geometry parameter identities. T904 freezes the session/lifetime categories above. Additional public methods remain subject to the same standard categories documented in their XML comments and focused tests.

## Regret decision

The 0.8 lifetime/failure model is suitable for 1.x. No public lock, task scheduler, lifetime token, diagnostics object, or exception hierarchy is justified.

Breaking changes to these semantics after T904 require an explicit fingerprint/migration/regret update before `0.9.0` stable closure; they are not deferred to `1.0.0`.
