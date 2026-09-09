# Icod.DCurses 0.6.0 — T607/T608 Presentation Acceptance and Freeze

**Release line:** `0.6.0`  
**Checkpoint:** `0.6.0-rc.1`  
**Stable predecessor:** `0.5.0`  
**Dependencies:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** T607 and T608 complete; T609 active

## T607 — Integrated presentation acceptance

T607 closes the integrated presentation/lifecycle work for the 0.6 contract.

Accepted behavior:

- every semantic rendition flag can be requested through `CursesStyle`;
- indexed foreground/background requests flow through the same session refresh path used by applications;
- the interactive showcase reports `CursesPresentationCapabilities`, exercises the complete rendition vocabulary, reports cursor-presentation availability, and draws a semantic border;
- semantic line cells remain semantic through logical storage and are resolved only at physical refresh time;
- normal session suspension neutralizes rendition before handing the terminal back to the host lifecycle path;
- session disposal after non-default rich presentation restores both terminal attributes and default colors.

The session integration acceptance now renders a style containing:

```text
Bold
Dim
Underline
Reverse
Standout
Italic
Blink
Conceal
Strikeout
```

with indexed foreground/background, verifies the corresponding physical rendition selectors were emitted, then verifies `DisposeAsync()` emits both the attribute reset and `OriginalColorPair` (`op`) restoration path.

No additional cursor API was added. The existing hidden / normal / very-visible contract is sufficient for the 0.6 managed presentation layer.

## T608 — Public API and analyzer regret gate

The 0.6 public delta is frozen in `docs/Public-API-Baseline-0.6.md` and guarded by `PublicPresentationApiContractTests`.

The accepted delta consists of:

- appended `CursesTextAttributes` flags `Italic`, `Blink`, `Conceal`, and `Strikeout`;
- `CursesPresentationCapabilities` and `CursesSession.PresentationCapabilities`;
- `CursesLineGlyph`;
- `CursesCell.LineGlyph`, `CursesCell.IsLineGlyph`, and `CursesCell.Line(...)`;
- semantic `CursesWindow` horizontal/vertical line and border overloads.

No new Terminal or TermInfo type enters the public DCurses signature surface.

### Warning/analyzer policy

The 0.5 publication cycle demonstrated that warnings visible only under the Release configuration could escape a Staging PR gate. The 0.6 regret review removes that blind spot without adding a duplicate build job:

- the library project now uses warning level 4 and `TreatWarningsAsErrors=true` under Staging;
- the test project uses warning level 4 and `TreatWarningsAsErrors=true` under Staging;
- Release retains warning level 4 and warnings-as-errors;
- ordinary PR builds therefore exercise the same compiler/analyzer severity policy that matters to Release while retaining Staging optimization/debug semantics.

This is preferred over compiling the same solution a second time in Release during every pull request.

### ACS recovery experiment

An attempted internal change to treat ACS state as unknown after every retained-screen invalidation caused an extra `rmacs` on the first normal refresh. That violated the accepted semantic-line contract and was backed out.

The stable 0.6 contract therefore retains the previously validated ACS run behavior. Output-failure recovery for an interrupted ACS sequence is an internal hardening concern and is not part of the 0.6 public API. It may be revisited only with a dedicated recovery test that does not alter normal first-refresh output.

A nullable warning in the ACS resolver discovered during this review was corrected at its source rather than suppressed.

## Validation

The final T607/T608 pre-RC head passed:

- Windows Staging restore/build/test;
- Linux Staging restore/build/test;
- macOS Staging restore/build/test;
- canonical Staging package validation and fresh package consumer;
- warning level 4 / warnings-as-errors for the library and test projects.

The public presentation contract is therefore frozen for `0.6.0-rc.1`.

## Next gate

T609 is release closure only:

1. validate the exact `0.6.0-rc.1` head;
2. make no feature/API additions;
3. promote the unchanged contract to stable `0.6.0`;
4. run the definitive stable-source PR matrix/package gate;
5. merge only that green stable source head;
6. require the exact merged `main` commit to pass the full Release matrix before tagging `v0.6.0`.
