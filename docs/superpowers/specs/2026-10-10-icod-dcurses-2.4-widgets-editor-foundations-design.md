# Icod.DCurses 2.4.0 — Widgets and dedit foundations

**Date:** 2026-10-10 (America/New_York)  
**Status:** Approved for planning on 2026-10-11; public signatures remain subject to the tranche API gate; not an implementation claim  
**Decision:** Focused DCurses mechanisms, with application and control policy above it  
**Baseline:** Icod.DCurses 2.3.1; direct Icod.Terminal 1.28.0  
**Target dependency:** published Icod.Terminal 1.29.0  
**Consumers:** [Icod.Widgets](https://github.com/uniblab/Icod.Widgets) and the [dedit planning PR](https://github.com/uniblab/Icod.Dedit/pull/1)

## Intent and success

Version 2.4 supplies the minimum terminal-facing mechanisms needed for reusable Widgets controls and a responsive multi-document editor. A Widgets editor must be able to project only its visible text, position and style the caret, copy/paste through a bounded optional host clipboard, and route captured pointer interaction without rebuilding an entire document or acquiring a second terminal session. The existing 2.3 text/layout/interaction contract remains a valid baseline. This release does not implement a text buffer, tab strip, scrollbar, dialog, file picker, save workflow, or editor application.

Success is demonstrated by a public-only integration witness driven by an application-owned event loop, package-only consumers on net8.0/net9.0/net10.0, deterministic tests of unavailable/failed terminal operations, and measured visible-viewport work. Acceptance must distinguish headless CI from live-terminal evidence.

## Ownership

| Owner | Responsibility |
|---|---|
| dedit | File bytes and identity, encoding/EOL policy, commands, save/close, active documents, event loop, scheduling |
| Widgets | Document model, revision/cache, editor view, selection policy, clipboard fallback, tabs, controls, focus policy, autoscroll timing |
| DCurses 2.4 | Retained visible-cell projection, Unicode/caret/selection geometry, damage and refresh, session-safe terminal-control facade, deterministic interaction mechanism |
| Terminal 1.29 | Actual clipboard protocol and local budgets, cursor-style observation/lease/restoration, input decoding, physical state, serialized output |
| TermInfo | Transitive capability data; no direct DCurses production dependency |

Widgets has one direct package reference to DCurses; it must not construct terminal protocol bytes or depend directly on Terminal/TermInfo. DCurses may translate Terminal types to its own small semantic public contracts instead of leaking a new direct Terminal dependency to Widgets. Existing DCurses public Terminal-bearing contracts are historical and need not be broken.

## Dependency and version identity

Pin every direct production, test, and package-consumer Terminal reference intentionally to `1.29.0` once a clean restore confirms publication. A freshly restored package consumer must see exactly one direct DCurses runtime dependency, Icod.Terminal 1.29.0, with TermInfo only transitively. Version and PackageVersion advance together to `2.4.0-alpha.1` during implementation, then through RC to `2.4.0` only at the stable-source gate. Preserve AssemblyVersion `2.0.0.0`, C# 13, net8.0/net9.0/net10.0, LGPL-3.0-or-later for the DLL, and GPL-3.0-or-later notices for tests/samples. The design/roadmap PR alone must not pretend the stable package exists.

## Clipboard facade

Terminal 1.29 has explicit `WriteClipboardAsync` and `ReadClipboardAsync`, independent read/write local budgets, a 65,536-byte default and a supported local ceiling of 16,777,216 bytes. These are local resource budgets, not negotiated terminal clipboard capacity or evidence of terminal acceptance. DCurses exposes an opt-in, session-owned facade with its own selection enum and options, forwarding to Terminal without duplicating OSC 52 framing, decoding, query correlation, or output ownership.

Proposed public contracts for API review:

```csharp
public enum CursesClipboardSelection { Clipboard, Primary, Secondary, Select }

public sealed class CursesClipboardOptions {
    public int MaximumReadBytes { get; init; } = 65_536;
    public int MaximumWriteBytes { get; init; } = 65_536;
}

public sealed partial class CursesSessionOptions {
    public CursesClipboardOptions ClipboardOptions { get; init; } = new();
}

public sealed partial class CursesSession {
    public ValueTask WriteClipboardAsync(
        CursesClipboardSelection selection, string text,
        CancellationToken cancellationToken = default);
    public ValueTask WriteClipboardAsync(
        CursesClipboardSelection selection, ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken = default);
    public ValueTask<byte[]> ReadClipboardAsync(
        CursesClipboardSelection selection, TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
```

Validate 1..16,777,216 on each independent budget before opening a session, pass the immutable snapshot to Terminal on DCurses-owned session creation, and do not silently override the options of an externally supplied TerminalSession. For that overload, infer the effective DCurses clipboard options from the supplied TerminalSession when DCurses options are omitted. If DCurses options are supplied and their budgets differ from the TerminalSession's established budgets, reject before ownership transfer rather than claim they were applied. The exact options-shape compatibility with existing callers is an API-regret gate.

The text writer uses Terminal's strict UTF-8 encoding; byte reads do not assume that remote bytes are UTF-8. A Widgets text adapter may decode explicitly and choose an internal clipboard fallback. Terminal denial, ignored query, timeout, malformed response, oversize response, noninteractive endpoint, cancellation and disposal remain distinct outcomes; DCurses does not turn a timeout into `Unsupported` or silently retry. Writes mean transmission, not confirmation that the host accepted data. Larger budgets are opt-in and do not allocate the ceiling eagerly in DCurses. No clipboard access occurs during session open, refresh, or disposal.

## Cursor style and visibility

Expose a thin semantic DCurses style enum and disposable lease corresponding to Terminal's block/underline/bar, blinking/steady choices. `AcquireCursorStyleAsync(style, timeout, cancellationToken)` obtains Terminal's reversible lease, with a DCurses-owned `IAsyncDisposable` wrapper. The outermost Terminal lease queries a real baseline before mutation; unsupported observation, timeout, cancellation and restoration failure remain explicit. Nested leases restore in LIFO order. Do not guess a reset style or synthesize a private escape sequence.

Cursor style is independent of visibility. The existing `CursesSessionOptions.HideCursor` defaults to true. An editor acceptance witness must explicitly configure visible-caret policy (for example `HideCursor = false`) and position the retained/physical cursor through existing refresh semantics. A style lease must not implicitly show a hidden cursor, and a fallback control remains usable if observation is unavailable. Scope and disposal tests must verify no style leak on modal transitions and session close.

## Visible text and overlays

The existing `CursesTextLayout` is immutable and measures one supplied string; `PresentTextLayout` already projects selected visual lines, and the geometry API already maps source positions, hit tests and selection rectangles. It is inappropriate to construct that layout over the whole document on every keypress. Widgets owns document indexing, revisions, wrap/cache invalidation, and retrieval of visible logical line slices. DCurses receives only bounded viewport-local text/layout work and translates it into retained cells with existing clipping, metadata, wide-cell coherence and damage rules.

First implement a measured public-only spike using current `CursesTextLayout`, `PresentTextLayout`, prepared cell operations and local-to-document coordinate translation in Widgets. This spike is acceptance evidence, not a throwaway substitute in production. Add a DCurses API only if a failing package-consumer witness demonstrates an irreducible gap (e.g., safe overlay or cell projection without redundant full-width mutation). At the public API gate, specify any resulting signature, source/visual coordinate mapping, viewport origin, clipping, style precedence, and invalidation semantics before coding it. A new DCurses cache of whole document text, a file-sized pad, and hidden document revision policy are excluded.

Selection and caret overlays must preserve underlying text, tabs, wide grapheme cells and semantic metadata. Cursor geometry must remain correct after wrap, horizontal scroll, resize, and line edits. A selection repaint clears old highlighting through bounded retained damage; no stale selection survives a tab switch. File text with control sequences must display safely without emitting raw terminal controls. Widgets owns the original source bytes and saving policy.

Performance target: edit and repaint work scales with affected visible rows/cells and bounded context, not total document bytes or line count. Measure many-line files, one very long logical line, Unicode-heavy text, selection drag and a large paste on the selected storage/layout design. Record allocations and latency; set numerical budgets from a baseline before freezing API/package claims.

## Interaction and caller-fed timing

Existing DCurses key phases, function-key numbers, bracketed paste, terminal focus events, regions/scopes and explicit pointer capture are the baseline. Widgets must consume Tab in an editor, traverse form controls elsewhere, keep modal commands out of background controls, and release capture on focus loss, region removal, modal change or disposal. Terminal's focus event does not itself dictate Widgets focus policy.

First demonstrate drag selection and scrollbar capture through the current router, including movement/release outside the original region. If its published routing contract cannot represent a required cancellation or captured release, isolate the exact failing event trace and add only the missing deterministic mechanism. Click-count recognition, autoscroll and key-repeat timing are caller-fed by elapsed time on the application loop; DCurses creates no timer, callback dispatcher, second event loop or hidden polling task. Widgets retains the policy for thresholds, selection extension and scrolling.

## Failure, compatibility and testing

- Assert clean 1.29.0 restore and source/package dependency metadata before advancing feature implementation. No unpublished prerelease is a substitute for the requested stable pin.
- Write failing tests for option validation, external-session option mismatch, 64 KiB default, independent larger budgets, strict text encoding, raw byte reads, cancellation, timeouts, malformed/oversized replies, output failure and disposal. Use deterministic Terminal fakes/transport fixtures rather than a real clipboard in unit tests.
- Test cursor nesting/restoration, unsupported baseline, timeout, session-close race, visibility independence and the fallback when a style cannot be acquired.
- Prove current or additive visible-slice projection with Unicode clusters, tabs, width-two edges, wrapping, scrolling, selection/caret overlays, tiny windows, resize and control-character safety; compare output/damage and memory against the 2.3 baseline.
- Prove input/capture event traces including paste as data, key release without duplicate action, outside drag release, focus loss and modal scope transitions. Add no public interaction API without an observed gap and a regression witness.
- Run warnings-as-errors Staging/Release builds and unit tests for net8.0/net9.0/net10.0, fresh NuGet-only consumers, API fingerprint review, Windows/Linux/macOS CI, and public-only Widgets/editor samples. Record available live terminal results separately and mark absent platforms unverified.
- Freeze version, package README/release notes, CHANGELOG, dependency metadata, licensing and baseline docs at an exact candidate head. Maintainer alone merges, tags and publishes.

## Explicit non-goals

No editor document store, undo/redo engine, tabs, scrollbars, dialog semantics, keyboard shortcut policy, filesystem access, encoding conversion, save/close workflow, autosave, host clipboard implementation, protocol parser/encoder, whole-file cache, frame timer, terminal emulator, PTY or change to Terminal's 1.29 buffer negotiation/budget policy. Features above the DCurses boundary require their own Widgets/dedit designs and tests.

## Decisions carried into the development roadmap

1. Confirm that the proposed clipboard/cursor public facades are the narrowest usable contracts for a Widgets-only direct dependency.
2. Confirm external TerminalSession clipboard options are either matched or explicitly rejected at DCurses open; never silently ignored.
3. Decide whether measured viewport acceptance needs an additive DCurses API after the baseline witness, not in advance.
4. Preserve the 2.3 public behavior and `AssemblyVersion 2.0.0.0`; no 2.4 feature moves into the 2.3.1 maintenance record.
