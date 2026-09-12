# Icod.DCurses 1.4.0 Development Roadmap

**Project:** `Icod.DCurses`  
**Release line:** `1.4.0`  
**Development branch:** `1.4.0-interaction-routing`  
**Published baseline:** `1.3.0`  
**Baseline tag:** `v1.3.0`  
**Baseline commit:** `c10ca043a666b85225f2d3b8955a1ac2075b0d31`  
**Planning-stage source identity:** `1.3.0` until the first implementation tranche  
**Final target package:** `1.4.0`  
**Assembly version policy:** retain `1.0.0.0` for compatible additive 1.x development  
**Baseline runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Theme:** deterministic interaction routing over semantic input, immutable geometry, retained panels, and Terminal-owned protocol state  
**Status:** architecture approved; T1401 contract freeze is the first gate

---

## Release objective

`Icod.DCurses 1.4.0` adds deterministic, application-owned interaction routing over the existing curses semantic-input and geometry foundations.

The release should make it possible to build higher-level controls later without requiring those controls to:

- parse terminal protocols;
- create a second input reader;
- invent a second geometry system;
- reimplement panel overlap precedence;
- reimplement focus traversal;
- reimplement key-chord matching;
- expose `Icod.Terminal` protocol types through their own public surface.

The intended application flow is:

```text
CursesSession.ReadEventAsync(...)
        |
        v
CursesInputEvent
        |
        v
application-owned interaction router
   |                   |
   |                   +--> semantic gesture -> command identity
   |
   +--> mouse coordinate -> target region
                            -> local coordinate
                            -> pointer-shape preference

logical focus
   -> current region
   -> forward/backward traversal
   -> deterministic repair when eligibility changes
```

The router never becomes a hidden application event loop. Applications continue to own their event loop and explicitly decide what routed results mean.

## Published 1.3 compatibility floor

The 1.4 branch starts from the published 1.3 contract:

```text
51 exported types
406 canonical declared contract lines
sha256 a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424
```

The 1.3 geometry contract is foundational to 1.4:

```text
CursesRectangle
CursesInsets
CursesLayout
CursesScreen.Bounds
CursesWindow.Bounds / SetBounds
CursesPanel.Bounds / Resize / SetBounds
```

Version 1.4 is additive by default. Any breaking correction to the published 1.3 contract requires a concrete regret finding and explicit approval before implementation.

## Architectural invariants

The following rules apply to every 1.4 tranche.

### Terminal ownership

`Icod.Terminal` remains authoritative for:

- the single terminal input reader;
- byte decoding and query-response routing;
- rich-input protocol acquisition/restoration;
- terminal/window-manager focus reports;
- terminal pointer-shape transport and lease lifetime;
- session lifecycle and state invalidation;
- output serialization.

DCurses routes only already-normalized curses events and never adds raw CSI/OSC/DCS/APC parsing or a competing reader.

### Geometry ownership

`CursesRectangle` is the only terminal-cell rectangle vocabulary. Interaction routing does not create another rectangle/point hierarchy merely for hit testing.

Ordinary interaction regions use screen-relative geometry. Panel-associated regions use panel-relative geometry which is translated and clipped against the panel's current screen-relative bounds.

### Interaction is not rendering

Interaction regions are not windows, panels, widgets, controls, event handlers, or layout owners. They are bounded logical targets plus routing metadata.

The interaction subsystem does not draw, mutate cells, own layout, or automatically resize application geometry.

### Structured results, not callbacks

Routing returns deterministic data describing target, command, local coordinates, and pointer preference. The core router does not invoke arbitrary application delegates while resolving input.

This avoids hidden reentrancy, makes routing independently testable, and keeps command execution/application policy outside the library mechanism.

### Logical focus is distinct from terminal focus

`CursesFocusEvent` continues to mean an actual terminal focus-in/focus-out report. The new 1.4 logical focus model identifies the application interaction region which should receive focused input.

A terminal focus-out report does not erase the application's logical focused region.

### Determinism and bounds

Registration, traversal, hit-test precedence, gesture resolution, command binding, and focus repair must have deterministic tie-breaking. Registries and binding collections must have documented finite bounds frozen by T1401 before implementation.

## Working public API concepts

The following names describe the approved architecture but are **working names until T1401 freezes the public contract**:

```text
CursesInteractionRouter
CursesInteractionRegion
CursesInteractionRegionOptions
CursesInteractionResult
CursesInteractionTarget
CursesFocusDirection
CursesKeyGesture
CursesCommand / CursesCommandId
CursesPointerShape
CursesPointerShapeLease
```

T1401 may rename or consolidate these concepts, but it may not weaken the architectural invariants above without explicit design reapproval.

## T1401 — interaction contract and public-surface candidate freeze

### Objective

Freeze the semantic contract before production implementation starts.

### Deliverables

- define the ownership relationship between `CursesScreen`, the application, and one interaction router;
- define region identity and one-way disposal semantics;
- freeze screen-relative versus panel-relative coordinate rules;
- freeze effective clipping against screen/panel bounds;
- freeze panel z-order precedence and same-scope region tie-breaking;
- freeze visual-transparency versus input-transparency semantics;
- define enabled/focusable/visible/eligible state;
- define logical focus, traversal ordering, wrap behavior, and deterministic repair;
- define which `CursesInputEventKind` values are routable and how each is represented in routing results;
- define semantic key-gesture normalization across traditional text delivery and modern Character key reports;
- define gesture conflict rules and local-versus-global command precedence;
- define pointer-shape preference semantics without terminal I/O in hit testing;
- freeze concrete maximum region/binding counts and overflow behavior;
- freeze single-writer/concurrency expectations;
- freeze exception/result behavior for invalid/disposed/foreign objects;
- produce an initial public API candidate and dependency-boundary impact audit;
- record explicit exclusions for widgets, event bubbling/capture, auto-focus policy, and pointer capture.

### Acceptance gate

No production API implementation begins until the design/spec and 1.4 roadmap agree on all items above and contain no unresolved semantic placeholders.

## T1402 — bounded interaction-region registry

### Objective

Implement the deterministic region/lifetime substrate without input routing or Terminal I/O.

### Deliverables

- create the frozen router/region public contracts from T1401;
- register ordinary screen-relative regions;
- register panel-associated regions with panel-relative bounds;
- expose immutable region identity/configuration observations required by callers;
- support explicit enable/disable, focusability changes, geometry changes, and one-way disposal according to the frozen contract;
- assign deterministic registration ordinals internally;
- enforce T1401 registry bounds before mutation;
- reject foreign-screen panel association atomically;
- keep the router application-owned and free of background work.

### Tests

- argument validation before mutation;
- bound exhaustion and recovery after disposal;
- registration ordinal stability;
- disposed-region behavior;
- foreign panel rejection;
- ordinary and panel-relative geometry snapshots;
- no terminal I/O and no event-loop ownership.

### Version gate

T1402 is the first source tranche. At its start, promote the development package to `1.4.0-alpha.1` while retaining `AssemblyVersion 1.0.0.0`.

## T1403 — deterministic hit testing and panel precedence

### Objective

Map terminal-cell coordinates to exactly one interaction target under explicit overlap rules.

### Deliverables

- hit test zero-based screen row/column coordinates;
- translate successful hits to region-local row/column coordinates;
- clip ordinary regions to current `CursesScreen.Bounds`;
- translate and clip panel-associated regions to current panel bounds;
- make hidden or disposed panel associations ineligible;
- use current panel z-order as the first discriminator among overlapping panel-associated targets;
- use T1401-frozen region priority/order rules within the same panel/screen scope;
- place ordinary screen regions below panel-associated regions unless T1401 explicitly freezes a reviewed overlay exception;
- keep blank-transparent visual cells input-opaque by default; click-through requires explicit interaction geometry/policy rather than inferred cell transparency.

### Tests

- edge/corner containment;
- clipped/empty effective rectangles;
- overlapping ordinary regions;
- overlapping panels and same-panel regions;
- panel move/reorder/hide/dispose behavior without re-registration;
- deterministic repeated results;
- no allocations in the accepted steady-state hot path where practical.

## T1404 — logical focus, traversal, and repair

### Objective

Provide application logical focus independent of terminal focus reporting.

### Deliverables

- query current logical focus;
- explicitly focus an eligible region;
- explicitly clear focus;
- move focus forward/backward through eligible regions;
- use explicit traversal order followed by registration ordinal as the deterministic order;
- define and implement wrap behavior exactly as frozen in T1401;
- repair focus when the current region becomes disabled, non-focusable, disposed, fully clipped, or associated with a hidden/disposed panel;
- preserve logical focus across terminal `Focused`/`Unfocused` reports;
- do not automatically focus a region merely because a mouse event hits it.

### Tests

- traversal order and ties;
- forward/backward wrap;
- no-eligible-region behavior;
- region state transitions;
- panel hide/dispose/resize repair;
- screen clipping repair;
- terminal-focus independence.

## T1405 — semantic keyboard gesture model

### Objective

Represent and match application gestures without protocol/backend branching.

### Deliverables

- implement the T1401-frozen immutable gesture contract;
- match semantic named keys, function keys, character identity, modifiers, and selected key phases;
- default ordinary command gestures to press semantics while allowing explicitly supported repeat/release matching;
- define exact handling of CapsLock/NumLock state according to T1401;
- normalize traditional text input and modern Character-key reports so common character commands do not require terminal-family branching;
- reject impossible/ambiguous gesture constructions at creation time;
- keep gesture equality/hash behavior deterministic and culture-independent.

### Tests

- named/navigation/function keys;
- modified character gestures;
- traditional text versus modern Character parity;
- Press/Repeat/Release distinctions;
- modifier validation;
- culture invariance;
- equality/hash stability.

## T1406 — command bindings and structured interaction routing

### Objective

Compose focus, gestures, mouse hit testing, and command identity into one deterministic routing mechanism.

### Deliverables

- bind semantic gestures to command identities within an interaction region;
- bind router-global gestures;
- reject duplicate gestures within one binding scope;
- resolve focused-region bindings before router-global bindings;
- route ordinary text/key/paste input to the current logical target according to the frozen T1401 contract;
- route mouse events through T1403 hit testing and carry region-local coordinates;
- represent unmatched-but-targeted input distinctly from matched commands;
- preserve the original normalized input payload in the structured result where the contract requires it;
- do not invoke application callbacks or automatically mutate focus.

### Tests

- local-versus-global binding precedence;
- duplicate detection;
- unmatched focused input;
- command match result identity;
- mouse target/local-coordinate result;
- no-focus and no-hit outcomes;
- deterministic replay of equivalent input/state.

## T1407 — pointer-shape abstraction and Terminal-owned lease integration

### Objective

Expose curses-shaped semantic pointer preferences without leaking Terminal protocol types or performing I/O during routing.

### Deliverables

- add the frozen DCurses pointer-shape semantic enum/value set;
- map it exhaustively to `Icod.Terminal` pointer-shape semantics internally;
- add a DCurses-owned asynchronous pointer-shape lease wrapper on `CursesSession`;
- preserve Terminal's nesting, cleanup, lifecycle, and failure ownership rather than recreating it;
- allow an interaction region to advertise an optional preferred pointer shape;
- include the desired pointer shape in hit-test/routing observations;
- require explicit application/session action to apply a changed shape;
- keep synchronous hit testing/routing free of terminal output/query operations;
- retain the existing public Terminal/TermInfo dependency allow-list unless a separately justified review changes it.

### Tests

- full mapping coverage;
- nested lease behavior through the DCurses wrapper;
- cancellation/failure propagation;
- pointer preference from hit results;
- no I/O during hit testing;
- public dependency-boundary audit.

## T1408 — resize, panel, disposal, and lifecycle coherence

### Objective

Prove that interaction state remains deterministic while geometry and retained surfaces change.

### Deliverables

- preserve application-owned ordinary region geometry rather than silently relayout it on screen resize;
- recompute effective clipping against current screen bounds on interaction operations;
- follow current panel move/resize/reorder/visibility/disposal state for panel-associated regions;
- repair logical focus at deterministic interaction/focus boundaries when eligibility has changed;
- define behavior across Terminal suspend/resume without creating a second lifecycle owner;
- preserve region/command registrations across ordinary session lifecycle invalidation unless the region itself becomes ineligible;
- keep pointer-shape restoration authoritative in Terminal-owned leases.

### Tests

- repeated screen grow/shrink with explicit application relayout;
- panel movement/resizing/order churn;
- hidden/disposed panel targets;
- focused region becoming clipped/ineligible;
- suspend/resume while regions and pointer leases exist;
- disposal races permitted by the single-writer contract;
- repeated lifecycle cycles with deterministic route results.

## T1409 — application acceptance sample

### Objective

Prove the 1.4 mechanisms compose into a realistic TUI without a widget framework.

### Sample scenario

Create an interactive sample containing:

- header/status regions;
- two ordinary layout-driven panes;
- a retained popup/dialog panel above the panes;
- Tab and Shift+Tab focus traversal;
- local shortcuts and at least one global command;
- mouse targeting with region-local coordinates;
- a panel-overlap case proving topmost precedence;
- pointer-shape preference changes over different targets;
- live terminal resize followed by explicit application relayout;
- terminal focus events demonstrating they do not erase logical focus.

The sample must consume only public 1.4 APIs and must not use internal router state or Terminal protocol details.

## T1410 — performance, allocation, bounds, and adversarial hardening

### Objective

Qualify the interaction core as a hot-path mechanism rather than a demo-only abstraction.

### Deliverables

- benchmark/accept repeated hit testing, focus traversal, and gesture routing with representative region counts;
- freeze steady-state allocation expectations for the common routing path;
- stress registration/disposal churn up to documented bounds;
- stress overlapping panel/region topologies;
- stress repeated geometry/focus eligibility changes;
- verify invalid input and disposed objects fail before partial mutation;
- verify deterministic results across repeated equivalent runs;
- audit for unbounded collections, accidental callback/reentrancy paths, and hidden Terminal I/O.

### Acceptance gate

Performance thresholds and allocation gates must be documented with a repeatable measurement protocol, following the same evidence-first approach used by 1.3 T1309.

## T1411 — public API, package, documentation, licensing, and regret gate

### Objective

Decide whether the complete 1.4 surface is suitable for stable 1.x publication.

### Deliverables

- generate the compiler-derived 1.4 public API fingerprint on all target frameworks;
- compare every additive public type/member against the published 1.3 baseline;
- conduct naming/mutability/ownership/ambiguity/nullability/exception regret review;
- verify the Terminal/TermInfo public dependency boundary;
- add fresh NuGet-only consumer coverage for regions, focus, gestures, routing, hit testing, and pointer-shape wrapper APIs;
- update XML documentation and README examples;
- audit samples and source headers/licensing;
- update package release notes;
- ensure documentation clearly distinguishes application logical focus from terminal focus and routing data from callback execution.

No RC promotion occurs while an identified API regret remains unresolved.

## T1412 — release candidate and stable-source closure

### Objective

Promote the unchanged qualified implementation through RC and stable identity.

### Sequence

1. promote the T1411-qualified source/API to `1.4.0-rc.1`;
2. run the complete Staging/package/runtime matrix on the exact RC head;
3. correct only demonstrated release-blocking defects, with full requalification after any correction;
4. if the RC is green and API/behavior remain accepted, promote the same implementation/API to stable-source `1.4.0`;
5. run the final exact-head package candidate plus Windows/Linux/macOS x64/ARM64 matrix;
6. merge only after explicit approval;
7. qualify the resulting `main` Release build before tagging/publishing.

Tagging, GitHub Release creation, and NuGet publication remain separate explicit release actions.

## Cross-cutting qualification policy

Every tranche follows repository conventions:

- C# 13 and existing 1TBS formatting conventions;
- braces on every `if`/`else` block;
- multiline call closing parentheses on their own line;
- public/protected/internal parameter validation at method entry;
- tests do not write stdout/stderr except process communication;
- warnings-as-errors under PR Staging qualification;
- no private/raw Terminal protocol implementation in DCurses;
- no new public Terminal/TermInfo types without explicit dependency-boundary review;
- package-only consumer validation before stable promotion;
- Windows, Linux, and macOS runtime qualification including x64/ARM64 where the repository matrix applies;
- public API fingerprint equality across `net8.0`, `net9.0`, and `net10.0`.

## Deliberate non-goals

Version 1.4 does not provide:

- widgets or controls;
- widget parent/child ownership;
- capture/bubble event propagation;
- automatic focus-on-click;
- spatial directional focus navigation;
- generalized mouse/pointer capture unless separately approved after a concrete deficiency is demonstrated;
- drag/drop framework semantics;
- flexbox/grid/constraint layout;
- retained automatic layout;
- accessibility-tree ownership;
- dependency injection or command-handler registration;
- application navigation/routing frameworks;
- raster placement/scene graphs;
- animation;
- PTY/process hosting.

## Definition of done

`Icod.DCurses 1.4.0` is repository-side complete only when:

- T1401-T1411 are complete with no unresolved contract/API regrets;
- the exact RC passes the full package/runtime matrix;
- the unchanged accepted implementation/API is promoted to stable source;
- the stable-source exact head passes the full package/runtime matrix;
- all supported target frameworks share one frozen public API fingerprint;
- package-only consumption proves the interaction surface from the packed artifact;
- the published 1.3 compatibility floor remains supported;
- no widget framework, hidden input loop, raw Terminal protocol path, or unbounded interaction registry has entered the package.
