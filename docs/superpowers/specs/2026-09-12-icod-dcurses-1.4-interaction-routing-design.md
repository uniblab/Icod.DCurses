# Icod.DCurses 1.4 Interaction Routing Design

**Date:** 2026-09-12  
**Status:** approved architectural direction; T1401 freezes exact public naming and numeric bounds before implementation  
**Release:** `Icod.DCurses 1.4.0`  
**Baseline:** published `Icod.DCurses 1.3.0` at `c10ca043a666b85225f2d3b8955a1ac2075b0d31`

## Purpose

Version 1.4 adds an interaction-routing substrate above the semantic input already provided by DCurses and the live-terminal ownership already provided by `Icod.Terminal`.

The feature exists so applications and future widget libraries can answer four questions deterministically:

1. Which logical region is currently focused?
2. Which logical region is under a mouse coordinate?
3. Does a semantic key gesture map to an application command in the current context?
4. What pointer shape does the target prefer?

The feature does not execute application commands, draw controls, own application layout, read terminal bytes, or create a second event loop.

## Architectural ownership

### Application

The application owns:

- its event loop;
- creation/disposal of the interaction router and regions;
- ordinary region geometry and relayout policy;
- whether a mouse click changes logical focus;
- execution of command identities returned by routing;
- whether/when a returned pointer preference is applied;
- all higher-level widget/application behavior.

### Icod.DCurses

DCurses owns:

- bounded region registration;
- deterministic region identity/order;
- hit testing and local-coordinate translation;
- panel-aware overlap precedence;
- logical focus/traversal/repair;
- semantic gesture normalization and binding lookup;
- structured routing results;
- curses-shaped pointer semantics and the wrapper over Terminal pointer leases.

### Icod.Terminal

Terminal remains authoritative for:

- the one terminal input reader;
- decoding bytes into semantic input;
- query/response correlation;
- mouse/focus/keyboard/paste reporting protocol ownership;
- pointer-shape wire protocol, lifetime, nesting, cleanup, and lifecycle behavior;
- terminal lifecycle and output serialization.

No 1.4 DCurses type parses raw terminal escape/protocol frames.

## Router model

The design uses one application-owned router bound to one `CursesScreen`.

Binding a router to a screen gives hit testing a stable coordinate authority and lets panel-associated regions consult the screen's existing panel ownership/z-order without creating another scene graph.

The router does not consume `CursesSession.ReadEventAsync()` itself. The application passes already-decoded `CursesInputEvent` values to routing operations explicitly.

A router is single-writer under the same broad application ownership expectations as logical screens/windows/panels. T1401 freezes the exact documented concurrency contract before source implementation.

## Interaction regions

A region represents a logical interaction target. It is not a rendering object.

Each active region has:

- a stable router-local identity;
- deterministic registration ordinal;
- geometry;
- enabled state;
- focusable state;
- traversal-order metadata;
- hit-test precedence metadata where required by the frozen contract;
- optional pointer-shape preference;
- local command bindings.

Disposal is one-way. A disposed region cannot be reattached or routed.

### Ordinary regions

An ordinary region stores screen-relative `CursesRectangle` geometry supplied by the application.

Screen resize does not silently rewrite that geometry. Effective hit geometry is clipped against current `CursesScreen.Bounds`. Applications remain responsible for explicit relayout, consistent with the 1.3 ownership model.

### Panel-associated regions

A panel-associated region stores panel-relative geometry and a reference to one active panel owned by the router's screen.

Effective hit geometry is:

```text
panel-relative region
    -> translated through current panel origin
    -> clipped to current panel bounds
    -> clipped to current screen bounds
```

Panel movement, resize, z-order changes, show/hide, and disposal therefore affect routing without requiring the application to destroy/re-register the region.

Association with a panel owned by another screen is rejected before mutation.

A hidden or disposed panel makes associated regions ineligible.

## Hit-test precedence

Hit testing returns at most one target.

The precedence model is:

```text
1. eligible panel-associated regions
      topmost panel first
      then frozen same-panel region precedence

2. eligible ordinary screen regions
      frozen screen-region precedence
```

Within equal precedence, deterministic registration order is the final tie-break.

The exact direction of the registration tie-break and any explicit region-priority property are frozen by T1401 and then tested as public behavior.

Panel visual transparency is not input transparency. `BlankCellsTransparent` affects composition, not hit-test ownership. Click-through behavior must be expressed by interaction geometry/policy rather than inferred from rendered blank cells.

A successful hit includes the region identity and region-local row/column coordinates.

## Eligibility

A region is eligible for hit testing only when all required state is active under the frozen contract. At minimum this includes:

- not disposed;
- enabled;
- non-empty effective geometry;
- for panel-associated regions, an active visible owning panel.

A region is focus-eligible only when it is hit-test/application eligible as defined by T1401 and is additionally marked focusable.

The contract must keep enabled, focusable, visible/effective, and disposed states distinct rather than folding them into one ambiguous Boolean.

## Logical focus

Logical focus belongs to the interaction router and is independent of `CursesFocusEvent`.

The public model supports:

- query current focus;
- explicitly focus a region;
- explicitly clear focus;
- move focus forward;
- move focus backward.

Version 1.4 intentionally omits spatial directional focus navigation.

Traversal order is deterministic:

```text
explicit traversal order
    -> registration ordinal tie-break
```

T1401 freezes wrap behavior. The approved default direction is to wrap once when moving forward/backward, because that matches conventional terminal Tab/Shift+Tab traversal and avoids requiring applications to implement an extra wrap layer.

### Focus repair

When the current focused region becomes ineligible, focus repair selects the next eligible region in deterministic forward traversal order beginning after the previous focused position, wrapping at most once. If no region is eligible, focus becomes empty.

Repair occurs at explicit interaction/focus boundaries rather than through a hidden background worker.

Terminal focus loss does not clear logical focus. The application may choose to react to terminal focus events separately.

Mouse hit testing does not automatically mutate logical focus.

## Keyboard gestures

Gestures are immutable semantic values. They do not contain terminal escape sequences.

The gesture model is built over existing DCurses semantics:

- `CursesKey`;
- character identity where applicable;
- `CursesKeyModifiers`;
- function-key number where applicable;
- `CursesKeyEventPhase`.

Normal command gestures default to press behavior. Repeat/release matching is opt-in only where the frozen gesture contract permits it.

CapsLock/NumLock are reported state bits and should not accidentally prevent ordinary command matching. T1401 freezes whether they are ignored by default with an explicit exact-state option, or represented separately in gesture matching. The implementation must choose one documented rule before T1405.

### Traditional text and modern Character parity

Terminal environments may report a printable character as ordinary `Text` or as a semantic Character key event under negotiated modern keyboard reporting.

A common command such as unmodified `q` must not require application code to branch on the terminal protocol family. T1401/T1405 freeze one normalization rule which maps equivalent traditional and modern semantic character input to the same gesture match where appropriate.

This normalization must not turn arbitrary pasted text into command gestures.

## Commands and bindings

Commands are identities, not delegates.

A region may bind gestures to command identities. The router may also contain global bindings.

Resolution order is:

```text
focused-region binding
    -> router-global binding
    -> unmatched input
```

Duplicate gestures inside one binding scope are rejected before mutation. The router does not silently choose between duplicates by registration order.

Routing a matched command returns the command identity and target context. Application code decides what that command does.

## Routed input

The router accepts already-normalized `CursesInputEvent` values.

The intended behavior is:

- `Key`: evaluate semantic command gestures, then target focused region according to the frozen result model;
- `Text`: target the focused region and participate in character-command normalization only under the narrow rule frozen by T1401;
- `Paste`: target the focused region; paste payload is never reinterpreted as a sequence of command gestures;
- `Mouse`: hit test screen coordinates and return target/local coordinates plus pointer preference;
- terminal `Focus`: preserve as terminal focus information and do not convert it into logical region focus;
- `EndOfInput`: no interaction target.

The router never consumes lifecycle events directly; lifecycle/resize effects are observed through current screen/panel state when routing or focus operations occur.

## Structured routing result

Routing returns data sufficient for the application to act without callbacks.

The exact public shape is frozen by T1401, but the semantic result needs to distinguish:

- no target/no command;
- targeted unmatched input;
- targeted command match;
- global command match;
- mouse target and local coordinates;
- desired pointer-shape preference where one exists.

The original normalized input should remain available where needed so a future widget layer can consume text/key/paste payload without reconstructing it.

## Pointer shapes

DCurses exposes its own semantic pointer vocabulary rather than adding `TerminalPointerShape` to the public dependency boundary.

The DCurses vocabulary maps exhaustively to the Terminal vocabulary accepted for the release.

The router only reports a desired pointer shape as data. Hit testing and routing are synchronous and emit no terminal output.

An explicit `CursesSession` pointer operation acquires a DCurses-owned lease which delegates to `TerminalSession.AcquirePointerShapeAsync(...)` internally. Terminal remains responsible for nesting, restoration/reset, failure, cancellation, and lifecycle behavior.

DCurses must not duplicate OSC 22 framing or state recovery.

## Resize and lifecycle behavior

Screen resize follows the 1.3 explicit-layout model:

```text
terminal resize
    -> CursesSession synchronizes Screen dimensions
    -> application recomputes ordinary layout
    -> application updates ordinary region geometry
```

Before application relayout, interaction operations clip ordinary regions to current screen bounds. They do not mutate stored application geometry implicitly.

Panel-associated regions follow current panel geometry and z-order automatically because those are properties of the referenced retained panel.

Suspend/resume does not destroy region registrations or logical focus merely because Terminal invalidates live protocol state. Eligibility/focus repair still applies if current geometry/panel state makes a region unavailable.

Pointer leases continue to follow Terminal lifecycle semantics.

## Bounds and resource policy

The interaction system must be bounded. T1401 freezes concrete maximum counts for:

- live regions per router;
- local command bindings per region;
- global command bindings per router;
- any auxiliary index/cache whose size scales with registrations.

Bound exhaustion must fail before mutation. Disposal/removal must release local capacity deterministically.

No unbounded event queue, callback queue, command queue, or hidden background task is added.

## Error model

Public/protected/internal methods validate parameters at entry under repository conventions.

Invalid enum/geometry/options values use argument exceptions. Foreign-screen association and incompatible ownership use argument/invalid-operation failures according to the frozen T1401 API contract. Operations on disposed router/region/lease objects follow the library's established disposed-object conventions.

Routing ordinary no-target/no-command input is not exceptional; it is a normal structured result.

## Public dependency boundary

The published 1.3 allow-list intentionally exposes only a small set of Terminal/TermInfo types.

Version 1.4 should preserve that allow-list. Pointer shape and interaction types are DCurses-owned public abstractions. Any proposed new public Terminal/TermInfo type requires an explicit dependency-boundary review and user approval.

## Performance model

Hit testing, focus traversal, and gesture matching are expected hot-path operations.

The design favors:

- deterministic bounded collections;
- immutable value inputs where practical;
- no callback dispatch inside the core router;
- no terminal I/O inside routing;
- no culture-sensitive comparisons;
- steady-state allocation avoidance for repeated common routing paths where the chosen public model permits it.

T1410 freezes and measures concrete allocation/performance gates using a repeatable multi-sample protocol rather than relying on one noisy allocation sample.

## Explicit non-goals

Version 1.4 does not define:

- widgets/controls;
- widget parent/child hierarchy;
- event capture/bubbling;
- automatic focus-on-click;
- directional/spatial focus;
- generalized pointer capture;
- drag/drop framework;
- automatic layout/flex/grid/constraints;
- accessibility tree;
- command callbacks/DI container;
- application navigation framework;
- raster scene graph;
- animation;
- PTY/process hosting.

If a future widget package can build on 1.4 without bypassing it, while applications which do not want widgets can use the router directly, the abstraction boundary is successful.

## Acceptance scenarios

The final 1.4 design must support these scenarios through public APIs:

1. two ordinary panes receive explicit layout rectangles; Tab/Shift+Tab traverses their focus regions deterministically;
2. a popup `CursesPanel` overlaps both panes and its interaction region wins mouse hit testing while visible/topmost;
3. hiding or disposing the popup removes it from hit-test eligibility and repairs focus deterministically if it owned focus;
4. a local `Ctrl+S` binding wins over a global `Ctrl+S` binding while its region is focused;
5. an unmodified character command behaves consistently whether the terminal produces traditional text or modern Character-key semantics under the frozen normalization rule;
6. pasted data reaches the focused target without being exploded into shortcut commands;
7. mouse routing reports region-local coordinates;
8. a target advertises a pointer preference without hit testing performing I/O; an explicit DCurses session pointer lease applies the preference through Terminal;
9. terminal focus-out/focus-in events do not erase the current logical focus;
10. terminal resize plus explicit application relayout updates ordinary region geometry without any hidden layout owner;
11. panel movement/reorder/resize changes effective panel-region routing without re-registration;
12. repeated equivalent state/input produces equivalent routing results across supported platforms.

## T1401 exit rule

T1401 is complete only when exact public names/signatures, concrete collection bounds, tie-break direction, focus wrap behavior, modifier-lock matching rule, text/Character gesture normalization, error behavior, and public dependency-boundary effects are all written down and accepted with no unresolved placeholders.

Only then may T1402 begin production implementation.
