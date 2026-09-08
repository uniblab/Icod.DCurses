# T206 — Rich-Input Showcase and Consumer Acceptance

**Project:** `Icod.DCurses`  
**Development line:** `0.2.0`  
**Development version:** `0.2.0-alpha.3`  
**Tranche:** T206 — showcase and consumer acceptance  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Implemented; Staging validation pending

---

## 1. Purpose

T206 makes the expanded `0.2` semantic-input contract observable in the existing live
`Icod.DCurses.Input.Showcase` without making modern keyboard negotiation mandatory.

The sample remains a curses consumer. It does not become a Terminal protocol debugger
or acquire a private input parser.

## 2. Keyboard reporting policy

The showcase independently requests:

```text
CursesKeyboardReportingMode.EventTypes
BracketedPaste
FocusReporting
MouseTrackingMode.ButtonEvents
```

`EventTypes` is the default showcase keyboard request because it can expose semantic
Press/Repeat/Release information without forcing every text-producing key into a key-event
escape sequence.

Each protocol request remains independent. If keyboard reporting is unavailable, the
showcase continues to run with traditional input and any other available protocols.

## 3. Expanded inspector

The live inspector now displays:

- semantic key name;
- key phase;
- complete modifier state;
- character identity;
- shifted-layout character identity;
- base-layout character identity;
- associated text;
- function-key number;
- existing mouse, focus, paste, timeout, and lifecycle information.

Associated text is escaped for carriage return, line feed, tab, and backslash so the
single-line inspector layout remains deterministic.

`Q` exits whether it arrives as ordinary text or as a semantic Character key event.
Escape remains observable rather than being treated as an exit key.

## 4. Capability absence

A failed keyboard-reporting request is a controlled unavailable result, not a sample
failure. The showcase reports each protocol status and continues with the available
input families.

This preserves usability on traditional terminals while allowing richer Terminal 1.0
semantics to appear automatically when supported.

## 5. Ownership boundary

The showcase continues to prove the intended dependency layering:

- Terminal owns negotiation, byte decoding, protocol state, and restoration;
- DCurses owns the curses-shaped event facade;
- the sample owns only display and application policy.

No new hard-coded keyboard, mouse, focus, or paste protocol sequence is introduced by
DCurses or the showcase.

## 6. Gate

T206 is complete when:

1. the showcase builds for `net8.0`, `net9.0`, and `net10.0`;
2. traditional terminals can still run the inspector when keyboard reporting is unavailable;
3. supporting terminals can display key phase and modern key metadata;
4. mouse, focus, paste, lifecycle, and resize display remain intact;
5. `Q` remains a reliable exit path for text and Character key events;
6. the sample documentation explains the optional keyboard-reporting behavior;
7. Windows/Linux/macOS Staging validation remains green;
8. the package candidate and fresh-consumer validation remain green as
   `0.2.0-alpha.3`.

After this gate, development proceeds to T207 — public API, documentation, and package
regret review.
