# T1208 — Panel Application, Performance, and Allocation Acceptance

**Release:** `Icod.DCurses 1.2.0`  
**Tranche:** T1208  
**Qualified head:** `bb00707779cf3dc6c2222455a6f942469d036881`  
**Workflow:** #596 / `34522859308`  
**Result:** seven jobs green  

## Purpose

T1208 validates the retained-panel implementation in application-shaped workloads and freezes resource expectations before the public API regret gate. It deliberately tests composition behavior rather than introducing new public APIs.

## Application acceptance

The acceptance suite covers:

- modal help which occludes application content and restores the retained base when hidden;
- a command palette moved without rebuilding its private retained content;
- a completion popup whose transparent blank cells reveal editor text;
- context-menu/status surfaces whose z-order can be changed deterministically;
- a hidden transient status panel which receives updates without destination churn and presents the retained update when shown;
- sparse visible edits on a 160 x 48 base screen with changed composed damage bounded to 1–3 cells.

The scenarios use the same retained composition machinery exercised by the live-session integration tranche. No application-specific widget layer is introduced.

## No-panel cost

A screen without panels uses the internal `HasPanels` presence check rather than allocating an empty z-order snapshot merely to decide whether composition is required.

The acceptance test performs 10,000 repeated no-panel presence checks and requires:

```text
thread allocation: 0 bytes
```

This protects the ordinary pre-1.2 refresh shape from panel overhead when applications never create a panel.

## Steady-state composition cost

The sparse-panel resource fixture uses a 120 x 40 screen with one 3 x 20 panel. After warm-up, 1,024 repeated settled compositions must:

- return the same retained composed `CursesVirtualScreen` instance;
- leave its dirty-cell count at zero;
- allocate no more than 131,072 bytes on the current thread across the full 1,024-call measurement.

This is a regression ceiling rather than a universal benchmark. It exists to catch accidental full-frame reconstruction or new per-cell/per-call allocation patterns.

## Optimization found during acceptance

T1208 found that the T1207 live-session bridge called `SnapshotPanelsBottomToTop()` only to determine whether the screen had panels. That allocated an empty array on each ordinary no-panel refresh. The accepted correction introduced the internal allocation-free `HasPanels` check and routed the live refresh fast path through it.

No public API changed in T1208.

## Qualification

Exact head:

```text
bb00707779cf3dc6c2222455a6f942469d036881
```

passed workflow #596 / `34522859308` across package validation and Windows/Linux/macOS x64/ARM64 runtime jobs.

The Linux ARM64 evidence leg reported:

```text
Build: 0 warnings, 0 errors
net8.0:  551 passed, 0 failed
net9.0:  551 passed, 0 failed
net10.0: 551 passed, 0 failed
```

## Decision

T1208 is complete. The retained panel representation and steady-state work profile are accepted for the T1209 public API/package/documentation regret gate.
