# T2402 Clipboard Integration

**Status:** Accepted at implementation commit `c3e4e61`  
**Development identity:** `2.4.0-alpha.1`  
**Dependency:** Icod.Terminal 1.29.0

## Contract

DCurses now exposes its own clipboard selection and budget types plus explicit
text-write, byte-write, and byte-read methods on `CursesSession`. The facade maps
to the single Terminal session already owned by DCurses; it does not expose a new
Terminal type, parse OSC 52, perform implicit clipboard I/O, or claim that a host
accepted a write.

Read and write budgets are independent. Both default to 65,536 bytes and accept
values from 1 through 16,777,216 bytes. They are local resource limits, not a
remote capacity negotiation. An owned Terminal session receives fresh snapshots
of the configured budgets. When a live Terminal session is supplied, omitted
DCurses options inherit its budgets and explicitly conflicting options fail before
ownership transfer.

## Deterministic evidence

The focused `CursesClipboard` group passed 20/20 tests on net10.0. It covers:

- all four selection mappings and exact OSC 52 request/write frames;
- strict UTF-8 text and byte-exact binary reads;
- default and larger independent budgets without eager maximum-size allocation;
- external-session inheritance and rejection while caller ownership remains intact;
- invalid selection, oversized writes, malformed Unicode, malformed/oversized
  replies, output failure, caller cancellation, session disposal, and timeout;
- ordinary input after an ignored query; and
- absence of automatic OSC 52 traffic during open, refresh, and close.

The complete net10.0 run reported 1,390 passing behavioral/compatibility tests.
Its sole failure is the intentionally deferred current-development fingerprint,
which still points to the immutable published 2.3 baseline. The observed additive
2.4 contract at this checkpoint is 107 exported types, 851 contract lines, SHA-256
`522c28b5252475312db09ea8ccdf4a87dd949b610e0aa718f2e33ec9ce10a7fc`.
That provisional value is evidence, not the T2408 freeze.

## Semantics retained from Terminal

A timed-out query remains `TimeoutException` and is not converted to an
unsupported result. Correlated malformed or oversized replies remain
`FormatException`; caller cancellation remains cancellation; session-driven
cancellation becomes `ObjectDisposedException`. Terminal continues to own query
correlation, bounded late-response recovery, input preservation, and framing.

Live host policy and clipboard consent remain unverified at this deterministic
gate and require an explicit later acceptance record.
