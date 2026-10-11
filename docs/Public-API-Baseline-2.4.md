# Icod.DCurses 2.4 Public API Baseline

**Release identity:** `2.4.0`  
**Status:** Stable-source contract frozen
**AssemblyVersion:** `2.0.0.0`  
**Direct production dependency:** `Icod.Terminal 1.29.0`

## Compiled fingerprint

```text
109 exported types
857 canonical declared contract lines
sha256 87a3ffe66174634f6db4044b914b05fd31d117b93ecf5b1a151d31d59732e65e
```

`Public-API-Fingerprint-2.4.json` is the immutable 2.4 contract fingerprint.
CI compiles and compares the assembly on .NET 8, 9 and 10. The published 2.3.1
fingerprint remains immutable at 105 exported types, 839 contract lines and
SHA-256 `b86ba658fd90d041eeccf5345ecc71a978bea785f0b2bbb9b8274091ccc4afc1`.

## Additive 2.4 surface

| Kind | Member |
| --- | --- |
| New type | `CursesClipboardSelection` |
| New type | `CursesClipboardOptions` |
| New type | `CursesCursorStyle` |
| New type | `CursesCursorStyleLease` |
| Session option | `CursesSessionOptions.ClipboardOptions` |
| Session method | `WriteClipboardAsync(CursesClipboardSelection, string, CancellationToken)` |
| Session method | `WriteClipboardAsync(CursesClipboardSelection, ReadOnlyMemory<byte>, CancellationToken)` |
| Session method | `ReadClipboardAsync(CursesClipboardSelection, TimeSpan, CancellationToken)` |
| Session method | `AcquireCursorStyleAsync(CursesCursorStyle, TimeSpan, CancellationToken)` |

No 2.3.1 type or member is removed or changed. No new Icod.Terminal type enters
the DCurses public surface, and no Icod.TermInfo type or direct dependency is
introduced.

## Contract summary

- Clipboard read and write limits are independent local byte budgets. Both
  default to 65,536 and may be configured from 1 through 16,777,216 before an
  owned session opens; this is not remote capacity negotiation.
- Clipboard operations are explicit. DCurses performs no clipboard query or
  write during open, refresh, lifecycle handling, or close. Text writes use
  strict UTF-8 and byte operations preserve exact payloads.
- A supplied Terminal session retains its configured limits. Omitted DCurses
  clipboard options inherit those limits; an explicit conflict fails before
  ownership transfer.
- A clipboard write confirms output completion, not host acceptance. A read
  timeout remains inconclusive and is not converted into unsupported status.
- Cursor style has six semantic values and is independent of visibility. The
  scoped lease delegates exact baseline observation, nesting, LIFO restoration,
  cancellation, and unsupported behavior to Terminal; DCurses emits no guessed
  reset.
- Existing layout, viewport, retained projection, interaction scope, focus, and
  pointer-capture APIs satisfied the editor witnesses. Document storage,
  selection policy, clipboard fallback, autoscroll timing, and scrollbar policy
  remain above DCurses.
