# T1405 — Semantic Keyboard Gestures

**Release:** `Icod.DCurses 1.4.0`  
**Tranche:** T1405  
**Package identity:** `1.4.0-alpha.1`  
**AssemblyVersion:** `1.0.0.0`  
**Published compatibility floor:** `1.3.0`  
**Runtime dependencies:** `Icod.Terminal 1.11.1`; `Icod.TermInfo 1.11.0`  
**Status:** implementation/fingerprint head qualified; documentation-complete head requires its own exact-head matrix

---

## Objective

T1405 adds the immutable semantic key-gesture value frozen by T1401. It gives later command routing a terminal-independent identity for named keys, Unicode characters, function keys, modifiers, and key-event phase without exposing protocol families or terminal-brand heuristics.

This tranche does not add command identities, binding registries, input routing, callbacks, pointer behavior, or terminal I/O.

## RED checkpoint

The T1405 gesture tests were committed first at:

`2b6688203fc932dca2ed18b73631620595fe722a`

Workflow #741 / `34720375290` failed as intended. The Linux ARM64 build recorded zero warnings and failed only because `CursesKeyGesture` did not yet exist (`CS0246` / `CS0103`). There was no unrelated source regression.

The RED suite covers:

- named-key, character, and function-key factory identity;
- default Press phase and no modifiers;
- rejection of dedicated/non-bindable named-key forms;
- undefined key/phase validation;
- function-key range `0..63`;
- rejection of CapsLock/NumLock state flags in binding identity;
- rejection of unknown modifier bits;
- exact ordinal/culture-independent value equality and hash behavior;
- masking CapsLock/NumLock state from incoming-event modifier comparison;
- CapsLock and NumLock actual key identities remaining bindable named keys;
- exact function-key-number matching;
- traditional text / modern Character-key Press parity;
- Space normalization through a character gesture;
- no fabricated modifiers, Repeat, or Release for traditional text;
- primary `Character` as the only character gesture identity;
- explicit modern Repeat and Release matching;
- non-keyboard input rejection.

## Public surface

T1405 adds exactly the frozen T1401 value type:

```csharp
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
```

The implementation landed at:

`ef234ff8da74b71eb5f7846fc9a17ec0245061c5`

Matching remains internal in T1405 so T1406 can consume the semantic mechanism without adding an extra public matching surface that T1401 did not freeze.

## Gesture identity

A gesture's equality/hash identity consists only of:

```text
Key
Character
Modifiers
Phase
FunctionKeyNumber
```

The record-struct value semantics therefore remain ordinal, deterministic, culture independent, and free of terminal/protocol metadata.

A default-zero `CursesKeyGesture` is not a valid bindable gesture. T1406 binding APIs validate this before registry mutation.

## Named keys

`ForKey(...)` accepts concrete bindable named key identities and rejects:

- `CursesKey.None`;
- `CursesKey.Character`;
- `CursesKey.Function`;
- `CursesKey.Space`;
- `CursesKey.Unrecognized`;
- undefined enum values.

`Character` and `Function` have dedicated factories. `Space` intentionally uses the character path so traditional text-space and a modern semantic Space key resolve to one application gesture identity.

Actual lock-key identities such as `CursesKey.CapsLock` and `CursesKey.NumLock` remain ordinary bindable named keys.

## Character gestures

`ForCharacter(...)` stores a Unicode `Rune` as the primary semantic identity and uses `CursesKey.Character`.

For Press gestures, matching intentionally unifies:

- `CursesInputEventKind.Text` carrying the same primary rune;
- modern `CursesKey.Character` key input carrying the same primary rune;
- `CursesKey.Space` for U+0020.

Traditional text has no reported binding modifiers or key phase. It therefore matches only an unmodified Press character gesture. DCurses does not infer Control/Alt/etc. and does not fabricate Repeat/Release.

Modern Character-key Repeat/Release can match only when the normalized key event explicitly carries the corresponding phase.

`ShiftedCharacter`, `BaseLayoutCharacter`, and `AssociatedText` remain useful input metadata but are deliberately not alternate command-gesture identities. Only the primary `Character` participates in character gesture matching.

## Function keys

`ForFunctionKey(...)` accepts the existing DCurses function-key range `0..63` and stores `CursesKey.Function` plus the exact function number.

Matching requires the same normalized function-key number in addition to modifiers and phase.

## Modifiers

Binding identity accepts only:

```text
Shift
Control
Alt
Super
Hyper
Meta
```

`CapsLock` and `NumLock` modifier flags describe keyboard state and are rejected by gesture factories. When matching a normalized key event, those state bits are masked before comparing binding modifiers.

Unknown modifier bits are rejected by factories rather than silently normalized.

## Phase

The default gesture phase is `Press`.

`Press`, `Repeat`, and `Release` remain distinct semantic identities. Undefined enum values fail before gesture creation.

The gesture layer does not collapse Repeat into Press and does not synthesize phases unavailable from a traditional input path.

## First GREEN evidence and API guard

Workflow #742 / `34720454349` built the implementation cleanly.

Across `net8.0`, `net9.0`, and `net10.0`, all **682 behavioral/non-fingerprint tests passed**. The only failing test was the expected current-development API fingerprint guard.

The compiler-derived T1405 contract was identical on all three TFMs:

```text
57 exported types
458 canonical declared contract lines
sha256 638288580b66974106913742dcb52268a2826bf9e30c08350334a8fb5af148b4
```

The only newly exported type is `Icod.DCurses.CursesKeyGesture`. No behavioral implementation correction was required after this run.

## Fingerprint qualification

`docs/Public-API-Fingerprint-1.4.json` was advanced to the compiler-derived T1405 contract at:

`e755b29951ba7446e1f7adcca45d9cb08cc3160c`

Workflow #743 / `34720537710` passed all seven PR jobs on that exact head:

- Package candidate;
- Runtime Windows x64;
- Runtime Windows ARM64;
- Runtime Linux x64;
- Runtime Linux ARM64;
- Runtime macOS x64;
- Runtime macOS ARM64.

This qualifies the semantic gesture implementation and the current 1.4 alpha public API fingerprint together.

## Scope audit

T1405 deliberately does **not** add:

- `CursesCommand`;
- local/global binding registries;
- binding precedence or duplicate handling;
- `CursesInteractionResult` or `Route(...)`;
- callbacks or automatic command execution;
- focus mutation while matching;
- mouse routing;
- pointer-shape state;
- terminal protocol parsing, queries, or output.

Those remain assigned to T1406/T1407 exactly as frozen by T1401.

## Exit gate

T1405 is complete when this documentation-complete head passes the full PR package/runtime matrix. After that exact-head qualification, T1406 may begin command identities, bounded local/global bindings, and structured routing with a fresh RED checkpoint.
