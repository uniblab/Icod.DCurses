# Icod.DCurses Development Roadmap

**Project:** `Icod.DCurses`  
**Repository:** `https://github.com/uniblab/Icod.DCurses`  
**Published compatibility floor:** `1.4.0`  
**Current published package:** `1.4.0`  
**Assembly version:** `1.0.0.0`  
**Current declared runtime dependencies:** `Icod.Terminal 1.15.0`; `Icod.TermInfo 1.14.0`  
**Target frameworks:** `net8.0`; `net9.0`; `net10.0`  
**Configurations:** `Debug`; `Staging`; `Release`  
**Active development target:** `1.5.0` — advanced interaction control  
**Status:** T150-T159 and final dependency refresh complete; current evidence/documentation head undergoing final exact-head qualification before explicit merge approval

---

## Current authorities

- `Icod.DCurses-1.5.0-Development-Roadmap.md`
- `docs/superpowers/specs/2026-09-14-icod-dcurses-1.5-advanced-interaction-control-design.md`
- `docs/superpowers/plans/2026-09-14-icod-dcurses-1.5-advanced-interaction-control.md`
- `docs/T150-1.5.0-Architecture-API-Regret-and-Contract-Freeze.md`
- `docs/T151-Bounded-Interaction-Scopes.md`
- `docs/T152-Explicit-Pointer-Capture.md`
- `docs/T153-Deterministic-Spatial-Focus.md`
- `docs/T154-Deterministic-Pointer-Gesture-Normalization.md`
- `docs/T155-Scoped-Command-Bindings-and-Precedence.md`
- `docs/T156-Coherence-and-Adversarial-Hardening.md`
- `docs/T157-Application-Acceptance-and-Package-Consumer.md`
- `docs/T158-Advanced-Interaction-Regret-and-Qualification-Gate.md`
- `docs/T159-RC-and-Stable-1.5.0-Closure.md`
- `docs/Public-API-Fingerprint-1.5.json`

The 1.0-1.4 tranche, roadmap, and release-closure documents remain historical compatibility/release authorities and are not rewritten to simulate current development state. Earlier 1.5 tranche records likewise retain the dependency floor that was true when each checkpoint was qualified; the final release dependency graph is governed by the project file, this roadmap, the 1.5 roadmap, and T159 closure.

## Release train

| Release | Theme | Status |
|---|---|---|
| `1.0.0` | Stable core contract | Historical stable baseline |
| `1.1.0` | Semantic metadata and hyperlinks | Historical stable baseline |
| `1.2.0` | Panels/layers/z-order composition | Historical stable baseline |
| `1.3.0` | Layout and resize primitives | Historical stable baseline |
| `1.4.0` | Interaction routing/focus/gestures/hit testing/pointer semantics | Current published stable release |
| `1.5.0` | Advanced interaction control | Stable-source + final dependency graph qualified; final evidence-only PR-head gate active |

The progression is intentionally cumulative:

```text
1.1  cells carry semantic meaning
1.2  retained surfaces overlap deterministically
1.3  surfaces have explicit immutable geometry and resize policy
1.4  normalized input can target logical application regions deterministically
1.5  interaction can be scoped, captured, spatially navigated, gesture-normalized, and scope-command aware
```

## Published 1.4 API floor

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

The tagged compatibility baseline is `v1.4.0`, whose merged source is rooted at:

```text
48d591aa427096be78c173ad8ed85566d7f671bf
```

Frozen stable-source 1.5 contract:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

T153 was behavior-only and left the T152 fingerprint unchanged. T154 added exactly two exported gesture types plus the nullable `CursesInteractionResult.PointerGesture` result slot. T155 added no exported type; it added `BindGesture` and `UnbindGesture` to the existing `CursesInteractionScope` type and promoted the candidate fingerprint to `1.5.0-alpha.5`. T156 is behavior-only and leaves the T155 fingerprint unchanged while closing stale pointer-ownership resurrection across scope, panel, and screen-resize lifecycle transitions. T157 is acceptance-only. T158 is qualification-only. T159 promoted the unchanged implementation/API through `1.5.0-rc.1` and then stable-source `1.5.0`. Publication preparation then advanced the final direct runtime references to `Icod.Terminal 1.15.0` and `Icod.TermInfo 1.14.0` without changing the DCurses public API.

Version 1.5 remains additive. The final dependency refresh is compatibility/package qualification work rather than an API redesign.

## 1.5 release objective

`Icod.DCurses 1.5.0` completes the next mechanism layer above the 1.4 router without becoming a widget framework.

The approved Family-1 track adds:

```text
bounded interaction scopes
    -> modal eligibility boundary
    -> deterministic focus save/restore

explicit pointer capture
    -> one region / one concrete button
    -> signed out-of-bounds local targeting

spatial focus
    -> Up / Down / Left / Right
    -> integer-only deterministic ranking

pointer gesture normalization
    -> click / drag phases / wheel
    -> no clock or drag-drop policy

scope command bindings
    -> region -> scope chain -> global
    -> command identity only
```

## 1.5 architectural boundaries

The 1.5 track is governed by these rules:

- `Icod.Terminal` remains the single live terminal/input/protocol authority.
- DCurses routes already-normalized `CursesInputEvent` values; it does not parse escape sequences or terminal-family protocols.
- `CursesInteractionRouter` remains the single interaction ownership coordinator.
- The existing implicit root behavior remains compatible for 1.4 consumers.
- Explicit interaction scopes have immutable parentage and bounded depth/count.
- Scope activation is explicit, lease-owned, descendant-only, and LIFO.
- Pointer capture is explicit and singular; capture never implies logical focus.
- `CursesInteractionHit` remains an in-bounds hit snapshot; captured out-of-bounds targeting uses a separate pointer-target abstraction.
- Forward/backward focus semantics remain unchanged; spatial navigation does not wrap.
- Spatial ranking is integer-only and deterministic across platforms.
- Pointer gesture normalization is clock-free; double-click timing and drag/drop policy remain outside core.
- Pointer press/drag state is fixed-size per concrete mouse button and is repaired when region, scope, panel, screen, or capture ownership becomes invalid.
- Command bindings return `CursesCommand` identities only; no callbacks, handlers, enabled predicates, or dependency-injection machinery are added.
- Scope command bindings participate in the existing `MaximumGestureBindings` router-wide total rather than creating a parallel capacity domain.
- Hit testing, focus navigation, capture, scope changes, lifecycle repair, gesture routing, and command resolution remain synchronous and perform no terminal I/O.
- Pointer ownership invalidation is terminal for the old ownership instance: later geometry/scope restoration cannot resurrect stale capture or press/drag state.
- Logical focus keeps the established lazy-repair contract where appropriate; pointer-lifecycle propagation must not force eager focus repair merely because a panel or screen changed.
- Public Terminal/TermInfo dependency exposure remains tightly allow-listed.
- Interaction registries and state remain bounded and deterministic.
- Existing single-writer expectations remain; 1.5 adds no multi-writer/thread-safe guarantee.
- Public consumers receive normalized `CursesInputEvent` instances through `CursesSession`; T157 deliberately did not widen internal synthetic-event factories merely to simplify downstream smoke testing.
- `Icod.TermInfo` remains a direct DCurses dependency because production DCurses source consumes TermInfo APIs directly; it is not treated as merely a transitive implementation detail of Terminal.

## 1.5 tranche sequence

```text
T150  architecture/API-regret gate, planning freeze, test housekeeping          complete
T151  bounded interaction scopes and active-scope eligibility                   complete
T152  explicit pointer capture and capture lifetime                             complete
T153  deterministic spatial focus navigation                                    complete
T154  deterministic pointer-gesture normalization                               complete
T155  scoped command bindings and precedence                                    complete
T156  resize/panel/scope/capture/disposal coherence and adversarial hardening   complete
T157  application acceptance sample and downstream/package consumer             complete
T158  performance/allocation/API/package/docs/dependency regret gate            complete
T159  RC and stable-source 1.5.0 closure                                        complete
final dependency refresh                                                        complete / qualified #915
```

Qualified implementation/API checkpoints:

```text
T150  eb34c8236a9e6c008b7ff486df177e8b52cba274  #818 / 34878235207
T151  ac0bfcbf38800a94533cc4ada4caf3b8feee4fb1  #829 / 34879561466
T152  b1e9e60df7ee6cfc3e2af10c4f3f819273c299fb  #839 / 34881203594
T153  6f440a9feda628f1948d11008402a0064bbd645b  #844 / 34882549455
T154  acae6104e7f0e4e431d7f5f836978be807008a40  #855 / 34892884559
T155  3e59e7613e214a5eaa84df13bbedba56abfefb6d  #864 / 34899684038
T156  e09324666bb6960d465f061d609a4fb53d8c5288  #876 / 34905503151
T157  596843f798999573162be7883051030db353b940  #884 / 34908524571
T158  9a8d0b2e2439bf4a936a5602d846a0c1bd20781f  #888 / 34914622882
T159 RC      23113b130d674da315ccbbcd384a60a0e6b47baa  #899 / 34993884108
T159 stable  af30a6c2df842d76b8cb9e9b7855aeb3a16e9ff9  #905 / 34994761777
Dependency refresh bc583d5ded07c0784bab27fe389f4bdab690394e #915 / 35003069419
```

Every listed checkpoint passed the complete seven-job Staging matrix: package candidate plus Windows/Linux/macOS on x64 and ARM64. The #915 package artifact was inspected directly: its net8.0, net9.0, and net10.0 dependency groups each declare direct `Icod.Terminal 1.15.0` and direct `Icod.TermInfo 1.14.0`, and the isolated package consumer passed. The compiler-derived public API remains the frozen 69/525/hash contract.

## Deliberate 1.5 non-goals

Version 1.5 does not add:

- a widget framework;
- buttons, text boxes, menus, controls, or application navigation;
- a retained event tree or capture/bubble phases;
- automatic focus-on-click;
- drag/drop payload or acceptance semantics;
- double-click/triple-click timing policy;
- flexbox/grid/general constraint layout;
- automatic layout ownership;
- accessibility-tree ownership;
- command callback/dependency-injection machinery;
- raster placement/scene ownership;
- backend-selection policy;
- animation;
- PTY/process hosting.

Future widget or mixed-media layers should be able to build on the 1.5 mechanisms without bypassing Terminal ownership or reimplementing scopes, focus, capture, gesture routing, or command identity.

## Immediate next step

The implementation, package identity, public API, and final dependency graph are qualified. Qualify the current evidence/documentation-only PR head through the same seven-job Staging matrix. If that exact head is green, no further branch change is planned before presenting PR #30 for explicit maintainer merge approval. Merge, post-merge Release validation, tagging, GitHub Release creation, and NuGet publication remain separate explicit maintainer actions.
