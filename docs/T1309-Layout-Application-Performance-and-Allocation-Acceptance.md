# T1309 Layout Application, Performance, and Allocation Acceptance

## Scope

T1309 validates the completed 1.3 geometry and bounds-application model under application-shaped use. It does not add a retained layout tree, background layout owner, widget system, constraint solver, or automatic resize policy.

The application remains responsible for three explicit operations:

1. derive rectangles from a current `CursesScreen.Bounds` value;
2. apply those rectangles to existing windows and panels;
3. refresh through the existing retained screen/panel machinery.

## Representative layout

`CursesLayoutApplicationAcceptanceTests` freezes a representative terminal application made from chained deterministic operations:

- a fixed top header;
- a fixed bottom status region;
- a fixed left sidebar;
- the remaining body region;
- a retained dialog panel derived from proportional row/column subdivision inside the body.

The test creates ordinary shared `CursesWindow` views for the four base regions and an independent retained `CursesPanel` for the dialog. It then applies all five computed rectangles through the public 1.3 `SetBounds` contract and verifies the composed result.

This is deliberately application policy expressed as local values. No layout rule is stored by `CursesScreen`, `CursesWindow`, `CursesPanel`, or `CursesLayout`.

## Pure geometry allocation gate

The geometry-only acceptance first performs 100,000 unmeasured representative layout calculations so JIT/tiering work is outside the acceptance samples. It then performs eight independent measured samples, each containing another 100,000 complete representative layout calculations.

Acceptance ceiling:

```text
minimum allocated bytes across stabilized samples: 0
```

The ceiling remains intentionally exact. `CursesRectangle` and `CursesInsets` are value types and the `CursesLayout` operations use caller-owned values plus `out` results; ordinary stabilized geometry calculation must demonstrate a zero-allocation measurement window.

The repeated-sample protocol exists only to isolate nondeterministic fixed runtime/test-host charges from deterministic per-operation allocation. During qualification, a single measurement window reported a tiny fixed charge on one TFM while the other TFMs reported zero; on a subsequent run the charge moved to a different TFM. A real allocation in the geometry operations would recur in every measured sample. T1309 therefore does not grant a nonzero allocation budget: at least one fully stabilized sample must still measure exactly zero bytes.

## Steady-state application gate

A second acceptance loop keeps the terminal dimensions unchanged and repeatedly:

1. recomputes the same five rectangles;
2. reapplies them to four windows and one panel;
3. composes the retained panel frame.

The test requires the same composed `CursesVirtualScreen` instance to survive all iterations and requires no dirty cells after the warm steady state.

For 1,024 iterations, the allocation ceiling is:

```text
allocated bytes <= 262,144
```

This ceiling intentionally includes the existing panel-composition bookkeeping and order snapshots. It is not a budget granted to the pure geometry layer, whose separate ceiling remains zero.

## Ownership audit

T1309 also freezes the architectural boundary directly:

- `CursesLayout` remains a static utility type;
- it has no instance state;
- it has no mutable static state;
- geometry values do not own terminal objects;
- screens, windows, and panels do not acquire retained layout rules as part of this tranche.

The 1.3 model therefore remains explicit recomputation rather than background or retained layout ownership.

## Qualification

The tranche is accepted only when the normal PR gate passes on one exact head:

- package candidate validation;
- Windows x64 and ARM64;
- Linux x64 and ARM64;
- macOS x64 and ARM64;
- all supported target frameworks (`net8.0`, `net9.0`, `net10.0`).

No public API change is planned for T1309. The provisional 1.3 public API fingerprint should therefore remain unchanged from T1306/T1308 unless the acceptance work exposes a demonstrated defect that requires an explicitly reviewed correction.
