# T2404 Cursor-Style Lifecycle

**Status:** Accepted at implementation commit `27ab58f` and lifecycle-test commit `f57eb41`  
**Development identity:** `2.4.0-alpha.1`  
**Dependency:** Icod.Terminal 1.29.0

## Contract

`CursesSession.AcquireCursorStyleAsync` exposes six DCurses-owned semantic styles
and returns a narrow `CursesCursorStyleLease`. Terminal remains responsible for
DECRQSS observation, DECSCUSR output, strict-LIFO ownership, exact restoration,
query correlation, and session-close cleanup.

The outermost lease must observe a real baseline before mutation. An explicit
unsupported response, timeout, cancellation, malformed response, or session
close during observation never substitutes a guessed reset. Cursor style and
visibility remain orthogonal: acquiring or releasing a style cannot show a
cursor hidden by the DCurses presentation lease.

## Deterministic evidence

The focused `CursesCursorStyle` group passed 8/8 tests on net10.0. It verifies:

- all six semantic mappings and the exact public lease/session signature;
- rejection of an invalid style before Terminal output;
- a real observed baseline restored after nested LIFO disposal;
- unsupported observation, zero-timeout, and caller cancellation with no style
  mutation;
- a hidden cursor remaining hidden throughout acquisition and release;
- concurrent session close and lease disposal restoring the baseline once; and
- pending baseline observation becoming `ObjectDisposedException` on close.

The complete net10.0 run reported 1,398 passing behavioral/compatibility tests.
Its sole failure remains the intentionally deferred 2.4 fingerprint freeze. The
provisional additive contract is 109 exported types, 857 contract lines, SHA-256
`87a3ffe66174634f6db4044b914b05fd31d117b93ecf5b1a151d31d59732e65e`.

## Fallback boundary

Callers may catch an unavailable acquisition and continue with their ordinary
caret presentation. DCurses does not infer emulator support, force cursor
visibility, or select a substitute style. Live rendering fidelity remains a
later terminal-specific acceptance item.
