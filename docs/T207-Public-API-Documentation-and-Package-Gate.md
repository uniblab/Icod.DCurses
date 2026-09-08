# T207 — Public API, Documentation, and Package Gate

**Project:** `Icod.DCurses`  
**Development line:** `0.2.0`  
**Development version:** `0.2.0-alpha.3`  
**Tranche:** T207 — public API, documentation, and package regret gate  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Implementation complete; release-candidate validation pending

---

## 1. Purpose

T207 reviews the complete `0.2` semantic-input surface before stable release closure.
The objective is to catch API regret while the release remains prerelease and to prove that
a fresh package consumer receives the same contract validated inside the repository.

## 2. Public API review result

No new public member introduced by T201-T206 requires removal or renaming.

The accepted additions are:

```text
CursesKey
    appended Terminal 1.0 semantic key vocabulary

CursesKeyModifiers
    Super
    Hyper
    Meta
    CapsLock
    NumLock

CursesKeyEventPhase
    Press
    Repeat
    Release

CursesInputEvent
    ShiftedCharacter
    BaseLayoutCharacter
    AssociatedText
    KeyPhase

CursesKeyboardReportingMode
    Disambiguated
    EventTypes
    AllKeys

CursesInputProtocolOptions.KeyboardReportingMode
CursesInputProtocolLease.KeyboardReportingMode
```

The additions remain curses-owned semantic types. No new Terminal type is required by an
ordinary consumer of the expanded input facade.

## 3. Numeric compatibility

The `0.1` enum prefix remains frozen:

```text
CursesKey.None      = 0
...
CursesKey.Function  = 17

CursesKeyModifiers.None    = 0
CursesKeyModifiers.Shift   = 1
CursesKeyModifiers.Control = 2
CursesKeyModifiers.Alt     = 4
```

New key values are appended through `CursesKey.Unrecognized = 80`.

The complete numeric and member baseline is recorded in
[`Public-API-Baseline-0.2.md`](Public-API-Baseline-0.2.md).

Machine tests protect the legacy prefix and drive the production conversion functions for
every stable Terminal key, every modifier mask from `0x00` through `0xff`, and every
Terminal key-event phase.

## 4. Dependency-boundary review

The direct package dependency graph is unchanged:

```text
Icod.DCurses 0.2.0
    -> Icod.Terminal 1.0.0
    -> Icod.TermInfo 1.10.0
```

No new direct dependency is introduced.

The approved lower-layer public-type allow-list remains exactly:

```text
Icod.Terminal.TerminalSession
Icod.Terminal.TerminalEndpoint
Icod.Terminal.TerminalControlResult<T>
Icod.TermInfo.TerminalDescription
Icod.TermInfo.TerminalSize
```

The existing reflection boundary test remains in force.

## 5. XML and consumer documentation

The new public enums, properties, and protocol members carry XML documentation.

The root README now contains a focused `0.2` keyboard-reporting example using only
curses-owned protocol types and explains controlled capability absence.

The live input showcase and `samples/README.md` document:

- key phase;
- full modifier state;
- alternate character identities;
- associated text;
- optional `EventTypes` negotiation;
- continued traditional-terminal usability.

## 6. Fresh package consumer

The isolated package-smoke project now validates the `0.2` surface from the generated
`.nupkg` rather than only through project references.

It checks:

- legacy enum numeric compatibility;
- keyboard-reporting options;
- key phase and modern key metadata properties;
- representative keypad/media/unrecognized keys;
- new modifier flags;
- keyboard mode exposure on the protocol lease;
- the approved transitive Terminal/TermInfo public-type boundary.

The smoke consumer remains outside `Icod.DCurses.sln`, restores the packed package from
the artifact directory into an isolated NuGet cache, and runs independently under
`net8.0`, `net9.0`, and `net10.0`.

## 7. Package verifier

The structural verifier requires no version-specific redesign for `0.2`:

- it reads the active package and assembly versions from `Icod.DCurses.csproj`;
- it verifies all three target-framework payloads;
- it continues to require exactly `Icod.Terminal 1.0.0` and
  `Icod.TermInfo 1.10.0` in each dependency group;
- it continues to reject bundled dependency assemblies and repository-only payloads.

## 8. Regret decisions

The review explicitly accepts:

1. keeping a curses-owned `CursesKey` vocabulary rather than exposing `TerminalKey`;
2. keeping `Function` plus `FunctionKeyNumber` for F0-F63;
3. making `KeyPhase` nullable because non-key events have no phase;
4. retaining alternate character identities only on Character key events;
5. allowing associated text on semantic key events where Terminal provides it;
6. exposing keyboard negotiation through the existing curses protocol lease rather than a
   separate keyboard-specific lifetime object;
7. preserving the stable Terminal/TermInfo dependency boundary without additional wrappers
   for the five already accepted upstream types.

No API change is required by this regret pass.

## 9. Gate

T207 is complete when:

1. the public API baseline is present and accurate;
2. XML and README documentation describe the new surface;
3. the rich-input showcase documents and compiles the new contract;
4. the public dependency-boundary allow-list remains unchanged;
5. the fresh package consumer compiles and executes the new surface under all three TFMs;
6. the structural package verifier accepts the package and dependency groups;
7. Staging runtime tests pass on Windows, Linux, and macOS;
8. the canonical package candidate and fresh-consumer job pass;
9. a release-candidate version can be assigned without further API changes.

After this gate, only T208 stable release closure remains for `0.2.0`.
