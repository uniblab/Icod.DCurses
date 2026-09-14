# T1407 — Pointer-Shape Integration

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1407  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** implementation/fingerprint head qualified; documentation-complete head requires its own exact-head matrix

---

## Objective

T1407 implements the pointer-shape contract frozen by T1401 without widening the DCurses public dependency boundary to Terminal pointer types.

The tranche adds:

- the stable DCurses semantic `CursesPointerShape` vocabulary;
- optional mutable pointer-shape preferences on interaction regions;
- immutable pointer-shape snapshots on resolved hits and mouse-route results;
- `CursesPointerShapeLease`;
- `CursesSession.AcquirePointerShapeAsync(...)`;
- exhaustive semantic-name mapping to the canonical `Icod.Terminal 1.11.1` pointer-shape lease API.

It deliberately does **not** perform pointer-shape output during hit testing or routing, query terminal support automatically, infer support from terminal identity, or expose `TerminalPointerShape` / `TerminalPointerShapeLease` through the DCurses public contract.

## RED checkpoint

The T1407 tests were committed first at:

`97fd5eb06c57780689d53bdb4a1d422e1ec483d0`

Workflow #755 / `34730200422` failed as intended. Linux x64/ARM64 and macOS ARM64 recorded **0 warnings** and failed compilation only because the frozen T1407 production surface did not yet exist:

- `CursesPointerShape`;
- `CursesPointerShapeLease`;
- `CursesInteractionRegionOptions.PointerShape`;
- `CursesInteractionRegion.PointerShape`;
- `CursesInteractionHit.PointerShape`;
- `CursesSession.AcquirePointerShapeAsync(...)`.

The RED suite covers:

- all 30 frozen semantic names and explicit numeric values;
- initialization and mutation of nullable region pointer preferences;
- rejection of undefined pointer-shape values;
- mutation rejection after region disposal;
- immutable hit snapshots across later region preference changes;
- mouse routing carrying the hit snapshot without changing logical focus;
- canonical OSC 22 wire-name mapping for every DCurses semantic value;
- nested DCurses pointer leases restoring the previous active shape;
- composition with an outer direct Terminal pointer-shape lease;
- final reset to terminal policy after the last owner is released;
- cancellation before terminal output;
- invalid-shape failure before terminal output.

## Public semantic vocabulary

T1407 adds exactly the 30-value semantic vocabulary frozen by T1401:

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

The numeric values are explicitly assigned in DCurses and form part of the 1.4 public contract.

## Region preference and hit snapshots

`CursesInteractionRegionOptions.PointerShape` is nullable and initializes the registered region preference.

`CursesInteractionRegion.PointerShape` is mutable while the region is live. Undefined enum values are rejected with `ArgumentOutOfRangeException`; mutation after disposal throws `ObjectDisposedException`.

The preference is advisory only. It does not affect region eligibility, focus, hit precedence, or panel precedence.

`CursesInteractionHit.PointerShape` captures the selected region's preference at hit-resolution time. The hit remains diagnosable and immutable even if the region preference subsequently changes or is cleared.

Because mouse routing carries the resolved `CursesInteractionHit`, `CursesInteractionResult.Hit.PointerShape` is the same immutable snapshot. Routing does not automatically apply it and does not change logical focus.

## Terminal ownership bridge

The session surface is:

```csharp
public ValueTask<CursesPointerShapeLease> AcquirePointerShapeAsync(
    CursesPointerShape shape,
    CancellationToken cancellationToken = default
);
```

`CursesPointerShapeLease` exposes only:

```csharp
public CursesPointerShape Shape { get; }
public ValueTask DisposeAsync();
```

Internally, acquisition delegates to the canonical `TerminalSession.AcquirePointerShapeAsync(...)` API from `Icod.Terminal 1.11.1`.

The DCurses-to-Terminal mapping is an exhaustive semantic-name switch. The implementation does **not** cast between enum numeric values, so a future Terminal numeric change cannot silently alter DCurses semantics.

The mapping is:

```text
Curses Alias                       -> Terminal Alias
Curses Cell                        -> Terminal Cell
Curses Copy                        -> Terminal Copy
Curses Crosshair                   -> Terminal Crosshair
Curses Default                     -> Terminal Default
Curses EastResize                  -> Terminal EastResize
Curses EastWestResize              -> Terminal EastWestResize
Curses Grab                        -> Terminal Grab
Curses Grabbing                    -> Terminal Grabbing
Curses Help                        -> Terminal Help
Curses Move                        -> Terminal Move
Curses NorthResize                 -> Terminal NorthResize
Curses NorthEastResize             -> Terminal NorthEastResize
Curses NorthEastSouthWestResize    -> Terminal NorthEastSouthWestResize
Curses NoDrop                      -> Terminal NoDrop
Curses NotAllowed                  -> Terminal NotAllowed
Curses NorthSouthResize            -> Terminal NorthSouthResize
Curses NorthWestResize             -> Terminal NorthWestResize
Curses NorthWestSouthEastResize    -> Terminal NorthWestSouthEastResize
Curses Pointer                     -> Terminal Pointer
Curses Progress                    -> Terminal Progress
Curses SouthResize                 -> Terminal SouthResize
Curses SouthEastResize             -> Terminal SouthEastResize
Curses SouthWestResize             -> Terminal SouthWestResize
Curses Text                        -> Terminal Text
Curses VerticalText                -> Terminal VerticalText
Curses WestResize                  -> Terminal WestResize
Curses Wait                        -> Terminal Wait
Curses ZoomIn                      -> Terminal ZoomIn
Curses ZoomOut                     -> Terminal ZoomOut
```

## Lease and lifecycle semantics

DCurses does not implement a second pointer-state manager. `CursesPointerShapeLease` forwards disposal to Terminal's identity-aware scoped lease.

Therefore the physical ownership semantics remain Terminal-authoritative:

- acquisition immediately emits the requested semantic pointer shape;
- nested owners may be disposed out of order;
- the newest active owner controls physical pointer shape;
- releasing an inner owner restores the newest remaining owner;
- releasing the final owner emits the OSC 22 empty-payload terminal-policy reset;
- repeated successful disposal is idempotent;
- if Terminal restoration/reset fails, the underlying owner remains retryable according to Terminal's contract.

Acquisition is serialized through the existing DCurses terminal-activity gate so it cannot begin after session disposal has started and participates in the same Terminal activity boundary as other DCurses terminal operations.

DCurses does not automatically query pointer-shape support. Successful acquisition proves only that Terminal emitted the complete request according to its existing contract.

## First GREEN evidence and API guard

The complete behavioral implementation landed at:

`75dc454cd55983f99e5fca96583c608a1e4c9f69`

Workflow #761 / `34731938672` built the implementation with **0 warnings / 0 errors**.

Across `net8.0`, `net9.0`, and `net10.0`, all **709 behavioral/non-fingerprint tests passed**. The only failing test was the expected current-development public API fingerprint guard.

The compiler-derived T1407 contract was identical on all three TFMs:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

The two newly exported types are:

- `Icod.DCurses.CursesPointerShape`;
- `Icod.DCurses.CursesPointerShapeLease`.

The remaining additive public changes are the frozen pointer-preference properties and session acquisition method.

## Fingerprint qualification

`docs/Public-API-Fingerprint-1.4.json` was advanced to the compiler-derived T1407 contract at:

`32ff70710a0be3b3d22b1751c0b5ec8723460fa5`

Workflow #762 / `34732031825` passed all seven PR jobs on that exact head:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

This qualifies the pointer-shape implementation and the current 1.4 alpha public API fingerprint together.

## Public dependency boundary

T1407 intentionally keeps Terminal pointer types internal to DCurses implementation.

No `Icod.Terminal.TerminalPointerShape` or `Icod.Terminal.TerminalPointerShapeLease` type appears in a public DCurses member signature. Applications use only the DCurses semantic enum and lease while Terminal remains the physical protocol/state owner.

No `Icod.TermInfo.Inspection` dependency is introduced.

## Scope audit

T1407 deliberately does **not** add:

- automatic application of a region pointer preference;
- pointer changes from `HitTest(...)` or `Route(...)`;
- pointer-shape capability/support queries;
- terminal-brand or protocol heuristics;
- callbacks or hidden input loops;
- focus changes on mouse routing;
- new raw OSC APIs;
- raster resources or placements;
- new layout ownership.

Those exclusions preserve the T1401 architecture: DCurses reports application interaction intent while Terminal owns reversible terminal state and protocol emission.

## Exit gate

T1407 is complete when this documentation-complete head passes the full PR package/runtime matrix. After that exact-head qualification, T1408 may begin resize, panel, and lifecycle coherence hardening with a fresh RED checkpoint.
