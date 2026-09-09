# Icod.DCurses 0.8 Public API Baseline

**Release line:** `0.8.0`  
**Stable predecessor:** `0.7.0`  
**Dependencies:** `Icod.Terminal 1.4.0`; `Icod.TermInfo 1.10.0`  
**Public API delta:** none  
**Status:** T808 hardening/regret gate

## Purpose

`0.8.0` is a production-hardening release. It deliberately changes lifetime, cancellation, stress coverage, and internal coordination without adding another public feature family.

The complete public type/member surface accepted for stable `0.7.0` remains the public surface for `0.8.0`.

## Concurrency and ownership semantics

The existing public APIs are now documented under this explicit concurrency contract:

- logical `CursesScreen`, `CursesWindow`, `CursesPad`, and `CursesPadViewport` mutation is single-writer unless a member explicitly states otherwise;
- one Curses/Terminal event consumer may wait concurrently with session refresh/output activity;
- competing independent event-reader loops are not a supported ownership pattern;
- terminal-mutating session activity remains internally serialized;
- disposal prevents new terminal activity and unblocks pending DCurses event/lifecycle waits;
- caller cancellation remains `OperationCanceledException`-compatible and retains the caller token where DCurses creates the cancellation exception;
- disposal-induced cancellation of a pending DCurses wait is surfaced as `ObjectDisposedException`;
- DCurses does not cancel or replace Terminal's underlying decoder read merely because one public wait is cancelled.

These rules tighten documented behavior; they do not add public members.

## Internal hardening machinery

The following remain implementation/test concerns and are not public API:

- the session-lifetime cancellation source;
- linked wait-cancellation scopes;
- terminal-activity serialization gates;
- lifecycle callback serialization;
- failure-injection outputs/transports;
- ownership-soak transports and control providers;
- scale/allocation stress helpers;
- hardening counters/measurements.

No public scheduler, event loop, diagnostics record, concurrency token, lock, or performance-statistics API is introduced.

## Existing dependency boundary

`0.8.0` adds no new `Icod.Terminal` or `Icod.TermInfo` type to public signatures.

The existing approved boundary continues to be enforced by `PublicDependencyBoundaryTests`.

## Package boundary

The package remains:

- `Icod.DCurses`;
- `net8.0`, `net9.0`, and `net10.0`;
- `LGPL-3.0-or-later`;
- dependent on exactly `Icod.Terminal 1.4.0` and `Icod.TermInfo 1.10.0` for every target-framework dependency group.

Package-only validation remains required.

## Regret decision

No new public surface is justified by T801-T807. The hardening machinery is valuable precisely because it strengthens the existing contract without making synchronization primitives, failure-injection infrastructure, or implementation scheduling permanent consumer obligations.
