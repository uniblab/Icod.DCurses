# T152 — Explicit Pointer Capture and Capture Lifetime

**Release:** `Icod.DCurses 1.5.0`  
**Tranche:** T152  
**Published compatibility floor:** `1.4.0`  
**Starting accepted tranche:** T151  
**Starting accepted head:** `ac0bfcbf38800a94533cc4ada4caf3b8feee4fb1`  
**Starting accepted workflow:** #829 / `34879561466`  
**Accepted T152 head:** `b1e9e60df7ee6cfc3e2af10c4f3f819273c299fb`  
**Accepted T152 workflow:** #839 / `34881203594`  
**Status:** complete; exact-head seven-job Staging qualification passed

---

## Purpose

T152 adds explicit singular pointer capture to the 1.5 interaction router without introducing click/drag gesture policy, callbacks, hidden terminal I/O, or implicit focus changes.

Pointer capture is an application-owned routing mechanism. It makes one enabled, currently pointer-eligible interaction region the target for matching button movement/release reports even when the pointer leaves the region's rectangle. It does not turn out-of-bounds coordinates into hits and does not alter logical focus.

## Public contract

T152 adds:

- `CursesPointerCaptureLease`;
- `CursesPointerTarget`;
- `CursesInteractionRouter.CapturePointer(CursesInteractionRegion, CursesMouseButton)`;
- nullable `CursesInteractionResult.PointerTarget`.

`CursesInteractionHit` remains unchanged and continues to represent an ordinary in-bounds hit. Captured routing uses `CursesPointerTarget`, whose signed local coordinates remain relative to the declared region origin and whose `IsInside` flag separately records whether the current pointer lies in the region's effective hit area.

## Ownership and lifetime

Exactly one pointer capture may be live per router.

Capture requires:

- a concrete defined `CursesMouseButton` other than `None`;
- an owned, non-disposed interaction region;
- current region enablement;
- current active-scope eligibility;
- a non-empty currently visible effective pointer area.

Focusability is deliberately irrelevant. Pointer capture does not call `Focus`, does not change `FocusedRegion`, and does not introduce automatic focus-on-click.

The returned lease is idempotent. A monotonically increasing internal generation prevents stale lease disposal from releasing a later capture.

Capture ends when:

- the owning lease is disposed;
- a matching-button release is routed;
- the captured region is disabled, emptied, or disposed;
- associated panel state makes the region pointer-ineligible;
- scope activation excludes the region;
- the router is disposed.

Region-owned invalidation performs immediate repair. Panel/scope changes remain consistent with the existing no-background-subscription design: capture is repaired before the next capture-sensitive routing/registration decision.

## Routing semantics

Before ordinary mouse hit testing, the router checks the current capture.

A captured `Move` or `Release` whose button matches the captured button is targeted directly to the captured region. The result carries:

- the original `CursesInputEvent`;
- `Kind = Targeted`;
- `Region = captured region`;
- `Hit = null`;
- non-null `PointerTarget` with signed local coordinates.

A matching release is routed to the captured region before capture is cleared.

A release for another button does not end the capture. Wheel reports and unrelated mouse reports continue through ordinary routing. Gesture classification remains intentionally deferred to T154.

## RED evidence

RED head:

```text
4dea9ce25a83369a7b488804b7a0f163197048f8
workflow #831 / 34880157369
```

All seven jobs failed at compilation for the intended missing T152 surface. Representative diagnostics identified only absent pointer-capture members/types:

- `CursesInteractionRouter.CapturePointer`;
- `CursesPointerCaptureLease`;
- `CursesPointerTarget`;
- `CursesInteractionResult.PointerTarget`.

The RED suite covers ownership validation, singular capture, no implicit focus, signed out-of-bounds coordinates, matching/nonmatching release behavior, idempotent stale leases, region/panel/scope invalidation, and router disposal.

## GREEN behavior evidence before fingerprint promotion

Intermediate implementation head:

```text
e357b834dd7874fd042d5ce2280c65779468cae0
workflow #836 / 34880799595
```

Linux ARM64 completed all behavioral tests on net8/net9/net10. Each TFM reported exactly one failure: the intentionally stale public-API fingerprint guard. The remaining **754 tests passed**, including all T152 capture tests.

Latest pre-promotion behavior head:

```text
5fd465d4fa338ccadfeb406700156592917e3a39
workflow #837 / 34880831129
```

This head additionally wires immediate region-owned capture repair for enablement/bounds/disposal. Linux ARM64 again built with zero warnings/errors and reported exactly one failure per TFM: the intentionally stale public-API fingerprint. All **754 non-fingerprint tests passed**.

## Compiler-derived API candidate

The accepted compiler-derived T152 surface is:

```text
67 exported types
515 canonical declared contract lines
sha256 30684c9670b9fb3df418648f6a7bb90b749065ae618b6b3198b86a93b5ececc5
```

`docs/Public-API-Fingerprint-1.5.json` is promoted to this `1.5.0-alpha.3` candidate while preserving all historical stable fingerprints.

## Compatibility and boundaries

T152 preserves ordinary 1.4 mouse hit behavior when no capture is active. Existing `Hit`, `Region`, and result-kind meanings remain representable exactly.

T152 deliberately does **not** add:

- click or drag classification;
- double-click timing;
- drag/drop payloads;
- pointer-capture callbacks;
- terminal protocol operations;
- terminal pointer-shape ownership changes;
- automatic logical focus changes;
- more than one simultaneous router capture.

## Final qualification

Evidence-complete head:

```text
b1e9e60df7ee6cfc3e2af10c4f3f819273c299fb
workflow #839 / 34881203594
```

All seven Staging jobs passed:

- package candidate;
- Windows x64;
- Windows ARM64;
- Linux x64;
- Linux ARM64;
- macOS x64;
- macOS ARM64.

T152 is therefore accepted. T153 may begin deterministic spatial focus navigation.
