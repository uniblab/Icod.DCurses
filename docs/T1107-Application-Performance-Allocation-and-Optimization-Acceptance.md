# T1107 — Application, Performance, Allocation, and Optimization Acceptance

**Project:** `Icod.DCurses`  
**Release:** `1.1.0`  
**Development package after closure:** `1.1.0-alpha.7`  
**Qualified implementation/acceptance head:** `bc226acf20fc5a8ab88f81d0d2053663d0120288`  
**Qualified workflow:** #516 / `34419328443`  
**Result:** seven jobs green  
**Public API delta:** none

---

## 1. Purpose

T1107 converts the semantic metadata and hyperlink implementation from feature-level correctness into application-shaped release evidence.

The acceptance gate intentionally exercises editor, pager/help, large-pad, and real Terminal-backed session behavior rather than relying only on small unit examples. It also records the storage and output-cost evidence needed to decide whether semantic content should re-enable any of the terminal-native structural shortcuts that were conservatively disabled in T1104/T1105.

T1107 introduces no public diagnostics, counters, benchmark surface, or new exported type.

---

## 2. Editor-shaped acceptance

The editor-shaped scenarios prove that semantic metadata remains independent from visual style and follows surviving content through realistic mutation.

Covered behavior includes:

- linked and unlinked content in the same logical screen;
- a two-column Unicode text element inside a linked run;
- insertion inside a linked span;
- insertion before an existing linked span;
- replacement styling while retaining the same hyperlink identity;
- repeated semantic-only hyperlink retargeting without changing visible text;
- retained refresh after those changes;
- coalescing of each resulting equivalent semantic run.

A visible style change therefore does not implicitly change semantic identity, and a semantic-only change remains sufficient to damage and repaint otherwise identical visible content.

---

## 3. Pager/help acceptance

Pager/help-shaped scenarios exercise both renderer scale and logical pad transformation.

Covered behavior includes:

- many independent links in one visible screen;
- a settled second refresh producing no repeated semantic payload;
- viewport source movement across a linked pad;
- line insertion before visible linked content;
- line deletion restoring its prior location;
- upward scrolling;
- preservation of hyperlink identity with the surviving logical line across all of those transforms.

The application-shaped pad test verifies that a link originally associated with `row-10` remains associated with that content after viewport movement, insertion, deletion, and scrolling rather than remaining attached to an obsolete coordinate.

---

## 4. Large-pad acceptance

The reference semantic surface remains:

```text
2,048 rows × 256 columns = 524,288 logical cells
```

T1107 exercises:

- an ordinary no-metadata baseline through the existing stable refresh/memory tests;
- a sparse semantic cell;
- a completely linked dense row;
- independent viewports over the same pad;
- semantic observation and acknowledgement by each viewport independently.

This keeps the representation decision tied to the scale that originally rejected an unconditional metadata slot inside every cell.

---

## 5. Sparse allocation shape

Both rejected inline candidates add an eight-byte slot per logical cell on the supported 64-bit validation matrix.

At the reference pad size:

```text
524,288 cells × 8 bytes = 4,194,304 bytes = 4 MiB
```

That cost would exist even when no semantic metadata is present.

The accepted `CursesSparseCellPlane<T>` has one top-level row-reference table and allocates individual row arrays only when a row contains metadata. For the deterministic ten-row acceptance shape:

```text
top-level row references     2,048 × 8 bytes = 16 KiB
10 populated row arrays     10 × 256 × 8 bytes = 20 KiB
                                                   ------
reference payload                                      36 KiB
```

The implementation-bound test verifies exactly ten allocated semantic rows, 4,608 materialized reference slots, and release of all per-row arrays after `Clear()`.

These numbers intentionally describe deterministic reference payload, not total managed heap bytes. Runtime-specific array/object headers and allocator bookkeeping are not promoted to a cross-runtime package contract.

---

## 6. Semantic output transaction shape

T1107 proves both ends of the hyperlink-run cost model.

For adjacent cells carrying equivalent hyperlink metadata:

```text
256 linked cells -> 1 bounded semantic hyperlink write
```

The renderer therefore does not emit one OSC 8 transaction per cell.

For intentionally distinct adjacent hyperlinks:

```text
32 distinct links -> 32 distinct bounded semantic hyperlink writes
```

That behavior is required because different semantic identities cannot be merged without changing meaning.

A settled refresh of unchanged linked content emits no repeated hyperlink payload.

---

## 7. Stable non-semantic baseline

T1107 does not change the stable non-semantic renderer path. Existing deterministic refresh-cost and regret fixtures remain authoritative, including:

```text
clean refresh                         0 payload bytes / 0 payload writes / 1 flush
one ASCII-cell update                10 bytes / 2 writes / 1 flush
one wide-cell update                 12 bytes / 2 writes / 1 flush
character-shift strict-win fixture    2 optimized vs 34 fallback bytes
line-shift strict-win fixture         4 optimized vs 166 fallback bytes
160 × 60 full repaint              9661 bytes / 121 writes / 1 flush
1000 one-cell updates              2000 bytes / 2000 writes / 1000 flushes
```

These are deterministic maintainer fixtures for the synthetic terminal descriptions used by the tests, not universal throughput claims for every terminal emulator.

---

## 8. Full Terminal-backed session acceptance

Real `TerminalSession`/`CursesSession` tests now cover semantic output with synchronized presentation both enabled and disabled.

With synchronized output enabled, canonical Terminal-owned OSC 8 begin/text/end framing is verified inside Terminal's mode-2026 synchronized-output bracket.

With synchronized output disabled, the same Terminal-owned bounded OSC 8 framing is verified without mode-2026 begin/end sequences.

The combined session scenario also proves:

- one Terminal-owned rich-input wait can coexist with semantic refresh;
- focus reporting remains decoded while linked rendering occurs;
- live terminal-size change resizes the curses screen and repaints retained linked content;
- suspend/resume invalidates and safely repaints semantic physical knowledge;
- acquired input protocol state is deterministically released;
- Terminal remains authoritative for terminal protocol framing and cleanup.

T1106 remains authoritative for exceptional cleanup and failure recovery. T1107 verifies the normal application-shaped coexistence path.

---

## 9. Structural terminal shortcut decision

T1107 **does not re-enable** terminal-native erase, character-shift, line-shift, or scrolling shortcuts while desired or retained physical semantic metadata exists.

This is not because the shortcuts lack performance value. The non-semantic regret tests already demonstrate strict byte wins for representative character and line shifts.

The blocker is semantic equivalence.

Terminfo capabilities describe physical character/line insertion, deletion, erase, and scrolling behavior, but they do not provide a portable guarantee that a terminal emulator will move or preserve OSC 8 hyperlink associations exactly as DCurses moves retained logical metadata. Treating physical hyperlink association as guaranteed would therefore make correctness depend on undocumented emulator behavior.

Representative T1107 tests freeze the conservative rule for:

- erase;
- character insertion/shift;
- line deletion/shift.

Full-width scrolling remains covered by the same semantic-state gate in the refresh engine.

A future optimization may be enabled only if DCurses gains portable semantic-equivalence evidence for the relevant physical operation. A terminal-name heuristic or emulator-specific assumption is not sufficient for the core library.

---

## 10. Public contract

T1107 adds no public API.

The provisional 1.1 contract remains:

```text
45 exported types
337 canonical declared contract lines
sha256 d7fb2040d9cd22ed71e90e788f453c73eb29d681f2cc0f7aaefa805792fab2ea
```

The only intentional new exported 1.1 types remain:

- `CursesHyperlink`;
- `CursesCellMetadata`.

---

## 11. Qualified checkpoint

The complete T1107 application/performance/allocation acceptance implementation is frozen at:

```text
bc226acf20fc5a8ab88f81d0d2053663d0120288
```

Workflow #516 / `34419328443` passed:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

The subsequent `1.1.0-alpha.7` documentation/version synchronization must receive its own exact-head seven-job gate before T1107 is considered documentation-complete.

---

## 12. Next gate

T1108 is the public API, package, documentation, and regret gate. It must re-verify the exact public fingerprint across target frameworks, package-only semantic consumption, XML documentation, dependency leakage, naming/nullability/equality/validation/extensibility, samples and release documentation before RC promotion.