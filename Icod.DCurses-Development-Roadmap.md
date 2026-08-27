# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`
**Package:** `Icod.DCurses`
**Repository:** `https://github.com/uniblab/Icod.DCurses`
**Initial development branch:** `initial_add`
**Language:** C# 13
**Initial target framework:** `net10.0`
**Configurations:** `Debug`; `Staging`; `Release`
**License:** LGPL-3.0-or-later
**Current development target:** `0.1.0`
**Current tranche:** T12 / Icod.Terminal T19 — rich-input and ProcPs acceptance
**Stable contract target:** `1.0.0`
**Status:** T01-T11 complete; Alpha-15 automated rich-input acceptance validated; Alpha-16 interactive Icod.Terminal T19 acceptance current

---

## 1. Purpose

`Icod.DCurses` is a managed, cross-platform curses-like library for .NET.

It occupies the layer above `Icod.TermInfo` and the low-level terminal-control facilities presently supplied by `Icod.CommandFramework.Terminal`.

The architectural responsibilities are intentionally distinct:

```text
Applications
    top / slabtop / watch / editors / pagers / TUIs
                         |
                    Icod.DCurses
          session / input / windows / screen
             refresh / lifecycle / events
                  /                 \
                 /                   \
        Icod.TermInfo       terminal-control substrate
      capability model      initially Icod.CommandFramework
                 \                   /
                  \                 /
              terminal / console / tty
```

`Icod.TermInfo` answers:

> What can this terminal do, and which terminal-specific capability strings implement those operations?

`Icod.DCurses` answers:

> How does an interactive application safely and efficiently operate a live terminal screen?

The library SHALL NOT duplicate the terminfo database, hard-code ANSI/DEC/xterm sequences where an `Icod.TermInfo` capability exists, or become a process-monitoring library.

The immediate reason for the project is to provide a reusable terminal UI foundation suitable for migrating `top`, `slabtop`, and `watch` into `Icod.ProcPs`.

Those programs are the acceptance drivers for version `0.1.0`.

---

## 2. Design Principles

### 2.1 Managed-first implementation

The implementation SHALL be managed C# except for narrowly scoped operating-system interop that cannot reasonably be supplied by .NET or an existing Icod library.

Native `ncurses`, `curses`, `libtinfo`, or `termcap` SHALL NOT be runtime dependencies.

### 2.2 Terminfo-driven output

Terminal output behavior SHALL be selected through `Icod.TermInfo`.

The library SHALL NOT assume that every interactive terminal is xterm-compatible merely because ANSI/xterm behavior is common.

Cursor addressing, erasure, cursor visibility, alternate-screen operation, attributes, colors, keypad modes, and similar operations SHALL use terminal capabilities when such capabilities exist.

### 2.3 Explicit session ownership

A curses session is an owned resource.

Entering an interactive presentation may modify terminal state. `Icod.DCurses` SHALL therefore make restoration a first-class contract and SHALL restore the terminal after normal completion, cancellation, exceptions, supported suspension/resumption, and disposal to the greatest extent the host permits.

### 2.4 No process-global `stdscr`

The primary managed API SHALL be instance-based.

There SHALL be no mandatory process-global current screen, current terminal, or equivalent of a required native `stdscr` global.

A later compatibility façade MAY expose familiar curses-shaped conveniences, but it SHALL be implemented on top of an explicit `CursesSession`.

### 2.5 Virtual screen before terminal writes

Applications SHALL draw into a logical screen/window model.

The library SHALL own the conversion from desired screen state to physical terminal operations.

Applications SHALL NOT need to clear and repaint the complete terminal on every refresh.

### 2.6 Testability without a real terminal

Session, input, screen, window, and refresh behavior SHALL be testable with an in-memory or scripted backend.

Core correctness tests SHALL NOT depend on a developer running an interactive terminal.

### 2.7 Unicode is part of the core contract

The screen model SHALL reason in terminal cells rather than assuming one UTF-16 `char` equals one screen column.

The implementation SHALL establish explicit behavior for Unicode scalar values, combining text, wide characters, clipping, and continuation cells before the public screen contract is frozen.

### 2.8 Application policy remains outside DCurses

`Icod.DCurses` SHALL provide mechanisms, not ProcPs policy.

For example:

- `top` owns process fields, sorting, filters, prompts, and command semantics;
- `slabtop` owns slab sorting and statistics;
- `watch` owns child-process execution, comparison policy, and interpretation of watched output;
- `Icod.DCurses` owns terminal session state, input events, windows, cells, refresh, attributes, and lifecycle.

### 2.9 Cross-platform behavior is intentional

Windows, Linux, and macOS SHALL be supported intentionally.

Windows Console and Windows Terminal behavior SHALL not be treated as an afterthought or implemented as scattered application-specific conditionals.

---

## 3. Dependency and Framework Policy

### 3.1 Initial dependencies

The `0.1.0` implementation SHOULD begin with:

```text
Icod.DCurses
    -> Icod.TermInfo 1.x
    -> Icod.CommandFramework 1.x
```

`Icod.TermInfo` is the terminal-capability authority.

`Icod.CommandFramework.Terminal` may initially provide the neutral terminal endpoint/control substrate needed for terminal observation and host mode capture/restoration.

### 3.2 Initial target framework

Because the current `Icod.CommandFramework` package targets `net10.0`, the first `Icod.DCurses` release SHALL target `net10.0`.

A later roadmap tranche MAY extract the genuinely generic terminal-control substrate into a smaller package such as `Icod.Terminal` or `Icod.TerminalControl`. If that happens, `Icod.DCurses` MAY subsequently broaden its target frameworks where its dependencies permit.

Framework expansion SHALL NOT block `0.1.0`.

### 3.3 Dependency direction

The dependency graph SHALL remain acyclic.

`Icod.TermInfo` SHALL NOT acquire a dependency on `Icod.DCurses`.

`Icod.CommandFramework` SHALL NOT acquire a dependency on `Icod.DCurses`.

ProcPs applications MAY depend on `Icod.DCurses`.

### 3.4 `Icod.Terminal` T10 integration checkpoint

The initial terminal-control arrangement above was intentionally provisional.
Beginning with `Icod.DCurses 0.1.0-Alpha-12`, the active dependency graph is:

```text
Icod.TermInfo
      ^
      |
Icod.Terminal
      ^
      |
Icod.DCurses
```

`Icod.Terminal` owns live endpoint observation, host input modes, terminal identity,
dimensions, lifecycle observation, input decoding, and reversible alternate-screen,
keypad, and cursor presentation state. `Icod.DCurses` retains curses-shaped events,
cells, styles, windows, logical/physical screen state, damage/refresh, rendition, and
color policy.

`Icod.CommandFramework` is no longer an active runtime dependency of `Icod.DCurses`.
Alpha-12 removed the former DCurses backend, native-mode, lifecycle-source, decoder,
and pre-Terminal session implementation from the active build. Alpha-14 physically
removes that retained migration-reference source and its obsolete implementation tests
after successful Alpha-12/13 validation.

Beginning with Alpha-13, relative event timeouts and Escape-sequence ambiguity waits
are supplied by `Icod.Terminal` through its `Icod.Timing 1.0.0` dependency. DCurses
does not acquire a direct `Icod.Timing` dependency while it owns no independent clock,
timer, or scheduler in the active build.

**Integration checkpoint status:** complete. Development proceeds to T12 ProcPs
acceptance.

---

## 4. Repository Shape

The repository SHALL follow the established `Icod.TermInfo` pattern.

```text
Icod.DCurses/
    .github/
    samples/
        Icod.DCurses.Sample/
    src/
    tests/
        Icod.DCurses.Tests/
            src/
    Directory.Build.props
    Icod.DCurses.csproj
    Icod.DCurses.sln
    LICENSE
    README.md
    icon.png
    build.cmd
    build.sh
    Icod.DCurses-Development-Roadmap.md
```

Additional sample projects, test fixtures, tools, compatibility tests, and package-verification projects MAY be introduced by later tranches when justified.

The root library project SHALL explicitly compile `src/**/*.cs` and SHALL not accidentally compile tests or samples.

---

## 5. Version Roadmap

| Version | Theme | Principal outcome |
|---|---|---|
| `0.1.0` | ProcPs foundation | Complete curses core required by `top`, `slabtop`, and `watch` |
| `0.2.0` | Window semantics | Mature window/subwindow editing, damage tracking, scrolling, borders, overlays |
| `0.3.0` | Input completion | Rich key/event model, sequence ambiguity handling, programmable input modes |
| `0.4.0` | Unicode correctness | Stable terminal-cell width, grapheme, combining, clipping, and text contracts |
| `0.5.0` | Rendition and color | Complete attribute/color model and terminal-capability portability |
| `0.6.0` | Pads and large surfaces | Pads, viewports, large virtual surfaces, scrolling optimization |
| `0.7.0` | Modern terminal interaction | Mouse, paste, focus, and selected modern input/event protocols |
| `0.8.0` | Platform and lifecycle hardening | Windows/POSIX parity, suspend/resume, failure recovery, performance |
| `0.9.0` | Contract freeze | API-regret audit, compatibility validation, packaging and documentation freeze |
| `1.0.0` | Stable release | Supported public contract suitable for general-purpose production TUIs |

The roadmap deliberately does not require every historical `ncurses` extension before `1.0.0`.

---

# 6. Version 0.1.0 — ProcPs Foundation

## 6.1 Release objective

Version `0.1.0` SHALL provide a genuinely usable curses core sufficient for faithful implementations of:

- `watch`;
- `slabtop`;
- `top`.

The goal is not merely to demonstrate cursor movement.

At the end of `0.1.0`, those applications SHOULD be able to use `Icod.DCurses` without retaining ProcPs-specific implementations of terminal mode management, full-screen lifecycle, resize signals, cursor visibility, direct escape sequences, or whole-frame terminal ownership.

---

## 6.2 T01 — Repository and package foundation

Create the initial repository contract.

Required work:

- create `Icod.DCurses.sln`;
- create root `Icod.DCurses.csproj`;
- create `src/`;
- create `tests/Icod.DCurses.Tests/`;
- create `samples/Icod.DCurses.Sample/`;
- add solution folders for `tests` and `samples`;
- define `Debug`, `Staging`, and `Release` for every solution project;
- establish C# 13, nullable reference types, implicit usings, deterministic builds, and explicit compile items;
- establish initial `net10.0` targeting;
- set both `<Version>` and `<PackageVersion>` to `0.1.0`;
- set assembly identity to `Icod.DCurses`;
- add XML documentation output;
- add NuGet/GitHub Packages metadata;
- pack `README.md`, `LICENSE`, and `icon.png`;
- include symbols and portable symbol package metadata;
- correct `build.cmd` and `build.sh` so they reference `Icod.DCurses.sln`;
- ensure build scripts support clean, restore, build, test, and pack;
- add a minimal smoke-test and sample so an empty structural package cannot appear healthy accidentally.

**Gate T01:** clean/restore/build/test/pack succeeds from the repository root.

---

## 6.3 T02 — Terminal backend boundary

Define the lowest DCurses-owned runtime boundary.

The public API SHALL distinguish:

- the curses session;
- the terminal endpoint;
- terminal capabilities;
- host terminal modes;
- terminal input;
- terminal output;
- terminal dimensions.

Implementation details MAY initially adapt `Icod.CommandFramework.Terminal`, but the higher-level screen/window model SHALL not depend directly on operating-system console APIs.

The backend boundary SHALL permit an in-memory test backend.

No public API SHALL require callers to know POSIX `termios`, Windows `DWORD` console modes, file descriptors, HANDLE values, or `ioctl` constants merely to use curses.

**Gate T02:** a fake backend can host a session without `Console`, a TTY, or native interop.

---

## 6.4 T03 — `CursesSession` lifecycle

Introduce explicit live-session ownership.

The session SHALL support:

- opening against standard input/output;
- explicit injected endpoints for tests and advanced callers;
- determining whether an interactive terminal is available;
- resolving the active `TerminalDescription`;
- capturing host terminal state before mutation;
- entering the presentation;
- alternate-screen entry when supported and requested;
- cursor hide/show;
- keypad/application-mode entry when required;
- canonical/cbreak/raw input policy sufficient for interactive ProcPs applications;
- echo/noecho;
- deterministic restoration;
- asynchronous disposal;
- cancellation-aware operations.

Restoration SHALL be idempotent.

A partially initialized session SHALL still attempt to restore every state transition that successfully occurred.

**Gate T03:** fault-injection tests prove restoration after success, exceptions, cancellation, and partial initialization.

---

## 6.5 T04 — Terminal lifecycle events

Provide a managed event model for terminal/process lifecycle events required by interactive screen applications.

The `0.1.0` contract SHALL account for:

- terminal resize;
- interrupt/termination cancellation;
- supported POSIX suspend before `SIGTSTP`;
- resume after `SIGCONT`;
- re-entering presentation modes after resume;
- window-size re-observation;
- Windows console cancellation behavior.

Applications SHALL not need their own `PosixSignalRegistration` or `Console.CancelKeyPress` plumbing merely to operate a curses session.

Signal callbacks SHALL perform only operations safe for the host/runtime context and SHALL hand ordinary work back to the managed event loop.

**Gate T04:** resize and resume events can force a repaint while terminal restoration remains correct.

---

## 6.6 T05 — Input event and key decoder foundation

Create a terminal-independent input model.

At minimum `0.1.0` SHALL represent:

- ordinary Unicode text input;
- Enter;
- Space;
- Escape;
- Backspace;
- Tab;
- Shift+Tab where distinguishable;
- arrow keys;
- Home;
- End;
- Page Up;
- Page Down;
- Insert/Delete where supplied by the terminal;
- function keys required by the selected terminfo profile;
- control-key combinations;
- EOF/disconnect;
- resize as an event or coalesced lifecycle notification;
- cancellation.

Terminal key sequences SHALL be derived from `Icod.TermInfo` capabilities rather than being represented only by hard-coded xterm escape strings.

The decoder SHALL handle the ambiguity between an isolated Escape key and an escape-prefixed key sequence with a documented bounded policy.

Input SHALL support waiting with cancellation and with a timeout/deadline so applications can combine periodic refresh with immediate keyboard handling.

**Gate T05:** scripted input tests exercise fragmented byte sequences, multiple keys in one read, Escape ambiguity, Unicode input, and resize wake-up.

---

## 6.7 T06 — Cell, style, and virtual-screen model

Introduce the logical display model.

A screen cell SHALL be able to represent at least:

- visible text content;
- continuation state for multi-column content;
- foreground color;
- background color;
- default-color state;
- bold/intense;
- dim where supported;
- underline;
- reverse;
- standout or its managed semantic equivalent;
- dirty/change state as an implementation concern.

The model SHALL distinguish application-requested state from the last known physical-screen state.

Color SHALL not be encoded as raw escape strings.

No application SHALL need to concatenate SGR escape codes into ordinary screen text.

**Gate T06:** an application can construct a styled virtual frame without writing to the terminal.

---

## 6.8 T07 — Standard screen and window model

Provide a curses-style `CursesWindow` abstraction.

The `0.1.0` window surface SHALL be sufficient for ProcPs use and SHALL include:

- the standard screen;
- rectangular windows or views;
- subwindows/views sharing or projecting screen state as chosen by the final contract;
- current logical cursor position;
- move;
- write text;
- write a rune/text element;
- write styled text;
- clear;
- erase;
- clear-to-end-of-line;
- clear-to-end-of-window;
- clipping at window boundaries;
- overwrite semantics;
- configurable wrapping behavior;
- vertical scrolling where required for `top`;
- attribute state;
- background/default cell state;
- invalidation/touch operations;
- resize/reflow hooks.

The API SHALL use zero-based managed coordinates unless a compelling reason is discovered before this tranche freezes.

Window operations SHALL validate coordinates and dimensions predictably.

**Gate T07:** multiple logical regions can be composed into one terminal frame without each application manually calculating terminal control sequences.

---

## 6.9 T08 — Refresh and physical-screen synchronization

Implement the defining curses behavior: convert desired logical state into efficient terminal updates.

The refresh engine SHALL:

- retain the last known physical-screen image;
- compare desired and physical state;
- identify changed spans/regions;
- move the cursor using terminfo capabilities;
- minimize needless rendition changes;
- use erase capabilities where they are semantically safe and beneficial;
- avoid complete-screen clear/repaint for ordinary small changes;
- repaint completely when physical state is unknown;
- invalidate after resize, resume, terminal-mode disruption, or explicit request;
- leave the cursor in a predictable requested position;
- flush output at refresh boundaries.

The first optimizer need not solve a globally minimal terminal-output problem.

Correctness and deterministic state tracking take priority over micro-optimization in `0.1.0`.

**Gate T08:** tests demonstrate that changing one small region does not repaint an unchanged full screen, while forced invalidation produces a correct complete repaint.

---

## 6.10 T09 — Dimensions, resize, and repaint contract

The session SHALL expose current terminal dimensions using the neutral terminal substrate.

Resize handling SHALL:

- coalesce repeated resize notifications where reasonable;
- refresh dimensions;
- resize/recreate the standard screen safely;
- mark physical-screen knowledge invalid;
- notify application code;
- permit the application to rebuild its layout;
- perform a correct subsequent repaint.

No `80x25` fallback SHALL silently masquerade as an observed live size in an interactive session.

If dimensions are unavailable, the API SHALL represent that fact explicitly or fail with a documented diagnostic path.

**Gate T09:** scripted dimension changes rebuild and repaint a screen without stale cells or out-of-range writes.

---

## 6.11 T10 — Beep, cursor state, and essential presentation operations

Provide the small terminal operations directly required by the acceptance applications:

- audible/visual alert through the best available capability;
- requested cursor visibility;
- cursor positioning;
- full-screen invalidation;
- rendition reset;
- alternate-screen enter/leave;
- keypad/application mode where needed;
- safe fallback where a capability is absent.

Every operation SHALL respect capability absence instead of assuming xterm behavior.

**Gate T10:** the test backend can prove the capability chosen for each operation.

---

## 6.12 T11 — Unicode baseline for ProcPs

Before `0.1.0` freezes, define enough terminal-cell behavior to prevent the API from baking in a one-`char`/one-column assumption.

Required baseline:

- input and output are Unicode-aware;
- UTF-16 surrogate pairs are not split as independent characters;
- combining marks do not independently consume an ordinary cell;
- known wide terminal characters may consume two cells;
- clipping SHALL not emit half of a wide cell;
- overwriting part of a wide character SHALL repair the affected cells;
- malformed input has deterministic replacement/error behavior;
- width computation is injectable or centrally owned so it can be improved without rewriting window APIs.

Full international text conformance is a later `0.4.0` focus, but the `0.1.0` public API SHALL not prevent it.

**Gate T11:** tests cover ASCII, supplementary-plane scalar values, combining content, and two-column content.

---

## 6.13 T12 — ProcPs acceptance harnesses

`0.1.0` is not complete until the library design has been exercised against the needs of all three target applications.

### `slabtop` acceptance

DCurses SHALL support an implementation that can:

- enter and restore a full-screen session;
- refresh on a timer;
- repaint on resize;
- accept `q`/`Q`;
- accept Space for immediate refresh;
- accept the interactive sort keys;
- update only changed display content where practical;
- suspend/resume correctly on supported POSIX hosts.

### `watch` acceptance

DCurses SHALL support an implementation that can:

- redraw a full terminal-sized presentation repeatedly;
- support title/no-title layouts;
- support wrapping or clipping policy at the application layer;
- show highlighted differences using curses attributes;
- display interpreted child colors through the cell/style model;
- beep on command failure when requested;
- repaint after resize;
- preserve the last presentation when application policy requires it;
- wait on timer and terminal input/lifecycle events without busy polling.

Parsing arbitrary ANSI produced by the watched child is application policy and is not required to become a general terminal-emulation subsystem inside DCurses.

### `top` acceptance

DCurses SHALL support an implementation that can:

- render summary and task areas;
- maintain multiple logical windows/regions;
- repaint rapidly without wholesale terminal clearing;
- respond immediately to Enter/Space refresh;
- handle arrows, Home, End, Page Up, Page Down, Tab, Shift+Tab where available, Escape, control keys, and ordinary character commands;
- support vertical and horizontal navigation policy in the application;
- present help and alternate screens within the curses model;
- support interactive prompts without corrupting screen state;
- use bold, reverse, underline, standout, foreground, and background styling where available;
- resize and completely re-layout;
- suspend, restore, resume, and repaint;
- hide/show the physical cursor according to application state.

The `Icod.DCurses` repository does not need to contain the complete ProcPs applications. Tests, focused harnesses, and/or temporary migration branches MAY be used to prove the contract.

### Icod.Terminal 0.2 rich-input acceptance checkpoint

Before T12 closes, DCurses SHALL also prove the Icod.Terminal T19 integration gate:

- rich mouse, focus, paste, and modified-key events flow through the existing curses event stream;
- mouse/focus/paste reporting is requested through reversible Icod.Terminal protocol leases;
- lifecycle suspend/resume leaves no rich-input protocol state active while suspended;
- disposal restores protocol state even when an application has not released every individual lease;
- DCurses contains no private rich-input escape parser, protocol emitter, or second terminal read loop.

Alpha-15 automated acceptance is complete. Alpha-16 extends the existing input showcase into a live rich-input acceptance consumer before ProcPs application acceptance continues.

The checkpoint is recorded in [`docs/Icod-Terminal-T19-Rich-Input-Acceptance.md`](docs/Icod-Terminal-T19-Rich-Input-Acceptance.md).

**Gate T12:** no generic full-screen terminal infrastructure remains necessary inside `Icod.ProcPs.Shared` for these three applications.

---

## 6.14 T13 — Tests, sample, documentation, and package gate

Before releasing `0.1.0`:

- unit tests SHALL cover session restoration, input decoding, screen/window behavior, refresh diffing, style transitions, dimensions, resize, and Unicode baseline;
- platform-sensitive tests SHALL distinguish unsupported host behavior from defects;
- tests SHALL run non-interactively in CI;
- an `Icod.DCurses.Sample` application SHALL demonstrate session creation, styled output, a movable or updating region, input, resize, and clean exit;
- public types and members SHALL have XML documentation appropriate to the maturity of the package;
- README SHALL explain architecture, supported hosts, dependency boundaries, and a minimal example;
- NuGet package contents SHALL be inspected;
- symbols SHALL be generated;
- package version SHALL be `0.1.0`;
- `<Version>` and `<PackageVersion>` SHALL remain synchronized.

**0.1.0 completion gate:** a fresh consumer can install the package and build a small interactive application without repository-local project references.

---

# 7. Version 0.2.0 — Window Semantics

Version `0.2.0` SHALL deepen the screen/window layer after the ProcPs migration has exposed real usage patterns.

Candidate scope:

- rigorous parent/subwindow ownership semantics;
- derived windows;
- move/resize of windows;
- scrolling regions;
- insert/delete line;
- insert/delete character;
- borders and line drawing;
- overlay/overwrite operations;
- background cells;
- copy operations;
- touch/untouch semantics;
- synchronization of related windows;
- stronger damage tracking;
- refresh ordering and batching;
- cursor leave/placement policies;
- window-specific input configuration where appropriate.

**Completion criterion:** the window API is expressive enough for general TUI composition without requiring applications to manipulate the backing screen directly.

---

# 8. Version 0.3.0 — Input Completion

Version `0.3.0` SHALL mature terminal input beyond the minimum ProcPs set.

Candidate scope:

- complete terminfo-described function-key coverage;
- richer modifier representation;
- configurable Escape/key-sequence timing;
- sequence trie/DFA optimization;
- deterministic handling of overlapping terminal key strings;
- typeahead;
- flush/unget/pushback facilities where useful;
- half-delay and timeout-shaped read policies;
- nonblocking reads;
- input mode scopes;
- keypad mode semantics;
- application cursor-key mode semantics;
- richer error/disconnect events;
- test corpus for xterm, screen, tmux, Linux console, Windows Terminal, and other supported profiles.

A managed API SHOULD expose meaningful events rather than reproduce integer `KEY_*` constants as its only representation.

---

# 9. Version 0.4.0 — Unicode and Text Correctness

Version `0.4.0` SHALL freeze the text-to-cell contract.

Candidate scope:

- centrally versioned terminal-width tables/algorithm;
- grapheme-cluster-aware writing where appropriate;
- combining sequences;
- variation selectors;
- emoji presentation;
- zero-width joiner sequences;
- East Asian wide/fullwidth behavior;
- ambiguous-width policy;
- tab expansion;
- control-character policy;
- safe truncation and ellipsis helpers;
- text measurement APIs;
- normalization policy;
- bidirectional-text policy documented explicitly;
- extensive Unicode regression corpus.

The library is a terminal screen manager, not a general text-shaping engine. Unsupported shaping cases SHALL be documented rather than silently misrepresented.

---

# 10. Version 0.5.0 — Rendition and Color Completion

Version `0.5.0` SHALL mature styling and color portability.

Candidate scope:

- indexed color;
- bright colors where semantically distinguishable;
- 256-color terminals;
- direct RGB/truecolor;
- default foreground/background restoration;
- color-pair compatibility façade where useful;
- bold/intensity interaction;
- dim;
- italic when available;
- underline variants where available;
- reverse;
- blink where available;
- invisible/conceal where available;
- strikeout where available;
- capability-aware degradation;
- optimized style-transition emission;
- palette/query APIs only where they can be made deterministic and portable.

The primary managed API SHALL prefer semantic foreground/background/style values over historical packed curses attribute integers.

---

# 11. Version 0.6.0 — Pads and Large Virtual Surfaces

Version `0.6.0` SHALL support curses-style content larger than the physical terminal.

Candidate scope:

- pads;
- pad viewports;
- efficient viewport refresh;
- vertical and horizontal panning;
- large scrollback-like virtual surfaces;
- clipping and damage propagation across viewports;
- optimized terminal scrolling where capabilities permit;
- insert/delete-line optimization;
- scroll-region optimization;
- memory bounds and allocation policy for very large surfaces.

This version SHOULD make pagers, editors, inspectors, and large tables substantially easier to build.

---

# 12. Version 0.7.0 — Modern Terminal Interaction

Version `0.7.0` MAY add modern interactive features that are useful but not required by the initial ProcPs target.

Candidate scope:

- mouse reporting and decoding;
- wheel events;
- bracketed paste;
- focus-in/focus-out events;
- selected extended keyboard protocols where they can coexist with terminfo semantics;
- richer modifier reporting;
- optional terminal feature negotiation with explicit opt-in;
- safe enable/disable lifecycle for modern terminal modes.

Active probing SHALL remain separate from ordinary capability lookup unless explicitly designed and documented.

Graphics protocols, terminal emulation, and PTY implementation remain outside the core contract.

---

# 13. Version 0.8.0 — Platform and Lifecycle Hardening

Version `0.8.0` SHALL concentrate on production robustness.

Candidate scope:

- Windows Console and Windows Terminal parity audit;
- Linux terminal audit;
- macOS terminal audit;
- screen/tmux behavior;
- SSH/session behavior where reproducible;
- suspend/resume stress testing;
- redirected-stream diagnostics;
- terminal disappearance/disconnect;
- partial writes;
- broken pipes;
- cancellation races;
- disposal races;
- repeated session entry/exit;
- nested-session policy;
- exception recovery;
- large-screen performance;
- high-frequency refresh;
- allocation reduction;
- thread-safety and concurrency contract;
- deterministic flush semantics.

This version SHALL also decide whether low-level terminal control should remain consumed from `Icod.CommandFramework` or be extracted to a smaller neutral package.

Any such extraction SHALL preserve the higher-level `Icod.DCurses` API wherever reasonable.

---

# 14. Version 0.9.0 — Contract Freeze and Release Candidate

Version `0.9.0` is the API-regret and compatibility release.

Required work SHOULD include:

- audit every public type and member;
- remove accidental public surface;
- freeze naming conventions;
- freeze coordinate and dimension semantics;
- freeze session ownership semantics;
- freeze window ownership/lifetime semantics;
- freeze cell/style/color semantics;
- freeze input event semantics;
- freeze refresh/invalidation semantics;
- freeze exception and cancellation behavior;
- review sync versus async API choices;
- verify nullable annotations;
- review allocation-sensitive APIs;
- introduce public-API snapshot/compatibility tooling;
- validate package metadata;
- validate Source Link;
- validate symbols;
- build fresh-consumer projects;
- expand XML documentation;
- write conceptual documentation;
- publish migration guidance from the old ProcPs full-screen abstraction;
- perform cross-platform release-candidate testing.

No major new feature family SHOULD enter after the `0.9.0` contract freeze unless it prevents a viable `1.0.0`.

---

# 15. Version 1.0.0 — Stable Managed Curses Contract

Version `1.0.0` SHALL represent a production-ready, documented, supportable public contract.

The release SHALL provide a stable managed foundation for:

- full-screen command-line applications;
- process monitors;
- pagers;
- text-mode dashboards;
- interactive administration tools;
- editors;
- file managers;
- installers;
- other terminal user interfaces.

A `1.0.0` release does **not** imply source- or ABI-compatible reproduction of every historical `ncurses` function.

The stable promise is the managed `Icod.DCurses` contract.

### 15.1 1.0 completion gate

Before `1.0.0`:

- `top`, `slabtop`, and `watch` SHALL no longer require private generic full-screen terminal infrastructure;
- supported Windows, Linux, and macOS configurations SHALL pass CI;
- public API compatibility tooling SHALL be in place;
- package installation SHALL be validated from a fresh consumer;
- README and conceptual documentation SHALL be complete;
- XML documentation SHALL be suitable for generated API reference;
- examples SHALL cover both simple and interactive use;
- terminal restoration SHALL be demonstrated under normal exit, cancellation, exceptions, resize, and supported suspend/resume;
- no known correctness defect SHALL remain in the virtual-to-physical refresh model;
- Unicode behavior SHALL be documented and tested;
- dependency boundaries SHALL be documented;
- package version and assembly version policy SHALL be frozen;
- NuGet and GitHub Packages publishing SHALL be repeatable.

---

## 16. Explicit 1.0 Non-Goals

The following are not required for the core `Icod.DCurses 1.0.0` contract:

- native `ncurses` ABI compatibility;
- exhaustive source-level compatibility with the C curses API;
- terminal emulation;
- pseudo-terminal creation or management;
- SSH transport;
- Sixel rendering;
- Kitty graphics;
- iTerm2 image protocols;
- general-purpose ANSI terminal emulation of arbitrary child output;
- shell/process execution;
- ProcPs-specific process models;
- forms;
- menus;
- a full panel library;
- widget/toolkit frameworks;
- a declarative UI framework.

Forms, menus, panels, widgets, or compatibility façades MAY later be implemented as focused extensions or sibling packages if there is sufficient demand.

---

## 17. Cross-Cutting Engineering Rules

The project SHALL maintain the following rules throughout development:

1. Keep `<Version>` and `<PackageVersion>` synchronized with the active development release.
2. Maintain `Debug`, `Staging`, and `Release` configurations across the solution.
3. `Release` SHALL treat warnings as errors, with only intentional documented exceptions.
4. Public/protected/internal methods SHALL validate parameters at entry where validation is applicable.
5. Braces SHALL be used for `if`/`else` bodies, including single statements.
6. Terminal capability output SHALL go through `Icod.TermInfo` when a terminfo capability models the operation.
7. Tests SHALL not require an interactive console unless explicitly categorized as integration/manual tests.
8. Tests SHALL not write unsolicited output to standard output or standard error.
9. Platform interop SHALL be isolated behind narrow abstractions.
10. Resource ownership and terminal restoration SHALL be explicit and testable.
11. Application-specific behavior SHALL not migrate into DCurses merely because one acceptance application needs it.
12. Public API additions SHALL be treated as contract decisions, especially after `0.7.0`.
13. Repository files SHALL remain portable across Windows, Linux, and macOS development environments.
14. Patches and generated files SHALL avoid trailing whitespace and brittle assumptions about local line-ending conversion.

---

## 18. Immediate Development Sequence

The active implementation sequence for `0.1.0` is:

```text
T01  repository / solution / package scaffold
  -> T02  terminal backend boundary
  -> T03  CursesSession lifecycle
  -> T04  resize / suspend / resume / termination events
  -> T05  key and input event decoder
  -> T06  cell / style / virtual screen
  -> T07  standard screen / windows
  -> T08  physical refresh and damage synchronization
  -> T09  dimensions / resize repaint contract
  -> T10  essential presentation operations
  -> T11  Unicode baseline
  -> Icod.Terminal T10 integration / terminal-substrate reset (Alpha-12 through Alpha-14; complete)
  -> T12  top / slabtop / watch acceptance harnesses
  -> T13  docs / samples / package / release gate
  -> 0.1.0
```

The current implementation tranche is therefore **T12**, exercising the shared stack against the `top`, `slabtop`, and `watch` acceptance requirements.
