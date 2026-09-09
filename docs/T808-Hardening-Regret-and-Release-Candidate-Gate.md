# T808 — Hardening Regret and Release-Candidate Gate

**Release line:** `0.8.0`  
**Candidate version:** `0.8.0-rc.1`  
**Assembly version:** `0.8.0.0`  
**Dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Status:** complete; release candidate accepted

## Objective

T808 closes feature work for `0.8.0` and proves that the production-hardening changes can ship without expanding the public API or weakening the established Terminal/TermInfo ownership boundary.

The accepted 0.8 contract is behavioral hardening over the stable 0.7 public surface.

## Accepted concurrency and lifetime model

- `CursesScreen`, `CursesWindow`, `CursesPad`, and `CursesPadViewport` mutation remains single-writer unless a member explicitly documents otherwise.
- One Terminal-owned event consumer may wait concurrently with refresh/output activity.
- Competing independent event-reader loops are not a supported ownership pattern.
- Terminal-mutating presentation, protocol-acquisition, refresh, alert, cursor, suspend, and disposal activity remains internally serialized.
- DCurses does not add per-cell locking or a second byte-stream reader.
- Disposal prevents new terminal activity and unblocks pending DCurses input/lifecycle waits.
- Caller cancellation remains cancellation; disposal-induced wait cancellation is surfaced as `ObjectDisposedException`.
- Canceling one DCurses wait does not discard Terminal's underlying decoder state or fragmented input.

## Hardening coverage accepted into the candidate

T802 through T807 add deterministic acceptance for:

- disposal during pending input/lifecycle waits;
- input waiting concurrently with refresh;
- fragmented UTF-8 preservation across caller cancellation;
- canceled refresh followed by safe repaint;
- concurrent refresh serialization;
- resize storms and repeated suspend/resume coordination;
- disposal while suspended;
- partial-progress output failure and physical-state invalidation;
- synchronized-output body/restoration dual failure preservation;
- rendition-reset failure while Terminal mode restoration remains authoritative;
- stable end-of-input mapping;
- eight repeated rich-input/full-screen ownership cycles;
- large-pad panning, 160x60 full-screen refresh, 1,000 sparse updates, and repeated no-op refreshes.

The first T805 failure-injection test assumed application text `XY` would be coalesced into one output write. That assumption was invalid. The harness was corrected to inject failure on either application-text chunk, preserving the intended partial-progress uncertainty test without constraining output chunking.

## Public API regret decision

`0.8.0` adds **no public API**.

The complete public type/member surface accepted for `0.7.0` remains the public surface for `0.8.0`. Session-lifetime cancellation sources, synchronization gates, hardening counters, stress transports, failure-injection outputs, and soak helpers remain private or test-only.

The release does not add a public scheduler, lock/token abstraction, diagnostics record, event-loop replacement, or performance-statistics surface.

## Architecture and package gate

The PR runtime matrix is intentionally expanded to match the release architecture set:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64.

Each runtime job builds/tests `net8.0`, `net9.0`, and `net10.0` under Staging warnings-as-errors.

Package validation proves:

- exact package identity/version;
- exact `Icod.Terminal 1.4.0` and `Icod.TermInfo 1.10.0` dependency groups;
- symbols/readme/license/package structure;
- a fresh package-only consumer.

## Pre-RC implementation checkpoint

Exact hardening implementation head `015b028167337cfacdd39f9a38550548644f6059` passed Windows x64, Windows ARM64, Linux x64, Linux ARM64, macOS x64, macOS ARM64, and package/fresh-consumer validation.

## Accepted release candidate

Exact `0.8.0-rc.1` head `6247b9dc290089e5db82f83929d07b4b674b8d8b` passed the complete seven-job gate:

- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64;
- package/fresh-consumer validation.

T808 is therefore complete. T809 may promote this unchanged behavioral/public contract to stable `0.8.0`.
