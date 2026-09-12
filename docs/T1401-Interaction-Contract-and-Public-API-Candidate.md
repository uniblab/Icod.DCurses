# T1401 — Interaction Contract and Public API Candidate

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1401  
**Published compatibility floor:** `1.3.0`  
**Baseline tag:** `v1.3.0`  
**Baseline commit:** `c10ca043a666b85225f2d3b8955a1ac2075b0d31`  
**Baseline public API:** 51 exported types / 406 canonical contract lines  
**Baseline API SHA-256:** `a655bd85e3c88f5bf38ad0d43a148e3bd06a9e943bbf3e3aa2e21575ffb07424`  
**Runtime dependencies at tranche entry:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Source/package identity during T1401:** `1.3.0`  
**AssemblyVersion:** `1.0.0.0`  
**Status:** contract/public-surface candidate frozen; implementation begins only after exact-head qualification

---

## Purpose

T1401 freezes the semantic and public-contract decisions that the remaining 1.4 tranches must implement. It deliberately contains no production implementation and does not change the package identity.

The 1.4 release remains an **interaction-routing foundation**, not a widget framework. Applications continue to own their event loop and application policy. DCurses provides deterministic mechanisms for interaction regions, hit testing, logical focus, semantic key gestures, command identities, structured route results, and pointer-shape requests.

Where the initial 1.4 design document used working names or deferred a concrete bound/tie-break rule to T1401, this document is the T1401 freeze authority.

## Ownership model

### Router ownership

`CursesInteractionRouter` is application-owned and is associated with exactly one `CursesScreen` for its lifetime.

The normal application model is one router for one logical interaction domain. DCurses does **not** enforce a singleton router per `CursesScreen`; two independently created routers may reference the same screen, but they do not coordinate focus, registrations, commands, or routing state.

A router:

- does not own or dispose its `CursesScreen`;
- does not own or dispose associated `CursesPanel` instances;
- does not read terminal input;
- does not subscribe a hidden application event loop;
- does not perform layout;
- does not perform terminal I/O;
- owns its registered interaction regions and gesture-binding registries;
- uses the current screen and panel state only when resolving effective geometry and eligibility.

Router disposal is one-way and idempotent. It disposes/removes all of its regions and clears all gesture bindings, but it does not dispose the screen or panels.

### Region ownership

A `CursesInteractionRegion` belongs to exactly one router and cannot move to another router.

Region disposal is one-way and idempotent. Disposal:

- removes the region from future hit testing, focus traversal, and routing;
- releases that region's gesture bindings from the router's total binding budget;
- repairs focus immediately when the disposed region is currently focused;
- does not dispose an associated panel;
- leaves immutable/observational region properties readable so previously returned routing results remain diagnosable.

Any region mutation or binding operation after disposal throws `ObjectDisposedException`.

## Geometry and coordinate spaces

`CursesRectangle` remains the only terminal-cell rectangle vocabulary.

### Ordinary regions

When `CursesInteractionRegionOptions.Panel` is `null`, `Bounds` is screen-relative.

The declared rectangle does not need to fit inside the current screen and may be empty. Effective geometry is:

```text
region.Bounds ∩ router.Screen.Bounds
```

A completely clipped or empty region is not visible for interaction purposes.

### Panel-associated regions

When `Panel` is non-null, `Bounds` is relative to the panel's retained content coordinate space.

The declared rectangle may extend outside the panel and may be empty. Its screen-space origin is:

```text
panel.Bounds origin + region.Bounds origin
```

The effective hit/focus rectangle is clipped by both the current panel bounds and current screen bounds.

Registration rejects a panel owned by another screen atomically. Registration of an already-disposed panel throws `ObjectDisposedException`.

A region associated with a panel follows the panel's current move, resize, z-order, visibility, and disposal state without re-registration. Panel disposal does not dispose the region; it makes that region permanently ineligible until the region itself is disposed.

### Local coordinates

A successful hit reports `LocalRow` and `LocalColumn` relative to the **declared region origin**, not relative to the clipped effective rectangle.

This preserves stable application-local coordinates when only clipping changes.

## Visibility, enablement, and eligibility

Version 1.4 does not add a separate mutable region visibility flag.

A region is **effectively visible** when:

- it is not disposed;
- its effective clipped rectangle is non-empty; and
- when panel-associated, the panel is visible and not disposed.

`IsEnabled` is independent application interaction policy. A disabled region:

- is ignored by hit testing;
- cannot receive logical focus;
- does not receive focused input;
- cannot supply a local command binding result.

`IsFocusable` affects logical focus only. A non-focusable enabled region may still be hit by the mouse.

A region is **focus eligible** when it is enabled, focusable, effectively visible, and not disposed.

### Visual transparency is not input transparency

`CursesPanelTransparency.BlankCellsTransparent` remains a rendering/composition rule only. Blank-transparent cells do not automatically click through an interaction region.

In 1.4, click-through is expressed through explicit interaction geometry or by disabling/removing the covering interaction region. No separate per-cell or region `InputTransparent` policy is added.

## Hit-test precedence

Hit testing accepts zero-based screen `row` and `column` coordinates.

Negative coordinates throw `ArgumentOutOfRangeException`. Coordinates outside the current screen return no hit rather than throwing; this permits stale-but-valid terminal mouse coordinates around resize boundaries to be handled conservatively.

A hit resolves to at most one region.

Precedence is frozen as follows:

1. eligible panel-associated regions are considered before ordinary screen regions;
2. among panel-associated candidates on different panels, the current topmost panel wins regardless of region priority;
3. within the same panel, the region with the larger `HitTestPriority` wins;
4. if same-panel priorities tie, the later registration ordinal wins;
5. among ordinary screen-region candidates, the larger `HitTestPriority` wins;
6. if ordinary-region priorities tie, the later registration ordinal wins.

The panel's current z-order is consulted on every hit test; moving a panel in z-order changes hit precedence without re-registration.

Registration ordinal is an internal monotonically increasing signed 64-bit value. It starts at zero, is never reused during one router lifetime, and is not exposed as application identity. If the ordinal domain is ever exhausted, a new registration fails with `InvalidOperationException` before mutation.

Hit testing never changes logical focus and performs no terminal I/O.

## Logical focus

Logical focus belongs to the interaction router and remains distinct from terminal/window-manager focus represented by `CursesFocusEvent`.

Terminal `Focused`/`Unfocused` reports do not clear or replace logical focus.

### Explicit focus

`Focus(region)`:

- validates ownership first;
- throws for a null, foreign, or disposed region;
- returns `false` without changing existing focus when the supplied region is currently ineligible;
- returns `true` and makes the supplied eligible region current otherwise.

`ClearFocus()` is idempotent.

### Traversal order

Eligible regions are ordered by:

1. ascending `TraversalOrder`;
2. ascending registration ordinal.

`MoveFocus(Forward)` and `MoveFocus(Backward)` always wrap.

When there is no current focus:

- `Forward` selects the first eligible region;
- `Backward` selects the last eligible region.

When there are no eligible regions, traversal clears focus and returns `null`.

### Focus repair

Focus repair is deterministic and uses the **forward** direction regardless of the last user traversal direction.

When the current region becomes ineligible, repair chooses the first eligible region after the former region's traversal slot and wraps to the first eligible region when necessary. If no eligible region exists, focus becomes `null`.

Region-owned changes that can invalidate current focus—disabling, clearing focusability, changing bounds to a fully clipped rectangle, or disposing the region—repair immediately.

External screen/panel changes are intentionally not observed through a background subscription graph. They are repaired lazily at the start of focus-sensitive operations:

- reading `FocusedRegion`;
- `Focus(...)`;
- `MoveFocus(...)`;
- routing text, key, or paste input.

Pure mouse `HitTest(...)` does not repair or mutate logical focus.

## Routable input kinds

`CursesInteractionRouter.Route(CursesInputEvent)` accepts every existing `CursesInputEventKind`, but only some kinds participate in target/command routing.

### Text

`Text` input:

- is eligible for character-gesture matching;
- resolves a focused-region binding first;
- then resolves a router-global binding;
- if no command matches and focus exists, returns a targeted result for the focused region;
- otherwise returns an unrouted result.

### Key

`Key` input follows the same local-command, global-command, then focused-target sequence as `Text` input.

### Paste

`Paste` input is routed to the current eligible focused region as a targeted result. Version 1.4 does not add paste gestures/commands.

### Mouse

`Mouse` input is routed exclusively through hit testing. A successful hit returns a targeted result carrying region-local coordinates and the region's optional pointer preference. Mouse routing does not automatically change focus and does not run key-gesture bindings.

### Focus

Terminal `Focus` input returns an unrouted interaction result and does not change logical focus.

### EndOfInput

`EndOfInput` returns an unrouted interaction result.

The original `CursesInputEvent` is retained in every `CursesInteractionResult`, including unrouted results.

## Semantic key gestures

`CursesKeyGesture` is an immutable value contract. Construction occurs through explicit factory methods so impossible key/function/character combinations are rejected at creation time.

### Factories

The candidate surface is:

```csharp
public static CursesKeyGesture ForKey(
    CursesKey key,
    CursesKeyModifiers modifiers = CursesKeyModifiers.None,
    CursesKeyEventPhase phase = CursesKeyEventPhase.Press
);

public static CursesKeyGesture ForCharacter(
    Rune character,
    CursesKeyModifiers modifiers = CursesKeyModifiers.None,
    CursesKeyEventPhase phase = CursesKeyEventPhase.Press
);

public static CursesKeyGesture ForFunctionKey(
    int functionKeyNumber,
    CursesKeyModifiers modifiers = CursesKeyModifiers.None,
    CursesKeyEventPhase phase = CursesKeyEventPhase.Press
);
```

`ForKey(...)` rejects:

- `CursesKey.None`;
- `CursesKey.Character`;
- `CursesKey.Function`;
- `CursesKey.Space`;
- `CursesKey.Unrecognized`;
- undefined enum values.

Space is deliberately expressed through `ForCharacter(new Rune(' '), ...)` so traditional text-space input and modern semantic Space/Character reports can share one application gesture.

`ForFunctionKey(...)` accepts only the existing DCurses function-key range `0..63`.

### Gesture properties

The frozen candidate exposes:

```text
Key
Character
Modifiers
Phase
FunctionKeyNumber
```

Exactly one identity form is active: ordinary named key, character, or numbered function key.

### Modifier normalization

Gesture identity permits these modifier flags:

```text
Shift
Control
Alt
Super
Hyper
Meta
```

`CapsLock` and `NumLock` are keyboard state, not 1.4 gesture identity. Gesture factories reject those flags, and matching masks them from event modifier state.

The existing `CursesKey.CapsLock` and `CursesKey.NumLock` key identities remain bindable as ordinary named keys; only their lock-state modifier flags are excluded from gesture identity.

### Character normalization

A `ForCharacter(rune, ..., Press)` gesture may match:

- ordinary `CursesInputEventKind.Text` carrying that rune;
- modern `CursesKey.Character` Press carrying that rune;
- semantic `CursesKey.Space` Press when the rune is U+0020 SPACE.

Ordinary Text input is normalized as **Press** because the traditional text event does not carry Repeat/Release information.

Character Repeat/Release gestures match only key events which explicitly report those phases. DCurses does not fabricate repeat/release phase for traditional text input.

Matching uses the event's primary `Character` identity. `ShiftedCharacter`, `BaseLayoutCharacter`, and `AssociatedText` remain observable input metadata but are not alternate 1.4 gesture identities.

If an older/traditional input path cannot report a modifier required by a gesture, DCurses does not infer that modifier.

### Equality

Gesture equality/hash behavior is ordinal, culture-independent, and consists only of the frozen semantic identity fields above.

A default-zero `CursesKeyGesture` value is not a valid bindable gesture; binding APIs validate the value before registry mutation.

## Command identity

Commands are identifiers only; DCurses never invokes application command delegates.

The candidate type is a sealed immutable record:

```csharp
public sealed record CursesCommand {
    public const int MaximumNameLength = 128;

    public CursesCommand( string name );

    public string Name { get; }

    public override string ToString();
}
```

Command names:

- must be non-null and non-whitespace;
- must contain at most 128 UTF-16 code units;
- are preserved exactly without trimming or Unicode normalization;
- compare by ordinal case-sensitive record equality.

Different gestures may map to the same command.

## Binding precedence and conflicts

Bindings exist in two scopes:

- region-local;
- router-global.

A binding scope may contain at most one command for a gesture. Attempting to bind a duplicate gesture in the same scope throws `InvalidOperationException` without replacing the existing command.

Changing a binding therefore uses explicit unbind then bind semantics.

For Text/Key routing:

1. repair/query current focus;
2. if an eligible focused region has a matching local binding, return that command with the focused region as the result region;
3. otherwise, if a global binding matches, return that command with `Region == null` to identify global scope;
4. otherwise, if focus exists, return a targeted result for the focused region;
5. otherwise return unrouted.

A global command does not implicitly capture or change logical focus.

## Registry and binding bounds

The following limits are frozen for 1.4:

```text
Maximum live regions per router                  4096
Maximum gesture bindings per live region          256
Maximum router-global gesture bindings            1024
Maximum total live gesture bindings per router   16384
```

The public constants are defined on `CursesInteractionRouter`:

```csharp
public const int MaximumRegions = 4096;
public const int MaximumRegionGestureBindings = 256;
public const int MaximumGlobalGestureBindings = 1024;
public const int MaximumGestureBindings = 16384;
```

Every registration/bind operation checks all applicable limits before mutation.

Exhaustion returns `InvalidOperationException`. Region disposal/unbinding immediately releases the corresponding live-budget count.

No hidden unbounded side registry, event callback list, or deferred command queue is part of the router.

## Pointer-shape contract

DCurses exposes its own semantic `CursesPointerShape` enum and `CursesPointerShapeLease`; no `Icod.Terminal.TerminalPointerShape` or `TerminalPointerShapeLease` type is added to the public dependency boundary.

`CursesPointerShape` mirrors the reviewed Terminal semantic vocabulary with explicit stable numeric values:

```text
0  Alias
1  Cell
2  Copy
3  Crosshair
4  Default
5  EastResize
6  EastWestResize
7  Grab
8  Grabbing
9  Help
10 Move
11 NorthResize
12 NorthEastResize
13 NorthEastSouthWestResize
14 NoDrop
15 NotAllowed
16 NorthSouthResize
17 NorthWestResize
18 NorthWestSouthEastResize
19 Pointer
20 Progress
21 SouthResize
22 SouthEastResize
23 SouthWestResize
24 Text
25 VerticalText
26 WestResize
27 Wait
28 ZoomIn
29 ZoomOut
```

Mapping to Terminal is exhaustive by semantic name; implementation must not rely on equal numeric values.

The session-level candidate surface is:

```csharp
public ValueTask<CursesPointerShapeLease> AcquirePointerShapeAsync(
    CursesPointerShape shape,
    CancellationToken cancellationToken = default
);
```

`CursesPointerShapeLease` exposes the requested `Shape` and forwards disposal ownership to Terminal's scoped pointer-shape lease.

Region `PointerShape` is only a nullable **preference** reported by hit testing/routing. Hit testing never performs output or support queries. Applications explicitly decide whether and when to acquire/apply a pointer-shape lease.

## Structured hit and route results

### Hit result

`CursesInteractionHit` is immutable and exposes:

```text
Region
LocalRow
LocalColumn
PointerShape
```

`PointerShape` is the region preference at hit-resolution time and may be null.

### Route result

`CursesInteractionResultKind` is frozen as:

```text
Unrouted
Targeted
Command
```

`CursesInteractionResult` is immutable and exposes:

```text
Kind
Input
Region
Command
Hit
```

Invariants:

- `Unrouted`: `Region`, `Command`, and `Hit` are null;
- keyboard/text/paste `Targeted`: `Region` is non-null, `Command` and `Hit` are null;
- mouse `Targeted`: `Region` and `Hit` are non-null and identify the same region; `Command` is null;
- region-local `Command`: `Command` and `Region` are non-null; `Hit` is null;
- router-global `Command`: `Command` is non-null and `Region`/`Hit` are null.

The result is a snapshot. Later region disposal or geometry mutation does not mutate an already-returned result object.

## Frozen public API candidate

T1401 freezes the following 1.4 candidate names. Later tranches may identify a concrete regret, but changing this candidate requires explicit recorded justification and requalification.

```csharp
public enum CursesFocusDirection {
    Forward = 0,
    Backward = 1
}

public enum CursesInteractionResultKind {
    Unrouted = 0,
    Targeted = 1,
    Command = 2
}

public enum CursesPointerShape {
    Alias = 0,
    Cell = 1,
    Copy = 2,
    Crosshair = 3,
    Default = 4,
    EastResize = 5,
    EastWestResize = 6,
    Grab = 7,
    Grabbing = 8,
    Help = 9,
    Move = 10,
    NorthResize = 11,
    NorthEastResize = 12,
    NorthEastSouthWestResize = 13,
    NoDrop = 14,
    NotAllowed = 15,
    NorthSouthResize = 16,
    NorthWestResize = 17,
    NorthWestSouthEastResize = 18,
    Pointer = 19,
    Progress = 20,
    SouthResize = 21,
    SouthEastResize = 22,
    SouthWestResize = 23,
    Text = 24,
    VerticalText = 25,
    WestResize = 26,
    Wait = 27,
    ZoomIn = 28,
    ZoomOut = 29
}

public readonly record struct CursesKeyGesture {
    public CursesKey Key { get; }
    public Rune? Character { get; }
    public CursesKeyModifiers Modifiers { get; }
    public CursesKeyEventPhase Phase { get; }
    public int? FunctionKeyNumber { get; }

    public static CursesKeyGesture ForKey(
        CursesKey key,
        CursesKeyModifiers modifiers = CursesKeyModifiers.None,
        CursesKeyEventPhase phase = CursesKeyEventPhase.Press
    );

    public static CursesKeyGesture ForCharacter(
        Rune character,
        CursesKeyModifiers modifiers = CursesKeyModifiers.None,
        CursesKeyEventPhase phase = CursesKeyEventPhase.Press
    );

    public static CursesKeyGesture ForFunctionKey(
        int functionKeyNumber,
        CursesKeyModifiers modifiers = CursesKeyModifiers.None,
        CursesKeyEventPhase phase = CursesKeyEventPhase.Press
    );
}

public sealed record CursesCommand {
    public const int MaximumNameLength = 128;
    public CursesCommand( string name );
    public string Name { get; }
    public override string ToString();
}

public sealed class CursesInteractionRegionOptions {
    public CursesInteractionRegionOptions( CursesRectangle bounds );
    public CursesRectangle Bounds { get; init; }
    public CursesPanel? Panel { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsFocusable { get; init; }
    public int TraversalOrder { get; init; }
    public int HitTestPriority { get; init; }
    public CursesPointerShape? PointerShape { get; init; }
}

public sealed class CursesInteractionRegion : IDisposable {
    public CursesRectangle Bounds { get; }
    public CursesPanel? Panel { get; }
    public bool IsEnabled { get; set; }
    public bool IsFocusable { get; set; }
    public int TraversalOrder { get; set; }
    public int HitTestPriority { get; set; }
    public CursesPointerShape? PointerShape { get; set; }

    public void SetBounds( CursesRectangle bounds );
    public void BindGesture( CursesKeyGesture gesture, CursesCommand command );
    public bool UnbindGesture( CursesKeyGesture gesture );
    public void Dispose();
}

public sealed class CursesInteractionHit {
    public CursesInteractionRegion Region { get; }
    public int LocalRow { get; }
    public int LocalColumn { get; }
    public CursesPointerShape? PointerShape { get; }
}

public sealed class CursesInteractionResult {
    public CursesInteractionResultKind Kind { get; }
    public CursesInputEvent Input { get; }
    public CursesInteractionRegion? Region { get; }
    public CursesCommand? Command { get; }
    public CursesInteractionHit? Hit { get; }
}

public sealed class CursesInteractionRouter : IDisposable {
    public const int MaximumRegions = 4096;
    public const int MaximumRegionGestureBindings = 256;
    public const int MaximumGlobalGestureBindings = 1024;
    public const int MaximumGestureBindings = 16384;

    public CursesInteractionRouter( CursesScreen screen );

    public CursesScreen Screen { get; }
    public CursesInteractionRegion? FocusedRegion { get; }

    public CursesInteractionRegion RegisterRegion(
        CursesInteractionRegionOptions options
    );

    public CursesInteractionHit? HitTest(
        int row,
        int column
    );

    public bool Focus( CursesInteractionRegion region );
    public void ClearFocus();
    public CursesInteractionRegion? MoveFocus( CursesFocusDirection direction );

    public void BindGlobalGesture(
        CursesKeyGesture gesture,
        CursesCommand command
    );

    public bool UnbindGlobalGesture( CursesKeyGesture gesture );

    public CursesInteractionResult Route( CursesInputEvent input );

    public void Dispose();
}

public sealed class CursesPointerShapeLease : IAsyncDisposable {
    public CursesPointerShape Shape { get; }
    public ValueTask DisposeAsync();
}

public sealed partial class CursesSession {
    public ValueTask<CursesPointerShapeLease> AcquirePointerShapeAsync(
        CursesPointerShape shape,
        CancellationToken cancellationToken = default
    );
}
```

Constructors for immutable result/lease types not intended for application construction remain non-public.

## Failure and validation contract

Public parameter validation occurs before mutation.

The frozen error policy is:

- null required reference argument -> `ArgumentNullException`;
- undefined enum or negative coordinate -> `ArgumentOutOfRangeException`;
- malformed gesture factory combination -> `ArgumentException` or `ArgumentOutOfRangeException` according to the offending value;
- invalid/oversized command name -> `ArgumentException` / `ArgumentOutOfRangeException`;
- foreign region/router ownership -> `ArgumentException`;
- foreign panel/screen ownership -> `ArgumentException`;
- disposed router used for routing/registration/focus/binding -> `ObjectDisposedException`;
- disposed region mutation/binding/focus request -> `ObjectDisposedException`;
- disposed panel supplied during new registration -> `ObjectDisposedException`;
- ineligible but otherwise valid region passed to `Focus(...)` -> `false` without mutation;
- duplicate gesture in one scope -> `InvalidOperationException` before mutation;
- registry/binding bound exhaustion -> `InvalidOperationException` before mutation;
- out-of-current-screen positive hit-test coordinate -> no hit, not an exception.

Pointer-shape acquisition preserves Terminal's authoritative transport/lifecycle exceptions through the DCurses wrapper rather than converting them into guessed support results.

## Concurrency contract

Interaction routers and regions follow the existing DCurses narrow ownership model: they are **single-writer application state** unless a later API explicitly documents otherwise.

T1401 does not add per-region locks or claim concurrent mutation/routing safety.

Applications must serialize router/region mutation with their own interaction-routing loop. Immutable input events, gestures, commands, hits, and route-result snapshots may be read after creation according to ordinary .NET object/value semantics.

The pointer-shape session wrapper participates in the existing `CursesSession`/Terminal serialized terminal-activity domain rather than creating a second output lock.

## Dependency-boundary audit

The T1401 public API candidate introduces no new exported `Icod.Terminal` or `Icod.TermInfo` types.

The existing approved dependency exposure remains exactly:

```text
Icod.Terminal.TerminalControlResult<T>
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalSession
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

`PublicDependencyBoundaryTests` therefore remains unchanged at T1401. T1407 must prove that the pointer-shape wrapper preserves this allow-list.

## Explicit 1.4 exclusions

T1401 freezes these exclusions for 1.4:

- widget/control framework;
- automatic layout ownership;
- hidden event loop or background input consumer;
- arbitrary application callbacks invoked by the router;
- capture/bubble event tree;
- automatic focus-on-click;
- directional/spatial focus navigation beyond Forward/Backward;
- generalized mouse/pointer capture;
- drag/drop framework;
- per-cell input-transparency masks;
- gesture sequences/chords longer than one semantic key event;
- paste-command gestures;
- mouse-command gestures;
- accessibility tree;
- animation system;
- raster placement/scene graph;
- raw Terminal pointer/protocol type exposure;
- automatic pointer-shape application during hit testing;
- terminal-brand, `TERM`, protocol-family, or backend branching in application-facing routing.

## T1401 acceptance checklist

T1401 is ready for exact-head qualification when all of the following are true:

- [x] router/screen/application ownership relationship frozen;
- [x] region identity/lifetime/disposal frozen;
- [x] screen-relative and panel-relative geometry frozen;
- [x] clipping/local-coordinate semantics frozen;
- [x] panel z-order and same-scope hit precedence frozen;
- [x] visual transparency versus interaction opacity frozen;
- [x] enabled/focusable/visible/eligible semantics frozen;
- [x] focus traversal, wrapping, and repair frozen;
- [x] routable input kinds and result semantics frozen;
- [x] character/text/modern-key gesture normalization frozen;
- [x] lock-state modifier treatment frozen;
- [x] gesture conflict and local/global precedence frozen;
- [x] pointer preference/application split frozen;
- [x] concrete registry/binding limits frozen;
- [x] validation/failure semantics frozen;
- [x] concurrency expectations frozen;
- [x] public API candidate names/signatures frozen;
- [x] dependency-boundary impact audited;
- [x] explicit exclusions frozen;
- [x] package/source identity intentionally remains 1.3.0 until T1402.

No production source change is part of T1401. The next tranche, T1402, begins only after this exact contract head passes the normal pull-request qualification matrix.