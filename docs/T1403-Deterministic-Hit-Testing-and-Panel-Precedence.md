# T1403 — Deterministic Hit Testing and Panel Precedence

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1403  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** implementation complete and qualified; documentation-complete head requires its own exact-head matrix

---

## Objective

T1403 maps one zero-based screen coordinate to at most one interaction region using the precedence and geometry semantics frozen by T1401. It adds no focus mutation, input loop, terminal protocol work, or terminal I/O.

## RED checkpoint

The T1403 tests were committed first at:

`e4795747fa382aaefd48b70a6c958a52f6cc09aa`

Workflow #732 / `34713216968` failed as intended because the frozen `CursesInteractionRouter.HitTest(...)` method and `CursesInteractionHit` type did not yet exist. The failure contained no unrelated warning or production regression.

The RED suite covers:

- negative coordinate validation and stale coordinates outside the current screen;
- ordinary screen-relative region containment and local coordinates;
- empty and screen-clipped effective geometry;
- enabled-state eligibility;
- ordinary-region priority and later-registration tie breaking;
- panel-relative geometry, local coordinates, movement, and resizing;
- panel-associated precedence over ordinary regions;
- current cross-panel z-order precedence independent of region priority;
- same-panel priority and later-registration tie breaking;
- panel hide/show/disposal without re-registration;
- blank-transparent rendering remaining input-opaque;
- deterministic repeated hit results;
- disposed-router behavior.

## Implementation

The public additive surface is:

```csharp
public sealed class CursesInteractionHit {
    public CursesInteractionRegion Region { get; }
    public int LocalRow { get; }
    public int LocalColumn { get; }
}

public sealed partial-contract CursesInteractionRouter {
    public CursesInteractionHit? HitTest(
        int row,
        int column
    );
}
```

`CursesInteractionHit` first landed at:

`783869b33df6baca78bcab5b46f1707c61d835f3`

The complete hit-testing implementation landed at:

`aa4da17058232e4f3d8bb64b190a5a4c9a897d8e`

### Geometry

Ordinary regions interpret `Bounds` in screen coordinates. A point must lie inside both the current screen and the declared region.

Panel-associated regions interpret `Bounds` in panel-local coordinates. A point must lie inside the panel's current screen-space bounds and the declared panel-local region. Hidden or disposed panels are ineligible.

Successful `LocalRow` and `LocalColumn` values are measured from the declared region origin, not from a clipped effective rectangle.

### Precedence

T1403 implements the exact T1401 precedence order:

1. panel-associated candidates precede ordinary candidates;
2. among different panels, the currently topmost panel wins regardless of region priority;
3. within the same panel, larger `HitTestPriority` wins;
4. same-panel priority ties choose the later registration ordinal;
5. among ordinary regions, larger `HitTestPriority` wins;
6. ordinary priority ties choose the later registration ordinal.

Panel position, dimensions, visibility, disposal, and z-order are read from current state on every hit test, so applications never need to re-register a region after those panel changes.

### Allocation-conscious panel precedence

The pre-existing panel-composition API exposes snapshot ordering for composition work. T1403 does not allocate that array merely to compare two panel candidates during a hit test.

An internal reference-identity order-index query was added to `CursesPanelOrder<T>` and exposed internally through `CursesScreen.GetPanelOrderIndex(...)`. This keeps current z-order authoritative while avoiding an intermediate panel-order snapshot in the normal hit-test path.

No new internal side registry or cached duplicate panel geometry was introduced.

## API fingerprint

The first implementation run correctly reached the public-API fingerprint guard after all behavioral tests compiled and ran. The compiler-derived contract was identical on `net8.0`, `net9.0`, and `net10.0`:

```text
55 exported types
437 canonical declared contract lines
sha256 8a54fa1b78ac9b4eea2b4ad60ac671dce2c36fa8a8ccc45ad953c814cb7386d5
```

`docs/Public-API-Fingerprint-1.4.json` was advanced to that exact alpha contract at:

`f97c116e84c46aed1153f7f3a90ef1ca0f41da3e`

The published 1.3 fingerprint remains unchanged historical authority.

## Qualification

Exact implementation/fingerprint head:

`f97c116e84c46aed1153f7f3a90ef1ca0f41da3e`

Workflow #735 / `34717891570` passed all seven PR jobs:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

The package candidate therefore also exercised the repository's packed-artifact validation path at the current alpha identity.

## Scope audit

T1403 deliberately does **not** add:

- logical focus or focus traversal;
- focus-on-click behavior;
- semantic key gestures;
- command bindings or callbacks;
- pointer-shape I/O;
- input transparency inferred from visual transparency;
- a hidden event loop;
- raw terminal protocol parsing.

Those remain assigned to later tranches exactly as frozen by T1401.

## Exit gate

T1403 is complete when this documentation-complete source head passes the full PR package/runtime matrix. After that exact-head qualification, T1404 may begin logical focus, traversal, and deterministic repair work.
